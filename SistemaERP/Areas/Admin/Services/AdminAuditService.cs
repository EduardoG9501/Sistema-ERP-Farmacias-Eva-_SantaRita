using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SistemaERP.Areas.Admin.Models;

namespace SistemaERP.Areas.Admin.Services
{
    public interface IAdminAuditService
    {
        Task LogActionAsync(int adminUsuarioId, int? empresaId, string accion, string detalle);
    }

    public class AdminAuditService : IAdminAuditService
    {
        private readonly AdminContext _adminContext;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AdminAuditService(AdminContext adminContext, IHttpContextAccessor httpContextAccessor)
        {
            _adminContext = adminContext;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogActionAsync(int adminUsuarioId, int? empresaId, string accion, string detalle)
        {
            try
            {
                var ip = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Local/Internal";
                
                var auditLog = new AdminAuditLog
                {
                    AdminUsuarioId = adminUsuarioId,
                    EmpresaId = empresaId,
                    Accion = accion,
                    Detalle = detalle,
                    FechaHora = DateTime.Now,
                    IP = ip
                };

                _adminContext.AdminAuditLogs.Add(auditLog);
                await _adminContext.SaveChangesAsync();
            }
            catch
            {
                // Un fallo al escribir en la tabla de auditorías no debería tumbar la operación principal.
            }
        }
    }
}
