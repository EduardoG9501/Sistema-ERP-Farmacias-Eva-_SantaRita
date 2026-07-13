using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaERP.Models;
using SistemaERP.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaERP.Controllers
{
    public class CuentasPorPagarController : Controller
    {
        private readonly ScmContext _context;

        public CuentasPorPagarController(ScmContext context)
        {
            _context = context;
        }

        private async Task<int> GetCurrentUserId()
        {
            var username = User.Identity?.Name;
            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Login.Trim() == (username ?? "").Trim());
            return user?.Id ?? 0;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                ViewBag.ProveedoresList = await _context.Proveedores.ToListAsync();
                ViewBag.BancosList = await _context.Bancos.ToListAsync();

                var parametros = await _context.ParametrosSistema.AsNoTracking().FirstOrDefaultAsync();
                string monedaCodigo = parametros?.MonedaOficial;
                string monedaSimbolo = "$";
                if (!string.IsNullOrEmpty(monedaCodigo))
                {
                    var moneda = await _context.Monedas.AsNoTracking().FirstOrDefaultAsync(m => m.Codigo == monedaCodigo);
                    monedaSimbolo = moneda?.Simbolo ?? "$";
                }
                ViewBag.MonedaSimbolo = monedaSimbolo;

                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error al iniciar módulo de Cuentas por Pagar: " + ex.Message;
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCuentas(string? proveedor, string? factura, string? estado, string? fechaDesde, string? fechaHasta)
        {
            try
            {
                var query = _context.CuentasPorPagar.AsQueryable();

                if (!string.IsNullOrWhiteSpace(factura))
                {
                    query = query.Where(c => c.Factura != null && c.Factura.Contains(factura));
                }

                if (!string.IsNullOrWhiteSpace(estado))
                {
                    query = query.Where(c => c.Estado == estado);
                }

                if (!string.IsNullOrWhiteSpace(fechaDesde))
                {
                    if (DateTime.TryParse(fechaDesde, out var desde))
                        query = query.Where(c => c.Fecha_Emision >= desde);
                }

                if (!string.IsNullOrWhiteSpace(fechaHasta))
                {
                    if (DateTime.TryParse(fechaHasta, out var hasta))
                    {
                        var hastaFinDia = hasta.Date.AddDays(1).AddTicks(-1);
                        query = query.Where(c => c.Fecha_Emision <= hastaFinDia);
                    }
                }

                var rawData = await query.ToListAsync();

                var proveedoresDict = await _context.Proveedores.ToDictionaryAsync(p => p.Id, p => p.Nombre ?? "");

                var result = rawData.Select(c => new {
                    c.Id,
                    c.Id_Compra,
                    c.Factura,
                    c.Id_Proveedor,
                    ProveedorNombre = proveedoresDict.ContainsKey(c.Id_Proveedor) ? proveedoresDict[c.Id_Proveedor] : "Desconocido",
                    FechaEmision = c.Fecha_Emision.ToString("yyyy-MM-dd"),
                    c.DiasPago,
                    FechaVencimiento = c.Fecha_Vencimiento.ToString("yyyy-MM-dd"),
                    c.Monto_Fact,
                    c.Monto_TotalPagar,
                    c.Monto_Abonado,
                    c.Monto_Saldo,
                    c.Estado
                })
                .Where(c => string.IsNullOrWhiteSpace(proveedor) || c.ProveedorNombre.Contains(proveedor, StringComparison.OrdinalIgnoreCase))
                .ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al cargar cuentas por pagar: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAbonos(long compraId)
        {
            try
            {
                var abonos = await _context.CuentasPorPagarAbonos
                    .Where(a => a.Id_Compra == compraId)
                    .OrderByDescending(a => a.FechaHora_Reg)
                    .ToListAsync();

                var bancosDict = await _context.Bancos.ToDictionaryAsync(b => b.Id, b => b.Nombre ?? "");

                var result = abonos.Select(a => new {
                    a.Id,
                    a.Id_Usuario,
                    a.Id_Compra,
                    FechaPago = a.Fecha_Pago.ToString("yyyy-MM-dd HH:mm"),
                    a.Monto_Efectivo,
                    a.Tipo_Pago,
                    Metodo = a.Tipo_Pago switch {
                        1 => "Efectivo",
                        2 => "Depósito Bancario",
                        3 => "Transferencia Bancaria",
                        _ => "Otro"
                    },
                    a.Num_Doc,
                    BancoNombre = (a.Id_Banco.HasValue && bancosDict.ContainsKey(a.Id_Banco.Value)) ? bancosDict[a.Id_Banco.Value] : "N/A"
                }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al obtener abonos: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCompraDetalle(long compraId)
        {
            try
            {
                var detalles = await _context.ComprasDetalle
                    .Where(d => d.Id_Factura == compraId.ToString())
                    .ToListAsync();

                var productIds = detalles.Select(d => d.Id_Producto).Distinct().ToList();
                var dbProducts = await _context.Productos
                    .Where(p => productIds.Contains(p.Id.ToString()) || (p.CodigoBarra != null && productIds.Contains(p.CodigoBarra)))
                    .ToListAsync();

                var productosDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var p in dbProducts)
                {
                    string idStr = p.Id.ToString();
                    if (!productosDict.ContainsKey(idStr))
                    {
                        productosDict[idStr] = p.CodigoBarra ?? idStr;
                    }
                    if (p.CodigoBarra != null && !productosDict.ContainsKey(p.CodigoBarra))
                    {
                        productosDict[p.CodigoBarra] = p.CodigoBarra;
                    }
                }

                var cxp = await _context.CuentasPorPagar.FirstOrDefaultAsync(c => c.Id_Compra == compraId);
                var compra = await _context.Compras.FirstOrDefaultAsync(c => c.Id == (int)compraId);
                string docNum = cxp?.Factura ?? (compra?.Factura ?? $"COMPRA-{compraId}");

                var result = detalles.Select(d => {
                    string barcode = d.Id_Producto ?? "";
                    if (d.Id_Producto != null && productosDict.ContainsKey(d.Id_Producto)) {
                        barcode = productosDict[d.Id_Producto];
                    }

                    return new {
                        codigoBarra = barcode,
                        producto = d.Producto ?? "",
                        presentacion = d.EsCaja == true ? "CAJA" : "UNIDAD",
                        cantidad = d.Cantidad ?? 0,
                        bonif = d.Bonificacion ?? 0,
                        pvp = d.PVF ?? 0,
                        subtotal = d.Subtotal ?? 0,
                        iva = d.IVA ?? 0,
                        descuento = d.Descuento ?? 0,
                        total = d.TotalPagar ?? 0,
                        lote = d.Lote ?? "N/A",
                        factura = docNum
                    };
                }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al obtener detalles: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RegistrarAbono([FromBody] RegistrarAbonoRequest req)
        {
            if (req == null || req.Monto <= 0)
                return Json(new { success = false, message = "Monto del abono no es válido." });

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var cuenta = await _context.CuentasPorPagar.FirstOrDefaultAsync(c => c.Id_Compra == req.IdCompra);
                if (cuenta == null)
                    return Json(new { success = false, message = "Registro de deuda no encontrado." });

                if (req.Monto > cuenta.Monto_Saldo)
                    return Json(new { success = false, message = $"El monto a abonar ({req.Monto:N2}) supera el saldo pendiente ({cuenta.Monto_Saldo:N2})." });

                int userId = await GetCurrentUserId();

                var abono = new CuentasPorPagarAbono
                {
                    Id_Usuario = userId > 0 ? userId : 1,
                    Id_Compra = req.IdCompra,
                    Fecha_Pago = DateTime.Now,
                    Monto_Efectivo = req.Monto,
                    FechaHora_Reg = DateTime.Now,
                    Tipo_Pago = req.TipoPago,
                    Num_Doc = req.Referencia,
                    Id_Banco = req.IdBanco > 0 ? req.IdBanco : (int?)null,
                    Adquirente = req.Adquirente
                };

                _context.CuentasPorPagarAbonos.Add(abono);

                cuenta.Monto_Abonado += req.Monto;
                cuenta.Monto_Saldo = cuenta.Monto_TotalPagar - cuenta.Monto_Abonado;

                if (cuenta.Monto_Saldo <= 0)
                {
                    cuenta.Monto_Saldo = 0;
                    cuenta.Estado = "PAGADA";
                    
                    var compra = await _context.Compras.FindAsync((int)cuenta.Id_Compra);
                    if (compra != null)
                    {
                        compra.Estado = "Pagado";
                        _context.Compras.Update(compra);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, message = "Abono registrado correctamente." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
            });
        }

        public class RegistrarAbonoRequest
        {
            public long IdCompra { get; set; }
            public decimal Monto { get; set; }
            public int TipoPago { get; set; }
            public string? Referencia { get; set; }
            public int? IdBanco { get; set; }
            public string? Adquirente { get; set; }
        }
    }
}
