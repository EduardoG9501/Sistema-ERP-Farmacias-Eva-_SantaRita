using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaERP.Models;
using SistemaERP.Models.Entities;

namespace SistemaERP.Controllers
{
    public class BodegasController : Controller
    {
        private readonly ScmContext _context;

        public BodegasController(ScmContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Bodegas.ToListAsync());
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Bodega bodega)
        {
            if (ModelState.IsValid)
            {
                if (bodega.EsPrincipal)
                {
                    // Quitar principal a otras
                    var otras = await _context.Bodegas.Where(b => b.EsPrincipal).ToListAsync();
                    foreach (var b in otras) b.EsPrincipal = false;
                }

                _context.Add(bodega);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Bodega creada exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            return View(bodega);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var bodega = await _context.Bodegas.FindAsync(id);
            if (bodega == null) return NotFound();
            return View(bodega);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Bodega bodega)
        {
            if (id != bodega.Id) return NotFound();

            if (ModelState.IsValid)
            {
                if (bodega.EsPrincipal)
                {
                    var otras = await _context.Bodegas.Where(b => b.Id != id && b.EsPrincipal).ToListAsync();
                    foreach (var b in otras) b.EsPrincipal = false;
                }

                _context.Update(bodega);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Bodega actualizada.";
                return RedirectToAction(nameof(Index));
            }
            return View(bodega);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var bodega = await _context.Bodegas.FindAsync(id);
            if (bodega != null)
            {
                _context.Bodegas.Remove(bodega);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }
    }
}
