using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SistemaERP.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(AuthenticationSchemes = "AdminCookies", Roles = "SuperAdmin")]
    public abstract class BaseAdminController : Controller
    {
        // Métodos auxiliares comunes para la administración maestra del ERP
        protected int CurrentAdminId
        {
            get
            {
                var claim = User.FindFirst("AdminId");
                return claim != null && int.TryParse(claim.Value, out int id) ? id : 0;
            }
        }
    }
}
