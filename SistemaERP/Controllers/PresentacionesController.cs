using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaERP.Models;
using SistemaERP.Models.Entities;

namespace SistemaERP.Controllers
{
    public class PresentacionesController : Controller
    {
        private readonly ScmContext _context;

        public PresentacionesController(ScmContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var list = await _context.Presentaciones.ToListAsync();
                return View(list);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error al obtener presentaciones: " + ex.Message;
                return View(new List<Presentacion>());
            }
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Presentacion presentacion)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(presentacion);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Presentación creada exitosamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error al crear: " + ex.Message;
                }
            }
            return View(presentacion);
        }

        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null) return NotFound();

            var presentacion = await _context.Presentaciones.FindAsync(id);
            if (presentacion == null) return NotFound();

            return View(presentacion);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, Presentacion presentacion)
        {
            if (id != presentacion.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(presentacion);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Presentación actualizada exitosamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error al actualizar: " + ex.Message;
                }
            }
            return View(presentacion);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(long id)
        {
            var presentacion = await _context.Presentaciones.FindAsync(id);
            if (presentacion == null) return Json(new { success = false, message = "Registro no encontrado." });

            try
            {
                _context.Presentaciones.Remove(presentacion);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Presentación eliminada exitosamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al eliminar: " + ex.Message });
            }
        }
    }
}
