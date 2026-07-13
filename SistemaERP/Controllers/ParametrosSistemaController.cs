using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaERP.Models;
using SistemaERP.Models.Entities;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using SistemaERP.Areas.Admin.Models;

namespace SistemaERP.Controllers
{
    [Authorize]
    public class ParametrosSistemaController : Controller
    {
        private readonly ScmContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly AdminContext _adminContext;

        public ParametrosSistemaController(ScmContext context, IWebHostEnvironment env, AdminContext adminContext)
        {
            _context = context;
            _env = env;
            _adminContext = adminContext;
        }

        // GET: ParametrosSistema
        public async Task<IActionResult> Index()
        {
            var config = await _context.ParametrosSistema.FirstOrDefaultAsync(p => p.Id == 1);
            if (config == null)
            {
                config = new ParametrosSistema { Id = 1 };
                _context.ParametrosSistema.Add(config);
                await _context.SaveChangesAsync();
            }

            var empresaIdClaim = User.FindFirst("EmpresaId")?.Value;
            if (empresaIdClaim != null && int.TryParse(empresaIdClaim, out int empId))
            {
                var centralEmpresa = await _adminContext.Empresas.FindAsync(empId);
                if (centralEmpresa != null)
                {
                    ViewBag.Subdominio = centralEmpresa.Subdominio;
                }
            }

            ViewBag.Monedas = await _context.Monedas.Where(m => m.Activo).OrderBy(m => m.Nombre).ToListAsync();
            return View(config);
        }

        // POST: ParametrosSistema
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ParametrosSistema model, IFormFile? LogoFile, bool RemoveLogo, IFormFile? WelcomeBgFile, bool RemoveWelcomeBg = false)
        {
            // Remove unnecessary model state errors for non-required fields
            ModelState.Remove("LogoFile");
            ModelState.Remove("LogoPath");
            ModelState.Remove("WelcomeBgFile");

            if (ModelState.IsValid)
            {
                try
                {
                    // Handle welcome background image
                    var welcomeBgEmpresaIdClaim = User.FindFirst("EmpresaId")?.Value;
                    if (welcomeBgEmpresaIdClaim != null && int.TryParse(welcomeBgEmpresaIdClaim, out int welcomeBgEmpId))
                    {
                        var dir = Path.Combine(_env.WebRootPath, "uploads", "empresa");
                        Directory.CreateDirectory(dir);
                        
                        if (RemoveWelcomeBg)
                        {
                            var existingFiles = Directory.GetFiles(dir, $"welcome_bg_{welcomeBgEmpId}.*");
                            foreach (var f in existingFiles)
                            {
                                try { System.IO.File.Delete(f); } catch { }
                            }
                        }
                        else if (WelcomeBgFile != null && WelcomeBgFile.Length > 0)
                        {
                            var existingFiles = Directory.GetFiles(dir, $"welcome_bg_{welcomeBgEmpId}.*");
                            foreach (var f in existingFiles)
                            {
                                try { System.IO.File.Delete(f); } catch { }
                            }

                            var ext = Path.GetExtension(WelcomeBgFile.FileName).ToLowerInvariant();
                            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
                            if (allowed.Contains(ext))
                            {
                                var fileName = $"welcome_bg_{welcomeBgEmpId}{ext}";
                                var filePath = Path.Combine(dir, fileName);
                                using var stream = new FileStream(filePath, FileMode.Create);
                                await WelcomeBgFile.CopyToAsync(stream);
                            }
                        }
                    }

                    var existing = await _context.ParametrosSistema.FirstOrDefaultAsync(p => p.Id == 1);
                    if (existing == null)
                    {
                        model.Id = 1;
                        model.UltimaActualizacion = DateTime.Now;

                        // Handle logo
                        if (RemoveLogo) model.LogoPath = null;
                        else if (LogoFile != null && LogoFile.Length > 0)
                            model.LogoPath = await SaveLogo(LogoFile, null);

                        _context.ParametrosSistema.Add(model);
                    }
                    else
                    {
                        // Handle logo
                        if (RemoveLogo && !string.IsNullOrEmpty(existing.LogoPath))
                        {
                            DeleteFile(existing.LogoPath);
                            existing.LogoPath = null;
                        }
                        else if (LogoFile != null && LogoFile.Length > 0)
                        {
                            existing.LogoPath = await SaveLogo(LogoFile, existing.LogoPath);
                        }

                        // Update all fields
                        existing.MonedaOficial = model.MonedaOficial;
                        existing.NombreEmpresa = model.NombreEmpresa;
                        existing.AliasEmpresa = model.AliasEmpresa;
                        existing.Telefono = model.Telefono;
                        existing.Telefono2 = model.Telefono2;
                        existing.Correo = model.Correo;
                        existing.Direccion = model.Direccion;
                        existing.RUC = model.RUC;
                        existing.SitioWeb = model.SitioWeb;
                        existing.NumItemsFactura = model.NumItemsFactura;
                        existing.AltoImpresion = model.AltoImpresion;
                        existing.MostrarLogoEnDocumentos = model.MostrarLogoEnDocumentos;
                        existing.ImpresoraPredeterminada = model.ImpresoraPredeterminada;
                        existing.TicketEncabezado = model.TicketEncabezado;
                        existing.TicketPie = model.TicketPie;
                        existing.TicketNotaAviso = model.TicketNotaAviso;
                        existing.TicketMostrarLogo = model.TicketMostrarLogo;
                        existing.TicketMostrarFecha = model.TicketMostrarFecha;
                        existing.TicketMostrarCajero = model.TicketMostrarCajero;
                        existing.TicketAncho = model.TicketAncho;
                        existing.ReporteEncabezado = model.ReporteEncabezado;
                        existing.ReportePie = model.ReportePie;
                        existing.ReporteMostrarLogo = model.ReporteMostrarLogo;
                        existing.DiasAlertaVencimiento = model.DiasAlertaVencimiento;
                        existing.MostrarAlertaStock = model.MostrarAlertaStock;
                        existing.ClaveAprobacionVencido = model.ClaveAprobacionVencido;
                        existing.StockMinimoCargar = model.StockMinimoCargar;
                        existing.NumVentanasSimultaneas = model.NumVentanasSimultaneas;
                        existing.FechaVigenciaDesde = model.FechaVigenciaDesde;
                        existing.FechaVigenciaHasta = model.FechaVigenciaHasta;
                        existing.UltimaActualizacion = DateTime.Now;

                        // Configuración del Catálogo Público
                        existing.CatalogoPublicoActivo = model.CatalogoPublicoActivo;
                        existing.CatalogoTitulo = model.CatalogoTitulo;
                        existing.CatalogoMostrarPrecios = model.CatalogoMostrarPrecios;
                        existing.CatalogoWhatsApp = model.CatalogoWhatsApp;
                        existing.PosLayout = model.PosLayout;
                        existing.IvaCodigoSri = model.IvaCodigoSri;
                        existing.IvaCodPorcentajeSri = model.IvaCodPorcentajeSri;
                        existing.IvaTarifa = model.IvaTarifa;

                        _context.ParametrosSistema.Update(existing);
                    }

                    await _context.SaveChangesAsync();

                    // Sync with central Admin DB Empresa if authenticated
                    var empresaIdClaim = User.FindFirst("EmpresaId")?.Value;
                    if (empresaIdClaim != null && int.TryParse(empresaIdClaim, out int empId))
                    {
                        var centralEmpresa = await _adminContext.Empresas.FindAsync(empId);
                        if (centralEmpresa != null)
                        {
                            var updatedLogo = (existing != null) ? existing.LogoPath : model.LogoPath;
                            centralEmpresa.Nombre = model.NombreEmpresa ?? centralEmpresa.Nombre;
                            if (!string.IsNullOrEmpty(updatedLogo))
                            {
                                centralEmpresa.Logo = updatedLogo;
                            }
                            else if (RemoveLogo)
                            {
                                centralEmpresa.Logo = null;
                            }
                            centralEmpresa.Telefono = model.Telefono ?? centralEmpresa.Telefono;
                            centralEmpresa.Email = model.Correo ?? centralEmpresa.Email;
                            centralEmpresa.Direccion = model.Direccion ?? centralEmpresa.Direccion;
                            _adminContext.Empresas.Update(centralEmpresa);
                            await _adminContext.SaveChangesAsync();
                        }
                    }

                    TempData["Success"] = "Parámetros del sistema guardados correctamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    string msg = ex.Message;
                    if (ex is UnauthorizedAccessException || msg.Contains("denied") || msg.Contains("acceso denegado") || msg.Contains("Access to the path"))
                    {
                        msg = "Error de permisos de escritura en el servidor. Por favor, asegúrese de otorgar permisos de ESCRITURA y MODIFICACIÓN a la carpeta 'wwwroot/uploads' y sus subcarpetas para el usuario de IIS (Application Pool Identity) en el panel de control de Plesk. Detalles del error original: " + ex.Message;
                    }
                    ModelState.AddModelError("", "Error al guardar los parámetros: " + msg);
                }
            }

            var empresaIdClaimFallback = User.FindFirst("EmpresaId")?.Value;
            if (empresaIdClaimFallback != null && int.TryParse(empresaIdClaimFallback, out int empIdFallback))
            {
                var centralEmpresa = await _adminContext.Empresas.FindAsync(empIdFallback);
                if (centralEmpresa != null)
                {
                    ViewBag.Subdominio = centralEmpresa.Subdominio;
                }
            }

            ViewBag.Monedas = await _context.Monedas.Where(m => m.Activo).OrderBy(m => m.Nombre).ToListAsync();
            return View(model);
        }

        // API: Get current params as JSON (used by reports, tickets, etc.)
        [HttpGet]
        public async Task<IActionResult> GetParams()
        {
            var config = await _context.ParametrosSistema.AsNoTracking().FirstOrDefaultAsync(p => p.Id == 1);
            if (config == null) return Json(new ParametrosSistema { Id = 1 });
            return Json(config);
        }

        private async Task<string?> SaveLogo(IFormFile logo, string? oldPath)
        {
            var ext = Path.GetExtension(logo.FileName).ToLowerInvariant();
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg" };
            if (!allowed.Contains(ext)) return oldPath;

            if (!string.IsNullOrEmpty(oldPath)) DeleteFile(oldPath);

            var dir = Path.Combine(_env.WebRootPath, "uploads", "empresa");
            Directory.CreateDirectory(dir);
            var fileName = $"logo_{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(dir, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await logo.CopyToAsync(stream);
            return $"/uploads/empresa/{fileName}?v={DateTime.Now.Ticks}";
        }

        private void DeleteFile(string relativePath)
        {
            try
            {
                // Strip query string before resolving path
                var cleanPath = relativePath.Split('?')[0];
                var fullPath = Path.Combine(_env.WebRootPath, cleanPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);
            }
            catch { /* silently ignore file delete errors */ }
        }
    }
}
