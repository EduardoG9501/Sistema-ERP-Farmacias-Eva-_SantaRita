using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaERP.Areas.Admin.Models;
using SistemaERP.Areas.Admin.Services;
using System.Security.Claims;

namespace SistemaERP.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly AdminContext _adminContext;
        private readonly IEmpresaConnectionService _empresaConnService;

        public AccountController(AdminContext adminContext, IEmpresaConnectionService empresaConnService)
        {
            _adminContext = adminContext;
            _empresaConnService = empresaConnService;
        }

        [HttpGet]
        public async Task<IActionResult> Login()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            await SetEmpresaFromHost();
            ViewBag.Ruc = null;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string? ruc, string username, string password)
        {
            if (string.IsNullOrEmpty(ruc))
            {
                var host = HttpContext.Request.Host.Host;
                var parts = host?.Split('.');
                var subdomain = parts != null && parts.Length > 2 ? parts[0] : null;
                if (!string.IsNullOrEmpty(subdomain))
                {
                    var emp = await _adminContext.Empresas
                        .FirstOrDefaultAsync(e => e.Estado && e.Subdominio == subdomain);
                    if (emp != null) ruc = emp.RUC;
                }
            }

            if (string.IsNullOrEmpty(ruc) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Por favor ingrese RUC, usuario y contraseña.";
                await SetEmpresaFromHost();
                return View();
            }

            // 1. Buscar empresa activa por RUC
            var empresa = await _adminContext.Empresas
                .FirstOrDefaultAsync(e => e.RUC == ruc.Trim() && e.Estado);
            if (empresa == null)
            {
                ViewBag.Error = $"Empresa con RUC '{ruc}' no encontrada o inactiva.";
                ViewBag.Ruc = ruc;
                return View();
            }

            // 2. Buscar usuario en UsuarioEmpresa
            var ue = await _adminContext.UsuariosEmpresa
                .FirstOrDefaultAsync(u => u.Login == username.Trim() && u.EmpresaId == empresa.Id && u.Activo);

            // Si no existe en UsuarioEmpresa, intentar sincronizar desde la BD de la empresa
            if (ue == null)
            {
                try
                {
                    using var tempCtx = _empresaConnService.GetContextForEmpresa(empresa);
                    var tenantUser = await tempCtx.Usuarios
                        .FirstOrDefaultAsync(u => u.Login.Trim() == username.Trim());
                    if (tenantUser != null)
                    {
                        _adminContext.UsuariosEmpresa.Add(new UsuarioEmpresa
                        {
                            Login = tenantUser.Login.Trim(),
                            EmpresaId = empresa.Id,
                            PasswordHash = tenantUser.Password,
                            Activo = tenantUser.Estado?.Trim().ToUpper() == "ACTIVO",
                            FechaCreacion = DateTime.Now
                        });
                        await _adminContext.SaveChangesAsync();
                        ue = await _adminContext.UsuariosEmpresa
                            .FirstOrDefaultAsync(u => u.Login == username.Trim() && u.EmpresaId == empresa.Id && u.Activo);
                    }
                }
                catch { }
            }

            if (ue == null)
            {
                ViewBag.Error = $"Usuario '{username}' no registrado para la empresa '{empresa.Nombre}'.";
                ViewBag.CompanyName = empresa.Nombre;
                ViewBag.CompanyLogo = empresa.Logo;
                ViewBag.Ruc = empresa.RUC;
                return View();
            }

            // 3. Conectar a la BD de la empresa para validar credenciales
            using var clientCtx = _empresaConnService.GetContextForEmpresa(empresa);
            var user = await clientCtx.Usuarios
                .FirstOrDefaultAsync(u => u.Login.Trim() == username.Trim());

            if (user == null)
            {
                ViewBag.Error = $"Usuario '{username}' no encontrado en la base de datos.";
                ViewBag.CompanyName = empresa.Nombre;
                ViewBag.CompanyLogo = empresa.Logo;
                ViewBag.Ruc = empresa.RUC;
                return View();
            }

            // 4. Verificar contraseña (Base64 UTF-16LE, BCrypt, plano)
            bool isValid = false;
            try
            {
                var bytes = System.Text.Encoding.Unicode.GetBytes(password.Trim());
                isValid = user.Password?.Trim() == Convert.ToBase64String(bytes);
            }
            catch { }

            if (!isValid && user.Password?.StartsWith("$2") == true)
            {
                try { isValid = BCrypt.Net.BCrypt.Verify(password.Trim(), user.Password.Trim()); }
                catch { }
            }

            if (!isValid)
                isValid = user.Password?.Trim() == password.Trim();

            if (!isValid)
            {
                ViewBag.Error = "Contraseña incorrecta.";
                ViewBag.CompanyName = empresa.Nombre;
                ViewBag.CompanyLogo = empresa.Logo;
                ViewBag.Ruc = empresa.RUC;
                return View();
            }

            if (user.Estado?.Trim().ToUpper() != "ACTIVO")
            {
                ViewBag.Error = $"Usuario inactivo. Estado: {user.Estado}";
                ViewBag.CompanyName = empresa.Nombre;
                ViewBag.CompanyLogo = empresa.Logo;
                ViewBag.Ruc = empresa.RUC;
                return View();
            }

            // 5. Crear cookie con EmpresaId para que ScmContext resuelva la BD correcta
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Login ?? username),
                new Claim(ClaimTypes.Role, user.Tipo_Usuario ?? "USER"),
                new Claim("EmpresaId", empresa.Id.ToString())
            };

            var identity = new ClaimsIdentity(claims, "Cookies");
            await HttpContext.SignInAsync("Cookies", new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = true });

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Logout(bool expired = false)
        {
            await HttpContext.SignOutAsync("Cookies");
            if (expired)
            {
                return RedirectToAction("Login", new { sessionExpired = true });
            }
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Ping()
        {
            return Json(new { authenticated = User.Identity?.IsAuthenticated == true });
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        // ─── Ayudantes ───────────────────────────────────────────
        private async Task SetEmpresaFromHost()
        {
            var host = HttpContext.Request.Host.Host;
            var parts = host?.Split('.');
            var subdomain = parts != null && parts.Length > 2 ? parts[0] : null;

            Empresa? empresa = null;
            if (!string.IsNullOrEmpty(host))
            {
                empresa = await _adminContext.Empresas.FirstOrDefaultAsync(e =>
                    e.Estado && (e.Subdominio == host ||
                        (!string.IsNullOrEmpty(subdomain) && e.Subdominio == subdomain)));
            }

            ViewBag.CompanyName = empresa?.Nombre ?? "ERP SCM";
            ViewBag.CompanyLogo = empresa?.Logo;
            ViewBag.Ruc = empresa?.RUC;
        }
    }
}
