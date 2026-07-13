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
    public class CobrosController : Controller
    {
        private readonly ScmContext _context;

        public CobrosController(ScmContext context)
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
                ViewBag.ClientesList = await _context.Clientes.ToListAsync();
                ViewBag.BancosList = await _context.Bancos.ToListAsync();
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error al iniciar módulo: " + ex.Message;
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCuentas(string? cliente, string? factura, string? estado)
        {
            try
            {
                var query = _context.FacturasPorCobrar.AsQueryable();

                if (!string.IsNullOrWhiteSpace(factura))
                {
                    query = query.Where(c => c.Factura != null && c.Factura.Contains(factura));
                }

                if (!string.IsNullOrWhiteSpace(estado))
                {
                    query = query.Where(c => c.Estado == estado);
                }

                var rawData = await query.ToListAsync();

                // Cargar clientes en memoria para evitar errores de traducción de EF
                var clientesDict = await _context.Clientes.ToDictionaryAsync(c => c.Id, c => c.Nombres_Apellidos ?? "");

                var result = rawData.Select(c => new {
                    c.Id,
                    c.Num_Venta,
                    c.Factura,
                    c.Id_Cliente,
                    ClienteNombre = clientesDict.ContainsKey(c.Id_Cliente) ? clientesDict[c.Id_Cliente] : "Desconocido",
                    FechaEmision = c.Fecha_Emision.ToString("yyyy-MM-dd"),
                    c.DiasPago,
                    FechaVencimiento = c.Fecha_Vencimiento.ToString("yyyy-MM-dd"),
                    c.Monto_Fact,
                    c.Monto_TotalPagar,
                    c.Monto_Abonado,
                    c.Monto_Saldo,
                    c.Estado
                })
                .Where(c => string.IsNullOrWhiteSpace(cliente) || c.ClienteNombre.Contains(cliente, StringComparison.OrdinalIgnoreCase))
                .ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al cargar cuentas: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAbonos(long ventaId)
        {
            try
            {
                var abonos = await _context.FacturasPorCobrarAbonos
                    .Where(a => a.Id_Venta == ventaId)
                    .OrderByDescending(a => a.FechaHora_Reg)
                    .ToListAsync();

                var usuariosDict = await _context.Usuarios.ToDictionaryAsync(u => u.Id, u => u.Login ?? "");
                var bancosDict = await _context.Bancos.ToDictionaryAsync(b => b.Id, b => b.Nombre ?? "");

                var result = abonos.Select(a => new {
                    a.Id,
                    a.Fecha_Cobro,
                    a.Monto_Efectivo,
                    FechaHoraReg = a.FechaHora_Reg.ToString("yyyy-MM-dd HH:mm:ss"),
                    Usuario = usuariosDict.ContainsKey(a.Id_Usuario) ? usuariosDict[a.Id_Usuario] : "Admin",
                    TipoPagoDesc = GetTipoPagoDesc(a.Tipo_Pago),
                    a.Num_Doc,
                    Banco = a.Id_Banco.HasValue && bancosDict.ContainsKey(a.Id_Banco.Value) ? bancosDict[a.Id_Banco.Value] : "N/A",
                    a.Monto_Multipago
                }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al cargar abonos: " + ex.Message });
            }
        }

        private static string GetTipoPagoDesc(int tipo)
        {
            return tipo switch
            {
                1 => "Efectivo",
                2 => "Tarjeta de Crédito",
                3 => "Transferencia Bancaria",
                4 => "Depósito Bancario",
                _ => "Otro"
            };
        }

        [HttpPost]
        public async Task<IActionResult> RegistrarAbono([FromBody] AbonoRequest req)
        {
            if (req == null || req.Monto <= 0)
            {
                return Json(new { success = false, message = "Datos del abono inválidos." });
            }

            try
            {
                var strategy = _context.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        var cxc = await _context.FacturasPorCobrar.FirstOrDefaultAsync(c => c.Num_Venta == req.VentaId);
                        if (cxc == null)
                        {
                            await transaction.RollbackAsync();
                            return Json(new { success = false, message = "Cuenta por cobrar no encontrada." });
                        }

                        if (req.Monto > cxc.Monto_Saldo)
                        {
                            await transaction.RollbackAsync();
                            return Json(new { success = false, message = $"El monto a abonar (${req.Monto:N2}) supera el saldo pendiente (${cxc.Monto_Saldo:N2})." });
                        }

                        int idUser = await GetCurrentUserId();
                        if (idUser == 0) idUser = 1; // Fallback to Admin if session is empty

                        var abono = new FacturasPorCobrarAbono
                        {
                            Id_Usuario = idUser,
                            Id_Venta = req.VentaId,
                            Fecha_Cobro = DateTime.Today,
                            Monto_Efectivo = req.Monto,
                            FechaHora_Reg = DateTime.Now,
                            Tipo_Pago = req.TipoPago,
                            Num_Doc = req.Referencia,
                            Id_Banco = req.IdBanco,
                            Monto_Multipago = req.Monto,
                            Imagen_Doc = "",
                            Adquirente = req.Adquirente ?? "",
                            Interes_MultiPago = 0
                        };

                        _context.FacturasPorCobrarAbonos.Add(abono);

                        // Actualizar saldos en FacturasPorCobrar
                        cxc.Monto_Abonado += req.Monto;
                        cxc.Monto_Saldo -= req.Monto;

                        if (cxc.Monto_Saldo <= 0.005m)
                        {
                            cxc.Monto_Saldo = 0;
                            cxc.Estado = "PAGADA";
                        }

                        _context.FacturasPorCobrar.Update(cxc);
                        await _context.SaveChangesAsync();

                        await transaction.CommitAsync();

                        return Json(new { success = true, message = "Abono registrado con éxito." });
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        return Json(new { success = false, message = "Error interno: " + ex.Message });
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error transaccional: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetVentaDetalle(long ventaId)
        {
            try
            {
                var detalles = await _context.VentasDetalle
                    .Where(d => d.Id_Venta == ventaId)
                    .ToListAsync();

                // Cruzar en memoria los códigos de barra reales
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

                // Obtener número de documento/factura real
                var cxc = await _context.FacturasPorCobrar.FirstOrDefaultAsync(c => c.Num_Venta == ventaId);
                var venta = await _context.Ventas.FirstOrDefaultAsync(v => v.Id == ventaId);
                string docNum = cxc?.Factura ?? (venta?.NumDoc ?? $"TICKET-{ventaId}");

                var result = detalles.Select(d => {
                    string barcode = d.Id_Producto;
                    if (productosDict.ContainsKey(d.Id_Producto)) {
                        barcode = productosDict[d.Id_Producto];
                    }

                    return new {
                        codigoBarra = barcode,
                        producto = d.Producto ?? "",
                        presentacion = d.Presentacion ?? "N/A",
                        cantidad = d.Cantidad ?? 0,
                        bonif = 0, // default bonification is 0
                        pvp = d.PVP ?? 0,
                        subtotal = d.Subtotal ?? 0,
                        iva = d.IVA ?? 0,
                        descuento = d.Descuento ?? 0,
                        total = d.Total_Pagar ?? 0,
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

        public class AbonoRequest
        {
            public long VentaId { get; set; }
            public decimal Monto { get; set; }
            public int TipoPago { get; set; }
            public string? Referencia { get; set; }
            public int? IdBanco { get; set; }
            public string? Adquirente { get; set; }
        }
    }
}
