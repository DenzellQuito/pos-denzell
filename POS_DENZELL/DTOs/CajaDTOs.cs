namespace POS_DENZELL.DTOs
{
    // DTO para el Arqueo y Cierre de Caja
    public class ResumenCajaDTO
    {
        public int IdTurno { get; set; }
        public decimal MontoInicial { get; set; }
        public decimal IngresosEfectivo { get; set; }
        public decimal IngresosYape { get; set; }
        public decimal Gastos { get; set; }
        public int CantidadTickets { get; set; } 
        public decimal TotalVentas => IngresosEfectivo + IngresosYape; 
        public decimal EfectivoEsperadoFisico => MontoInicial + IngresosEfectivo - Gastos;
    }

    // DTO para el Historial y Auditoría de Turnos
    public class TurnoHistorialDTO
    {
        public int IdTurno { get; set; }
        public string NombreCajero { get; set; } = string.Empty;
        public DateTime FechaApertura { get; set; }
        public DateTime? FechaCierre { get; set; }
        public decimal MontoInicial { get; set; }
        public decimal MontoFinalCalculado { get; set; }
        public decimal MontoFinalReal { get; set; }
        public string Estado { get; set; } = string.Empty;
        public decimal Descuadre => MontoFinalReal - MontoFinalCalculado;
    }

    // DTO para auditar cada ticket dentro de un turno
    public class VentaTurnoDTO
    {
        public int IdVenta { get; set; }
        public string Serie { get; set; } = string.Empty;
        public int Correlativo { get; set; }
        public DateTime FechaHora { get; set; }
        public string NombreCajero { get; set; } = string.Empty;
        public decimal MontoTotal { get; set; }
        public decimal PagoEfectivo { get; set; }
        public decimal PagoYape { get; set; }
        public decimal Vuelto { get; set; }
        public string? NumeroOperacion { get; set; }
        public string? RutaFotoComprobante { get; set; }

        public string NumeroComprobante => Correlativo > 0
            ? $"{Serie}-{Correlativo:D6}"
            : $"T001-{IdVenta:D6}";
    }
}