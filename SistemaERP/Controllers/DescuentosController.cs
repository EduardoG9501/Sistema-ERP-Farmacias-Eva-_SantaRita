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
    public class DescuentosController : Controller
    {
        private readonly ScmContext _context;

        public DescuentosController(ScmContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Cargar productos con descuentos clásicos configurados
                var prodClasicos = await _context.Productos
                    .Where(p => p.Descuento != null && p.Descuento > 0)
                    .ToListAsync();

                // Cargar todas las líneas de descuento
                var lineas = await _context.DescuentosLinea.ToListAsync();
                // Cargar mapeos de productos para líneas
                var lineasDetalles = await _context.DescuentosLineaDetalle.ToListAsync();

                // Cargar todos los combos
                var combos = await _context.DescuentosCombo.ToListAsync();
                // Cargar mapeos de productos para combos
                var combosDetalles = await _context.DescuentosComboProductos.ToListAsync();

                // Cargar todos los precios especiales
                var especiales = await _context.VentasPreciosEspeciales.ToListAsync();

                // Cargar descuentos por proveedor
                var descProveedores = await _context.DescuentosProveedor.ToListAsync();
                var proveedoresList = await _context.Proveedores.ToListAsync();
                var proveedoresDict = proveedoresList.ToDictionary(p => p.Id, p => p.Nombre ?? "");

                // Diccionario de todos los productos para lookup de nombres
                var productosDict = await _context.Productos
                    .ToDictionaryAsync(p => p.Id, p => p.Nombre ?? "");

                ViewBag.ProductosDict = productosDict;
                ViewBag.ProductosClasicos = prodClasicos;
                ViewBag.Lineas = lineas;
                ViewBag.LineasDetalles = lineasDetalles;
                ViewBag.Combos = combos;
                ViewBag.CombosDetalles = combosDetalles;
                ViewBag.Especiales = especiales;
                ViewBag.DescuentosProveedor = descProveedores;
                ViewBag.ProveedoresList = proveedoresList;
                ViewBag.ProveedoresDict = proveedoresDict;

                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error al obtener descuentos: " + ex.Message;
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> BuscarProductos(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return Json(new List<object>());
            }
            var query = term.ToLower();
            var list = await _context.Productos
                .Where(p => (p.Nombre != null && p.Nombre.ToLower().Contains(query)) || (p.CodigoBarra != null && p.CodigoBarra.Contains(query)))
                .Take(20)
                .Select(p => new {
                    id = p.Id,
                    nombre = p.Nombre,
                    codigoBarra = p.CodigoBarra,
                    pvp = p.PVP,
                    pvf = p.PVF
                })
                .ToListAsync();
            return Json(list);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProductDiscount([FromBody] ProductDiscountRequest req)
        {
            if (req == null) return Json(new { success = false, message = "Datos nulos." });
            try
            {
                var prod = await _context.Productos.FindAsync(req.ProductoId);
                if (prod == null) return Json(new { success = false, message = "Producto no encontrado." });

                prod.Descuento = req.Descuento;
                prod.Descuento_Desde = req.Desde;
                prod.Descuento_Hasta = req.Hasta;

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Descuento clásico de producto actualizado." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveLineDiscount([FromBody] LineDiscountRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Descripcion))
            {
                return Json(new { success = false, message = "Datos inválidos o descripción vacía." });
            }

            try
            {
                var strategy = _context.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        DescuentoLineaProducto line;
                        if (req.Id > 0)
                        {
                            line = await _context.DescuentosLinea.FindAsync(req.Id);
                            if (line == null) return Json(new { success = false, message = "Línea de descuento no encontrada." });
                            
                            line.Descripcion = req.Descripcion;
                            line.Porcentaje = req.Porcentaje;
                            line.Vence_Desde = req.VenceDesde;
                            line.Vence_Hasta = req.VenceHasta;
                            line.Estado = req.Estado;
                        }
                        else
                        {
                            line = new DescuentoLineaProducto
                            {
                                Descripcion = req.Descripcion,
                                Porcentaje = req.Porcentaje,
                                Vence_Desde = req.VenceDesde,
                                Vence_Hasta = req.VenceHasta,
                                Estado = req.Estado,
                                CreateDate = DateTime.Now,
                                CreateUser = "admin"
                            };
                            _context.DescuentosLinea.Add(line);
                        }

                        await _context.SaveChangesAsync();

                        if (req.Id > 0)
                        {
                            var oldDets = await _context.DescuentosLineaDetalle
                                .Where(d => d.Id_Descuento_Linea == line.Id)
                                .ToListAsync();
                            _context.DescuentosLineaDetalle.RemoveRange(oldDets);
                        }

                        if (req.ProductIds != null)
                        {
                            foreach (var pId in req.ProductIds)
                            {
                                _context.DescuentosLineaDetalle.Add(new DescuentoLineaProductoDet
                                {
                                    Id_Descuento_Linea = line.Id,
                                    Id_Producto = pId
                                });
                            }
                        }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        return Json(new { success = true, message = "Descuento de línea guardado exitosamente." });
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteLineDiscount(int id)
        {
            try
            {
                var strategy = _context.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        var line = await _context.DescuentosLinea.FindAsync(id);
                        if (line == null) return Json(new { success = false, message = "Línea no encontrada." });

                        var dets = await _context.DescuentosLineaDetalle
                            .Where(d => d.Id_Descuento_Linea == id)
                            .ToListAsync();
                        _context.DescuentosLineaDetalle.RemoveRange(dets);
                        _context.DescuentosLinea.Remove(line);

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        return Json(new { success = true, message = "Línea de descuento eliminada." });
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveCombo([FromBody] ComboRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Descripcion))
            {
                return Json(new { success = false, message = "Datos inválidos o descripción vacía." });
            }

            try
            {
                var strategy = _context.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        DescuentoCombo combo;
                        if (req.Id > 0)
                        {
                            combo = await _context.DescuentosCombo.FindAsync(req.Id);
                            if (combo == null) return Json(new { success = false, message = "Combo no encontrado." });
                            
                            combo.Descripcion = req.Descripcion;
                            combo.Precio_Combo = req.PrecioCombo;
                            combo.Cant_Min = req.CantMin;
                            combo.Cant_Max = req.CantMax;
                            combo.Vence_Desde = req.VenceDesde;
                            combo.Vence_Hasta = req.VenceHasta;
                            combo.Estado = req.Estado;
                        }
                        else
                        {
                            combo = new DescuentoCombo
                            {
                                Descripcion = req.Descripcion,
                                Precio_Combo = req.PrecioCombo,
                                Cant_Min = req.CantMin,
                                Cant_Max = req.CantMax,
                                Vence_Desde = req.VenceDesde,
                                Vence_Hasta = req.VenceHasta,
                                Estado = req.Estado,
                                CreateDate = DateTime.Now,
                                CreateUser = "admin"
                            };
                            _context.DescuentosCombo.Add(combo);
                        }

                        await _context.SaveChangesAsync();

                        if (req.Id > 0)
                        {
                            var oldDets = await _context.DescuentosComboProductos
                                .Where(d => d.Id_Descuento_Combo == combo.Id)
                                .ToListAsync();
                            _context.DescuentosComboProductos.RemoveRange(oldDets);
                        }

                        if (req.ProductIds != null)
                        {
                            foreach (var pId in req.ProductIds)
                            {
                                _context.DescuentosComboProductos.Add(new DescuentoComboProducto
                                {
                                    Id_Descuento_Combo = combo.Id,
                                    Id_Producto = pId
                                });
                            }
                        }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        return Json(new { success = true, message = "Combo guardado exitosamente." });
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCombo(int id)
        {
            try
            {
                var strategy = _context.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        var combo = await _context.DescuentosCombo.FindAsync(id);
                        if (combo == null) return Json(new { success = false, message = "Combo no encontrado." });

                        var dets = await _context.DescuentosComboProductos
                            .Where(d => d.Id_Descuento_Combo == id)
                            .ToListAsync();
                        _context.DescuentosComboProductos.RemoveRange(dets);
                        _context.DescuentosCombo.Remove(combo);

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        return Json(new { success = true, message = "Combo eliminado." });
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveSpecialPrice([FromBody] SpecialPriceRequest req)
        {
            if (req == null) return Json(new { success = false, message = "Datos nulos." });
            try
            {
                VentaPrecioEspecial spe;
                if (req.Id > 0)
                {
                    spe = await _context.VentasPreciosEspeciales.FindAsync(req.Id);
                    if (spe == null) return Json(new { success = false, message = "Precio especial no encontrado." });

                    spe.Id_Producto = req.ProductoId;
                    spe.Precio_Especial = req.PrecioEspecial;
                    spe.Cantidad = req.Cantidad;
                    spe.Estado = req.Estado;
                }
                else
                {
                    spe = new VentaPrecioEspecial
                    {
                        Id_Producto = req.ProductoId,
                        Precio_Especial = req.PrecioEspecial,
                        Cantidad = req.Cantidad,
                        Estado = req.Estado ?? "A",
                        CreateDate = DateTime.Now
                    };
                    _context.VentasPreciosEspeciales.Add(spe);
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Precio especial guardado exitosamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSpecialPrice(int id)
        {
            try
            {
                var spe = await _context.VentasPreciosEspeciales.FindAsync(id);
                if (spe == null) return Json(new { success = false, message = "Precio especial no encontrado." });

                _context.VentasPreciosEspeciales.Remove(spe);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Precio especial eliminado." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveSupplierDiscount([FromBody] SupplierDiscountRequest req)
        {
            if (req == null || req.ProveedorId <= 0 || string.IsNullOrWhiteSpace(req.Descripcion))
            {
                return Json(new { success = false, message = "Datos inválidos o descripción vacía." });
            }
            try
            {
                DescuentoProveedor dp;
                if (req.Id > 0)
                {
                    dp = await _context.DescuentosProveedor.FindAsync(req.Id);
                    if (dp == null) return Json(new { success = false, message = "Descuento de proveedor no encontrado." });

                    dp.Id_Proveedor = req.ProveedorId;
                    dp.Descripcion = req.Descripcion;
                    dp.Porcentaje = req.Porcentaje;
                    dp.Vence_Desde = req.VenceDesde;
                    dp.Vence_Hasta = req.VenceHasta;
                    dp.Estado = req.Estado;
                }
                else
                {
                    dp = new DescuentoProveedor
                    {
                        Id_Proveedor = req.ProveedorId,
                        Descripcion = req.Descripcion,
                        Porcentaje = req.Porcentaje,
                        Vence_Desde = req.VenceDesde,
                        Vence_Hasta = req.VenceHasta,
                        Estado = req.Estado,
                        CreateDate = DateTime.Now,
                        CreateUser = "admin"
                    };
                    _context.DescuentosProveedor.Add(dp);
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Descuento por proveedor guardado exitosamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSupplierDiscount(int id)
        {
            try
            {
                var dp = await _context.DescuentosProveedor.FindAsync(id);
                if (dp == null) return Json(new { success = false, message = "Descuento de proveedor no encontrado." });

                _context.DescuentosProveedor.Remove(dp);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Descuento por proveedor eliminado exitosamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
    }

    public class ProductDiscountRequest
    {
        public long ProductoId { get; set; }
        public decimal? Descuento { get; set; }
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
    }

    public class LineDiscountRequest
    {
        public int Id { get; set; }
        public string Descripcion { get; set; } = null!;
        public decimal Porcentaje { get; set; }
        public DateTime VenceDesde { get; set; }
        public DateTime VenceHasta { get; set; }
        public bool Estado { get; set; }
        public List<long>? ProductIds { get; set; }
    }

    public class ComboRequest
    {
        public int Id { get; set; }
        public string Descripcion { get; set; } = null!;
        public decimal PrecioCombo { get; set; }
        public long CantMin { get; set; }
        public long CantMax { get; set; }
        public DateTime VenceDesde { get; set; }
        public DateTime VenceHasta { get; set; }
        public bool Estado { get; set; }
        public List<long>? ProductIds { get; set; }
    }

    public class SpecialPriceRequest
    {
        public int Id { get; set; }
        public long ProductoId { get; set; }
        public decimal PrecioEspecial { get; set; }
        public decimal Cantidad { get; set; }
        public string? Estado { get; set; }
    }

    public class SupplierDiscountRequest
    {
        public int Id { get; set; }
        public long ProveedorId { get; set; }
        public string Descripcion { get; set; } = null!;
        public decimal Porcentaje { get; set; }
        public DateTime VenceDesde { get; set; }
        public DateTime VenceHasta { get; set; }
        public bool Estado { get; set; }
    }
}
