using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaERP.Models;
using SistemaERP.Models.Entities;

namespace SistemaERP.Controllers
{
    public class GastosController : Controller
    {
        private readonly ScmContext _context;

        public GastosController(ScmContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(DateTime? desde, DateTime? hasta, int? cuentaId)
        {
            var query = _context.Gastos.Include(g => g.CuentaContable).AsQueryable();

            var dateDesde = desde ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var dateHasta = hasta ?? DateTime.Today;

            query = query.Where(g => g.Fecha >= dateDesde.Date && g.Fecha <= dateHasta.Date);

            if (cuentaId.HasValue && cuentaId.Value > 0)
            {
                query = query.Where(g => g.CuentaContableId == cuentaId.Value);
            }

            var gastos = await query.OrderByDescending(g => g.Fecha).ToListAsync();

            ViewBag.Desde = dateDesde.ToString("yyyy-MM-dd");
            ViewBag.Hasta = dateHasta.ToString("yyyy-MM-dd");
            ViewBag.SelectedCuentaId = cuentaId;
            ViewBag.CuentasGasto = await _context.CuentasContables
                .Where(c => c.Activo && c.Tipo == "Gasto")
                .OrderBy(c => c.Codigo)
                .ToListAsync();

            return View(gastos);
        }

        public async Task<IActionResult> Create()
        {
            var cuentas = await _context.CuentasContables
                .Where(c => c.Activo && c.Tipo == "Gasto")
                .OrderBy(c => c.Codigo)
                .ToListAsync();
            
            ViewBag.CuentasGasto = new SelectList(cuentas, "Id", "Nombre");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Gasto gasto)
        {
            if (ModelState.IsValid)
            {
                gasto.FechaRegistro = DateTime.Now;
                gasto.UsuarioRegistro = User.Identity?.Name ?? "Admin";
                _context.Add(gasto);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Gasto registrado con éxito.";
                return RedirectToAction(nameof(Index));
            }
            
            var cuentas = await _context.CuentasContables
                .Where(c => c.Activo && c.Tipo == "Gasto")
                .OrderBy(c => c.Codigo)
                .ToListAsync();
            ViewBag.CuentasGasto = new SelectList(cuentas, "Id", "Nombre", gasto.CuentaContableId);
            return View(gasto);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var gasto = await _context.Gastos.FindAsync(id);
            if (gasto == null) return NotFound();

            var cuentas = await _context.CuentasContables
                .Where(c => c.Activo && c.Tipo == "Gasto")
                .OrderBy(c => c.Codigo)
                .ToListAsync();
            ViewBag.CuentasGasto = new SelectList(cuentas, "Id", "Nombre", gasto.CuentaContableId);
            return View(gasto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Gasto gasto)
        {
            if (id != gasto.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.Gastos.FindAsync(id);
                    if (existing == null) return NotFound();

                    existing.Fecha = gasto.Fecha;
                    existing.Monto = gasto.Monto;
                    existing.Descripcion = gasto.Descripcion;
                    existing.CuentaContableId = gasto.CuentaContableId;

                    _context.Update(existing);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Gasto actualizado con éxito.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GastoExists(gasto.Id)) return NotFound();
                    throw;
                }
            }
            var cuentas = await _context.CuentasContables
                .Where(c => c.Activo && c.Tipo == "Gasto")
                .OrderBy(c => c.Codigo)
                .ToListAsync();
            ViewBag.CuentasGasto = new SelectList(cuentas, "Id", "Nombre", gasto.CuentaContableId);
            return View(gasto);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var gasto = await _context.Gastos.FindAsync(id);
            if (gasto == null) return Json(new { success = false, message = "Gasto no encontrado." });

            _context.Gastos.Remove(gasto);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Gasto eliminado con éxito." });
        }

        private bool GastoExists(int id)
        {
            return _context.Gastos.Any(e => e.Id == id);
        }
    }
}
