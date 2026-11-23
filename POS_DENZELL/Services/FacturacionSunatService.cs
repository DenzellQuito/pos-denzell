using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using POS_DENZELL.Helpers;
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
        Task<RespuestaSunatBetaDTO> EmitirComprobanteBetaAsync(Venta venta, List<(VentaDetalle Detalle, string DescripcionProducto, string? CodigoBarras)> items);
    }

    public class FacturacionSunatService : IFacturacionSunatService
    {
        private readonly HttpClient _httpClient;
        private readonly SunatSettings _settings;
        private readonly IWebHostEnvironment _env;

        public FacturacionSunatService(
            HttpClient httpClient,
            IOptions<SunatSettings> settings,
            IWebHostEnvironment env)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _env = env;
        }

        public async Task<RespuestaSunatBetaDTO> EmitirComprobanteBetaAsync(
            Venta venta,
            List<(VentaDetalle Detalle, string DescripcionProducto, string? CodigoBarras)> items)
        {
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
                string nombreArchivoBase = $"{_settings.RucEmisor}-{venta.TipoDocumento}-{numeracion}";

                // 1. Generar XML UBL 2.1 oficial
                string xmlSinFirma = ConstruirXmlUbl21(venta, items, numeracion);

                // 2. Firmar XML con el certificado .p12 y obtener el DigestValue oficial
                string rutaCertificado = Path.Combine(_env.ContentRootPath, _settings.RutaCertificadoPfx);
                if (!File.Exists(rutaCertificado))
                    throw new FileNotFoundException($"No se encontró el certificado en: {rutaCertificado}");

                var (xmlFirmado, digestValue) = FirmarXmlConPfx(xmlSinFirma, rutaCertificado, _settings.ClaveCertificadoPfx);

                // Guardar el XML firmado en wwwroot/sunat_xml
                string carpetaXml = Path.Combine(_env.WebRootPath, "sunat_xml");
                if (!Directory.Exists(carpetaXml)) Directory.CreateDirectory(carpetaXml);

                string rutaFisicaXml = Path.Combine(carpetaXml, $"{nombreArchivoBase}.xml");
                await File.WriteAllTextAsync(rutaFisicaXml, xmlFirmado, new UTF8Encoding(false));

                // 3. Enviar el ZIP por SOAP (sendBill) al servidor Beta de SUNAT
                var (aceptado, mensajeSunat, cdrZip) = await EnviarSoapSunatAsync(nombreArchivoBase, xmlFirmado);

                // Guardar el CDR (Constancia de Recepción) si SUNAT respondió con el ZIP
                if (cdrZip != null)
                {
                    string rutaCdr = Path.Combine(carpetaXml, $"R-{nombreArchivoBase}.zip");
                    await File.WriteAllBytesAsync(rutaCdr, cdrZip);
                }

                return new RespuestaSunatBetaDTO
                {
                    Exito = aceptado,
                    EstadoSunat = aceptado ? "Aceptado (Beta)" : "Rechazado (Beta)",
                    CodigoHash = digestValue,
                    Mensaje = mensajeSunat,
                    RutaXml = $"/sunat_xml/{nombreArchivoBase}.xml"
                };
            }
            catch (Exception ex)
            {
                return new RespuestaSunatBetaDTO
                {
                    Exito = false,
                    EstadoSunat = "Pendiente",
                    Mensaje = "Error SUNAT Beta: " + ex.Message
                };
            }
        }

        private string ConstruirXmlUbl21(
            Venta venta,
            List<(VentaDetalle Detalle, string DescripcionProducto, string? CodigoBarras)> items,
            string numeracion)
        {
            XNamespace ns = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";
            XNamespace cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
            XNamespace cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
            XNamespace ext = "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2";

            // Precalculamos líneas para garantizar cuadratura matemática exacta en UBL 2.1
            var lineasCalculadas = items.Select((item, idx) =>
            {
                decimal totalLinea = Math.Round(item.Detalle.SubTotal, 2, MidpointRounding.AwayFromZero);
                decimal valorVentaLinea = Math.Round(totalLinea / 1.18m, 2, MidpointRounding.AwayFromZero);
                decimal igvLinea = totalLinea - valorVentaLinea;
                decimal cantidad = item.Detalle.Cantidad;
                decimal precioUnitarioConIgv = cantidad > 0
                    ? Math.Round(totalLinea / cantidad, 4, MidpointRounding.AwayFromZero)
                    : item.Detalle.PrecioUnitario;
                decimal valorUnitarioSinIgv = cantidad > 0
                    ? Math.Round(valorVentaLinea / cantidad, 6, MidpointRounding.AwayFromZero)
                    : Math.Round(item.Detalle.PrecioUnitario / 1.18m, 6, MidpointRounding.AwayFromZero);

                return new
                {
                    Id = idx + 1,
                    Cantidad = cantidad,
                    Descripcion = item.DescripcionProducto,
                    TotalLinea = totalLinea,
                    ValorVentaLinea = valorVentaLinea,
                    IgvLinea = igvLinea,
                    PrecioUnitarioConIgv = precioUnitarioConIgv,
                    ValorUnitarioSinIgv = valorUnitarioSinIgv,
                    CodigoBarras = item.CodigoBarras?.Trim()
                };
            }).ToList();

            decimal totalOpGravada = lineasCalculadas.Sum(x => x.ValorVentaLinea);
            decimal totalIgv = lineasCalculadas.Sum(x => x.IgvLinea);
            decimal totalPagar = lineasCalculadas.Sum(x => x.TotalLinea);

            // Datos del receptor (Catálogo 06)
            string codigoTipoDocCliente = "0";
            string numDocCliente = string.IsNullOrWhiteSpace(venta.ClienteDocumento) ? "-" : venta.ClienteDocumento.Trim();
            string nombreCliente = string.IsNullOrWhiteSpace(venta.ClienteNombre) ? "CLIENTES VARIOS" : venta.ClienteNombre.Trim();

            if (venta.TipoDocumento == "01")
                codigoTipoDocCliente = "6";
            else if (numDocCliente.Length == 8 && numDocCliente != "00000000")
                codigoTipoDocCliente = "1";
            else if (numDocCliente.Length == 11)
                codigoTipoDocCliente = "6";

            // Ajusta el nombre del método si en tu Helpers/NumeroALetras.cs lo llamaste distinto (ej. ConvertirSoles)
            string montoEnLetras = NumeroALetras.ConvertirSoles(totalPagar);

            var invoice = new XElement(ns + "Invoice",
                new XAttribute(XNamespace.Xmlns + "cac", cac),
                new XAttribute(XNamespace.Xmlns + "cbc", cbc),
                new XAttribute(XNamespace.Xmlns + "ext", ext),

                // Nodo reservado donde se incrustará la firma digital
                new XElement(ext + "UBLExtensions",
                    new XElement(ext + "UBLExtension",
                        new XElement(ext + "ExtensionContent")
                    )
                ),

                new XElement(cbc + "UBLVersionID", "2.1"),
                new XElement(cbc + "CustomizationID", "2.0"),
                new XElement(cbc + "ID", numeracion),
                new XElement(cbc + "IssueDate", venta.FechaHora.ToString("yyyy-MM-dd")),
                new XElement(cbc + "IssueTime", venta.FechaHora.ToString("HH:mm:ss")),
                new XElement(cbc + "InvoiceTypeCode",
                    new XAttribute("listID", "0101"),
                    new XAttribute("listAgencyName", "PE:SUNAT"),
                    new XAttribute("listName", "Tipo de Documento"),
                    new XAttribute("listURI", "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo01"),
                    venta.TipoDocumento),
                new XElement(cbc + "Note",
                    new XAttribute("languageLocaleID", "1000"),
                    montoEnLetras),
                new XElement(cbc + "DocumentCurrencyCode",
                    new XAttribute("listID", "ISO 4217 Alpha"),
                    new XAttribute("listName", "Currency"),
                    new XAttribute("listAgencyName", "United Nations Economic Commission for Europe"),
                    "PEN"),

                // Referencia a la firma
                new XElement(cac + "Signature",
                    new XElement(cbc + "ID", "IDSignSP"),
                    new XElement(cac + "SignatoryParty",
                        new XElement(cac + "PartyIdentification",
                            new XElement(cbc + "ID", _settings.RucEmisor)),
                        new XElement(cac + "PartyName",
                            new XElement(cbc + "Name", _settings.RazonSocial))
                    ),
                    new XElement(cac + "DigitalSignatureAttachment",
                        new XElement(cac + "ExternalReference",
                            new XElement(cbc + "URI", "#SignatureSP")))
                ),

                // Emisor
                new XElement(cac + "AccountingSupplierParty",
                    new XElement(cac + "Party",
                        new XElement(cac + "PartyIdentification",
                            new XElement(cbc + "ID",
                                new XAttribute("schemeID", "6"),
                                new XAttribute("schemeName", "Documento de Identidad"),
                                new XAttribute("schemeAgencyName", "PE:SUNAT"),
                                new XAttribute("schemeURI", "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo06"),
                                _settings.RucEmisor)),
                        new XElement(cac + "PartyName",
                            new XElement(cbc + "Name", _settings.RazonSocial)),
                        new XElement(cac + "PartyLegalEntity",
                            new XElement(cbc + "RegistrationName", _settings.RazonSocial),
                            new XElement(cac + "RegistrationAddress",
                                new XElement(cbc + "AddressTypeCode",
                                    new XAttribute("listAgencyName", "PE:SUNAT"),
                                    new XAttribute("listName", "Establecimientos anexos"),
                                    "0000")))
                    )
                ),

                // Cliente / Receptor
                new XElement(cac + "AccountingCustomerParty",
                    new XElement(cac + "Party",
                        new XElement(cac + "PartyIdentification",
                            new XElement(cbc + "ID",
                                new XAttribute("schemeID", codigoTipoDocCliente),
                                new XAttribute("schemeName", "Documento de Identidad"),
                                new XAttribute("schemeAgencyName", "PE:SUNAT"),
                                new XAttribute("schemeURI", "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo06"),
                                numDocCliente)),
                        new XElement(cac + "PartyLegalEntity",
                            new XElement(cbc + "RegistrationName", nombreCliente))
                    )
                )
            );

            // Forma de pago (obligatorio para Factura "01" en SUNAT)
            if (venta.TipoDocumento == "01")
            {
                invoice.Add(new XElement(cac + "PaymentTerms",
                    new XElement(cbc + "ID", "FormaPago"),
                    new XElement(cbc + "PaymentMeansID", "Contado")
                ));
            }

            // Totales de impuestos (IGV 18%)
            invoice.Add(
                new XElement(cac + "TaxTotal",
                    new XElement(cbc + "TaxAmount", new XAttribute("currencyID", "PEN"), Fmt(totalIgv)),
                    new XElement(cac + "TaxSubtotal",
                        new XElement(cbc + "TaxableAmount", new XAttribute("currencyID", "PEN"), Fmt(totalOpGravada)),
                        new XElement(cbc + "TaxAmount", new XAttribute("currencyID", "PEN"), Fmt(totalIgv)),
                        new XElement(cac + "TaxCategory",
                            new XElement(cac + "TaxScheme",
                                new XElement(cbc + "ID",
                                    new XAttribute("schemeID", "UN/ECE 5153"),
                                    new XAttribute("schemeAgencyID", "6"),
                                    "1000"),
                                new XElement(cbc + "Name", "IGV"),
                                new XElement(cbc + "TaxTypeCode", "VAT")
                            )
                        )
                    )
                ),
                new XElement(cac + "LegalMonetaryTotal",
                    new XElement(cbc + "LineExtensionAmount", new XAttribute("currencyID", "PEN"), Fmt(totalOpGravada)),
                    new XElement(cbc + "TaxInclusiveAmount", new XAttribute("currencyID", "PEN"), Fmt(totalPagar)),
                    new XElement(cbc + "PayableAmount", new XAttribute("currencyID", "PEN"), Fmt(totalPagar))
                )
            );

            // Líneas del comprobante
            foreach (var l in lineasCalculadas)
            {
                invoice.Add(new XElement(cac + "InvoiceLine",
                    new XElement(cbc + "ID", l.Id),
                    new XElement(cbc + "InvoicedQuantity", new XAttribute("unitCode", "NIU"), l.Cantidad.ToString(CultureInfo.InvariantCulture)),
                    new XElement(cbc + "LineExtensionAmount", new XAttribute("currencyID", "PEN"), Fmt(l.ValorVentaLinea)),
                    new XElement(cac + "PricingReference",
                        new XElement(cac + "AlternativeConditionPrice",
                            new XElement(cbc + "PriceAmount", new XAttribute("currencyID", "PEN"), l.PrecioUnitarioConIgv.ToString("0.0000", CultureInfo.InvariantCulture)),
                            new XElement(cbc + "PriceTypeCode", "01")
                        )
                    ),
                    new XElement(cac + "TaxTotal",
                        new XElement(cbc + "TaxAmount", new XAttribute("currencyID", "PEN"), Fmt(l.IgvLinea)),
                        new XElement(cac + "TaxSubtotal",
                            new XElement(cbc + "TaxableAmount", new XAttribute("currencyID", "PEN"), Fmt(l.ValorVentaLinea)),
                            new XElement(cbc + "TaxAmount", new XAttribute("currencyID", "PEN"), Fmt(l.IgvLinea)),
                            new XElement(cac + "TaxCategory",
                                new XElement(cbc + "Percent", "18.00"),
                                new XElement(cbc + "TaxExemptionReasonCode", "10"),
                                new XElement(cac + "TaxScheme",
                                    new XElement(cbc + "ID", "1000"),
                                    new XElement(cbc + "Name", "IGV"),
                                    new XElement(cbc + "TaxTypeCode", "VAT")
                                )
                            )
                        )
                    ),
                    new XElement(cac + "Item",
                        new XElement(cbc + "Description", l.Descripcion.Trim()),
                        !string.IsNullOrWhiteSpace(l.CodigoBarras)
                            ? new XElement(cac + "SellersItemIdentification",
                                new XElement(cbc + "ID", l.CodigoBarras.Length > 30 ? l.CodigoBarras[..30] : l.CodigoBarras))
                            : null
                    ),
                    new XElement(cac + "Price",
                        new XElement(cbc + "PriceAmount", new XAttribute("currencyID", "PEN"), l.ValorUnitarioSinIgv.ToString("0.000000", CultureInfo.InvariantCulture))
                    )
                ));
            }

            var doc = new XDocument(new XDeclaration("1.0", "utf-8", null), invoice);
            using var ms = new MemoryStream();
            using (var writer = XmlWriter.Create(ms, new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = false }))
            {
                doc.Save(writer);
            }
            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private (string XmlFirmado, string DigestValue) FirmarXmlConPfx(string xmlSinFirma, string rutaCertificado, string claveCertificado)
        {
            using var certificado = X509CertificateLoader.LoadPkcs12FromFile(
                rutaCertificado,
                claveCertificado,
                X509KeyStorageFlags.Exportable);

            var xmlDoc = new XmlDocument { PreserveWhitespace = true };
            xmlDoc.LoadXml(xmlSinFirma);

            var nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
            nsmgr.AddNamespace("ext", "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2");
            var nodoExtension = xmlDoc.SelectSingleNode("//ext:ExtensionContent", nsmgr)
                ?? throw new Exception("No se encontró el nodo ext:ExtensionContent.");

            var signedXml = new SignedXml(xmlDoc)
            {
                SigningKey = certificado.GetRSAPrivateKey()
            };

            var reference = new Reference(string.Empty);
            reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
            signedXml.AddReference(reference);

            var keyInfo = new KeyInfo();
            keyInfo.AddClause(new KeyInfoX509Data(certificado));
            signedXml.KeyInfo = keyInfo;
            signedXml.Signature.Id = "SignatureSP";

            signedXml.ComputeSignature();

            XmlElement xmlFirma = signedXml.GetXml();
            nodoExtension.AppendChild(xmlDoc.ImportNode(xmlFirma, true));

            string digestValue = xmlFirma.GetElementsByTagName("DigestValue")[0]?.InnerText ?? string.Empty;
            return (xmlDoc.OuterXml, digestValue);
        }

        private async Task<(bool Aceptado, string Mensaje, byte[]? CdrZip)> EnviarSoapSunatAsync(
            string nombreArchivoBase,
            string xmlFirmado)
        {
            byte[] bytesZip;
            using (var ms = new MemoryStream())
            {
                using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
                {
                    var entry = archive.CreateEntry($"{nombreArchivoBase}.xml", CompressionLevel.Optimal);
                    using var entryStream = entry.Open();
                    using var writer = new StreamWriter(entryStream, new UTF8Encoding(false));
                    await writer.WriteAsync(xmlFirmado);
                }
                bytesZip = ms.ToArray();
            }

            string zipBase64 = Convert.ToBase64String(bytesZip);
            string usuarioCompleto = $"{_settings.RucEmisor}{_settings.UsuarioSol}";

            string soapEnvelope = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/""
                  xmlns:ser=""http://service.sunat.gob.pe""
                  xmlns:wsse=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd"">
    <soapenv:Header>
        <wsse:Security>
            <wsse:UsernameToken>
                <wsse:Username>{usuarioCompleto}</wsse:Username>
                <wsse:Password>{_settings.ClaveSol}</wsse:Password>
            </wsse:UsernameToken>
        </wsse:Security>
    </soapenv:Header>
    <soapenv:Body>
        <ser:sendBill>
            <fileName>{nombreArchivoBase}.zip</fileName>
            <contentFile>{zipBase64}</contentFile>
        </ser:sendBill>
    </soapenv:Body>
</soapenv:Envelope>";

            using var request = new HttpRequestMessage(HttpMethod.Post, _settings.UrlServicio)
            {
                Content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml")
            };
            request.Headers.Add("SOAPAction", "urn:sendBill");

            var response = await _httpClient.SendAsync(request);
            string respuestaXml = await response.Content.ReadAsStringAsync();

            var docResp = new XmlDocument();
            docResp.LoadXml(respuestaXml);

            var nodoCdr = docResp.GetElementsByTagName("applicationResponse");
            if (nodoCdr.Count > 0 && !string.IsNullOrEmpty(nodoCdr[0]?.InnerText))
            {
                byte[] cdrBytes = Convert.FromBase64String(nodoCdr[0]!.InnerText);
                var (codigoCdr, descripcionCdr) = LeerRespuestaCdr(cdrBytes);
                return (codigoCdr == "0", descripcionCdr, cdrBytes);
            }

            string faultCode = docResp.GetElementsByTagName("faultcode")[0]?.InnerText ?? "";
            string faultString = docResp.GetElementsByTagName("faultstring")[0]?.InnerText ?? "Error desconocido al enviar a SUNAT.";
            return (false, $"{faultCode}: {faultString}".Trim(':', ' '), null);
        }

        private (string Codigo, string Descripcion) LeerRespuestaCdr(byte[] cdrZipBytes)
        {
            using var ms = new MemoryStream(cdrZipBytes);
            using var archive = new ZipArchive(ms, ZipArchiveMode.Read);
            var xmlEntry = archive.Entries.FirstOrDefault(e => e.Name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));
            if (xmlEntry == null) return ("0", "CDR recibido de SUNAT.");

            using var reader = new StreamReader(xmlEntry.Open(), Encoding.UTF8);
            var doc = new XmlDocument();
            doc.LoadXml(reader.ReadToEnd());

            string codigo = doc.GetElementsByTagName("cbc:ResponseCode")[0]?.InnerText ?? "0";
            string descripcion = doc.GetElementsByTagName("cbc:Description")[0]?.InnerText ?? "Comprobante aceptado por SUNAT.";
            return (codigo, descripcion);
        }

        private static string Fmt(decimal valor) => valor.ToString("0.00", CultureInfo.InvariantCulture);
    }
}