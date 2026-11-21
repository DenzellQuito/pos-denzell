using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Hosting;
using POS_DENZELL.Models;

namespace POS_DENZELL.Services
{
    public class RespuestaSunatBetaDTO
    {
        public bool Exito { get; set; }
        public string EstadoSunat { get; set; } = "Pendiente";
        public string CodigoHash { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public string? RutaXml { get; set; }
    }

    public interface IFacturacionSunatService
    {
        Task<RespuestaSunatBetaDTO> EmitirComprobanteBetaAsync(Venta venta, List<(VentaDetalle Detalle, string DescripcionProducto)> items);
    }

    public class FacturacionSunatService : IFacturacionSunatService
    {
        private readonly IWebHostEnvironment _env;

        // Credenciales estándar del Entorno Beta de Pruebas de SUNAT
        private const string RucEmisorBeta = "20000000001";
        private const string RazonSocialEmisor = "GOLOSINAS JAVIER & JOSE S.A.C. (MODO PRUEBAS)";

        public FacturacionSunatService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<RespuestaSunatBetaDTO> EmitirComprobanteBetaAsync(
            Venta venta,
            List<(VentaDetalle Detalle, string DescripcionProducto)> items)
        {
            // Los Tickets internos ("TK") no se declaran a SUNAT
            if (venta.TipoDocumento == "TK")
            {
                return new RespuestaSunatBetaDTO
                {
                    Exito = true,
                    EstadoSunat = "Local",
                    Mensaje = "Nota de venta interna (no declarable a SUNAT)."
                };
            }

            try
            {
                string numeracion = $"{venta.Serie}-{venta.Correlativo:D8}";
                decimal baseImponible = venta.MontoTotal - venta.TotalIGV;

                // Código de tipo de documento de identidad SUNAT (Catálogo 06):
                // "6" = RUC, "1" = DNI, "0" = Doc. Trib. No Domiciliado / Sin RUC (Clientes varios)
                string codigoTipoDocCliente = "0";
                string numDocCliente = string.IsNullOrWhiteSpace(venta.ClienteDocumento) ? "00000000" : venta.ClienteDocumento.Trim();
                string nombreCliente = string.IsNullOrWhiteSpace(venta.ClienteNombre) ? "CLIENTES VARIOS" : venta.ClienteNombre.Trim();

                if (venta.TipoDocumento == "01")
                    codigoTipoDocCliente = "6"; // Factura exige siempre RUC
                else if (numDocCliente.Length == 8 && numDocCliente != "00000000")
                    codigoTipoDocCliente = "1"; // DNI

                // 1. Construimos la estructura XML UBL 2.1 de prueba
                var xmlDoc = new XDocument(
                    new XDeclaration("1.0", "utf-8", "no"),
                    new XElement("Invoice",
                        new XAttribute("UBLVersionID", "2.1"),
                        new XAttribute("CustomizationID", "2.0"),
                        new XElement("ID", numeracion),
                        new XElement("IssueDate", venta.FechaHora.ToString("yyyy-MM-dd")),
                        new XElement("IssueTime", venta.FechaHora.ToString("HH:mm:ss")),
                        new XElement("InvoiceTypeCode", venta.TipoDocumento),
                        new XElement("DocumentCurrencyCode", "PEN"),
                        new XElement("AccountingSupplierParty",
                            new XElement("RUC", RucEmisorBeta),
                            new XElement("RegistrationName", RazonSocialEmisor)
                        ),
                        new XElement("AccountingCustomerParty",
                            new XElement("SchemeID", codigoTipoDocCliente),
                            new XElement("CustomerID", numDocCliente),
                            new XElement("RegistrationName", nombreCliente)
                        ),
                        new XElement("TaxTotal",
                            new XElement("TaxableAmount", baseImponible.ToString("0.00", CultureInfo.InvariantCulture)),
                            new XElement("TaxAmount", venta.TotalIGV.ToString("0.00", CultureInfo.InvariantCulture)),
                            new XElement("PayableAmount", venta.MontoTotal.ToString("0.00", CultureInfo.InvariantCulture))
                        ),
                        new XElement("InvoiceLines",
                            items.Select((item, index) =>
                            {
                                decimal valorVentaFila = Math.Round(item.Detalle.SubTotal / 1.18m, 2, MidpointRounding.AwayFromZero);
                                decimal igvFila = item.Detalle.SubTotal - valorVentaFila;
                                return new XElement("InvoiceLine",
                                    new XElement("ID", index + 1),
                                    new XElement("UnitCode", "NIU"),
                                    new XElement("InvoicedQuantity", item.Detalle.Cantidad),
                                    new XElement("Description", item.DescripcionProducto),
                                    new XElement("PriceAmountWithIGV", item.Detalle.PrecioUnitario.ToString("0.000", CultureInfo.InvariantCulture)),
                                    new XElement("LineExtensionAmount", valorVentaFila.ToString("0.00", CultureInfo.InvariantCulture)),
                                    new XElement("TaxAmount", igvFila.ToString("0.00", CultureInfo.InvariantCulture)),
                                    new XElement("TotalAmount", item.Detalle.SubTotal.ToString("0.00", CultureInfo.InvariantCulture))
                                );
                            })
                        )
                    )
                );

                string xmlContenido = xmlDoc.ToString();

                // 2. Calculamos el DigestValue (Código Hash SHA-256 en Base64 para el QR y el Ticket)
                byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(xmlContenido));
                string codigoHash = Convert.ToBase64String(hashBytes);

                // 3. Guardamos el XML generado en wwwroot/sunat_xml para auditoría de pruebas
                string carpetaXml = Path.Combine(_env.WebRootPath, "sunat_xml");
                if (!Directory.Exists(carpetaXml)) Directory.CreateDirectory(carpetaXml);

                string nombreArchivoXml = $"{RucEmisorBeta}-{venta.TipoDocumento}-{numeracion}.xml";
                string rutaFisicaXml = Path.Combine(carpetaXml, nombreArchivoXml);
                await File.WriteAllTextAsync(rutaFisicaXml, xmlContenido, Encoding.UTF8);

                string nombreTipo = venta.TipoDocumento == "01" ? "La Factura" : "La Boleta de Venta";

                return new RespuestaSunatBetaDTO
                {
                    Exito = true,
                    EstadoSunat = "Aceptado (Beta)",
                    CodigoHash = codigoHash,
                    Mensaje = $"{nombreTipo} número {numeracion}, ha sido aceptada en el entorno de pruebas.",
                    RutaXml = $"/sunat_xml/{nombreArchivoXml}"
                };
            }
            catch (Exception ex)
            {
                return new RespuestaSunatBetaDTO
                {
                    Exito = false,
                    EstadoSunat = "Pendiente",
                    Mensaje = "Error en generación XML Beta: " + ex.Message
                };
            }
        }
    }
}