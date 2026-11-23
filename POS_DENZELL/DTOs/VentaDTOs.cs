namespace POS_DENZELL.DTOs
{
    
        public class CarritoItemDTO
        {
            public int id { get; set; }
            public decimal precio { get; set; }
            public int cantidad { get; set; }
        }

        public class TicketVentaDTO
        {
            public int IdVenta { get; set; }
            public DateTime FechaHora { get; set; }
            public string NombreCajero { get; set; } = string.Empty;
            public decimal MontoTotal { get; set; }
            public decimal PagoEfectivo { get; set; }
            public decimal PagoYape { get; set; }
            public decimal Vuelto { get; set; }
            public List<TicketDetalleDTO> Detalles { get; set; } = new List<TicketDetalleDTO>();
            public string? ClienteDocumento { get; set; }
            public string? ClienteNombre { get; set; }
            public string? ClienteDireccion { get; set; }
            public decimal TotalIGV { get; set; }
            public string NumeroComprobante { get; set; } = "";

            public string TipoDocumento { get; set; } = "TK"; // "TK", "03" (Boleta), "01" (Factura)
            public string Serie { get; set; } = "T001";
            public int Correlativo { get; set; }
            public string EstadoSunat { get; set; } = "Local";
            public string? CodigoHashSunat { get; set; }
            public string CadenaQr
            {
                get
                {
                    string tipoDocCli = string.IsNullOrWhiteSpace(ClienteDocumento) ? "0"
                        : (ClienteDocumento.Trim().Length == 11 ? "6" : "1");
                    string numDocCli = string.IsNullOrWhiteSpace(ClienteDocumento) ? "00000000" : ClienteDocumento.Trim();

                    return $"10608523001|{TipoDocumento}|{Serie}|{Correlativo:D8}|{TotalIGV:0.00}|{MontoTotal:0.00}|{FechaHora:yyyy-MM-dd}|{tipoDocCli}|{numDocCli}|{CodigoHashSunat}";
                }
            }
            public string MontoEnLetras => POS_DENZELL.Helpers.NumeroALetras.ConvertirSoles(MontoTotal);
    }

        public class TicketDetalleDTO
        {
            public int Cantidad { get; set; }
            public string Descripcion { get; set; } = string.Empty;
            public decimal PrecioUnitario { get; set; }
            public decimal Subtotal { get; set; }
        }
    
}
