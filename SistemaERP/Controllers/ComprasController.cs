using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaERP.Models;
using SistemaERP.Models.Entities;

namespace SistemaERP.Controllers
{
    public class ComprasController : Controller
    {
        private readonly ScmContext _context;

        public ComprasController(ScmContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Optimización: Usar AsNoTracking y limitar a las últimas 100 compras
            var compras = await _context.Compras
                .AsNoTracking()
                .OrderByDescending(c => c.FechaHora_Reg)
                .Take(100)
                .ToListAsync();
            
            // Optimización: Cargar solo los nombres de proveedores necesarios
            var proveedoresIds = compras.Select(c => (long?)c.Id_Proveedor).Distinct().ToList();
            var proveedores = await _context.Proveedores
                .AsNoTracking()
                .Where(p => proveedoresIds.Contains(p.Id))
                .ToDictionaryAsync(p => (int)p.Id, p => p.Nombre);
            ViewBag.Proveedores = proveedores;

            // Optimización: Usar AsNoTracking y filtrar por IDs específicos
            var facturasIds = compras.Select(c => c.Id.ToString()).ToList();
            var totals = await _context.ComprasDetalle
                .AsNoTracking()
                .Where(d => facturasIds.Contains(d.Id_Factura))
                .GroupBy(d => d.Id_Factura)
                .Select(g => new { Id = g.Key, Total = g.Sum(x => x.TotalPagar ?? 0) })
                .ToDictionaryAsync(x => x.Id, x => x.Total);
            
            ViewBag.Totales = totals;

            return View(compras);
        }

        [HttpGet]
        public async Task<IActionResult> ExportarExcel()
        {
            var compras = await _context.Compras
                .AsNoTracking()
                .OrderByDescending(c => c.FechaHora_Reg)
                .Take(1000)
                .ToListAsync();

            var proveedoresIds = compras.Select(c => (long?)c.Id_Proveedor).Distinct().ToList();
            var proveedores = await _context.Proveedores
                .AsNoTracking()
                .Where(p => proveedoresIds.Contains(p.Id))
                .ToDictionaryAsync(p => (int)p.Id, p => p.Nombre ?? "N/A");

            var facturasIds = compras.Select(c => c.Id.ToString()).ToList();
            var totales = await _context.ComprasDetalle
                .AsNoTracking()
                .Where(d => facturasIds.Contains(d.Id_Factura))
                .GroupBy(d => d.Id_Factura)
                .Select(g => new { Id = g.Key, Total = g.Sum(x => x.TotalPagar ?? 0) })
                .ToDictionaryAsync(x => x.Id, x => x.Total);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Fecha;Factura #;Proveedor;Estado;Total");

            foreach (var c in compras)
            {
                var fechaStr = c.FechaHora_Reg?.ToString("dd/MM/yyyy HH:mm") ?? "N/A";
                var facturaNum = c.Factura ?? "N/A";
                proveedores.TryGetValue(c.Id_Proveedor ?? 0, out var provName);
                var estado = c.Estado ?? "N/A";
                totales.TryGetValue(c.Id.ToString(), out var totalVal);

                sb.AppendLine($"{fechaStr};{facturaNum};{provName ?? "N/A"};{estado};{totalVal:F2}");
            }

            var fileBytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(fileBytes, "text/csv; charset=utf-8", $"HistorialCompras_{DateTime.Now:yyyyMMddHHmmss}.csv");
        }

        [HttpGet]
        public async Task<IActionResult> ComprasPrint()
        {
            var compras = await _context.Compras
                .AsNoTracking()
                .OrderByDescending(c => c.FechaHora_Reg)
                .Take(1000)
                .ToListAsync();

            var proveedoresIds = compras.Select(c => (long?)c.Id_Proveedor).Distinct().ToList();
            ViewBag.Proveedores = await _context.Proveedores
                .AsNoTracking()
                .Where(p => proveedoresIds.Contains(p.Id))
                .ToDictionaryAsync(p => (int)p.Id, p => p.Nombre ?? "N/A");

            var facturasIds = compras.Select(c => c.Id.ToString()).ToList();
            ViewBag.Totales = await _context.ComprasDetalle
                .AsNoTracking()
                .Where(d => facturasIds.Contains(d.Id_Factura))
                .GroupBy(d => d.Id_Factura)
                .Select(g => new { Id = g.Key, Total = g.Sum(x => x.TotalPagar ?? 0) })
                .ToDictionaryAsync(x => x.Id, x => x.Total);

            var parametros = await _context.ParametrosSistema.AsNoTracking().FirstOrDefaultAsync();
            ViewBag.Parametros = parametros;
            
            string monedaSimbolo = "$";
            if (parametros != null && !string.IsNullOrEmpty(parametros.MonedaOficial))
            {
                var moneda = await _context.Monedas.AsNoTracking().FirstOrDefaultAsync(m => m.Codigo == parametros.MonedaOficial);
                monedaSimbolo = moneda?.Simbolo ?? "$";
            }
            ViewBag.MonedaSimbolo = monedaSimbolo;

            return View(compras);
        }

        [HttpGet]
        public async Task<IActionResult> Ticket(int? id)
        {
            if (id == null) return NotFound();

            var compra = await _context.Compras.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            if (compra == null) return NotFound();

            var proveedor = await _context.Proveedores.AsNoTracking().FirstOrDefaultAsync(p => p.Id == (long?)compra.Id_Proveedor);
            ViewBag.Proveedor = proveedor;

            var detalles = await _context.ComprasDetalle
                .AsNoTracking()
                .Where(d => d.Id_Factura == id.ToString())
                .ToListAsync();
            ViewBag.Detalles = detalles;

            var parametros = await _context.ParametrosSistema.AsNoTracking().FirstOrDefaultAsync();
            ViewBag.Parametros = parametros;
            
            string monedaSimbolo = "$";
            if (parametros != null && !string.IsNullOrEmpty(parametros.MonedaOficial))
            {
                var moneda = await _context.Monedas.AsNoTracking().FirstOrDefaultAsync(m => m.Codigo == parametros.MonedaOficial);
                monedaSimbolo = moneda?.Simbolo ?? "$";
            }
            ViewBag.MonedaSimbolo = monedaSimbolo;

            return View(compra);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var compra = await _context.Compras.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            if (compra == null) return NotFound();

            var proveedor = await _context.Proveedores.AsNoTracking().FirstOrDefaultAsync(p => p.Id == (long?)compra.Id_Proveedor);
            ViewBag.Proveedor = proveedor?.Nombre ?? "N/A";

            var detalles = await _context.ComprasDetalle
                .AsNoTracking()
                .Where(d => d.Id_Factura == id.ToString())
                .ToListAsync();

            ViewBag.Detalles = detalles;
            return View(compra);
        }



        public async Task<IActionResult> Create()
        {
            ViewBag.Proveedores = new SelectList(await _context.Proveedores.AsNoTracking().ToListAsync(), "Id", "Nombre");
            ViewBag.Productos = await _context.Productos.AsNoTracking().Where(p => p.Estado == "Activo").ToListAsync();
            ViewBag.Presentaciones = await _context.Presentaciones.AsNoTracking().ToListAsync();
            ViewBag.Bodegas = await _context.Bodegas.Where(b => b.Estado).ToListAsync();

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

        [HttpPost]
        public async Task<IActionResult> GuardarCompra([FromBody] CompraRequest request)
        {
            if (request == null || request.Detalles == null || !request.Detalles.Any())
                return Json(new { success = false, message = "La compra no tiene productos." });

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                CargoCompra compra;
                bool esCredito = string.Equals(request.MetodoPago, "CREDITO", StringComparison.OrdinalIgnoreCase);

                if (request.BorradorId.HasValue && request.BorradorId.Value > 0)
                {
                    if (!string.IsNullOrWhiteSpace(request.Factura))
                    {
                        bool facturaExiste = await _context.Compras
                            .AnyAsync(c => c.Factura == request.Factura && c.Id != request.BorradorId.Value && c.Estado != "Borrador");
                        if (facturaExiste)
                        {
                            await transaction.RollbackAsync();
                            return Json(new { success = false, message = $"Ya existe una compra registrada con la factura N° '{request.Factura}'. No se puede duplicar." });
                        }
                    }

                    compra = await _context.Compras.FindAsync(request.BorradorId.Value);
                    if (compra == null)
                    {
                        await transaction.RollbackAsync();
                        return Json(new { success = false, message = "Borrador de compra no encontrado." });
                    }

                    compra.Factura = request.Factura;
                    compra.Id_Proveedor = (int?)request.IdProveedor;
                    compra.Fecha_Documento = request.FechaDocumento;
                    compra.Observacion = request.Observacion;
                    compra.IVA = request.IvaTotal;
                    compra.Desc_Porc = request.DescuentoTotal;
                    compra.Estado = esCredito ? "Credito" : "Pagado";
                    compra.Dias_Pago = esCredito ? request.DiasCredito : 0;
                    compra.Fecha_Pago = esCredito ? request.FechaDocumento.AddDays(request.DiasCredito) : (DateTime?)null;
                    compra.FechaHora_Reg = DateTime.Now;

                    _context.Compras.Update(compra);

                    // Eliminar detalles previos del borrador
                    var oldDetalles = await _context.ComprasDetalle.Where(d => d.Id_Factura == compra.Id.ToString()).ToListAsync();
                    _context.ComprasDetalle.RemoveRange(oldDetalles);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(request.Factura))
                    {
                        bool facturaExiste = await _context.Compras
                            .AnyAsync(c => c.Factura == request.Factura && c.Estado != "Borrador");
                        if (facturaExiste)
                        {
                            await transaction.RollbackAsync();
                            return Json(new { success = false, message = $"Ya existe una compra registrada con la factura N° '{request.Factura}'. No se puede duplicar." });
                        }
                    }

                    compra = new CargoCompra
                    {
                        Factura = request.Factura,
                        Id_Proveedor = (int?)request.IdProveedor,
                        Fecha_Documento = request.FechaDocumento,
                        Observacion = request.Observacion,
                        IVA = request.IvaTotal,
                        Desc_Porc = request.DescuentoTotal,
                        Estado = esCredito ? "Credito" : "Pagado",
                        Dias_Pago = esCredito ? request.DiasCredito : 0,
                        Fecha_Pago = esCredito ? request.FechaDocumento.AddDays(request.DiasCredito) : (DateTime?)null,
                        FechaHora_Reg = DateTime.Now
                    };

                    _context.Compras.Add(compra);
                    await _context.SaveChangesAsync();
                }

                decimal totalFactura = 0;

                foreach (var item in request.Detalles)
                {
                    var producto = await _context.Productos.FindAsync((long)item.IdProducto);
                    var detalle = new CargoCompra_Producto
                    {
                        Id_Factura = compra.Id.ToString(),
                        Id_Producto = item.IdProducto.ToString(),
                        Producto = item.NombreProducto,
                        Cantidad = item.Cantidad,
                        Bonificacion = item.Bonificacion,
                        PVF = item.PrecioCosto,
                        Subtotal = item.Subtotal,
                        IVA = item.Iva,
                        Descuento = item.Descuento,
                        TotalPagar = item.Total,
                        Id_Usuario = 1,
                        Vencimiento = item.FechaVencimiento,
                        Lote = item.Lote,
                        EsCaja = item.EsCaja,
                        Contiene = producto?.Contiene,
                        PUR = item.PVP,
                        Id_Bodega = item.IdBodega
                    };
                    _context.ComprasDetalle.Add(detalle);

                    totalFactura += item.Total;

                    if (producto != null)
                    {
                        // Se ingresan unidades físicas = (Cantidad + Bonificación) * empaque
                        int cantidadArticulos = item.Cantidad + item.Bonificacion;
                        int cantidadIngreso = item.EsCaja ? cantidadArticulos * (producto.Contiene ?? 1) : cantidadArticulos;
                        
                        producto.Disponible = (producto.Disponible ?? 0) + cantidadIngreso;
                        producto.PVF = item.EsCaja ? item.PrecioCosto : (producto.Contiene > 1 ? item.PrecioCosto * (producto.Contiene ?? 1) : item.PrecioCosto);
                        producto.Lote = item.Lote;
                        producto.Vencimiento = item.FechaVencimiento;
                        if (item.PVP.HasValue && item.PVP.Value > 0)
                        {
                            producto.PVP = item.PVP.Value;
                        }
                        _context.Productos.Update(producto);

                        // Actualizar Stock por Bodega (Buscando primero en el contexto local para evitar duplicados)
                        var stockBodega = _context.StockBodegas.Local.FirstOrDefault(sb => sb.Id_Bodega == item.IdBodega && sb.Id_Producto == item.IdProducto.ToString());
                        if (stockBodega == null)
                        {
                            stockBodega = await _context.StockBodegas.FirstOrDefaultAsync(sb => sb.Id_Bodega == item.IdBodega && sb.Id_Producto == item.IdProducto.ToString());
                        }

                        if (stockBodega == null)
                        {
                            stockBodega = new StockBodega { 
                                Id_Bodega = item.IdBodega, 
                                Id_Producto = item.IdProducto.ToString(), 
                                Stock = 0
                            };
                            _context.StockBodegas.Add(stockBodega);
                        }
                        stockBodega.Stock += cantidadIngreso;

                        _context.Kardex.Add(new Stock_Producto
                        {
                            Id_Factura = compra.Id.ToString(),
                            Id_Producto = item.IdProducto.ToString(),
                            Producto = (item.EsCaja ? "[CAJA] " : "[DETALLE] ") + item.NombreProducto + (item.Bonificacion > 0 ? $" (Bonif: {item.Bonificacion})" : ""),
                            Cantidad = cantidadIngreso,
                            PVF = item.PrecioCosto,
                            Subtotal = item.Subtotal,
                            TipoMovimiento = "INGRESO POR COMPRA",
                            Estado = true,
                            FechaRegistro = DateTime.Now,
                            Stock = (int?)stockBodega.Stock,
                            Id_Bodega = item.IdBodega,
                            Id_Usuario = 1
                        });
                    }
                }

                // Si es crédito, registrar en CuentasPorPagar
                if (esCredito)
                {
                    var cxp = new CuentasPorPagar
                    {
                        Id_Compra = compra.Id,
                        Factura = compra.Factura,
                        Id_Proveedor = request.IdProveedor,
                        Fecha_Emision = request.FechaDocumento,
                        DiasPago = request.DiasCredito,
                        Fecha_Vencimiento = request.FechaDocumento.AddDays(request.DiasCredito),
                        Monto_Fact = totalFactura,
                        Monto_NC = 0.00m,
                        Monto_RET = 0.00m,
                        Monto_TotalPagar = totalFactura,
                        Monto_Abonado = 0.00m,
                        Monto_Saldo = totalFactura,
                        Estado = "POR PAGAR",
                        Monto_RET_IVA = 0.00m
                    };
                    _context.CuentasPorPagar.Add(cxp);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Json(new { success = true, message = esCredito ? "Compra a crédito registrada y enviada a Cuentas por Pagar." : "Compra al contado registrada exitosamente." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                var innerMsg = ex.InnerException?.InnerException?.Message
                            ?? ex.InnerException?.Message
                            ?? "Sin excepción interna";
                return Json(new { success = false, message = "Error: " + ex.Message + " | DETALLE: " + innerMsg });
            }
            }); // fin ExecutionStrategy
        }

        // ─── BORRADOR ────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> GuardarBorrador([FromBody] CompraRequest request)
        {
            if (request == null || request.Detalles == null || !request.Detalles.Any())
                return Json(new { success = false, message = "La compra no tiene productos." });

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                CargoCompra compra;
                bool esCredito = string.Equals(request.MetodoPago, "CREDITO", StringComparison.OrdinalIgnoreCase);

                if (request.BorradorId.HasValue && request.BorradorId.Value > 0)
                {
                    compra = await _context.Compras.FindAsync(request.BorradorId.Value);
                    if (compra == null)
                    {
                        await transaction.RollbackAsync();
                        return Json(new { success = false, message = "Borrador de compra no encontrado para actualizar." });
                    }

                    compra.Factura = request.Factura;
                    compra.Id_Proveedor = (int?)request.IdProveedor;
                    compra.Fecha_Documento = request.FechaDocumento;
                    compra.Observacion = request.Observacion;
                    compra.IVA = request.IvaTotal;
                    compra.Desc_Porc = request.DescuentoTotal;
                    compra.Estado = "Borrador";
                    compra.Dias_Pago = esCredito ? request.DiasCredito : 0;
                    compra.Fecha_Pago = esCredito ? request.FechaDocumento.AddDays(request.DiasCredito) : (DateTime?)null;
                    compra.FechaHora_Reg = DateTime.Now;

                    _context.Compras.Update(compra);

                    // Eliminar detalles previos del borrador
                    var oldDetalles = await _context.ComprasDetalle.Where(d => d.Id_Factura == compra.Id.ToString()).ToListAsync();
                    _context.ComprasDetalle.RemoveRange(oldDetalles);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    compra = new CargoCompra
                    {
                        Factura = request.Factura,
                        Id_Proveedor = (int?)request.IdProveedor,
                        Fecha_Documento = request.FechaDocumento,
                        Observacion = request.Observacion,
                        IVA = request.IvaTotal,
                        Desc_Porc = request.DescuentoTotal,
                        Estado = "Borrador",
                        Dias_Pago = esCredito ? request.DiasCredito : 0,
                        Fecha_Pago = esCredito ? request.FechaDocumento.AddDays(request.DiasCredito) : (DateTime?)null,
                        FechaHora_Reg = DateTime.Now
                    };

                    _context.Compras.Add(compra);
                    await _context.SaveChangesAsync();
                }

                foreach (var item in request.Detalles)
                {
                    var producto = await _context.Productos.FindAsync((long)item.IdProducto);
                    _context.ComprasDetalle.Add(new CargoCompra_Producto
                    {
                        Id_Factura = compra.Id.ToString(),
                        Id_Producto = item.IdProducto.ToString(),
                        Producto = item.NombreProducto,
                        Cantidad = item.Cantidad,
                        Bonificacion = item.Bonificacion,
                        PVF = item.PrecioCosto,
                        Subtotal = item.Subtotal,
                        IVA = item.Iva,
                        Descuento = item.Descuento,
                        TotalPagar = item.Total,
                        Id_Usuario = 1,
                        Vencimiento = item.FechaVencimiento,
                        Lote = item.Lote,
                        EsCaja = item.EsCaja,
                        Contiene = producto?.Contiene,
                        PUR = item.PVP,
                        Id_Bodega = item.IdBodega
                    });
                }
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Json(new { success = true, message = "Borrador guardado. Puedes continuar editándolo desde el historial.", id = compra.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Error al guardar borrador: " + ex.Message });
            }
            }); // fin ExecutionStrategy
        }

        [HttpGet]
        public async Task<IActionResult> EditarBorrador(int id)
        {
            var compra = await _context.Compras.FindAsync(id);
            if (compra == null || compra.Estado != "Borrador") return NotFound();

            var detalles = await _context.ComprasDetalle
                .Where(d => d.Id_Factura == id.ToString())
                .ToListAsync();

            ViewBag.Proveedores = new SelectList(await _context.Proveedores.ToListAsync(), "Id", "Nombre", compra.Id_Proveedor);
            ViewBag.Productos = await _context.Productos.Where(p => p.Estado == "Activo").ToListAsync();
            ViewBag.Presentaciones = await _context.Presentaciones.ToListAsync();
            ViewBag.Bodegas = await _context.Bodegas.Where(b => b.Estado).ToListAsync();
            ViewBag.BorradorId = id;
            ViewBag.BorradorDetalle = detalles;
            ViewBag.Compra = compra;

            var parametros = await _context.ParametrosSistema.AsNoTracking().FirstOrDefaultAsync();
            string monedaCodigo = parametros?.MonedaOficial;
            string monedaSimbolo = "$";
            if (!string.IsNullOrEmpty(monedaCodigo))
            {
                var moneda = await _context.Monedas.AsNoTracking().FirstOrDefaultAsync(m => m.Codigo == monedaCodigo);
                monedaSimbolo = moneda?.Simbolo ?? "$";
            }
            ViewBag.MonedaSimbolo = monedaSimbolo;

            return View("Create", compra);
        }

        [HttpPost]
        public async Task<IActionResult> FinalizarCompra([FromBody] FinalizarRequest request)
        {
            var compra = await _context.Compras.FindAsync(request.Id);
            if (compra == null) return Json(new { success = false, message = "Compra no encontrada." });
            if (compra.Estado != "Borrador") return Json(new { success = false, message = "Solo se pueden finalizar borradores." });

            bool facturaExiste = await _context.Compras
                .AnyAsync(c => c.Factura == compra.Factura && c.Id != compra.Id && c.Estado != "Borrador");
            if (facturaExiste)
                return Json(new { success = false, message = $"Ya existe una compra finalizada con la factura '{compra.Factura}'." });

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var detalles = await _context.ComprasDetalle
                    .Where(d => d.Id_Factura == compra.Id.ToString())
                    .ToListAsync();

                decimal totalFactura = 0;

                foreach (var item in detalles)
                {
                    totalFactura += item.TotalPagar ?? 0;

                    if (!long.TryParse(item.Id_Producto, out long prodId)) continue;
                    var producto = await _context.Productos.FindAsync(prodId);
                    if (producto != null)
                    {
                        bool esCaja = item.EsCaja == true;
                        int cantidadArticulos = (item.Cantidad ?? 0) + (item.Bonificacion ?? 0);
                        int cantidadIngreso = esCaja ? cantidadArticulos * (producto.Contiene ?? 1) : cantidadArticulos;
                        
                        producto.Disponible = (producto.Disponible ?? 0) + cantidadIngreso;
                        producto.PVF = esCaja ? item.PVF : (producto.Contiene > 1 ? item.PVF * (producto.Contiene ?? 1) : item.PVF);
                        producto.Lote = item.Lote;
                        producto.Vencimiento = item.Vencimiento;
                        if (item.PUR.HasValue && item.PUR.Value > 0)
                        {
                            producto.PVP = item.PUR.Value;
                        }
                        _context.Productos.Update(producto);

                        // Actualizar Stock por Bodega al finalizar la compra desde el borrador
                        int affectedBodegaId = item.Id_Bodega ?? compra.Id_Bodega ?? 1;
                        var stockBodega = _context.StockBodegas.Local.FirstOrDefault(sb => sb.Id_Bodega == affectedBodegaId && sb.Id_Producto == item.Id_Producto);
                        if (stockBodega == null)
                        {
                            stockBodega = await _context.StockBodegas.FirstOrDefaultAsync(sb => sb.Id_Bodega == affectedBodegaId && sb.Id_Producto == item.Id_Producto);
                        }

                        if (stockBodega == null)
                        {
                            stockBodega = new StockBodega { 
                                Id_Bodega = affectedBodegaId, 
                                Id_Producto = item.Id_Producto, 
                                Stock = 0
                            };
                            _context.StockBodegas.Add(stockBodega);
                        }
                        stockBodega.Stock += cantidadIngreso;

                        _context.Kardex.Add(new Stock_Producto
                        {
                            Id_Factura = compra.Id.ToString(),
                            Id_Producto = item.Id_Producto,
                            Producto = (esCaja ? "[CAJA] " : "[DETALLE] ") + item.Producto + (item.Bonificacion > 0 ? $" (Bonif: {item.Bonificacion})" : ""),
                            Cantidad = cantidadIngreso,
                            PVF = item.PVF,
                            Subtotal = item.Subtotal,
                            TipoMovimiento = "INGRESO POR COMPRA",
                            Estado = true,
                            FechaRegistro = DateTime.Now,
                            Stock = (int?)stockBodega.Stock,
                            Id_Bodega = affectedBodegaId,
                            Id_Usuario = 1
                        });
                    }
                }

                bool esCredito = compra.Dias_Pago > 0;
                compra.Estado = esCredito ? "Credito" : "Pagado";
                _context.Compras.Update(compra);

                if (esCredito)
                {
                    var cxp = new CuentasPorPagar
                    {
                        Id_Compra = compra.Id,
                        Factura = compra.Factura,
                        Id_Proveedor = compra.Id_Proveedor ?? 0,
                        Fecha_Emision = compra.Fecha_Documento ?? DateTime.Now,
                        DiasPago = compra.Dias_Pago ?? 0,
                        Fecha_Vencimiento = (compra.Fecha_Documento ?? DateTime.Now).AddDays(compra.Dias_Pago ?? 0),
                        Monto_Fact = totalFactura,
                        Monto_NC = 0.00m,
                        Monto_RET = 0.00m,
                        Monto_TotalPagar = totalFactura,
                        Monto_Abonado = 0.00m,
                        Monto_Saldo = totalFactura,
                        Estado = "POR PAGAR",
                        Monto_RET_IVA = 0.00m
                    };
                    _context.CuentasPorPagar.Add(cxp);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Json(new { success = true, message = "Compra finalizada y stock actualizado." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Error al finalizar: " + ex.Message });
            }
            }); // fin ExecutionStrategy
        }

        public class FinalizarRequest { public int Id { get; set; } }


        // Clases de apoyo para el JSON
        public class CompraRequest
        {
            public int? BorradorId { get; set; }
            public string? Factura { get; set; }
            public long IdProveedor { get; set; }
            public DateTime FechaDocumento { get; set; }
            public string? Observacion { get; set; }
            public decimal IvaTotal { get; set; }
            public decimal DescuentoTotal { get; set; }
            public int IdBodega { get; set; }
            public string MetodoPago { get; set; } = "EFECTIVO"; // "EFECTIVO" o "CREDITO"
            public int DiasCredito { get; set; } = 0;
            public List<DetalleRequest> Detalles { get; set; } = new();
        }

        public class DetalleRequest
        {
            public long IdProducto { get; set; }
            public string? NombreProducto { get; set; }
            public int Cantidad { get; set; }
            public int Bonificacion { get; set; } = 0;
            public decimal PrecioCosto { get; set; }
            public decimal Subtotal { get; set; }
            public decimal Iva { get; set; }
            public decimal Descuento { get; set; } = 0;
            public decimal Total { get; set; }
            public DateTime? FechaVencimiento { get; set; }
            public string? Lote { get; set; }
            public bool EsCaja { get; set; }
            public decimal? PVP { get; set; }
            public int IdBodega { get; set; }
        }
    }
}
