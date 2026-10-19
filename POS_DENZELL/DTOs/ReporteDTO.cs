namespace POS_DENZELL.DTOs
{
    public class DashboardBiDTO
    {
        // Filtros activos
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public string PeriodoSeleccionado { get; set; } = "mes";

        // KPIs de Flujo de Dinero (Prioridad 5A)
        public decimal TotalVentasBrutas { get; set; }
        public decimal TotalEfectivoNeto { get; set; }
        public decimal TotalYape { get; set; }
        public decimal TotalGastosCaja { get; set; }
        public decimal IngresoNetoReal => TotalVentasBrutas - TotalGastosCaja;

        public int CantidadTickets { get; set; }
        public decimal TicketPromedio => CantidadTickets > 0
            ? Math.Round(TotalVentasBrutas / CantidadTickets, 2, MidpointRounding.AwayFromZero)
            : 0m;

        public decimal PorcentajeEfectivo => TotalVentasBrutas > 0
            ? Math.Round((TotalEfectivoNeto / TotalVentasBrutas) * 100, 1, MidpointRounding.AwayFromZero)
            : 0m;

        public decimal PorcentajeYape => TotalVentasBrutas > 0
            ? Math.Round((TotalYape / TotalVentasBrutas) * 100, 1, MidpointRounding.AwayFromZero)
            : 0m;

        // KPI Especial: Impacto Categoría "🎉 Fiesta" (Prioridad 4B)
        public decimal TotalVentasFiesta { get; set; }
        public int UnidadesVendidasFiesta { get; set; }
        public decimal PorcentajeFiesta => TotalVentasBrutas > 0
            ? Math.Round((TotalVentasFiesta / TotalVentasBrutas) * 100, 1, MidpointRounding.AwayFromZero)
            : 0m;

        // Listas para Gráficos y Ranking
        public List<VentaPorCategoriaDTO> VentasPorCategoria { get; set; } = new();
        public List<VentaPorFechaDTO> EvolucionVentas { get; set; } = new();
        public List<ProductoTopDTO> TopProductos { get; set; } = new();
    }

    public class VentaPorCategoriaDTO
    {
        public string Categoria { get; set; } = string.Empty;
        public decimal TotalVendido { get; set; }
        public int UnidadesVendidas { get; set; }
    }

    public class VentaPorFechaDTO
    {
        public string EtiquetaFecha { get; set; } = string.Empty;
        public decimal TotalVendido { get; set; }
    }

    public class ProductoTopDTO
    {
        public string CodigoBarras { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public bool EsFiesta { get; set; }
        public int CantidadVendida { get; set; }
        public decimal TotalGenerado { get; set; }
    }
}