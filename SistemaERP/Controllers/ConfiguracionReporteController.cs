using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaERP.Models;
using SistemaERP.Models.Entities;

namespace SistemaERP.Controllers
{
    public class ConfiguracionReporteController : Controller
    {
        private readonly ScmContext _context;

        public ConfiguracionReporteController(ScmContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var rows = await _context.ConfiguracionEstadoResultados
                .Include(r => r.CuentaContable)
                .OrderBy(r => r.Orden)
                .ToListAsync();
            return View(rows);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Cuentas = await _context.CuentasContables.Where(c => c.Activo).OrderBy(c => c.Codigo).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ConfiguracionEstadoResultados config)
        {
            if (ModelState.IsValid)
            {
                _context.Add(config);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Fila de reporte agregada con éxito.";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Cuentas = await _context.CuentasContables.Where(c => c.Activo).OrderBy(c => c.Codigo).ToListAsync();
            return View(config);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var config = await _context.ConfiguracionEstadoResultados.FindAsync(id);
            if (config == null) return NotFound();

            ViewBag.Cuentas = await _context.CuentasContables.Where(c => c.Activo).OrderBy(c => c.Codigo).ToListAsync();
            return View(config);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ConfiguracionEstadoResultados config)
        {
            if (id != config.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(config);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Fila de reporte actualizada con éxito.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ConfigExists(config.Id)) return NotFound();
                    throw;
                }
            }
            ViewBag.Cuentas = await _context.CuentasContables.Where(c => c.Activo).OrderBy(c => c.Codigo).ToListAsync();
            return View(config);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var config = await _context.ConfiguracionEstadoResultados.FindAsync(id);
            if (config == null) return Json(new { success = false, message = "Fila no encontrada." });

            _context.ConfiguracionEstadoResultados.Remove(config);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Fila eliminada con éxito." });
        }

        public async Task<IActionResult> Integracion()
        {
            var paramsSistema = await _context.ParametrosSistema.FirstOrDefaultAsync(p => p.Id == 1);
            if (paramsSistema == null)
            {
                paramsSistema = new ParametrosSistema { Id = 1 };
                _context.ParametrosSistema.Add(paramsSistema);
                await _context.SaveChangesAsync();
            }

            ViewBag.Cuentas = await _context.CuentasContables.Where(c => c.Activo).OrderBy(c => c.Codigo).ToListAsync();
            return View(paramsSistema);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Integracion(int? cuentaVentasId, int? cuentaCostosId)
        {
            var paramsSistema = await _context.ParametrosSistema.FirstOrDefaultAsync(p => p.Id == 1);
            if (paramsSistema != null)
            {
                paramsSistema.CuentaContableVentasId = cuentaVentasId;
                paramsSistema.CuentaContableCostosId = cuentaCostosId;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Integración contable guardada con éxito.";
            }
            return RedirectToAction(nameof(Integracion));
        }

        private bool ConfigExists(int id)
        {
            return _context.ConfiguracionEstadoResultados.Any(e => e.Id == id);
        }
    }
}
