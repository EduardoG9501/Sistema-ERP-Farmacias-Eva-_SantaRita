using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaERP.Models;
using SistemaERP.Models.Entities;

namespace SistemaERP.Controllers
{
    public partial class FacturacionController : Controller
    {
        public async Task<IActionResult> RIDE(long id)
        {
            var venta = await _context.Ventas.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id);
            if (venta == null) return NotFound();

            var cliente = await _context.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == (long?)venta.Id_Cliente);
            var detalles = await _context.VentasDetalle.AsNoTracking().Where(d => d.Id_Venta == id).ToListAsync();
            var settings = await _context.ConfiguracionSRI.AsNoTracking().FirstOrDefaultAsync();

            // Si el cajero capturó datos del comprador manualmente (CF > monto mínimo),
            // sobrescribir los datos del cliente genérico con los reales
            if (!string.IsNullOrWhiteSpace(venta.NameClienteTem))
            {
                ViewBag.NombreComprador    = venta.NameClienteTem.ToUpper();
                ViewBag.IdentificacionComp = venta.CF_Cedula ?? "9999999999999";
                ViewBag.DireccionComp      = venta.CF_Direccion ?? "S/N";
            }
            else
            {
                ViewBag.NombreComprador    = cliente?.Nombres_Apellidos?.ToUpper() ?? "CONSUMIDOR FINAL";
                ViewBag.IdentificacionComp = cliente?.Cedula ?? cliente?.RUC ?? "9999999999999";
                ViewBag.DireccionComp      = cliente?.Direccion ?? "S/N";
            }

            ViewBag.ClienteObj = cliente;
            ViewBag.Detalles   = detalles;
            ViewBag.Settings   = settings;

            return View(venta);
        }
    }
}
