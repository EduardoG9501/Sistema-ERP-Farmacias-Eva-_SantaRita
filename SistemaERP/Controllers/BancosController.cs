using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaERP.Models;
using SistemaERP.Models.Entities;
using System.Diagnostics;

namespace SistemaERP.Controllers
{
    public class BancosController : Controller
    {
        private readonly ScmContext _context;

        public BancosController(ScmContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var bancos = await _context.Bancos.ToListAsync();
                return View(bancos);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error al conectar con la base de datos: " + ex.Message;
                return View(new List<SistemaERP.Models.Entities.Banco>());
            }
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Banco banco)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(banco);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Banco creado exitosamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error al crear el banco: " + ex.Message;
                }
            }
            return View(banco);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var banco = await _context.Bancos.FindAsync(id);
            if (banco == null) return NotFound();

            return View(banco);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Banco banco)
        {
            if (id != banco.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(banco);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Banco actualizado exitosamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error al actualizar el banco: " + ex.Message;
                }
            }
            return View(banco);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var banco = await _context.Bancos.FindAsync(id);
            if (banco == null) return Json(new { success = false, message = "Banco no encontrado." });

            try
            {
                _context.Bancos.Remove(banco);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Banco eliminado exitosamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al eliminar: " + ex.Message });
            }
        }

    }
}
