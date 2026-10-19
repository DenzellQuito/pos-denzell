using Microsoft.EntityFrameworkCore;
using POS_DENZELL.Data;
using POS_DENZELL.DTOs;
using POS_DENZELL.Models;

namespace POS_DENZELL.Services
{
    
    public interface ICajaService
    {
        Task<TurnoCaja?> ObtenerTurnoAbiertoAsync();
        Task<decimal> ObtenerTotalVentasTurnoAsync(int idTurno);
        Task AbrirTurnoAsync(int idUsuario, decimal montoInicial);
        Task RegistrarGastoAsync(decimal monto, string descripcion);
        Task<ResumenCajaDTO> ObtenerResumenTurnoAsync(int idTurno);
        Task CerrarTurnoAsync(int idTurno, decimal montoFinalReal);
        Task<List<TurnoHistorialDTO>> ObtenerHistorialTurnosAsync();

        Task<List<VentaTurnoDTO>> ObtenerVentasPorTurnoAsync(int idTurno);
    }

    public class CajaService : ICajaService
    {
        private readonly ApplicationDbContext _context;

        public CajaService(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1 BUSCAR SI HAY UNA CAJA ABIERTA
        public async Task<TurnoCaja?> ObtenerTurnoAbiertoAsync()
        {
            return await _context.TurnosCaja
                .Include(t => t.UsuarioCajero)
                .FirstOrDefaultAsync(t => t.Estado == "Abierto");
        }

        // 2 SUMAR VENTAS DEL TURNO PARA EL DASHBOARD (HOME)
        public async Task<decimal> ObtenerTotalVentasTurnoAsync(int idTurno)
        {
            return await _context.Ventas
                .Where(v => v.IdTurno == idTurno)
                .SumAsync(v => v.MontoTotal);
        }

        // 3 ABRIR UN NUEVO TURNO CON CANDADO DE SEGURIDAD
        public async Task AbrirTurnoAsync(int idUsuario, decimal montoInicial)
        {
            bool yaExisteAbierto = await _context.TurnosCaja.AnyAsync(t => t.Estado == "Abierto");
            if (yaExisteAbierto)
            {
                throw new Exception("Ya existe un turno de caja abierto.");
            }

            var nuevoTurno = new TurnoCaja
            {
                IdUsuarioApertura = idUsuario,
                FechaHoraApertura = DateTime.Now,
                MontoInicial = montoInicial,
                Estado = "Abierto"
            };

            _context.TurnosCaja.Add(nuevoTurno);
            await _context.SaveChangesAsync();
        }

        // 4 REGISTRAR SALIDA DE DINERO (GASTO)
        public async Task RegistrarGastoAsync(decimal monto, string descripcion)
        {
            var cajaAbierta = await ObtenerTurnoAbiertoAsync();
            if (cajaAbierta == null)
            {
                throw new Exception("No hay un turno abierto para registrar gastos.");
            }

            var movimiento = new MovimientoCaja
            {
                IdTurno = cajaAbierta.IdTurno,
                Tipo = "GASTO",
                Monto = monto,
                Descripcion = descripcion.Trim(),
                FechaHora = DateTime.Now
            };

            _context.MovimientosCaja.Add(movimiento);
            await _context.SaveChangesAsync();
        }

        // 5 RESUMEN MATEMÁTICO DEL TURNO
        public async Task<ResumenCajaDTO> ObtenerResumenTurnoAsync(int idTurno)
        {
            var turno = await _context.TurnosCaja.FindAsync(idTurno);
            if (turno == null) throw new Exception("Turno no encontrado.");

            var ventas = await _context.Ventas.Where(v => v.IdTurno == idTurno).ToListAsync();

            var gastos = await _context.MovimientosCaja
                .Where(m => m.IdTurno == idTurno && m.Tipo == "GASTO")
                .SumAsync(m => m.Monto);

            decimal efectivoRealIngresado = ventas.Sum(v => v.PagoEfectivo - v.Vuelto);
            decimal yapeIngresado = ventas.Sum(v => v.PagoYape);

            return new ResumenCajaDTO
            {
                IdTurno = turno.IdTurno,
                MontoInicial = turno.MontoInicial,
                IngresosEfectivo = efectivoRealIngresado,
                IngresosYape = yapeIngresado,
                Gastos = gastos,
                CantidadTickets = ventas.Count 
            };
        }

        // 6 CERRAR Y AUDITAR EL TURNO
        public async Task CerrarTurnoAsync(int idTurno, decimal montoFinalReal)
        {
            var turno = await _context.TurnosCaja.FindAsync(idTurno);
            if (turno == null) throw new Exception("Turno no encontrado.");

            var resumen = await ObtenerResumenTurnoAsync(idTurno);

            turno.Estado = "Cerrado";
            turno.FechaHoraCierre = DateTime.Now;
            turno.MontoFinalCalculado = resumen.EfectivoEsperadoFisico;
            turno.MontoFinalReal = montoFinalReal;

            _context.TurnosCaja.Update(turno);
            await _context.SaveChangesAsync();
        }

        // 7 HISTORIAL DE TURNOS
        public async Task<List<TurnoHistorialDTO>> ObtenerHistorialTurnosAsync()
        {
            return await (from t in _context.TurnosCaja
                          join u in _context.Usuarios on t.IdUsuarioApertura equals u.IdUsuario
                          orderby t.FechaHoraApertura descending
                          select new TurnoHistorialDTO
                          {
                              IdTurno = t.IdTurno,
                              NombreCajero = u.Nombre,
                              FechaApertura = t.FechaHoraApertura,
                              FechaCierre = t.FechaHoraCierre,
                              MontoInicial = t.MontoInicial,
                              MontoFinalCalculado = t.MontoFinalCalculado ?? 0,
                              MontoFinalReal = t.MontoFinalReal ?? 0,
                              Estado = t.Estado
                          }).ToListAsync();
        }

        // 8 OBTENER TODAS LAS VENTAS DE UN TURNO (Para Auditoría y Reimpresión)
        public async Task<List<VentaTurnoDTO>> ObtenerVentasPorTurnoAsync(int idTurno)
        {
            return await (from v in _context.Ventas
                          join u in _context.Usuarios on v.IdUsuario equals u.IdUsuario
                          where v.IdTurno == idTurno
                          orderby v.FechaHora descending
                          select new VentaTurnoDTO
                          {
                              IdVenta = v.IdVenta,
                              Serie = v.Serie,
                              Correlativo = v.Correlativo,
                              FechaHora = v.FechaHora,
                              NombreCajero = u.Nombre,
                              MontoTotal = v.MontoTotal,
                              PagoEfectivo = v.PagoEfectivo,
                              PagoYape = v.PagoYape,
                              Vuelto = v.Vuelto,
                              NumeroOperacion = v.NumeroOperacion,
                              RutaFotoComprobante = v.RutaFotoComprobante
                          }).ToListAsync();
        }
    }
}