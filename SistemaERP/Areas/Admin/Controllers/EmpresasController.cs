using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaERP.Areas.Admin.Models;
using SistemaERP.Areas.Admin.Services;

namespace SistemaERP.Areas.Admin.Controllers
{
    public class EmpresasController : BaseAdminController
    {
        private readonly AdminContext _context;
        private readonly IEncryptionService _encryptionService;
        private readonly IEmpresaConnectionService _empresaConnService;
        private readonly IRubroService _rubroService;
        private readonly IAdminAuditService _auditService;

        public EmpresasController(
            AdminContext context,
            IEncryptionService encryptionService,
            IEmpresaConnectionService empresaConnService,
            IRubroService rubroService,
            IAdminAuditService auditService)
        {
            _context = context;
            _encryptionService = encryptionService;
            _empresaConnService = empresaConnService;
            _rubroService = rubroService;
            _auditService = auditService;
        }

        public async Task<IActionResult> Index()
        {
            var empresas = await _context.Empresas
                .Include(e => e.Rubro)
                .OrderByDescending(e => e.FechaAlta)
                .ToListAsync();

            return View(empresas);
        }

        [HttpGet]
        public async Task<IActionResult> Crear()
        {
            ViewBag.RubroId = new SelectList(await _context.Rubros.Where(r => r.Activo).ToListAsync(), "Id", "Nombre");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Empresa model, string connectionStringPlan)
        {
            if (string.IsNullOrEmpty(connectionStringPlan))
            {
                ModelState.AddModelError("ConnectionStringEncriptado", "La cadena de conexión es obligatoria.");
            }

            // ConnectionStringEncriptado se asigna manualmente abajo, removemos validación automática del modelo
            ModelState.Remove("ConnectionStringEncriptado");

            if (ModelState.IsValid)
            {
                try
                {
                    // 1. Probar conexión antes de guardar
                    bool conexionExitosa = _empresaConnService.TestConnection(connectionStringPlan);
                    if (!conexionExitosa)
                    {
                        ModelState.AddModelError("ConnectionStringEncriptado", "No se pudo conectar a la base de datos con esta cadena de conexión. Verifique el servidor, usuario y clave.");
                        ViewBag.RubroId = new SelectList(await _context.Rubros.Where(r => r.Activo).ToListAsync(), "Id", "Nombre", model.RubroId);
                        return View(model);
                    }

                    // 2. Cifrar la connection string
                    model.ConnectionStringEncriptado = _encryptionService.Encrypt(connectionStringPlan);
                    model.FechaAlta = DateTime.Now;
                    model.Estado = true;

                    _context.Empresas.Add(model);
                    await _context.SaveChangesAsync();

                    // Registrar en auditoría
                    await _auditService.LogActionAsync(CurrentAdminId, model.Id, "CREAR_EMPRESA", $"Se creó la empresa {model.Nombre} con RUC {model.RUC}.");

                    // 3. Inicializar Rubro automáticamente (Catálogos y Presentaciones por defecto)
                    int insertados = await _rubroService.AplicarRubroAsync(model.Id, model.RubroId);
                    
                    await _auditService.LogActionAsync(CurrentAdminId, model.Id, "APLICAR_RUBRO", $"Se inicializaron {insertados} registros de catálogos y parámetros para el rubro en la empresa.");

                    TempData["SuccessMessage"] = $"Empresa '{model.Nombre}' creada y configurada con {insertados} catálogos predeterminados exitosamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    var msg = ex.Message;
                    if (ex.InnerException != null)
                    {
                        msg += " | Detalle: " + ex.InnerException.Message;
                        if (ex.InnerException.InnerException != null)
                        {
                            msg += " | Adicional: " + ex.InnerException.InnerException.Message;
                        }
                    }
                    ModelState.AddModelError("", $"Error al crear la empresa: {msg}");
                }
            }

            ViewBag.RubroId = new SelectList(await _context.Rubros.Where(r => r.Activo).ToListAsync(), "Id", "Nombre", model.RubroId);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var empresa = await _context.Empresas.FindAsync(id);
            if (empresa == null) return NotFound();

            // Descifrar para que el admin pueda ver/editar
            ViewBag.ConnectionStringPlan = _encryptionService.Decrypt(empresa.ConnectionStringEncriptado);
            ViewBag.RubroId = new SelectList(await _context.Rubros.Where(r => r.Activo).ToListAsync(), "Id", "Nombre", empresa.RubroId);

            return View(empresa);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, Empresa model, string connectionStringPlan)
        {
            if (id != model.Id) return NotFound();

            if (string.IsNullOrEmpty(connectionStringPlan))
            {
                ModelState.AddModelError("ConnectionStringEncriptado", "La cadena de conexión es obligatoria.");
            }

            // ConnectionStringEncriptado se asigna manualmente, removemos validación del modelo
            ModelState.Remove("ConnectionStringEncriptado");

            if (ModelState.IsValid)
            {
                try
                {
                    var dbEmpresa = await _context.Empresas.FindAsync(id);
                    if (dbEmpresa == null) return NotFound();

                    // Probar conexión si cambió
                    string currentConn = _encryptionService.Decrypt(dbEmpresa.ConnectionStringEncriptado);
                    if (currentConn != connectionStringPlan)
                    {
                        bool conexionExitosa = _empresaConnService.TestConnection(connectionStringPlan);
                        if (!conexionExitosa)
                        {
                            ModelState.AddModelError("ConnectionStringEncriptado", "No se pudo conectar a la base de datos con la nueva cadena de conexión.");
                            ViewBag.RubroId = new SelectList(await _context.Rubros.Where(r => r.Activo).ToListAsync(), "Id", "Nombre", model.RubroId);
                            return View(model);
                        }
                        dbEmpresa.ConnectionStringEncriptado = _encryptionService.Encrypt(connectionStringPlan);
                    }

                    // Actualizar campos permitidos
                    dbEmpresa.Nombre = model.Nombre;
                    dbEmpresa.RUC = model.RUC;
                    dbEmpresa.Logo = model.Logo;
                    dbEmpresa.Subdominio = model.Subdominio;
                    dbEmpresa.Telefono = model.Telefono;
                    dbEmpresa.Email = model.Email;
                    dbEmpresa.Direccion = model.Direccion;
                    dbEmpresa.Ciudad = model.Ciudad;
                    dbEmpresa.NombreContacto = model.NombreContacto;
                    dbEmpresa.FechaVencimiento = model.FechaVencimiento;
                    dbEmpresa.VersionSistema = model.VersionSistema;

                    // Si cambió el rubro, se puede volver a aplicar las semillas
                    if (dbEmpresa.RubroId != model.RubroId)
                    {
                        dbEmpresa.RubroId = model.RubroId;
                        int insertados = await _rubroService.AplicarRubroAsync(dbEmpresa.Id, dbEmpresa.RubroId);
                        await _auditService.LogActionAsync(CurrentAdminId, dbEmpresa.Id, "CAMBIAR_RUBRO", $"Se cambió rubro a {model.RubroId} e inicializaron {insertados} catálogos.");
                    }

                    _context.Entry(dbEmpresa).State = EntityState.Modified;
                    await _context.SaveChangesAsync();

                    await _auditService.LogActionAsync(CurrentAdminId, dbEmpresa.Id, "EDITAR_EMPRESA", $"Se actualizó la información de {dbEmpresa.Nombre}.");

                    TempData["SuccessMessage"] = "Empresa actualizada exitosamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error al guardar: {ex.Message}");
                }
            }

            ViewBag.RubroId = new SelectList(await _context.Rubros.Where(r => r.Activo).ToListAsync(), "Id", "Nombre", model.RubroId);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleEstado(int id)
        {
            var empresa = await _context.Empresas.FindAsync(id);
            if (empresa == null) return NotFound();

            empresa.Estado = !empresa.Estado;
            _context.Entry(empresa).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            await _auditService.LogActionAsync(CurrentAdminId, empresa.Id, "TOGGLE_ESTADO", $"Se cambió el estado de la empresa a {(empresa.Estado ? "Activo" : "Inactivo")}.");

            return Json(new { success = true, nuevoEstado = empresa.Estado });
        }

        [HttpPost]
        public async Task<IActionResult> TestConexion(string connectionString)
        {
            bool test = _empresaConnService.TestConnection(connectionString);
            return Json(new { success = test });
        }

        [HttpPost]
        public async Task<IActionResult> Eliminar(int id)
        {
            var empresa = await _context.Empresas.FindAsync(id);
            if (empresa == null) return Json(new { success = false, message = "Empresa no encontrada." });

            try
            {
                _context.Empresas.Remove(empresa);
                await _context.SaveChangesAsync();
                await _auditService.LogActionAsync(CurrentAdminId, id, "ELIMINAR_EMPRESA", $"Se eliminó la empresa {empresa.Nombre} (RUC: {empresa.RUC}).");
                return Json(new { success = true, message = $"Empresa '{empresa.Nombre}' eliminada correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error al eliminar: {ex.Message}" });
            }
        }
    }
}
