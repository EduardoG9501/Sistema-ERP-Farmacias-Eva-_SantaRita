using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaERP.Models;
using SistemaERP.Models.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SistemaERP.Areas.Admin.Models;

namespace SistemaERP.Controllers
{
    [Authorize(Roles = "ADMIN,ADMINISTRADOR")]
    public class UsuariosController : Controller
    {
        private readonly ScmContext _context;
        private readonly AdminContext _adminContext;
        private readonly IWebHostEnvironment _env;

        public UsuariosController(ScmContext context, AdminContext adminContext, IWebHostEnvironment env)
        {
            _context = context;
            _adminContext = adminContext;
            _env = env;
        }

        private string EncodePassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return "";
            byte[] bytes = System.Text.Encoding.Unicode.GetBytes(password.Trim());
            return Convert.ToBase64String(bytes);
        }

        private string DecodePassword(string encoded)
        {
            if (string.IsNullOrEmpty(encoded)) return "";
            try
            {
                byte[] bytes = Convert.FromBase64String(encoded.Trim());
                return System.Text.Encoding.Unicode.GetString(bytes).Trim();
            }
            catch
            {
                return encoded.Trim();
            }
        }

        private async Task<string?> SaveUserPhoto(IFormFile? photo, string? oldPhotoPath = null)
        {
            if (photo == null || photo.Length == 0) return null;

            // Delete old photo if exists
            if (!string.IsNullOrEmpty(oldPhotoPath))
            {
                var oldFullPath = Path.Combine(_env.WebRootPath, oldPhotoPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(oldFullPath))
                    System.IO.File.Delete(oldFullPath);
            }

            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "usuarios");
            Directory.CreateDirectory(uploadsDir);

            var ext = Path.GetExtension(photo.FileName).ToLowerInvariant();
            var allowedExts = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            if (!allowedExts.Contains(ext)) return null;

            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadsDir, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await photo.CopyToAsync(stream);

            return $"/uploads/usuarios/{fileName}";
        }

        public async Task<IActionResult> Index()
        {
            var users = await _context.Usuarios.OrderBy(u => u.Login).ToListAsync();

            // Auto-sync missing tenant users to the central database
            var empresaIdClaim = User.FindFirst("EmpresaId")?.Value;
            if (int.TryParse(empresaIdClaim, out int empresaId))
            {
                var centralUsers = await _adminContext.UsuariosEmpresa
                    .Where(u => u.EmpresaId == empresaId)
                    .Select(u => u.Login.Trim().ToLower())
                    .ToListAsync();

                bool changes = false;
                foreach (var user in users)
                {
                    if (!string.IsNullOrEmpty(user.Login) && !centralUsers.Contains(user.Login.Trim().ToLower()))
                    {
                        var newUe = new UsuarioEmpresa
                        {
                            Login = user.Login.Trim(),
                            EmpresaId = empresaId,
                            PasswordHash = user.Password,
                            Activo = user.Estado?.Trim().ToUpper() == "ACTIVO",
                            FechaCreacion = DateTime.Now
                        };
                        _adminContext.UsuariosEmpresa.Add(newUe);
                        changes = true;
                    }
                }
                if (changes)
                {
                    await _adminContext.SaveChangesAsync();
                }
            }

            return View(users);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Modulos = await _context.ModulosWeb.Where(m => m.Estado).OrderBy(m => m.DisplayName).ToListAsync();
            ViewBag.Privilegios = await _context.PrivilegiosWeb.Where(p => p.Estado).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Usuario usuario, int[]? selectedPrivilegios, int[]? heredarPrivilegios, IFormFile? FotoFile)
        {
            if (ModelState.IsValid)
            {
                if (!string.IsNullOrEmpty(usuario.Password))
                {
                    usuario.Password = EncodePassword(usuario.Password);
                }
                usuario.Estado = usuario.Estado ?? "ACTIVO";

                // Handle photo upload
                var fotoPath = await SaveUserPhoto(FotoFile);
                if (fotoPath != null) usuario.FotoPath = fotoPath;

                var strategy = _context.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync<IActionResult>(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        _context.Add(usuario);
                        await _context.SaveChangesAsync(); // Generates the user ID

                        if (selectedPrivilegios != null)
                        {
                            foreach (var privId in selectedPrivilegios)
                            {
                                bool hereda = heredarPrivilegios != null && heredarPrivilegios.Contains(privId);
                                _context.UsuarioPrivilegiosWeb.Add(new UsuarioPrivilegioWeb
                                {
                                    Id_Usuario = usuario.Id,
                                    Id_Privilegio = privId,
                                    Fecha_Sistema = DateTime.Now,
                                    Heredar_Permiso = hereda
                                });
                            }
                            await _context.SaveChangesAsync();
                        }

                        // Sync to AdminContext
                        var empresaIdClaim = User.FindFirst("EmpresaId")?.Value;
                        if (int.TryParse(empresaIdClaim, out int empresaId))
                        {
                            var exists = await _adminContext.UsuariosEmpresa
                                .AnyAsync(u => u.Login == usuario.Login.Trim() && u.EmpresaId == empresaId);
                            if (!exists)
                            {
                                var newUe = new UsuarioEmpresa
                                {
                                    Login = usuario.Login.Trim(),
                                    EmpresaId = empresaId,
                                    PasswordHash = usuario.Password,
                                    Activo = usuario.Estado?.Trim().ToUpper() == "ACTIVO",
                                    FechaCreacion = DateTime.Now
                                };
                                _adminContext.UsuariosEmpresa.Add(newUe);
                                await _adminContext.SaveChangesAsync();
                            }
                        }

                        await transaction.CommitAsync();
                        TempData["Success"] = "Usuario creado exitosamente con sus respectivos privilegios.";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        ModelState.AddModelError("", "Error al guardar el usuario y privilegios: " + ex.Message);
                        ViewBag.Modulos = await _context.ModulosWeb.Where(m => m.Estado).OrderBy(m => m.DisplayName).ToListAsync();
                        ViewBag.Privilegios = await _context.PrivilegiosWeb.Where(p => p.Estado).ToListAsync();
                        return View(usuario);
                    }
                });
            }

            ViewBag.Modulos = await _context.ModulosWeb.Where(m => m.Estado).OrderBy(m => m.DisplayName).ToListAsync();
            ViewBag.Privilegios = await _context.PrivilegiosWeb.Where(p => p.Estado).ToListAsync();
            return View(usuario);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();
            
            // Decrypt password to plaintext for standard masking & copying
            if (!string.IsNullOrEmpty(usuario.Password))
            {
                usuario.Password = DecodePassword(usuario.Password);
            }
            
            ViewBag.Modulos = await _context.ModulosWeb.Where(m => m.Estado).OrderBy(m => m.DisplayName).ToListAsync();
            ViewBag.Privilegios = await _context.PrivilegiosWeb.Where(p => p.Estado).ToListAsync();
            ViewBag.UserPrivilegios = await _context.UsuarioPrivilegiosWeb
                .Where(up => up.Id_Usuario == id)
                .ToDictionaryAsync(up => up.Id_Privilegio, up => up.Heredar_Permiso);

            return View(usuario);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Usuario usuario, string? NewPassword, int[]? selectedPrivilegios, int[]? heredarPrivilegios, IFormFile? FotoFile, bool RemoveFoto = false)
        {
            if (id != usuario.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var strategy = _context.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync<IActionResult>(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        var userDb = await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
                        if (userDb == null)
                        {
                            await transaction.RollbackAsync();
                            return NotFound();
                        }

                        if (!string.IsNullOrEmpty(usuario.Password))
                        {
                            usuario.Password = EncodePassword(usuario.Password);
                        }
                        else
                        {
                            usuario.Password = userDb.Password;
                        }

                        // Handle photo
                        if (RemoveFoto && !string.IsNullOrEmpty(userDb.FotoPath))
                        {
                            // Delete the existing photo
                            var oldFullPath = Path.Combine(_env.WebRootPath, userDb.FotoPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                            if (System.IO.File.Exists(oldFullPath))
                                System.IO.File.Delete(oldFullPath);
                            usuario.FotoPath = null;
                        }
                        else if (FotoFile != null && FotoFile.Length > 0)
                        {
                            var newPath = await SaveUserPhoto(FotoFile, userDb.FotoPath);
                            usuario.FotoPath = newPath ?? userDb.FotoPath;
                        }
                        else
                        {
                            usuario.FotoPath = userDb.FotoPath;
                        }

                        _context.Update(usuario);
                        await _context.SaveChangesAsync();

                        // Clear existing user privileges
                        var existingPerms = await _context.UsuarioPrivilegiosWeb.Where(up => up.Id_Usuario == id).ToListAsync();
                        _context.UsuarioPrivilegiosWeb.RemoveRange(existingPerms);
                        await _context.SaveChangesAsync();

                        // Insert newly selected privileges
                        if (selectedPrivilegios != null)
                        {
                            foreach (var privId in selectedPrivilegios)
                            {
                                bool hereda = heredarPrivilegios != null && heredarPrivilegios.Contains(privId);
                                _context.UsuarioPrivilegiosWeb.Add(new UsuarioPrivilegioWeb
                                {
                                    Id_Usuario = id,
                                    Id_Privilegio = privId,
                                    Fecha_Sistema = DateTime.Now,
                                    Heredar_Permiso = hereda
                                });
                            }
                            await _context.SaveChangesAsync();
                        }

                        // Sync to AdminContext
                        var empresaIdClaim = User.FindFirst("EmpresaId")?.Value;
                        if (int.TryParse(empresaIdClaim, out int empresaId))
                        {
                            var ue = await _adminContext.UsuariosEmpresa
                                .FirstOrDefaultAsync(u => u.Login == userDb.Login.Trim() && u.EmpresaId == empresaId);
                            if (ue != null)
                            {
                                ue.Login = usuario.Login.Trim();
                                ue.PasswordHash = usuario.Password;
                                ue.Activo = usuario.Estado?.Trim().ToUpper() == "ACTIVO";
                                _adminContext.UsuariosEmpresa.Update(ue);
                            }
                            else
                            {
                                var newUe = new UsuarioEmpresa
                                {
                                    Login = usuario.Login.Trim(),
                                    EmpresaId = empresaId,
                                    PasswordHash = usuario.Password,
                                    Activo = usuario.Estado?.Trim().ToUpper() == "ACTIVO",
                                    FechaCreacion = DateTime.Now
                                };
                                _adminContext.UsuariosEmpresa.Add(newUe);
                            }
                            await _adminContext.SaveChangesAsync();
                        }

                        await transaction.CommitAsync();
                        TempData["Success"] = "Usuario y privilegios actualizados correctamente.";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        ModelState.AddModelError("", "Error al actualizar el usuario y privilegios: " + ex.Message);
                        ViewBag.Modulos = await _context.ModulosWeb.Where(m => m.Estado).OrderBy(m => m.DisplayName).ToListAsync();
                        ViewBag.Privilegios = await _context.PrivilegiosWeb.Where(p => p.Estado).ToListAsync();
                        ViewBag.UserPrivilegios = selectedPrivilegios != null 
                            ? selectedPrivilegios.ToDictionary(privId => privId, privId => heredarPrivilegios != null && heredarPrivilegios.Contains(privId))
                            : new Dictionary<int, bool>();
                        return View(usuario);
                    }
                });
            }

            ViewBag.Modulos = await _context.ModulosWeb.Where(m => m.Estado).OrderBy(m => m.DisplayName).ToListAsync();
            ViewBag.Privilegios = await _context.PrivilegiosWeb.Where(p => p.Estado).ToListAsync();
            ViewBag.UserPrivilegios = selectedPrivilegios != null 
                ? selectedPrivilegios.ToDictionary(privId => privId, privId => heredarPrivilegios != null && heredarPrivilegios.Contains(privId))
                : new Dictionary<int, bool>();
            return View(usuario);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(m => m.Id == id);
            if (usuario == null) return NotFound();

            return View(usuario);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync<IActionResult>(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var usuario = await _context.Usuarios.FindAsync(id);
                    if (usuario != null)
                    {
                        // Delete photo file if exists
                        if (!string.IsNullOrEmpty(usuario.FotoPath))
                        {
                            var fullPath = Path.Combine(_env.WebRootPath, usuario.FotoPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                            if (System.IO.File.Exists(fullPath))
                                System.IO.File.Delete(fullPath);
                        }

                        // Clean up user privileges first
                        var existingPerms = await _context.UsuarioPrivilegiosWeb.Where(up => up.Id_Usuario == id).ToListAsync();
                        _context.UsuarioPrivilegiosWeb.RemoveRange(existingPerms);
                        _context.Usuarios.Remove(usuario);
                        await _context.SaveChangesAsync();

                        // Sync to AdminContext
                        var empresaIdClaim = User.FindFirst("EmpresaId")?.Value;
                        if (int.TryParse(empresaIdClaim, out int empresaId))
                        {
                            var ue = await _adminContext.UsuariosEmpresa
                                .FirstOrDefaultAsync(u => u.Login == usuario.Login.Trim() && u.EmpresaId == empresaId);
                            if (ue != null)
                            {
                                _adminContext.UsuariosEmpresa.Remove(ue);
                                await _adminContext.SaveChangesAsync();
                            }
                        }
                    }

                    await transaction.CommitAsync();
                    TempData["Success"] = "Usuario eliminado correctamente.";
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    TempData["Error"] = "Error al eliminar el usuario: " + ex.Message;
                }

                return RedirectToAction(nameof(Index));
            });
        }

        private bool UsuarioExists(int id)
        {
            return _context.Usuarios.Any(e => e.Id == id);
        }
    }
}
