using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaERP.Models;
using SistemaERP.Models.Entities;

namespace SistemaERP.Controllers
{
    public class AjustesController : Controller
    {
        private readonly ScmContext _context;

        public AjustesController(ScmContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Combinar ingresos y egresos para un historial unificado o separado
            var ingresos = await _context.AjustesIngreso.OrderByDescending(a => a.FechaHora_Registro).Take(50).ToListAsync();
            var egresos = await _context.AjustesEgreso.OrderByDescending(a => a.FechaHora_Registro).Take(50).ToListAsync();
            
            ViewBag.Ingresos = ingresos;
            ViewBag.Egresos = egresos;
            
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ExportarIngresosExcel()
        {
            var data = await _context.AjustesIngreso.OrderByDescending(a => a.FechaHora_Registro).Take(1000).ToListAsync();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Fecha;Ajuste Ingreso #;Observacion/Motivo;Estado");
            foreach (var item in data)
            {
                sb.AppendLine($"{item.FechaHora_Registro?.ToString("dd/MM/yyyy HH:mm")};#{item.Id};{item.Observacion};{item.Estado}");
            }
            var fileBytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(fileBytes, "text/csv; charset=utf-8", $"AjustesIngresos_{DateTime.Now:yyyyMMddHHmmss}.csv");
        }

        [HttpGet]
        public async Task<IActionResult> ExportarEgresosExcel()
        {
            var data = await _context.AjustesEgreso.OrderByDescending(a => a.FechaHora_Registro).Take(1000).ToListAsync();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Fecha;Ajuste Egreso #;Observacion/Motivo;Estado");
            foreach (var item in data)
            {
                sb.AppendLine($"{item.FechaHora_Registro?.ToString("dd/MM/yyyy HH:mm")};#{item.Id};{item.Observacion};{item.Estado}");
            }
            var fileBytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(fileBytes, "text/csv; charset=utf-8", $"AjustesEgresos_{DateTime.Now:yyyyMMddHHmmss}.csv");
        }

        [HttpGet]
        public async Task<IActionResult> IngresosPrint()
        {
            var data = await _context.AjustesIngreso.OrderByDescending(a => a.FechaHora_Registro).Take(1000).ToListAsync();
            ViewBag.Parametros = await _context.ParametrosSistema.AsNoTracking().FirstOrDefaultAsync();
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> EgresosPrint()
        {
            var data = await _context.AjustesEgreso.OrderByDescending(a => a.FechaHora_Registro).Take(1000).ToListAsync();
            ViewBag.Parametros = await _context.ParametrosSistema.AsNoTracking().FirstOrDefaultAsync();
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> TicketIngreso(long? id)
        {
            if (id == null) return NotFound();
            var ajuste = await _context.AjustesIngreso.FindAsync(id);
            if (ajuste == null) return NotFound();

            var detalles = await _context.AjustesIngresoDetalle.Where(d => d.Id_Factura == id.Value).ToListAsync();
            ViewBag.Detalles = detalles;

            var parametros = await _context.ParametrosSistema.AsNoTracking().FirstOrDefaultAsync();
            ViewBag.Parametros = parametros;

            return View(ajuste);
        }

        [HttpGet]
        public async Task<IActionResult> TicketEgreso(long? id)
        {
            if (id == null) return NotFound();
            var ajuste = await _context.AjustesEgreso.FindAsync(id);
            if (ajuste == null) return NotFound();

            var detalles = await _context.AjustesEgresoDetalle.Where(d => d.Id_Factura == id.Value).ToListAsync();
            ViewBag.Detalles = detalles;

            var parametros = await _context.ParametrosSistema.AsNoTracking().FirstOrDefaultAsync();
            ViewBag.Parametros = parametros;

            return View(ajuste);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Productos = await _context.Productos.AsNoTracking().Where(p => p.Estado == "Activo").ToListAsync();
            ViewBag.Bodegas = await _context.Bodegas.AsNoTracking().Where(b => b.Estado).ToListAsync();
            ViewBag.Presentaciones = await _context.Presentaciones.AsNoTracking().ToListAsync();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetDetalleIngreso(long id)
        {
            var detalles = await _context.AjustesIngresoDetalle
                .Where(d => d.Id_Factura == id)
                .ToListAsync();
            return Json(detalles);
        }

        [HttpGet]
        public async Task<IActionResult> GetDetalleEgreso(long id)
        {
            var detalles = await _context.AjustesEgresoDetalle
                .Where(d => d.Id_Factura == id)
                .ToListAsync();
            return Json(detalles);
        }

        [HttpPost]
        public async Task<IActionResult> GuardarAjuste([FromBody] AjusteRequest request)
        {
            if (request == null || !request.Detalles.Any())
                return Json(new { success = false, message = "El ajuste no tiene productos." });

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    if (request.Tipo == "INGRESO")
                    {
                        var ajuste = new CargoAjuste {
                            Movimiento = "AJUSTE MANUAL INGRESO",
                            Observacion = request.Observacion,
                            FechaHora_Registro = DateTime.Now,
                            Id_Resp = 1,
                            Estado = "Procesado"
                        };
                        _context.AjustesIngreso.Add(ajuste);
                        await _context.SaveChangesAsync();

                        foreach (var item in request.Detalles) {
                            var prod = await _context.Productos.FindAsync((long)item.IdProducto);
                            int cantidadReal = item.EsCaja && (prod?.Contiene ?? 0) > 0 ? item.Cantidad * (prod!.Contiene ?? 1) : item.Cantidad;

                            _context.AjustesIngresoDetalle.Add(new CargoAjuste_Producto {
                                Id_Factura = ajuste.Id,
                                Id_Producto = item.IdProducto.ToString(),
                                Producto = item.NombreProducto,
                                Cantidad = item.Cantidad,
                                Lote = item.Lote,
                                FechaVence = item.FechaVencimiento,
                                EsCaja = item.EsCaja,
                                Contiene = prod?.Contiene
                            });

                            if (prod != null) {
                                prod.Disponible = (prod.Disponible ?? 0) + cantidadReal;
                                prod.Lote = item.Lote;
                                prod.Vencimiento = item.FechaVencimiento;
                            }

                            // Actualizar Stock por Bodega (Contexto Local para evitar inconsistencias)
                            var stockBodega = _context.StockBodegas.Local.FirstOrDefault(sb => sb.Id_Bodega == request.IdBodega && sb.Id_Producto == item.IdProducto.ToString());
                            if (stockBodega == null)
                            {
                                stockBodega = await _context.StockBodegas.FirstOrDefaultAsync(sb => sb.Id_Bodega == request.IdBodega && sb.Id_Producto == item.IdProducto.ToString());
                            }

                            if (stockBodega == null)
                            {
                                stockBodega = new StockBodega { Id_Bodega = request.IdBodega, Id_Producto = item.IdProducto.ToString(), Stock = 0 };
                                _context.StockBodegas.Add(stockBodega);
                            }
                            stockBodega.Stock += cantidadReal;

                            _context.Kardex.Add(new Stock_Producto {
                                Id_Producto = item.IdProducto.ToString(),
                                Producto = item.NombreProducto,
                                Cantidad = cantidadReal,
                                TipoMovimiento = "INGRESO POR AJUSTE",
                                Estado = true,
                                FechaRegistro = DateTime.Now,
                                Stock = (int?)stockBodega.Stock,
                                Id_Bodega = request.IdBodega,
                                Id_Usuario = 1
                            });
                        }
                    }
                    else // EGRESO
                    {
                        var ajuste = new DescargoAjuste {
                            Observacion = request.Observacion,
                            FechaHora_Registro = DateTime.Now,
                            Id_Resp = 1,
                            Estado = "Procesado"
                        };
                        _context.AjustesEgreso.Add(ajuste);
                        await _context.SaveChangesAsync();

                        foreach (var item in request.Detalles) {
                            var prod = await _context.Productos.FindAsync((long)item.IdProducto);
                            int cantidadReal = item.EsCaja && (prod?.Contiene ?? 0) > 0 ? item.Cantidad * (prod!.Contiene ?? 1) : item.Cantidad;

                            _context.AjustesEgresoDetalle.Add(new DescargoAjuste_Producto {
                                Id_Factura = ajuste.Id,
                                Id_Producto = item.IdProducto.ToString(),
                                Producto = item.NombreProducto,
                                Cantidad = item.Cantidad,
                                Lote = item.Lote,
                                FechaVence = item.FechaVencimiento,
                                EsCaja = item.EsCaja
                            });

                            if (prod != null) prod.Disponible = (prod.Disponible ?? 0) - cantidadReal;

                            // Actualizar Stock por Bodega (Contexto Local)
                            var stockBodega = _context.StockBodegas.Local.FirstOrDefault(sb => sb.Id_Bodega == request.IdBodega && sb.Id_Producto == item.IdProducto.ToString());
                            if (stockBodega == null)
                            {
                                stockBodega = await _context.StockBodegas.FirstOrDefaultAsync(sb => sb.Id_Bodega == request.IdBodega && sb.Id_Producto == item.IdProducto.ToString());
                            }

                            if (stockBodega == null)
                            {
                                stockBodega = new StockBodega { Id_Bodega = request.IdBodega, Id_Producto = item.IdProducto.ToString(), Stock = 0 };
                                _context.StockBodegas.Add(stockBodega);
                            }
                            stockBodega.Stock -= cantidadReal;

                            _context.Kardex.Add(new Stock_Producto {
                                Id_Producto = item.IdProducto.ToString(),
                                Producto = item.NombreProducto,
                                Cantidad = -cantidadReal,
                                TipoMovimiento = "EGRESO POR AJUSTE",
                                Estado = true,
                                FechaRegistro = DateTime.Now,
                                Stock = (int?)stockBodega.Stock,
                                Id_Bodega = request.IdBodega,
                                Id_Usuario = 1
                            });
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return Json(new { success = true, message = "Ajuste procesado correctamente." });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return Json(new { success = false, message = "Error: " + ex.Message });
                }
            });
        }

        [HttpPost]
        public async Task<IActionResult> GuardarBorrador([FromBody] AjusteRequest request)
        {
            if (request == null || !request.Detalles.Any())
                return Json(new { success = false, message = "El ajuste no tiene productos." });

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (request.Tipo == "INGRESO")
                {
                    var ajuste = new CargoAjuste {
                        Movimiento = "AJUSTE MANUAL INGRESO",
                        Observacion = request.Observacion,
                        FechaHora_Registro = DateTime.Now,
                        Id_Resp = 1,
                        Estado = "Borrador"
                    };
                    _context.AjustesIngreso.Add(ajuste);
                    await _context.SaveChangesAsync();

                    foreach (var item in request.Detalles) {
                        var prod = await _context.Productos.FindAsync((long)item.IdProducto);
                        _context.AjustesIngresoDetalle.Add(new CargoAjuste_Producto {
                            Id_Factura = ajuste.Id,
                            Id_Producto = item.IdProducto.ToString(),
                            Producto = item.NombreProducto,
                            Cantidad = item.Cantidad,
                            Lote = item.Lote,
                            FechaVence = item.FechaVencimiento,
                            EsCaja = item.EsCaja,
                            Contiene = prod?.Contiene
                        });
                    }
                }
                else
                {
                    var ajuste = new DescargoAjuste {
                        Observacion = request.Observacion,
                        FechaHora_Registro = DateTime.Now,
                        Id_Resp = 1,
                        Estado = "Borrador"
                    };
                    _context.AjustesEgreso.Add(ajuste);
                    await _context.SaveChangesAsync();

                    foreach (var item in request.Detalles) {
                        _context.AjustesEgresoDetalle.Add(new DescargoAjuste_Producto {
                            Id_Factura = ajuste.Id,
                            Id_Producto = item.IdProducto.ToString(),
                            Producto = item.NombreProducto,
                            Cantidad = item.Cantidad,
                            Lote = item.Lote,
                            FechaVence = item.FechaVencimiento,
                            EsCaja = item.EsCaja
                        });
                    }
                }
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Json(new { success = true, message = "Borrador guardado correctamente." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Error al guardar borrador: " + ex.Message });
            }
            });
        }

        public class AjusteRequest {
            public string? Tipo { get; set; } // INGRESO o EGRESO
            public string? Observacion { get; set; }
            public int IdBodega { get; set; }
            public List<AjusteDetalle> Detalles { get; set; } = new();
        }

        public class AjusteDetalle {
            public long IdProducto { get; set; }
            public string? NombreProducto { get; set; }
            public int Cantidad { get; set; }
            public string? Lote { get; set; }
            public DateTime? FechaVencimiento { get; set; }
            public bool EsCaja { get; set; }
        }
    }
}
