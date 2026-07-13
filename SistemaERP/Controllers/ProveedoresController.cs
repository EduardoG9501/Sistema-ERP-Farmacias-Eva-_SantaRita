using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaERP.Models;
using SistemaERP.Models.Entities;

namespace SistemaERP.Controllers
{
    public class ProveedoresController : Controller
    {
        private readonly ScmContext _context;

        public ProveedoresController(ScmContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var list = await _context.Proveedores.ToListAsync();
                return View(list);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error al obtener proveedores: " + ex.Message;
                return View(new List<Proveedor>());
            }
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Proveedor proveedor)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(proveedor);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Proveedor registrado exitosamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error al registrar: " + ex.Message;
                }
            }
            return View(proveedor);
        }

        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null) return NotFound();

            var proveedor = await _context.Proveedores.FindAsync(id);
            if (proveedor == null) return NotFound();

            return View(proveedor);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, Proveedor proveedor)
        {
            if (id != proveedor.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(proveedor);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Proveedor actualizado exitosamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error al actualizar: " + ex.Message;
                }
            }
            return View(proveedor);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(long id)
        {
            var proveedor = await _context.Proveedores.FindAsync(id);
            if (proveedor == null) return Json(new { success = false, message = "Registro no encontrado." });

            try
            {
                _context.Proveedores.Remove(proveedor);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Proveedor eliminado exitosamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al eliminar: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CrearRapido([FromBody] ProveedorRapidoRequest req)
        {
            if (string.IsNullOrWhiteSpace(req?.Nombre))
                return Json(new { success = false, message = "El nombre del proveedor es obligatorio." });
            try
            {
                var proveedor = new Proveedor
                {
                    Nombre = req.Nombre,
                    RUC = req.RUC,
                    Direccion = req.Direccion ?? "ND",
                    Telefono = req.Telefono ?? "ND"
                };
                _context.Proveedores.Add(proveedor);
                await _context.SaveChangesAsync();
                return Json(new { success = true, id = proveedor.Id, nombre = proveedor.Nombre });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al crear proveedor: " + ex.Message });
            }
        }

        public class ProveedorRapidoRequest
        {
            public string? Nombre { get; set; }
            public string? RUC { get; set; }
            public string? Direccion { get; set; }
            public string? Telefono { get; set; }
        }
    }
}
