using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaERP.Areas.Admin.Models;

namespace SistemaERP.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AllowAnonymous]
    public class AdminAccountController : Controller
    {
        private readonly AdminContext _context;

        public AdminAccountController(AdminContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true && HttpContext.User.HasClaim(ClaimTypes.Role, "SuperAdmin"))
            {
                return RedirectToAction("Index", "Monitor", new { area = "Admin" });
            }

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string login, string password, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Usuario y contraseña son obligatorios.";
                ModelState.AddModelError("", ViewBag.Error);
                return View();
            }

            var adminUser = await _context.AdminUsuarios
                .FirstOrDefaultAsync(u => u.Login == login && u.Activo);

            if (adminUser == null)
            {
                ViewBag.Error = "Usuario no encontrado en AdminUsuarios.";
                ModelState.AddModelError("", ViewBag.Error);
                return View();
            }

            // Verificar contraseña con múltiples formatos
            bool passwordValida = false;
            var hashGuardado = (adminUser.PasswordHash ?? "").Trim();
            var passIngresada = (password ?? "").Trim();

            // Mostrar info de diagnóstico (quitar después)
            System.Diagnostics.Debug.WriteLine($"[AdminLogin] Usuario encontrado: Login='{adminUser.Login}', PasswordHash='{hashGuardado.Substring(0, Math.Min(20, hashGuardado.Length))}...', Activo={adminUser.Activo}");

            // 1. BCrypt (formato estándar)
            if (hashGuardado.StartsWith("$2"))
            {
                try
                {
                    passwordValida = BCrypt.Net.BCrypt.Verify(passIngresada, hashGuardado);
                    System.Diagnostics.Debug.WriteLine($"[AdminLogin] BCrypt result: {passwordValida}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AdminLogin] BCrypt exception: {ex.Message}");
                }
            }

            // 2. Texto plano (fallback)
            if (!passwordValida)
            {
                passwordValida = hashGuardado == passIngresada;
                System.Diagnostics.Debug.WriteLine($"[AdminLogin] Plain text result: {passwordValida}");
            }

            if (!passwordValida)
            {
                ViewBag.Error = "Contraseña incorrecta.";
                ModelState.AddModelError("", ViewBag.Error);
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, adminUser.Login),
                new Claim(ClaimTypes.GivenName, adminUser.Nombre),
                new Claim(ClaimTypes.Email, adminUser.Email),
                new Claim(ClaimTypes.Role, "SuperAdmin"),
                new Claim("AdminId", adminUser.Id.ToString())
            };

            var identity = new ClaimsIdentity(claims, "AdminCookies");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("AdminCookies", principal, new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(4)
            });

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Monitor", new { area = "Admin" });
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("AdminCookies");
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
