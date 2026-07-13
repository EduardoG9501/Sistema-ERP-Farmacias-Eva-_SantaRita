using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaERP.Models;
using SistemaERP.Areas.Admin.Models;

namespace SistemaERP.Controllers
{
    [AllowAnonymous]
    public class AdminCheckController : Controller
    {
        private readonly ScmContext _context;
        private readonly AdminContext _adminContext;

        public AdminCheckController(ScmContext context, AdminContext adminContext)
        {
            _context = context;
            _adminContext = adminContext;
        }

        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var users = await _context.Usuarios.Take(10).ToListAsync();
                return Json(new { LegacyUsers = users });
            }
            catch (Exception ex)
            {
                return Json(new { Error = ex.Message });
            }
        }

        public async Task<IActionResult> ResetAdminPassword()
        {
            try
            {
                var adminUser = await _adminContext.AdminUsuarios
                    .FirstOrDefaultAsync(u => u.Login == "superadmin");

                if (adminUser == null)
                {
                    return Json(new { success = false, message = "Usuario superadmin no encontrado." });
                }

                adminUser.PasswordHash = "Admin@2026"; // Texto plano
                await _adminContext.SaveChangesAsync();

                return Json(new { success = true, message = "Contraseña de superadmin restablecida a 'Admin@2026' con éxito." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }
    }
}
