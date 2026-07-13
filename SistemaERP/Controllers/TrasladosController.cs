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
    public class TrasladosController : Controller
    {
        private readonly ScmContext _context;

        public TrasladosController(ScmContext context)
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
            var traslados = await _context.Traslados
                .OrderByDescending(t => t.Fecha_Envio)
                .ToListAsync();

            ViewBag.Bodegas = await _context.Bodegas.AsNoTracking().ToDictionaryAsync(b => b.Id, b => b.Nombre ?? "N/A");
            ViewBag.Usuarios = await _context.Usuarios.AsNoTracking().ToDictionaryAsync(u => u.Id, u => u.Login ?? "N/A");

            return View(traslados);
        }

        [HttpGet]
        public async Task<IActionResult> ExportarExcel()
        {
            var traslados = await _context.Traslados
                .AsNoTracking()
                .OrderByDescending(t => t.Fecha_Envio)
                .Take(1000)
                .ToListAsync();

            var bodegas = await _context.Bodegas.AsNoTracking().ToDictionaryAsync(b => b.Id, b => b.Nombre ?? "N/A");
            var usuarios = await _context.Usuarios.AsNoTracking().ToDictionaryAsync(u => u.Id, u => u.Login ?? "N/A");

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Fecha Envio;Orden #;Bodega Origen;Bodega Destino;Estado;Fecha Recepcion");

            foreach (var t in traslados)
            {
                var fechaEnvioStr = t.Fecha_Envio.ToString("dd/MM/yyyy HH:mm");
                var ordenNum = $"#{t.Id}";
                bodegas.TryGetValue(t.Id_Bodega_Origen, out var bOrigen);
                bodegas.TryGetValue(t.Id_Bodega_Destino, out var bDestino);
                var estado = t.Estado ?? "PENDIENTE";
                var fechaRecStr = t.Fecha_Recepcion?.ToString("dd/MM/yyyy HH:mm") ?? "N/A";

                sb.AppendLine($"{fechaEnvioStr};{ordenNum};{bOrigen ?? "N/A"};{bDestino ?? "N/A"};{estado};{fechaRecStr}");
            }

            var fileBytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(fileBytes, "text/csv; charset=utf-8", $"Traslados_{DateTime.Now:yyyyMMddHHmmss}.csv");
        }

        [HttpGet]
        public async Task<IActionResult> TrasladosPrint()
        {
            var traslados = await _context.Traslados
                .AsNoTracking()
                .OrderByDescending(t => t.Fecha_Envio)
                .Take(1000)
                .ToListAsync();

            ViewBag.Bodegas = await _context.Bodegas.AsNoTracking().ToDictionaryAsync(b => b.Id, b => b.Nombre ?? "N/A");
            ViewBag.Usuarios = await _context.Usuarios.AsNoTracking().ToDictionaryAsync(u => u.Id, u => u.Login ?? "N/A");
            ViewBag.Parametros = await _context.ParametrosSistema.AsNoTracking().FirstOrDefaultAsync();

            return View(traslados);
        }

        [HttpGet]
        public async Task<IActionResult> Ticket(long? id)
        {
            if (id == null) return NotFound();

            var traslado = await _context.Traslados.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
            if (traslado == null) return NotFound();

            ViewBag.BodegaOrigen = await _context.Bodegas.AsNoTracking().FirstOrDefaultAsync(b => b.Id == traslado.Id_Bodega_Origen);
            ViewBag.BodegaDestino = await _context.Bodegas.AsNoTracking().FirstOrDefaultAsync(b => b.Id == traslado.Id_Bodega_Destino);

            var usuarioEnvio = await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == traslado.Id_Usuario_Envio);
            ViewBag.UsuarioEnvio = usuarioEnvio?.Login ?? "N/A";
            
            var usuarioRecepcion = traslado.Id_Usuario_Recepcion.HasValue 
                ? await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == traslado.Id_Usuario_Recepcion.Value)
                : null;
            ViewBag.UsuarioRecepcion = usuarioRecepcion?.Login ?? "N/A";

            var detalles = await _context.TrasladosDetalle
                .AsNoTracking()
                .Where(d => d.Id_Traslado == id)
                .ToListAsync();
            ViewBag.Detalles = detalles;

            var parametros = await _context.ParametrosSistema.AsNoTracking().FirstOrDefaultAsync();
            ViewBag.Parametros = parametros;

            return View(traslado);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Bodegas = await _context.Bodegas.AsNoTracking().Where(b => b.Estado).ToListAsync();
            ViewBag.Productos = await _context.Productos.AsNoTracking().Where(p => p.Estado == "Activo").OrderBy(p => p.Nombre).ToListAsync();
            ViewBag.Presentaciones = await _context.Presentaciones.AsNoTracking().ToListAsync();
            
            // Cargar stock por bodega para la validación del lado del cliente
            var stocks = await _context.StockBodegas.AsNoTracking().ToListAsync();
            ViewBag.StockPorBodega = stocks
                .GroupBy(s => s.Id_Producto)
                .ToDictionary(g => g.Key ?? "", g => g.ToList());

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GuardarTraslado([FromBody] TrasladoRequest request)
        {
            if (request == null || !request.Detalles.Any())
                return Json(new { success = false, message = "El traslado no tiene productos." });

            if (request.IdBodegaOrigen == request.IdBodegaDestino)
                return Json(new { success = false, message = "La bodega de origen y destino no pueden ser la misma." });

            var idUser = await GetCurrentUserId();

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    long nextId = (await _context.Traslados.AnyAsync()) ? await _context.Traslados.MaxAsync(t => t.Id) + 1 : 1;

                    var traslado = new Traslado
                    {
                        Id = nextId,
                        Fecha_Envio = DateTime.Now,
                        Id_Bodega_Origen = request.IdBodegaOrigen,
                        Id_Bodega_Destino = request.IdBodegaDestino,
                        Id_Usuario_Envio = idUser,
                        Estado = "PENDIENTE",
                        Observacion = request.Observacion
                    };

                    _context.Traslados.Add(traslado);

                    foreach (var item in request.Detalles)
                    {
                        var prod = await _context.Productos.FindAsync((long)item.IdProducto);
                        if (prod == null)
                        {
                            await transaction.RollbackAsync();
                            return Json(new { success = false, message = $"Producto con ID {item.IdProducto} no encontrado." });
                        }

                        // Validar stock disponible en la bodega de origen en el momento del envío
                        var stockOrigen = await _context.StockBodegas
                            .FirstOrDefaultAsync(sb => sb.Id_Bodega == request.IdBodegaOrigen && sb.Id_Producto == item.IdProducto.ToString());
                        
                        int cantidadReal = item.EsCaja ? item.Cantidad * (prod.Contiene ?? 1) : item.Cantidad;

                        if (stockOrigen == null || stockOrigen.Stock < cantidadReal)
                        {
                            await transaction.RollbackAsync();
                            return Json(new { success = false, message = $"Stock insuficiente en la bodega de origen para el producto: {prod.Nombre}. Disponible: {(stockOrigen?.Stock ?? 0)} unidades." });
                        }

                        var detalle = new Traslado_Producto
                        {
                            Id_Traslado = traslado.Id,
                            Id_Producto = item.IdProducto.ToString(),
                            Producto = (item.EsCaja ? "[CAJA] " : "[DETALLE] ") + prod.Nombre,
                            Cantidad = item.Cantidad,
                            EsCaja = item.EsCaja,
                            Contiene = prod.Contiene ?? 1
                        };

                        _context.TrasladosDetalle.Add(detalle);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return Json(new { success = true, message = "Traslado registrado con éxito como PENDIENTE (Orden de Salida generada)." });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return Json(new { success = false, message = "Error al guardar el traslado: " + ex.Message });
                }
            });
        }

        public async Task<IActionResult> Recepcionar(long id)
        {
            var traslado = await _context.Traslados.FindAsync(id);
            if (traslado == null) return NotFound();

            var detalles = await _context.TrasladosDetalle
                .Where(d => d.Id_Traslado == id)
                .ToListAsync();

            ViewBag.Detalles = detalles;
            ViewBag.Bodegas = await _context.Bodegas.AsNoTracking().ToDictionaryAsync(b => b.Id, b => b.Nombre ?? "N/A");
            ViewBag.Usuarios = await _context.Usuarios.AsNoTracking().ToDictionaryAsync(u => u.Id, u => u.Login ?? "N/A");

            return View(traslado);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmarRecepcion(long id)
        {
            var traslado = await _context.Traslados.FindAsync(id);
            if (traslado == null) return Json(new { success = false, message = "Traslado no encontrado." });
            if (traslado.Estado != "PENDIENTE") return Json(new { success = false, message = "Solo se pueden recepcionar traslados pendientes." });

            var idUser = await GetCurrentUserId();
            var detalles = await _context.TrasladosDetalle.Where(d => d.Id_Traslado == id).ToListAsync();

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    foreach (var item in detalles)
                    {
                        if (!long.TryParse(item.Id_Producto, out long prodId)) continue;
                        var prod = await _context.Productos.FindAsync(prodId);
                        if (prod == null) continue;

                        int cantidadReal = item.EsCaja ? item.Cantidad * item.Contiene : item.Cantidad;

                        // 1. Decrementar de la bodega de origen
                        var stockOrigen = _context.StockBodegas.Local.FirstOrDefault(sb => sb.Id_Bodega == traslado.Id_Bodega_Origen && sb.Id_Producto == item.Id_Producto);
                        if (stockOrigen == null)
                        {
                            stockOrigen = await _context.StockBodegas.FirstOrDefaultAsync(sb => sb.Id_Bodega == traslado.Id_Bodega_Origen && sb.Id_Producto == item.Id_Producto);
                        }

                        if (stockOrigen == null || stockOrigen.Stock < cantidadReal)
                        {
                            await transaction.RollbackAsync();
                            return Json(new { success = false, message = $"Stock insuficiente en la bodega de origen para: {item.Producto}. Disponible: {(stockOrigen?.Stock ?? 0)} unidades." });
                        }
                        stockOrigen.Stock -= cantidadReal;

                        // 2. Incrementar en la bodega de destino
                        var stockDestino = _context.StockBodegas.Local.FirstOrDefault(sb => sb.Id_Bodega == traslado.Id_Bodega_Destino && sb.Id_Producto == item.Id_Producto);
                        if (stockDestino == null)
                        {
                            stockDestino = await _context.StockBodegas.FirstOrDefaultAsync(sb => sb.Id_Bodega == traslado.Id_Bodega_Destino && sb.Id_Producto == item.Id_Producto);
                        }

                        if (stockDestino == null)
                        {
                            stockDestino = new StockBodega { Id_Bodega = traslado.Id_Bodega_Destino, Id_Producto = item.Id_Producto, Stock = 0 };
                            _context.StockBodegas.Add(stockDestino);
                        }
                        stockDestino.Stock += cantidadReal;

                        // 3. Registrar en Kardex para la Bodega de Origen (EGRESO POR TRASLADO)
                        _context.Kardex.Add(new Stock_Producto
                        {
                            Id_Factura = traslado.Id.ToString(),
                            Id_Producto = item.Id_Producto,
                            Producto = "[EGRESO TRASLADO] " + item.Producto,
                            Cantidad = -cantidadReal,
                            PVF = prod.PVF,
                            TipoMovimiento = "EGRESO POR TRASLADO",
                            Estado = true,
                            FechaRegistro = DateTime.Now,
                            Stock = (int?)stockOrigen.Stock,
                            Id_Bodega = traslado.Id_Bodega_Origen,
                            Id_Usuario = idUser
                        });

                        // 4. Registrar en Kardex para la Bodega de Destino (INGRESO POR TRASLADO)
                        _context.Kardex.Add(new Stock_Producto
                        {
                            Id_Factura = traslado.Id.ToString(),
                            Id_Producto = item.Id_Producto,
                            Producto = "[INGRESO TRASLADO] " + item.Producto,
                            Cantidad = cantidadReal,
                            PVF = prod.PVF,
                            TipoMovimiento = "INGRESO POR TRASLADO",
                            Estado = true,
                            FechaRegistro = DateTime.Now,
                            Stock = (int?)stockDestino.Stock,
                            Id_Bodega = traslado.Id_Bodega_Destino,
                            Id_Usuario = idUser
                        });
                    }

                    // Actualizar el estado del traslado
                    traslado.Estado = "RECIBIDO";
                    traslado.Fecha_Recepcion = DateTime.Now;
                    traslado.Id_Usuario_Recepcion = idUser;

                    _context.Traslados.Update(traslado);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return Json(new { success = true, message = "Traslado aprobado y recibido. Stock transferido correctamente entre bodegas." });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return Json(new { success = false, message = "Error al confirmar la recepción: " + ex.Message });
                }
            });
        }

        [HttpPost]
        public async Task<IActionResult> Anular(long id)
        {
            var traslado = await _context.Traslados.FindAsync(id);
            if (traslado == null) return Json(new { success = false, message = "Traslado no encontrado." });
            if (traslado.Estado != "PENDIENTE") return Json(new { success = false, message = "Solo se pueden anular traslados pendientes." });

            try
            {
                traslado.Estado = "ANULADO";
                _context.Traslados.Update(traslado);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Traslado anulado exitosamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al anular traslado: " + ex.Message });
            }
        }

        // DTOs para JSON
        public class TrasladoRequest
        {
            public int IdBodegaOrigen { get; set; }
            public int IdBodegaDestino { get; set; }
            public string? Observacion { get; set; }
            public List<TrasladoDetalleRequest> Detalles { get; set; } = new();
        }

        public class TrasladoDetalleRequest
        {
            public long IdProducto { get; set; }
            public int Cantidad { get; set; }
            public bool EsCaja { get; set; }
        }
    }
}
