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
            public decimal TotalIGV { get; set; }
            public string NumeroComprobante { get; set; } = "";
    }

        public class TicketDetalleDTO
        {
            public int Cantidad { get; set; }
            public string Descripcion { get; set; } = string.Empty;
            public decimal PrecioUnitario { get; set; }
            public decimal Subtotal { get; set; }
        }
    
}
