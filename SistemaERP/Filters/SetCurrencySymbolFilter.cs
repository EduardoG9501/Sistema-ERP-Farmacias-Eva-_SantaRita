using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using SistemaERP.Models;
using SistemaERP.Models.Entities;

namespace SistemaERP.Filters
{
    public class SetCurrencySymbolFilter : IAsyncActionFilter
    {
        private readonly ScmContext _context;

        public SetCurrencySymbolFilter(ScmContext context)
        {
            _context = context;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Skip if request is for the Admin Area
            var areaName = context.RouteData.Values["area"]?.ToString();
            if (string.Equals(areaName, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                await next();
                return;
            }

            try
            {
                var monedaCodigo = await _context.ParametrosSistema
                    .Select(p => p.MonedaOficial)
                    .FirstOrDefaultAsync();

                string simbolo = "$";
                if (!string.IsNullOrEmpty(monedaCodigo))
                {
                    var moneda = await _context.Monedas.FindAsync(monedaCodigo);
                    if (moneda != null && !string.IsNullOrEmpty(moneda.Simbolo))
                        simbolo = moneda.Simbolo;
                }

                if (context.Controller is Controller controller)
                    controller.ViewBag.MonedaSimbolo = simbolo;
            }
            catch
            {
                if (context.Controller is Controller controller)
                    controller.ViewBag.MonedaSimbolo = "$";
            }

            await next();
        }
    }
}
