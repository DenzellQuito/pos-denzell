namespace POS_DENZELL.DTOs
{
    public class ClienteResumenDTO
    {
        public int IdCliente { get; set; }
        public string TipoDocumento { get; set; } = string.Empty;
        public string NumeroDocumento { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string? Direccion { get; set; }
        public bool Activo { get; set; }
        public int TotalCompras { get; set; }
        public decimal TotalGastado { get; set; }
    }

    public class ClienteBusquedaPosDTO
    {
        public int IdCliente { get; set; }
        public string TipoDocumento { get; set; } = string.Empty;
        public string NumeroDocumento { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
    }
}