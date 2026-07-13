using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaERP.Areas.Admin.Models;
using SistemaERP.Areas.Admin.Services;

namespace SistemaERP.Areas.Admin.Controllers
{
    public class BackupsController : BaseAdminController
    {
        private readonly AdminContext _context;
        private readonly IBackupService _backupService;
        private readonly IAdminAuditService _auditService;

        public BackupsController(
            AdminContext context,
            IBackupService backupService,
            IAdminAuditService auditService)
        {
            _context = context;
            _backupService = backupService;
            _auditService = auditService;
        }

        public async Task<IActionResult> Index(int? empresaId)
        {
            var query = _context.Backups
                .Include(b => b.Empresa)
                .Include(b => b.AdminUsuario)
                .AsQueryable();

            if (empresaId.HasValue && empresaId.Value > 0)
            {
                query = query.Where(b => b.EmpresaId == empresaId.Value);
                ViewBag.SelectedEmpresaId = empresaId.Value;
            }

            var backups = await query.OrderByDescending(b => b.FechaBackup).ToListAsync();
            
            ViewBag.Empresas = new SelectList(await _context.Empresas.Where(e => e.Estado).ToListAsync(), "Id", "Nombre", empresaId);

            return View(backups);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(int empresaId, string? observaciones)
        {
            if (empresaId <= 0)
            {
                TempData["ErrorMessage"] = "Debe seleccionar una empresa válida.";
                return RedirectToAction(nameof(Index));
            }

            var empresa = await _context.Empresas.FindAsync(empresaId);
            if (empresa == null)
            {
                TempData["ErrorMessage"] = "La empresa no existe.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Dispara el backup de manera asíncrona
                var backup = await _backupService.CreateBackupAsync(empresaId, CurrentAdminId, observaciones);

                if (backup.Estado == "Exitoso")
                {
                    await _auditService.LogActionAsync(CurrentAdminId, empresaId, "BACKUP_EXITOSO", $"Copia de seguridad exitosa para {empresa.Nombre} de {backup.TamanoMB} MB.");
                    TempData["SuccessMessage"] = $"Copia de seguridad para '{empresa.Nombre}' completada exitosamente. Tamaño: {backup.TamanoMB} MB.";
                }
                else
                {
                    await _auditService.LogActionAsync(CurrentAdminId, empresaId, "BACKUP_FALLIDO", $"Fallo en la copia de seguridad de {empresa.Nombre}.");
                    TempData["ErrorMessage"] = $"Fallo al crear la copia de seguridad: {backup.Observaciones}";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error catastrófico en el proceso de respaldo: {ex.Message}";
            }

            return RedirectToAction(nameof(Index), new { empresaId });
        }

        [HttpGet]
        public async Task<IActionResult> Descargar(int id)
        {
            var backup = await _context.Backups.Include(b => b.Empresa).FirstOrDefaultAsync(b => b.Id == id);
            if (backup == null) return NotFound();

            if (backup.Estado != "Exitoso")
            {
                TempData["ErrorMessage"] = "El respaldo seleccionado no se completó con éxito, no se puede descargar.";
                return RedirectToAction(nameof(Index));
            }

            if (!System.IO.File.Exists(backup.RutaArchivo))
            {
                TempData["ErrorMessage"] = "El archivo físico de respaldo no se encuentra en la ruta especificada de este servidor. Puede haberse almacenado en un disco de red remota.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var fileBytes = await System.IO.File.ReadAllBytesAsync(backup.RutaArchivo);
                var fileName = Path.GetFileName(backup.RutaArchivo);
                
                await _auditService.LogActionAsync(CurrentAdminId, backup.EmpresaId, "BACKUP_DESCARGAR", $"Descarga del archivo de copia de seguridad {fileName}.");
                
                return base.File(fileBytes, "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"No se pudo descargar el archivo: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
