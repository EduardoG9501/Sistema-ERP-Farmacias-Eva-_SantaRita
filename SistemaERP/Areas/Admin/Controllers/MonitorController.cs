using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaERP.Areas.Admin.Models;
using SistemaERP.Areas.Admin.Services;

namespace SistemaERP.Areas.Admin.Controllers
{
    public class MonitorController : BaseAdminController
    {
        private readonly AdminContext _context;
        private readonly IEmpresaConnectionService _empresaConnService;

        public MonitorController(AdminContext context, IEmpresaConnectionService empresaConnService)
        {
            _context = context;
            _empresaConnService = empresaConnService;
        }

        public async Task<IActionResult> Index()
        {
            // 1. Estadísticas Generales
            var totalEmpresas = await _context.Empresas.CountAsync();
            var activas = await _context.Empresas.CountAsync(e => e.Estado);
            var inactivas = totalEmpresas - activas;

            var totalBackups = await _context.Backups.CountAsync();
            var backupsExitosos = await _context.Backups.CountAsync(b => b.Estado == "Exitoso");

            // 2. Alertas: Empresas por vencer (próximos 30 días)
            var limiteFecha = DateTime.Now.AddDays(30);
            var porVencer = await _context.Empresas
                .Where(e => e.Estado && e.FechaVencimiento.HasValue && e.FechaVencimiento.Value <= limiteFecha)
                .OrderBy(e => e.FechaVencimiento)
                .ToListAsync();

            // 3. Distribución por Rubros (para gráficos)
            var rubrosData = await _context.Empresas
                .Include(e => e.Rubro)
                .GroupBy(e => e.Rubro!.Nombre)
                .Select(g => new { Rubro = g.Key, Cantidad = g.Count() })
                .ToDictionaryAsync(x => x.Rubro, x => x.Cantidad);

            ViewBag.TotalEmpresas = totalEmpresas;
            ViewBag.Activas = activas;
            ViewBag.Inactivas = inactivas;
            ViewBag.TotalBackups = totalBackups;
            ViewBag.BackupsExitosos = backupsExitosos;
            ViewBag.PorVencer = porVencer;
            ViewBag.RubrosData = rubrosData;

            // 4. Ping rápido a las bases de datos de clientes activos
            var empresasActivas = await _context.Empresas.Where(e => e.Estado).Take(5).ToListAsync();
            var pings = new Dictionary<string, bool>();

            foreach (var emp in empresasActivas)
            {
                bool connOk = false;
                try
                {
                    connOk = _empresaConnService.TestConnection(_empresaConnService.GetContextForEmpresa(emp).Database.GetDbConnection().ConnectionString);
                }
                catch
                {
                    connOk = false;
                }
                pings[emp.Nombre] = connOk;
            }

            ViewBag.DatabasePings = pings;

            // 5. Historial reciente de auditoría admin
            var auditorias = await _context.AdminAuditLogs
                .Include(a => a.AdminUsuario)
                .Include(a => a.Empresa)
                .OrderByDescending(a => a.FechaHora)
                .Take(5)
                .ToListAsync();

            return View(auditorias);
        }
    }
}
