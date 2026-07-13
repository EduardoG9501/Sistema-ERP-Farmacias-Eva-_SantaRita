using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SistemaERP.Models;
using SistemaERP.Models.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaERP.Filters
{
    public class DynamicAuthorizationFilter : IAsyncActionFilter
    {
        private readonly ScmContext _context;

        public DynamicAuthorizationFilter(ScmContext context)
        {
            _context = context;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // 0. Skip if request is for the Admin Area
            var areaName = context.RouteData.Values["area"]?.ToString();
            if (string.Equals(areaName, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                await next();
                return;
            }

            // 1. Skip if Action or Controller has [AllowAnonymous]
            var hasAllowAnonymous = context.ActionDescriptor.EndpointMetadata
                .Any(em => em.GetType() == typeof(AllowAnonymousAttribute));
            if (hasAllowAnonymous)
            {
                await next();
                return;
            }

            var controllerName = context.RouteData.Values["controller"]?.ToString();
            var actionName = context.RouteData.Values["action"]?.ToString();

            // 2. Ignore public/basic system controllers
            if (string.Equals(controllerName, "Account", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(controllerName, "Home", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(controllerName, "AdminCheck", StringComparison.OrdinalIgnoreCase))
            {
                await next();
                return;
            }

            if (!string.IsNullOrEmpty(controllerName) && !string.IsNullOrEmpty(actionName))
            {
                // Ensure this controller is registered dynamically in ModulosWeb
                var modulo = await _context.ModulosWeb.FirstOrDefaultAsync(m => m.ControllerName == controllerName);
                if (modulo == null)
                {
                    // Humanize controller names dynamically
                    string displayName = controllerName;
                    if (string.Equals(controllerName, "CatGastos", StringComparison.OrdinalIgnoreCase)) displayName = "Catálogos Gastos";
                    else if (string.Equals(controllerName, "AjustesSRI", StringComparison.OrdinalIgnoreCase)) displayName = "Parámetros SRI";
                    else if (string.Equals(controllerName, "Facturacion", StringComparison.OrdinalIgnoreCase)) displayName = "Facturación / POS";
                    else if (string.Equals(controllerName, "Ajustes", StringComparison.OrdinalIgnoreCase)) displayName = "Ajustes Inventario";
                    else if (string.Equals(controllerName, "Reportes", StringComparison.OrdinalIgnoreCase)) displayName = "Centro de Reportes";
                    else if (string.Equals(controllerName, "Traslados", StringComparison.OrdinalIgnoreCase)) displayName = "Traslados Inventario";
                    else if (string.Equals(controllerName, "Bodegas", StringComparison.OrdinalIgnoreCase)) displayName = "Bodegas / Almacén";
                    else if (string.Equals(controllerName, "Cobros", StringComparison.OrdinalIgnoreCase)) displayName = "Cuentas por Cobrar";
                    else if (string.Equals(controllerName, "Descuentos", StringComparison.OrdinalIgnoreCase)) displayName = "Módulo Descuentos";
                    else if (string.Equals(controllerName, "ParametrosSistema", StringComparison.OrdinalIgnoreCase)) displayName = "Parámetros del Sistema";
                    
                    modulo = new ModuloWeb
                    {
                        ControllerName = controllerName,
                        DisplayName = displayName,
                        Estado = true
                    };
                    _context.ModulosWeb.Add(modulo);
                    await _context.SaveChangesAsync();
                }

                // Ensure this action is registered dynamically in PrivilegiosWeb
                var privilegio = await _context.PrivilegiosWeb
                    .FirstOrDefaultAsync(p => p.Id_Modulo == modulo.Id && p.ActionName == actionName);
                
                if (privilegio == null)
                {
                    // Humanize action names dynamically
                    string actionDisplayName = actionName;
                    if (string.Equals(actionName, "Index", StringComparison.OrdinalIgnoreCase)) actionDisplayName = "Ver";
                    else if (string.Equals(actionName, "Create", StringComparison.OrdinalIgnoreCase)) actionDisplayName = "Crear";
                    else if (string.Equals(actionName, "Edit", StringComparison.OrdinalIgnoreCase)) actionDisplayName = "Editar";
                    else if (string.Equals(actionName, "Delete", StringComparison.OrdinalIgnoreCase)) actionDisplayName = "Eliminar";
                    else if (string.Equals(actionName, "POS", StringComparison.OrdinalIgnoreCase)) actionDisplayName = "Punto de Venta (POS)";
                    else if (string.Equals(actionName, "Kardex", StringComparison.OrdinalIgnoreCase)) actionDisplayName = "Kardex Inventario";
                    else if (string.Equals(actionName, "StockReport", StringComparison.OrdinalIgnoreCase)) actionDisplayName = "Reporte de Stock";
                    else if (string.Equals(actionName, "KardexExportExcel", StringComparison.OrdinalIgnoreCase)) actionDisplayName = "Exportar Kardex a Excel";
                    else if (string.Equals(actionName, "KardexPrint", StringComparison.OrdinalIgnoreCase)) actionDisplayName = "Imprimir/Guardar PDF Kardex";
                    else if (string.Equals(actionName, "DescargarXml", StringComparison.OrdinalIgnoreCase)) actionDisplayName = "Descargar XML Factura SRI";
                    else if (string.Equals(actionName, "Ticket", StringComparison.OrdinalIgnoreCase)) actionDisplayName = "Imprimir Ticket de Venta";
                    else if (string.Equals(actionName, "TransmitirSRI", StringComparison.OrdinalIgnoreCase)) actionDisplayName = "Transmitir Factura al SRI";
                    else if (string.Equals(actionName, "StockReportExportExcel", StringComparison.OrdinalIgnoreCase)) actionDisplayName = "Exportar Stock a Excel";

                    privilegio = new PrivilegioWeb
                    {
                        Id_Modulo = modulo.Id,
                        ActionName = actionName,
                        DisplayName = actionDisplayName,
                        Estado = true
                    };
                    _context.PrivilegiosWeb.Add(privilegio);
                    await _context.SaveChangesAsync();
                }

                // 3. Check authorization if user is authenticated
                var user = context.HttpContext.User;
                if (user.Identity?.IsAuthenticated == true)
                {
                    var username = user.Identity.Name;
                    var dbUser = await _context.Usuarios.FirstOrDefaultAsync(u => u.Login.Trim() == (username ?? "").Trim());
                    if (dbUser != null)
                    {
                        // Administrators have total access bypassing restriction
                        if (string.Equals(dbUser.Tipo_Usuario?.Trim(), "ADMINISTRADOR", StringComparison.OrdinalIgnoreCase))
                        {
                            await next();
                            return;
                        }

                        // For CIERTO ACCESO, check active mapping in Usuario_PrivilegiosWeb for this specific privilege action
                        var hasAccess = await _context.UsuarioPrivilegiosWeb
                            .AnyAsync(up => up.Id_Usuario == dbUser.Id && up.Id_Privilegio == privilegio.Id);

                        if (!hasAccess)
                        {
                            // Redirect to AccessDenied page
                            context.Result = new RedirectToActionResult("AccessDenied", "Account", new { ReturnUrl = context.HttpContext.Request.Path });
                            return;
                        }
                    }
                }
            }

            await next();
        }
    }
}
