using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaERP.Models;
using SistemaERP.Models.Entities;

namespace SistemaERP.Controllers
{
    public class CuentasContablesController : Controller
    {
        private readonly ScmContext _context;

        public CuentasContablesController(ScmContext context)
        {
            _context = context;
        }

        private async Task SembrarDatosPorDefecto()
        {
            if (!await _context.CuentasContables.AnyAsync())
            {
                // 1. Sembrar Catálogo de Cuentas
                var cVentas = new CuentaContable { Codigo = "4.01.01", Nombre = "VENTAS", Tipo = "Ingreso", Activo = true };
                var cCostos = new CuentaContable { Codigo = "5.01.01", Nombre = "COSTO DE VENTA", Tipo = "Costo", Activo = true };
                var cGastosAdmin = new CuentaContable { Codigo = "6.02", Nombre = "GASTOS DE ADMINISTRACION", Tipo = "Gasto", Activo = true };
                
                _context.CuentasContables.AddRange(cVentas, cCostos, cGastosAdmin);
                await _context.SaveChangesAsync();

                var cAguaEnergia = new CuentaContable { Codigo = "6.02.01", Nombre = "SERVICIOS BASICOS (AGUA Y ENERGIA)", Tipo = "Gasto", IdPadre = cGastosAdmin.Id, Activo = true };
                var cInternet = new CuentaContable { Codigo = "6.02.02", Nombre = "INTERNET", Tipo = "Gasto", IdPadre = cGastosAdmin.Id, Activo = true };
                var cRenta = new CuentaContable { Codigo = "6.02.03", Nombre = "RENTA DE LOCAL", Tipo = "Gasto", IdPadre = cGastosAdmin.Id, Activo = true };
                var cSalarios = new CuentaContable { Codigo = "6.02.04", Nombre = "SALARIOS Y VIATICOS DE TRANSPORTE", Tipo = "Gasto", IdPadre = cGastosAdmin.Id, Activo = true };
                var cGastosFin = new CuentaContable { Codigo = "6.04", Nombre = "GASTOS FINANCIEROS", Tipo = "Gasto", Activo = true };

                _context.CuentasContables.AddRange(cAguaEnergia, cInternet, cRenta, cSalarios, cGastosFin);
                await _context.SaveChangesAsync();

                // 2. Sembrar Configuracion Estado de Resultados
                if (!await _context.ConfiguracionEstadoResultados.AnyAsync())
                {
                    _context.ConfiguracionEstadoResultados.AddRange(
                        new ConfiguracionEstadoResultados { Orden = 1, Descripcion = "VENTAS", TipoFila = "Cuenta", CuentaContableId = cVentas.Id, EsNegrita = false },
                        new ConfiguracionEstadoResultados { Orden = 2, Descripcion = "COSTO DE VENTA", TipoFila = "Cuenta", CuentaContableId = cCostos.Id, EsNegrita = false },
                        new ConfiguracionEstadoResultados { Orden = 3, Descripcion = "UTILIDAD BRUTA", TipoFila = "Formula", Formula = "{1} - {2}", EsNegrita = true },
                        new ConfiguracionEstadoResultados { Orden = 4, Descripcion = "SERVICIOS BASICOS (AGUA Y ENERGIA)", TipoFila = "Cuenta", CuentaContableId = cAguaEnergia.Id, EsNegrita = false },
                        new ConfiguracionEstadoResultados { Orden = 5, Descripcion = "INTERNET", TipoFila = "Cuenta", CuentaContableId = cInternet.Id, EsNegrita = false },
                        new ConfiguracionEstadoResultados { Orden = 6, Descripcion = "RENTA DE LOCAL", TipoFila = "Cuenta", CuentaContableId = cRenta.Id, EsNegrita = false },
                        new ConfiguracionEstadoResultados { Orden = 7, Descripcion = "SALARIOS Y VIATICOS DE TRANSPORTE", TipoFila = "Cuenta", CuentaContableId = cSalarios.Id, EsNegrita = false },
                        new ConfiguracionEstadoResultados { Orden = 8, Descripcion = "GASTOS DE ADMINISTRACION", TipoFila = "Cuenta", CuentaContableId = cGastosAdmin.Id, EsNegrita = true },
                        new ConfiguracionEstadoResultados { Orden = 9, Descripcion = "UTILIDAD DE OPERACION", TipoFila = "Formula", Formula = "{3} - {8}", EsNegrita = true },
                        new ConfiguracionEstadoResultados { Orden = 10, Descripcion = "GASTOS FINANCIEROS", TipoFila = "Cuenta", CuentaContableId = cGastosFin.Id, EsNegrita = false },
                        new ConfiguracionEstadoResultados { Orden = 11, Descripcion = "DÉFICIT DEL EJERCICIO", TipoFila = "Formula", Formula = "{9} - {10}", EsNegrita = true }
                    );
                    await _context.SaveChangesAsync();
                }

                // 3. Configurar mapeos iniciales en ParametrosSistema
                var paramsSistema = await _context.ParametrosSistema.FirstOrDefaultAsync(p => p.Id == 1);
                if (paramsSistema != null)
                {
                    paramsSistema.CuentaContableVentasId = cVentas.Id;
                    paramsSistema.CuentaContableCostosId = cCostos.Id;
                    await _context.SaveChangesAsync();
                }
            }
        }

        public async Task<IActionResult> Index()
        {
            await SembrarDatosPorDefecto();
            var cuentas = await _context.CuentasContables.OrderBy(c => c.Codigo).ToListAsync();
            return View(cuentas);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.CuentasPadre = await _context.CuentasContables.Where(c => c.Activo).OrderBy(c => c.Codigo).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CuentaContable cuenta)
        {
            if (ModelState.IsValid)
            {
                _context.Add(cuenta);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Cuenta contable creada con éxito.";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.CuentasPadre = await _context.CuentasContables.Where(c => c.Activo).OrderBy(c => c.Codigo).ToListAsync();
            return View(cuenta);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var cuenta = await _context.CuentasContables.FindAsync(id);
            if (cuenta == null) return NotFound();

            ViewBag.CuentasPadre = await _context.CuentasContables.Where(c => c.Activo && c.Id != id).OrderBy(c => c.Codigo).ToListAsync();
            return View(cuenta);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CuentaContable cuenta)
        {
            if (id != cuenta.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(cuenta);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cuenta contable actualizada con éxito.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CuentaExists(cuenta.Id)) return NotFound();
                    throw;
                }
            }
            ViewBag.CuentasPadre = await _context.CuentasContables.Where(c => c.Activo && c.Id != id).OrderBy(c => c.Codigo).ToListAsync();
            return View(cuenta);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var cuenta = await _context.CuentasContables.FindAsync(id);
            if (cuenta == null) return Json(new { success = false, message = "Cuenta no encontrada." });

            // Verificar si tiene subcuentas
            if (await _context.CuentasContables.AnyAsync(c => c.IdPadre == id))
            {
                return Json(new { success = false, message = "No se puede eliminar porque esta cuenta tiene subcuentas asociadas." });
            }

            // Verificar si tiene gastos asociados
            if (await _context.Gastos.AnyAsync(g => g.CuentaContableId == id))
            {
                return Json(new { success = false, message = "No se puede eliminar porque esta cuenta tiene gastos registrados." });
            }

            _context.CuentasContables.Remove(cuenta);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Cuenta contable eliminada con éxito." });
        }

        private bool CuentaExists(int id)
        {
            return _context.CuentasContables.Any(e => e.Id == id);
        }
    }
}
