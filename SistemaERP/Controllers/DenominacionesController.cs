using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaERP.Models.Entities;
using SistemaERP.Models;

namespace SistemaERP.Controllers
{
    public class DenominacionesController : Controller
    {
        private readonly ScmContext _context;
        public DenominacionesController(ScmContext context)
        {
            _context = context;
        }

        // GET: Denominaciones
        public async Task<IActionResult> Index()
        {
            var list = await _context.Denominaciones.ToListAsync();
            return View(list);
        }

        // GET: Denominaciones/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var denominacion = await _context.Denominaciones.FirstOrDefaultAsync(m => m.Id == id);
            if (denominacion == null) return NotFound();
            return View(denominacion);
        }

        // GET: Denominaciones/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Denominaciones/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Nombre,Valor,Tipo,Activo")] Denominacion denominacion)
        {
            if (ModelState.IsValid)
            {
                _context.Add(denominacion);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(denominacion);
        }

        // GET: Denominaciones/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var denominacion = await _context.Denominaciones.FindAsync(id);
            if (denominacion == null) return NotFound();
            return View(denominacion);
        }

        // POST: Denominaciones/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Valor,Tipo,Activo")] Denominacion denominacion)
        {
            if (id != denominacion.Id) return NotFound();
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(denominacion);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DenominacionExists(denominacion.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(denominacion);
        }

        // GET: Denominaciones/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var denominacion = await _context.Denominaciones.FirstOrDefaultAsync(m => m.Id == id);
            if (denominacion == null) return NotFound();
            return View(denominacion);
        }

        // POST: Denominaciones/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var denominacion = await _context.Denominaciones.FindAsync(id);
            _context.Denominaciones.Remove(denominacion);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DenominacionExists(int id)
        {
            return _context.Denominaciones.Any(e => e.Id == id);
        }
    }

    public class CajaController : Controller
    {
        private readonly ScmContext _context;
        public CajaController(ScmContext context)
        {
            _context = context;
        }

        // GET: Caja/Apertura
        public IActionResult Apertura()
        {
            return View();
        }

        // POST: Caja/Apertura
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apertura([Bind("UsuarioId,MontoInicial,Observaciones")] AperturaCaja apertura)
        {
            if (ModelState.IsValid)
            {
                apertura.FechaApertura = DateTime.Now;
                _context.AperturasCaja.Add(apertura);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Historial));
            }
            return View(apertura);
        }

        // GET: Caja/Cierre/{id}
        public async Task<IActionResult> Cierre(int? id)
        {
            if (id == null) return NotFound();
            var apertura = await _context.AperturasCaja.FindAsync(id);
            if (apertura == null) return NotFound();
            var model = new CierreCaja { AperturaCajaId = apertura.Id, UsuarioId = apertura.UsuarioId };
            return View(model);
        }

        // POST: Caja/Cierre
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cierre([Bind("AperturaCajaId,UsuarioId,MontoFinal,Observaciones,DetalleDenominaciones,MetodosPago")]
            CierreCaja cierre)
        {
            if (ModelState.IsValid)
            {
                cierre.FechaCierre = DateTime.Now;
                // Calculate difference
                cierre.Diferencia = cierre.MontoFinal - (await _context.AperturasCaja
                    .Where(a => a.Id == cierre.AperturaCajaId)
                    .Select(a => a.MontoInicial).FirstOrDefaultAsync());
                _context.CierresCaja.Add(cierre);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Historial));
            }
            return View(cierre);
        }

        // GET: Caja/Historial
        public async Task<IActionResult> Historial()
        {
            var historial = await _context.CierresCaja.Include(c => c.Apertura).Include(c => c.Usuario).ToListAsync();
            return View(historial);
        }
    }
}
