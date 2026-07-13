using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaERP.Models;
using SistemaERP.Models.Entities;

namespace SistemaERP.Controllers
{
    public class CatGastosController : Controller
    {
        private readonly ScmContext _context;

        public CatGastosController(ScmContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var gastos = await _context.CatGastos.ToListAsync();
                return View(gastos);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error al obtener catálogo de gastos: " + ex.Message;
                return View(new List<CatGasto>());
            }
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CatGasto gasto)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    gasto.CreateDate = DateTime.Now;
                    gasto.CreateUser = User.Identity?.Name ?? "Admin";
                    gasto.Estado = true; 

                    _context.Add(gasto);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Tipo de gasto registrado exitosamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error al conectar con la base de datos: " + ex.Message;
                }
            }
            else
            {
                TempData["Error"] = "Por favor, complete todos los campos requeridos correctamente.";
            }
            return View(gasto);
        }


        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var gasto = await _context.CatGastos.FindAsync(id);
            if (gasto == null) return NotFound();

            return View(gasto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CatGasto gasto)
        {
            if (id != gasto.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.CatGastos.FindAsync(id);
                    if (existing == null) return NotFound();

                    existing.Descripcion = gasto.Descripcion;
                    existing.Estado = gasto.Estado;
                    
                    _context.Update(existing);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Tipo de gasto actualizado exitosamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error al actualizar: " + ex.Message;
                }
            }
            return View(gasto);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var gasto = await _context.CatGastos.FindAsync(id);
            if (gasto == null) return Json(new { success = false, message = "Registro no encontrado." });

            try
            {
                _context.CatGastos.Remove(gasto);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Tipo de gasto eliminado exitosamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al eliminar: " + ex.Message });
            }
        }
    }
}
