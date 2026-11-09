using Microsoft.EntityFrameworkCore;
using POS_DENZELL.Data;
using POS_DENZELL.DTOs;
using POS_DENZELL.Models;

namespace POS_DENZELL.Services
{
    public interface IClienteService
    {
        Task<List<ClienteResumenDTO>> ObtenerTodosConHistorialAsync();
        Task<List<ClienteBusquedaPosDTO>> BuscarClientesPosAsync(string termino);
        Task<Cliente> CrearClienteAsync(string tipoDoc, string numeroDoc, string nombre, string? telefono, string? direccion);
        Task EditarClienteAsync(int idCliente, string tipoDoc, string numeroDoc, string nombre, string? telefono, string? direccion);
        Task CambiarEstadoAsync(int idCliente);
    }

    public class ClienteService : IClienteService
    {
        private readonly ApplicationDbContext _context;

        public ClienteService(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Listado para el panel administrativo con total de compras y dinero gastado
        public async Task<List<ClienteResumenDTO>> ObtenerTodosConHistorialAsync()
        {
            return await _context.Clientes
                .OrderByDescending(c => c.Activo)
                .ThenBy(c => c.NombreCompleto)
                .Select(c => new ClienteResumenDTO
                {
                    IdCliente = c.IdCliente,
                    TipoDocumento = c.TipoDocumento,
                    NumeroDocumento = c.NumeroDocumento,
                    NombreCompleto = c.NombreCompleto,
                    Telefono = c.Telefono,
                    Direccion = c.Direccion,
                    Activo = c.Activo,
                    TotalCompras = c.Compras.Count(),
                    TotalGastado = c.Compras.Sum(v => (decimal?)v.MontoTotal) ?? 0m
                })
                .ToListAsync();
        }

        // 2. Buscador ultrarrápido para el Modal de Cobro en el Punto de Venta
        public async Task<List<ClienteBusquedaPosDTO>> BuscarClientesPosAsync(string termino)
        {
            if (string.IsNullOrWhiteSpace(termino))
                return new List<ClienteBusquedaPosDTO>();

            string filtro = termino.Trim().ToLower();

            return await _context.Clientes
                .Where(c => c.Activo &&
                           (c.NumeroDocumento.ToLower().Contains(filtro) ||
                            c.NombreCompleto.ToLower().Contains(filtro)))
                .OrderBy(c => c.NombreCompleto)
                .Take(6)
                .Select(c => new ClienteBusquedaPosDTO
                {
                    IdCliente = c.IdCliente,
                    TipoDocumento = c.TipoDocumento,
                    NumeroDocumento = c.NumeroDocumento,
                    NombreCompleto = c.NombreCompleto
                })
                .ToListAsync();
        }

        // 3. Crear Cliente (sirve tanto para la vista de Clientes como para el registro rápido en Caja)
        public async Task<Cliente> CrearClienteAsync(string tipoDoc, string numeroDoc, string nombre, string? telefono, string? direccion)
        {
            string docLimpio = numeroDoc.Trim();
            string nombreLimpio = nombre.Trim();

            if (string.IsNullOrEmpty(docLimpio) || string.IsNullOrEmpty(nombreLimpio))
                throw new Exception("El número de documento y el nombre son obligatorios.");

            bool existe = await _context.Clientes
                .AnyAsync(c => c.NumeroDocumento.ToLower() == docLimpio.ToLower());

            if (existe)
                throw new Exception($"El documento '{docLimpio}' ya está registrado en otro cliente.");

            var nuevo = new Cliente
            {
                TipoDocumento = string.IsNullOrWhiteSpace(tipoDoc) ? "DNI" : tipoDoc.Trim().ToUpper(),
                NumeroDocumento = docLimpio,
                NombreCompleto = nombreLimpio,
                Telefono = telefono?.Trim(),
                Direccion = direccion?.Trim(),
                FechaRegistro = DateTime.Now,
                Activo = true
            };

            _context.Clientes.Add(nuevo);
            await _context.SaveChangesAsync();
            return nuevo;
        }

        // 4. Editar datos del cliente
        public async Task EditarClienteAsync(int idCliente, string tipoDoc, string numeroDoc, string nombre, string? telefono, string? direccion)
        {
            var cliente = await _context.Clientes.FindAsync(idCliente);
            if (cliente == null) throw new Exception("Cliente no encontrado.");

            string docLimpio = numeroDoc.Trim();

            bool duplicado = await _context.Clientes
                .AnyAsync(c => c.IdCliente != idCliente && c.NumeroDocumento.ToLower() == docLimpio.ToLower());

            if (duplicado)
                throw new Exception($"El documento '{docLimpio}' ya pertenece a otro cliente.");

            cliente.TipoDocumento = tipoDoc.Trim().ToUpper();
            cliente.NumeroDocumento = docLimpio;
            cliente.NombreCompleto = nombre.Trim();
            cliente.Telefono = telefono?.Trim();
            cliente.Direccion = direccion?.Trim();

            await _context.SaveChangesAsync();
        }

        // 5. Borrado Lógico (Activar / Desactivar)
        public async Task CambiarEstadoAsync(int idCliente)
        {
            var cliente = await _context.Clientes.FindAsync(idCliente);
            if (cliente == null) throw new Exception("Cliente no encontrado.");

            cliente.Activo = !cliente.Activo;
            await _context.SaveChangesAsync();
        }
    }
}