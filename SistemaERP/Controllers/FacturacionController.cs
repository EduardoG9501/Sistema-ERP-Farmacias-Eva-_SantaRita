using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaERP.Models;
using SistemaERP.Models.Entities;

namespace SistemaERP.Controllers
{
    public partial class FacturacionController : Controller
    {
        private readonly ScmContext _context;
        private readonly Services.ISriService _sriService;
        private readonly Areas.Admin.Models.AdminContext _adminContext;
        private readonly Areas.Admin.Services.ICamposPersonalizadosService _camposService;

        public FacturacionController(
            ScmContext context, 
            Services.ISriService sriService,
            Areas.Admin.Models.AdminContext adminContext,
            Areas.Admin.Services.ICamposPersonalizadosService camposService)
        {
            _context = context;
            _sriService = sriService;
            _adminContext = adminContext;
            _camposService = camposService;
        }

        [HttpGet]
        public async Task<IActionResult> TransmitirSRI(long id)
        {
            try {
                var result = await _sriService.ProcesarFactura(id);
                return Json(new { success = !result.StartsWith("Error"), message = result });
            } catch (Exception ex) {
                return Json(new { success = false, message = "Error interno: " + ex.Message });
            }
        }

        public async Task<IActionResult> DescargarXml(long id)
        {
            var venta = await _context.Ventas.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id);
            if (venta == null || string.IsNullOrEmpty(venta.SRI_ClaveAcceso)) return NotFound();

            var settings = await _context.ConfiguracionSRI.AsNoTracking().FirstOrDefaultAsync();
            string pathXml = settings?.PathFileSaveXml ?? "C:\\SRI\\XML";
            string filePath = Path.Combine(pathXml, venta.SRI_ClaveAcceso + "_firmado.xml");

            if (!System.IO.File.Exists(filePath)) 
            {
                // Backup: intentar con el original si el firmado no existe
                filePath = Path.Combine(pathXml, venta.SRI_ClaveAcceso + ".xml");
                if (!System.IO.File.Exists(filePath)) 
                    return NotFound("El archivo físico no fue encontrado en el servidor. Ruta buscada: " + filePath);
            }

            byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, "application/xml", $"{venta.SRI_ClaveAcceso}.xml");
        }

        private async Task<string> GetMonedaSimbolo(string? monedaCodigo)
        {
            if (string.IsNullOrEmpty(monedaCodigo)) return "$";
            var moneda = await _context.Monedas.AsNoTracking().FirstOrDefaultAsync(m => m.Codigo == monedaCodigo);
            return moneda?.Simbolo ?? "$";
        }

        private async Task<int> GetCurrentUserId()
        {
            var username = User.Identity?.Name;
            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Login.Trim() == (username ?? "").Trim());
            return user?.Id ?? 0;
        }

        private DateTime GetUserLocalDateTime()
        {
            if (Request.Cookies.TryGetValue("UserTimeZoneOffset", out var offsetStr) && int.TryParse(offsetStr, out var offset))
            {
                return DateTime.UtcNow.AddMinutes(-offset);
            }
            return DateTime.Now; // Fallback to server local time
        }

        private DateTime GetUserLocalDate()
        {
            return GetUserLocalDateTime().Date;
        }

        [HttpPost]
        public async Task<IActionResult> AbrirCaja(decimal monto)
        {
            try
            {
                var idUser = await GetCurrentUserId();
                if (idUser == 0) return Json(new { success = false, message = "Usuario no identificado." });

                var today = GetUserLocalDate();
                var nextDay = today.AddDays(1);
                var yaTieneCaja = await _context.AperturasCaja
                    .AnyAsync(a => a.UsuarioId == idUser && a.FechaApertura >= today && a.FechaApertura < nextDay);

                if (yaTieneCaja) return Json(new { success = false, message = "Ya tiene una apertura de caja para hoy." });

                var nuevaApertura = new AperturaCaja
                {
                    FechaApertura = today,
                    MontoInicial = monto,
                    UsuarioId = idUser
                };

                _context.AperturasCaja.Add(nuevaApertura);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Caja abierta correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al abrir caja: " + ex.Message });
            }
        }

        public async Task<IActionResult> Index(string? sriEstado, int? tipoPago)
        {
            var query = _context.Ventas.AsNoTracking();

            if (!string.IsNullOrEmpty(sriEstado))
            {
                string state = sriEstado.Trim().ToUpper();
                if (state == "PENDIENTE") {
                    query = query.Where(v => string.IsNullOrEmpty(v.SRI_Estado) || (v.SRI_Estado ?? "").ToUpper().Trim() == "PENDIENTE");
                } else if (state == "ERROR") {
                    query = query.Where(v => (v.SRI_Estado ?? "").ToUpper().Trim() == "ERROR" || (v.SRI_Estado ?? "").ToUpper().Trim() == "DEVUELTA");
                } else {
                    query = query.Where(v => (v.SRI_Estado ?? "").ToUpper().Trim() == state);
                }
            }

            if (tipoPago.HasValue)
            {
                var queryAbonos = _context.FacturasPorCobrarAbonos.AsNoTracking();
                if (tipoPago.Value == 1)
                {
                    queryAbonos = queryAbonos.Where(a => a.Tipo_Pago == 1);
                }
                else if (tipoPago.Value == 2)
                {
                    queryAbonos = queryAbonos.Where(a => a.Tipo_Pago == 2 && 
                        !(a.Adquirente ?? "").ToUpper().Contains("DEBITO") && 
                        !(a.Num_Doc ?? "").ToUpper().Contains("DEBITO"));
                }
                else if (tipoPago.Value == 6) // Tarjeta de Débito
                {
                    queryAbonos = queryAbonos.Where(a => a.Tipo_Pago == 2 && 
                        ((a.Adquirente ?? "").ToUpper().Contains("DEBITO") || 
                         (a.Num_Doc ?? "").ToUpper().Contains("DEBITO")));
                }
                else if (tipoPago.Value == 3)
                {
                    queryAbonos = queryAbonos.Where(a => a.Tipo_Pago == 3);
                }
                else if (tipoPago.Value == 4) // Cheque
                {
                    queryAbonos = queryAbonos.Where(a => a.Tipo_Pago == 4);
                }
                else if (tipoPago.Value == 5) // Depósito Bancario
                {
                    queryAbonos = queryAbonos.Where(a => a.Tipo_Pago == 4 && string.IsNullOrEmpty(a.Imagen_Doc));
                }

                var ventasConEsePago = await queryAbonos
                    .Select(a => a.Id_Venta)
                    .Distinct()
                    .ToListAsync();
                query = query.Where(v => ventasConEsePago.Contains(v.Id));
            }

            // Optimización: Limitar a las últimas 100 ventas para mejorar rendimiento
            var ventas = await query.OrderByDescending(v => v.FechaHora_Registro).Take(100).ToListAsync();
            
            ViewBag.Clientes = await _context.Clientes.AsNoTracking().ToDictionaryAsync(c => c.Id, c => c.Nombres_Apellidos ?? "S/N");
            
            var ventaIds = ventas.Select(v => v.Id).ToList();
            ViewBag.Totales = await _context.VentasDetalle
                .AsNoTracking()
                .Where(d => ventaIds.Contains(d.Id_Venta))
                .GroupBy(d => d.Id_Venta)
                .Select(g => new { Id = g.Key, Total = g.Sum(x => x.Total_Pagar ?? 0) })
                .ToDictionaryAsync(x => x.Id, x => x.Total);

            ViewBag.CurrentEstado = sriEstado;
            ViewBag.CurrentTipoPago = tipoPago;
            return View(ventas);
        }

        public async Task<IActionResult> CierreCaja(DateTime? fecha)
        {
            var date = fecha ?? GetUserLocalDate();
            var nextDay = date.Date.AddDays(1);
            var userId = await GetCurrentUserId();

            // Current user's apertura for the day
            var apertura = await _context.AperturasCaja
                .FirstOrDefaultAsync(a => a.UsuarioId == userId && a.FechaApertura >= date.Date && a.FechaApertura < nextDay);

            ViewBag.Apertura = apertura;
            ViewBag.TotalApertura = apertura?.MontoInicial ?? 0;

            // All payments for today by this user, with references
            var pagos = await _context.FacturasPorCobrarAbonos
                .Where(p => p.Fecha_Cobro >= date.Date && p.Fecha_Cobro < nextDay && p.Id_Usuario == userId)
                .OrderBy(p => p.FechaHora_Reg)
                .ToListAsync();

            var efectivoPagos = pagos.Where(p => p.Tipo_Pago == 1).ToList();
            var tarjetaPagos  = pagos.Where(p => p.Tipo_Pago == 2).ToList();
            var transPagos    = pagos.Where(p => p.Tipo_Pago == 3).ToList();
            var otrosPagos    = pagos.Where(p => p.Tipo_Pago != 1 && p.Tipo_Pago != 2 && p.Tipo_Pago != 3).ToList();

            ViewBag.EfectivoPagos = efectivoPagos;
            ViewBag.TarjetaPagos  = tarjetaPagos;
            ViewBag.TransPagos    = transPagos;
            ViewBag.OtrosPagos    = otrosPagos;

            ViewBag.Efectivo       = efectivoPagos.Sum(p => p.Monto_Efectivo);
            ViewBag.Tarjeta        = tarjetaPagos.Sum(p => p.Monto_Efectivo);
            ViewBag.Transferencia  = transPagos.Sum(p => p.Monto_Efectivo);
            ViewBag.Otros          = otrosPagos.Sum(p => p.Monto_Efectivo);

            var totalCobrado = ViewBag.Efectivo + ViewBag.Tarjeta + ViewBag.Transferencia + ViewBag.Otros;
            ViewBag.TotalCobrado = totalCobrado;
            ViewBag.TotalEnCaja = (apertura?.MontoInicial ?? 0) + ViewBag.Efectivo;

            // Credit sales for today linked to this user's ventas
            var userVentaIds = await _context.Ventas
                .Where(v => v.Id_Usuario == userId && v.FechaHora_Registro >= date.Date && v.FechaHora_Registro < nextDay)
                .Select(v => v.Id)
                .ToListAsync();

            var creditSales = userVentaIds.Any()
                ? await _context.FacturasPorCobrar
                    .Where(c => userVentaIds.Contains(c.Num_Venta))
                    .ToListAsync()
                : new List<FacturasPorCobrar>();

            ViewBag.CreditSales = creditSales;
            ViewBag.TotalCredito = creditSales.Sum(c => c.Monto_Fact);
            ViewBag.SaldoCredito = creditSales.Sum(c => c.Monto_Saldo);

            // Check if this user already has a CierreCaja for today
            CierreCaja? cierreExistente = null;
            if (apertura != null)
            {
                cierreExistente = await _context.CierresCaja
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.AperturaCajaId == apertura.Id);
            }

            ViewBag.CierreExistente = cierreExistente;
            ViewBag.YaCerrado = cierreExistente != null;

            // Asegurar que existan denominaciones en la base de datos (Ecuador / USD por defecto)
            if (!await _context.Denominaciones.AnyAsync())
            {
                var plantilla = new List<Denominacion>
                {
                    new Denominacion { Nombre = "100 Dólares", Valor = 100.00m, Tipo = "Billete", Activo = true },
                    new Denominacion { Nombre = "50 Dólares",  Valor = 50.00m,  Tipo = "Billete", Activo = true },
                    new Denominacion { Nombre = "20 Dólares",  Valor = 20.00m,  Tipo = "Billete", Activo = true },
                    new Denominacion { Nombre = "10 Dólares",  Valor = 10.00m,  Tipo = "Billete", Activo = true },
                    new Denominacion { Nombre = "5 Dólares",   Valor = 5.00m,   Tipo = "Billete", Activo = true },
                    new Denominacion { Nombre = "2 Dólares",   Valor = 2.00m,   Tipo = "Billete", Activo = true },
                    new Denominacion { Nombre = "1 Dólar",     Valor = 1.00m,   Tipo = "Billete", Activo = true },
                    new Denominacion { Nombre = "1 Dólar (Moneda)", Valor = 1.00m, Tipo = "Moneda",  Activo = true },
                    new Denominacion { Nombre = "50 Centavos", Valor = 0.50m,  Tipo = "Moneda",  Activo = true },
                    new Denominacion { Nombre = "25 Centavos", Valor = 0.25m,  Tipo = "Moneda",  Activo = true },
                    new Denominacion { Nombre = "10 Centavos", Valor = 0.10m,  Tipo = "Moneda",  Activo = true },
                    new Denominacion { Nombre = "5 Centavos",  Valor = 0.05m,  Tipo = "Moneda",  Activo = true },
                    new Denominacion { Nombre = "1 Centavo",   Valor = 0.01m,  Tipo = "Moneda",  Activo = true }
                };

                _context.Denominaciones.AddRange(plantilla);
                await _context.SaveChangesAsync();
            }

            var denominaciones = await _context.Denominaciones
                .Where(d => d.Activo)
                .OrderByDescending(d => d.Valor)
                .ThenBy(d => d.Tipo)
                .ToListAsync();
            ViewBag.Denominaciones = denominaciones;

            var bancos = await _context.Bancos.AsNoTracking().ToDictionaryAsync(b => b.Id, b => b.Nombre ?? "");
            ViewBag.Bancos = bancos;

            var user = await _context.Usuarios.FindAsync(userId);
            ViewBag.IsAdmin = user?.Tipo_Usuario == "ADMINISTRADOR" || user?.Tipo_Usuario == "ADMIN";

            ViewBag.Fecha = date.ToString("yyyy-MM-dd");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CierreCaja(DateTime fecha, string detalleDenominaciones, string metodosPago)
        {
            var nextDay = fecha.Date.AddDays(1);
            var userId = await GetCurrentUserId();

            // Find current user's apertura
            var apertura = await _context.AperturasCaja
                .FirstOrDefaultAsync(a => a.UsuarioId == userId && a.FechaApertura >= fecha.Date && a.FechaApertura < nextDay);

            if (apertura == null)
                return Json(new { success = false, message = "No tiene apertura de caja para esta fecha." });

            // Check if already closed
            var yaCerrado = await _context.CierresCaja.AnyAsync(c => c.AperturaCajaId == apertura.Id);
            if (yaCerrado)
                return Json(new { success = false, message = "Su caja ya fue cerrada anteriormente." });

            // Parse detalleDenominaciones JSON to calculate MontoFinal
            var montoFinal = 0m;
            if (!string.IsNullOrEmpty(detalleDenominaciones))
            {
                var items = System.Text.Json.JsonSerializer.Deserialize<List<DenominacionCount>>(detalleDenominaciones);
                if (items != null)
                {
                    foreach (var item in items)
                    {
                        var denom = await _context.Denominaciones.FindAsync(item.DenominacionId);
                        if (denom != null)
                            montoFinal += denom.Valor * item.Cantidad;
                    }
                }
            }

            // Calculate expected cash (Apertura + Efectivo from sales by this user)
            var efectivo = await _context.FacturasPorCobrarAbonos
                .Where(p => p.Fecha_Cobro >= fecha.Date && p.Fecha_Cobro < nextDay && p.Id_Usuario == userId && p.Tipo_Pago == 1)
                .SumAsync(p => p.Monto_Efectivo);
            var esperado = apertura.MontoInicial + efectivo;

            // Recalculate/override the Apertura and Efectivo values in metodosPago securely on the server
            if (!string.IsNullOrEmpty(metodosPago))
            {
                try
                {
                    var items = System.Text.Json.JsonSerializer.Deserialize<List<MetodoPagoItem>>(metodosPago);
                    if (items != null)
                    {
                        var itemApertura = items.FirstOrDefault(i => i.Metodo == "Apertura");
                        if (itemApertura != null)
                        {
                            itemApertura.Monto = apertura.MontoInicial;
                        }
                        else
                        {
                            items.Add(new MetodoPagoItem { Metodo = "Apertura", Monto = apertura.MontoInicial });
                        }

                        var itemEfectivo = items.FirstOrDefault(i => i.Metodo == "Efectivo");
                        if (itemEfectivo != null)
                        {
                            itemEfectivo.Monto = efectivo;
                        }
                        else
                        {
                            items.Add(new MetodoPagoItem { Metodo = "Efectivo", Monto = efectivo });
                        }

                        metodosPago = System.Text.Json.JsonSerializer.Serialize(items);
                    }
                }
                catch
                {
                    // Fallback to original string if deserialization fails
                }
            }

            var cierre = new CierreCaja
            {
                AperturaCajaId = apertura.Id,
                UsuarioCierraId = userId,
                FechaCierre = DateTime.Now,
                MontoFinal = montoFinal,
                Diferencia = montoFinal - efectivo,
                DetalleDenominaciones = detalleDenominaciones,
                MetodosPago = metodosPago
            };

            _context.CierresCaja.Add(cierre);
            await _context.SaveChangesAsync();

            return Json(new {
                success = true,
                message = "Cierre de caja guardado correctamente.",
                diferencia = cierre.Diferencia.ToString("N2"),
                cierreId = cierre.Id
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReiniciarCierreCaja(int id)
        {
            var userId = await GetCurrentUserId();
            var user = await _context.Usuarios.FindAsync(userId);
            bool isAdmin = user != null && (user.Tipo_Usuario == "ADMINISTRADOR" || user.Tipo_Usuario == "ADMIN");
            
            if (!isAdmin)
            {
                return Json(new { success = false, message = "No tiene privilegios de administrador para realizar esta acción." });
            }

            var cierre = await _context.CierresCaja.FindAsync(id);
            if (cierre == null)
            {
                return Json(new { success = false, message = "El cierre de caja especificado no existe." });
            }

            _context.CierresCaja.Remove(cierre);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "El cierre de caja ha sido reiniciado correctamente. Ahora puede volver a facturar y realizar un nuevo cierre." });
        }

        public async Task<IActionResult> ImprimirCierre(int id)
        {
            var cierre = await _context.CierresCaja
                .Include(c => c.Apertura)
                .Include(c => c.UsuarioCierra)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cierre == null)
                return NotFound();

            // Load denominations
            var denominaciones = await _context.Denominaciones
                .Where(d => d.Activo)
                .OrderByDescending(d => d.Valor)
                .ThenBy(d => d.Tipo)
                .ToListAsync();
            ViewBag.Denominaciones = denominaciones;

            var bancos = await _context.Bancos.AsNoTracking().ToDictionaryAsync(b => b.Id, b => b.Nombre ?? "");
            ViewBag.Bancos = bancos;

            var parametros = await _context.ParametrosSistema.AsNoTracking().FirstOrDefaultAsync();
            ViewBag.Parametros = parametros;
            ViewBag.MonedaSimbolo = await GetMonedaSimbolo(parametros?.MonedaOficial);

            return View(cierre);
        }

        private class DenominacionCount
        {
            public int DenominacionId { get; set; }
            public int Cantidad { get; set; }
        }

        private class MetodoPagoItem
        {
            public string Metodo { get; set; } = "";
            public decimal Monto { get; set; }
            public string Referencia { get; set; } = "";
        }

        public async Task<IActionResult> Details(long? id)
        {
            if (id == null) return NotFound();

            var venta = await _context.Ventas.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id);
            if (venta == null) return NotFound();

            var cliente = await _context.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == (long?)venta.Id_Cliente);
            ViewBag.Cliente = cliente?.Nombres_Apellidos ?? "Consumidor Final";

            var detalles = await _context.VentasDetalle
                .AsNoTracking()
                .Where(d => d.Id_Venta == id)
                .ToListAsync();

            ViewBag.Detalles = detalles;
            return View(venta);
        }

        public async Task<IActionResult> Kardex(
            string? idProducto,
            string? codBarra,
            string? tipoMovimiento,
            DateTime? desde,
            DateTime? hasta,
            int? idBodega,
            string? laboratorio)
        {
            var query = _context.Kardex.AsNoTracking().AsQueryable();

            if (!string.IsNullOrEmpty(idProducto))
            {
                query = query.Where(k => k.Id_Producto == idProducto);
            }

            if (!string.IsNullOrEmpty(laboratorio))
            {
                var matchingProdIds = await _context.Productos.AsNoTracking()
                    .Where(p => _context.Proveedores.Any(prov => prov.Id == p.Id_Proveedor && prov.Nombre.Contains(laboratorio.Trim())))
                    .Select(p => p.Id.ToString())
                    .ToListAsync();
                
                query = query.Where(k => matchingProdIds.Contains(k.Id_Producto));
            }

            if (!string.IsNullOrEmpty(codBarra))
            {
                var prod = await _context.Productos.AsNoTracking().FirstOrDefaultAsync(p => p.CodigoBarra == codBarra.Trim());
                if (prod != null)
                {
                    query = query.Where(k => k.Id_Producto == prod.Id.ToString());
                }
                else
                {
                    query = query.Where(k => k.Id_Producto == "0"); // Filter to return nothing if barcode doesn't exist
                }
            }

            if (!string.IsNullOrEmpty(tipoMovimiento) && tipoMovimiento != "TODOS")
            {
                query = query.Where(k => k.TipoMovimiento == tipoMovimiento);
            }

            if (desde.HasValue)
            {
                query = query.Where(k => k.FechaRegistro >= desde.Value.Date);
            }

            if (hasta.HasValue)
            {
                query = query.Where(k => k.FechaRegistro < hasta.Value.Date.AddDays(1));
            }

            if (idBodega.HasValue)
            {
                query = query.Where(k => k.Id_Bodega == idBodega);
            }

            // Fetch movements (up to 1000 for safety and performance)
            var movimientos = await query.OrderByDescending(k => k.FechaRegistro).Take(1000).ToListAsync();

            // Populate filter values in ViewBag to preserve state in the UI
            ViewBag.IdProducto = idProducto;
            ViewBag.CodBarra = codBarra;
            ViewBag.TipoMovimiento = tipoMovimiento;
            ViewBag.Desde = desde?.ToString("yyyy-MM-dd");
            ViewBag.Hasta = hasta?.ToString("yyyy-MM-dd");
            ViewBag.IdBodega = idBodega;
            ViewBag.FilterLaboratorio = laboratorio;

            // Load dependencies
            ViewBag.Productos = await _context.Productos.AsNoTracking().OrderBy(p => p.Nombre).ToListAsync();
            ViewBag.Bodegas = await _context.Bodegas.AsNoTracking().Where(b => b.Estado).ToListAsync();
            ViewBag.Usuarios = await _context.Usuarios.AsNoTracking().ToDictionaryAsync(u => u.Id, u => u.Login ?? "N/A");
            ViewBag.StockPorBodega = (await _context.StockBodegas.AsNoTracking().ToListAsync())
                .GroupBy(sb => sb.Id_Producto)
                .ToDictionary(g => g.Key ?? "", g => g.ToList());
            ViewBag.Proveedores = await _context.Proveedores.AsNoTracking().ToDictionaryAsync(pv => pv.Id, pv => pv.Nombre ?? "N/A");
            ViewBag.ProveedoresList = await _context.Proveedores.AsNoTracking().OrderBy(pv => pv.Nombre).ToListAsync();

            return View(movimientos);
        }

        [HttpGet]
        public async Task<IActionResult> ExportarVentasExcel(string? sriEstado, int? tipoPago)
        {
            var query = _context.Ventas.AsNoTracking();

            if (!string.IsNullOrEmpty(sriEstado))
            {
                string state = sriEstado.Trim().ToUpper();
                if (state == "PENDIENTE") {
                    query = query.Where(v => string.IsNullOrEmpty(v.SRI_Estado) || (v.SRI_Estado ?? "").ToUpper().Trim() == "PENDIENTE");
                } else if (state == "ERROR") {
                    query = query.Where(v => (v.SRI_Estado ?? "").ToUpper().Trim() == "ERROR" || (v.SRI_Estado ?? "").ToUpper().Trim() == "DEVUELTA");
                } else {
                    query = query.Where(v => (v.SRI_Estado ?? "").ToUpper().Trim() == state);
                }
            }

            if (tipoPago.HasValue)
            {
                var queryAbonos = _context.FacturasPorCobrarAbonos.AsNoTracking();
                if (tipoPago.Value == 1)
                {
                    queryAbonos = queryAbonos.Where(a => a.Tipo_Pago == 1);
                }
                else if (tipoPago.Value == 2)
                {
                    queryAbonos = queryAbonos.Where(a => a.Tipo_Pago == 2 && 
                        !(a.Adquirente ?? "").ToUpper().Contains("DEBITO") && 
                        !(a.Num_Doc ?? "").ToUpper().Contains("DEBITO"));
                }
                else if (tipoPago.Value == 6) // Tarjeta de Débito
                {
                    queryAbonos = queryAbonos.Where(a => a.Tipo_Pago == 2 && 
                        ((a.Adquirente ?? "").ToUpper().Contains("DEBITO") || 
                         (a.Num_Doc ?? "").ToUpper().Contains("DEBITO")));
                }
                else if (tipoPago.Value == 3)
                {
                    queryAbonos = queryAbonos.Where(a => a.Tipo_Pago == 3);
                }
                else if (tipoPago.Value == 4) // Cheque
                {
                    queryAbonos = queryAbonos.Where(a => a.Tipo_Pago == 4);
                }
                else if (tipoPago.Value == 5) // Depósito Bancario
                {
                    queryAbonos = queryAbonos.Where(a => a.Tipo_Pago == 4 && string.IsNullOrEmpty(a.Imagen_Doc));
                }

                var ventasConEsePago = await queryAbonos
                    .Select(a => a.Id_Venta)
                    .Distinct()
                    .ToListAsync();
                query = query.Where(v => ventasConEsePago.Contains(v.Id));
            }

            var ventas = await query.OrderByDescending(v => v.FechaHora_Registro).Take(1000).ToListAsync();
            var clientes = await _context.Clientes.AsNoTracking().ToDictionaryAsync(c => c.Id, c => c.Nombres_Apellidos ?? "S/N");
            var ventaIds = ventas.Select(v => v.Id).ToList();
            var totales = await _context.VentasDetalle
                .AsNoTracking()
                .Where(d => ventaIds.Contains(d.Id_Venta))
                .GroupBy(d => d.Id_Venta)
                .Select(g => new { Id = g.Key, Total = g.Sum(x => x.Total_Pagar ?? 0) })
                .ToDictionaryAsync(x => x.Id, x => x.Total);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Fecha;Ticket #;Cliente;Estado SRI;Total");

            foreach (var v in ventas)
            {
                var fechaStr = v.FechaHora_Registro?.ToString("dd/MM/yyyy HH:mm") ?? "N/A";
                var ticketNum = v.NumDoc ?? "N/A";
                clientes.TryGetValue(v.Id_Cliente ?? 0, out var clienteNom);
                clienteNom = v.Id_Cliente.HasValue ? (clienteNom ?? "S/N") : "Consumidor Final";
                var state = v.SRI_Estado ?? "PENDIENTE";
                totales.TryGetValue(v.Id, out var totalVal);
                
                sb.AppendLine($"{fechaStr};{ticketNum};{clienteNom};{state};{totalVal:F2}");
            }

            var fileBytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(fileBytes, "text/csv; charset=utf-8", $"HistorialVentas_{DateTime.Now:yyyyMMddHHmmss}.csv");
        }

        [HttpGet]
        public async Task<IActionResult> VentasPrint(string? sriEstado, int? tipoPago)
        {
            var query = _context.Ventas.AsNoTracking();

            if (!string.IsNullOrEmpty(sriEstado))
            {
                string state = sriEstado.Trim().ToUpper();
                if (state == "PENDIENTE") {
                    query = query.Where(v => string.IsNullOrEmpty(v.SRI_Estado) || (v.SRI_Estado ?? "").ToUpper().Trim() == "PENDIENTE");
                } else if (state == "ERROR") {
                    query = query.Where(v => (v.SRI_Estado ?? "").ToUpper().Trim() == "ERROR" || (v.SRI_Estado ?? "").ToUpper().Trim() == "DEVUELTA");
                } else {
                    query = query.Where(v => (v.SRI_Estado ?? "").ToUpper().Trim() == state);
                }
            }

            if (tipoPago.HasValue)
            {
                var queryAbonos = _context.FacturasPorCobrarAbonos.AsNoTracking();
                if (tipoPago.Value == 1)
                {
                    queryAbonos = queryAbonos.Where(a => a.Tipo_Pago == 1);
                }
                else if (tipoPago.Value == 2)
                {
                    queryAbonos = queryAbonos.Where(a => a.Tipo_Pago == 2 && 
                        !(a.Adquirente ?? "").ToUpper().Contains("DEBITO") && 
                        !(a.Num_Doc ?? "").ToUpper().Contains("DEBITO"));
                }
                else if (tipoPago.Value == 6) // Tarjeta de Débito
                {
                    queryAbonos = queryAbonos.Where(a => a.Tipo_Pago == 2 && 
                        ((a.Adquirente ?? "").ToUpper().Contains("DEBITO") || 
                         (a.Num_Doc ?? "").ToUpper().Contains("DEBITO")));
                }
                else if (tipoPago.Value == 3)
                {
                    queryAbonos = queryAbonos.Where(a => a.Tipo_Pago == 3);
                }
                else if (tipoPago.Value == 4) // Cheque
                {
                    queryAbonos = queryAbonos.Where(a => a.Tipo_Pago == 4);
                }
                else if (tipoPago.Value == 5) // Depósito Bancario
                {
                    queryAbonos = queryAbonos.Where(a => a.Tipo_Pago == 4 && string.IsNullOrEmpty(a.Imagen_Doc));
                }

                var ventasConEsePago = await queryAbonos
                    .Select(a => a.Id_Venta)
                    .Distinct()
                    .ToListAsync();
                query = query.Where(v => ventasConEsePago.Contains(v.Id));
            }

            var ventas = await query.OrderByDescending(v => v.FechaHora_Registro).Take(1000).ToListAsync();
            ViewBag.Clientes = await _context.Clientes.AsNoTracking().ToDictionaryAsync(c => c.Id, c => c.Nombres_Apellidos ?? "S/N");
            var ventaIds = ventas.Select(v => v.Id).ToList();
            ViewBag.Totales = await _context.VentasDetalle
                .AsNoTracking()
                .Where(d => ventaIds.Contains(d.Id_Venta))
                .GroupBy(d => d.Id_Venta)
                .Select(g => new { Id = g.Key, Total = g.Sum(x => x.Total_Pagar ?? 0) })
                .ToDictionaryAsync(x => x.Id, x => x.Total);

            ViewBag.Parametros = await _context.ParametrosSistema.AsNoTracking().FirstOrDefaultAsync();

            return View(ventas);
        }

        [HttpGet]
        public async Task<IActionResult> KardexExportExcel(
            string? idProducto,
            string? codBarra,
            string? tipoMovimiento,
            DateTime? desde,
            DateTime? hasta,
            int? idBodega,
            string? laboratorio)
        {
            var query = _context.Kardex.AsNoTracking().AsQueryable();

            if (!string.IsNullOrEmpty(idProducto))
            {
                query = query.Where(k => k.Id_Producto == idProducto);
            }

            if (!string.IsNullOrEmpty(laboratorio))
            {
                var matchingProdIds = await _context.Productos.AsNoTracking()
                    .Where(p => _context.Proveedores.Any(prov => prov.Id == p.Id_Proveedor && prov.Nombre.Contains(laboratorio.Trim())))
                    .Select(p => p.Id.ToString())
                    .ToListAsync();
                
                query = query.Where(k => matchingProdIds.Contains(k.Id_Producto));
            }

            if (!string.IsNullOrEmpty(codBarra))
            {
                var prod = await _context.Productos.AsNoTracking().FirstOrDefaultAsync(p => p.CodigoBarra == codBarra.Trim());
                if (prod != null)
                {
                    query = query.Where(k => k.Id_Producto == prod.Id.ToString());
                }
                else
                {
                    query = query.Where(k => k.Id_Producto == "0");
                }
            }

            if (!string.IsNullOrEmpty(tipoMovimiento) && tipoMovimiento != "TODOS")
            {
                query = query.Where(k => k.TipoMovimiento == tipoMovimiento);
            }

            if (desde.HasValue)
            {
                query = query.Where(k => k.FechaRegistro >= desde.Value.Date);
            }

            if (hasta.HasValue)
            {
                query = query.Where(k => k.FechaRegistro < hasta.Value.Date.AddDays(1));
            }

            if (idBodega.HasValue)
            {
                query = query.Where(k => k.Id_Bodega == idBodega);
            }

            var movimientos = await query.OrderByDescending(k => k.FechaRegistro).ToListAsync();
            
            var bodegasList = await _context.Bodegas.AsNoTracking().Where(b => b.Estado).ToListAsync();
            var prodMap = await _context.Productos.AsNoTracking().ToDictionaryAsync(p => p.Id.ToString(), p => p);
            var usersMap = await _context.Usuarios.AsNoTracking().ToDictionaryAsync(u => u.Id, u => u.Login ?? "N/A");
            var proveMap = await _context.Proveedores.AsNoTracking().ToDictionaryAsync(pv => pv.Id, pv => pv.Nombre ?? "N/A");
            var stockDict = (await _context.StockBodegas.AsNoTracking().ToListAsync())
                .GroupBy(sb => sb.Id_Producto)
                .ToDictionary(g => g.Key ?? "", g => g.ToList());

            var sb = new System.Text.StringBuilder();
            // Write UTF-8 BOM
            sb.AppendLine("Fecha;Hora;Producto;Código de Barra;Proveedor;Movimiento;Ref. Documento;Cantidad;Disponible (por Bodega);Responsable");

            foreach (var item in movimientos)
            {
                var fechaStr = item.FechaRegistro.ToString("dd/MM/yyyy");
                var horaStr = item.FechaRegistro.ToString("hh:mm tt");
                
                prodMap.TryGetValue(item.Id_Producto ?? "", out var p);
                var prodNom = p?.Nombre ?? item.Producto ?? "N/A";
                var barCode = p?.CodigoBarra ?? "N/A";
                
                var provNom = "N/A";
                if (p?.Id_Proveedor.HasValue == true)
                {
                    proveMap.TryGetValue(p.Id_Proveedor.Value, out var pName);
                    provNom = pName ?? "N/A";
                }
                
                var movNom = item.TipoMovimiento ?? "N/A";
                var refDoc = string.IsNullOrEmpty(item.Id_Factura) ? "N/A" : "#" + item.Id_Factura;
                var cant = (item.Cantidad >= 0 ? "+" : "") + item.Cantidad;
                
                var affectedBodegaId = item.Id_Bodega ?? 1;
                var affectedBodega = bodegasList.FirstOrDefault(b => b.Id == affectedBodegaId);
                var dispStr = "N/A";

                if (affectedBodega != null)
                {
                    long stockQty = item.Stock ?? 0;
                    long contiene = p?.Contiene ?? 1;
                    if (contiene <= 0) contiene = 1;
                    long cajas = stockQty / contiene;
                    long fracs = stockQty % contiene;
                    dispStr = $"{cajas} F {fracs} ({affectedBodega.Nombre})";
                }
                
                var resp = "N/A";
                if (item.Id_Usuario.HasValue && usersMap.TryGetValue(item.Id_Usuario.Value, out var uName))
                {
                    resp = uName;
                }

                sb.AppendLine($"{fechaStr};{horaStr};{prodNom};{barCode};{provNom};{movNom};{refDoc};{cant};{dispStr};{resp}");
            }

            var fileBytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(fileBytes, "text/csv; charset=utf-8", $"Kardex_{DateTime.Now:yyyyMMddHHmmss}.csv");
        }

        public async Task<IActionResult> KardexPrint(
            string? idProducto,
            string? codBarra,
            string? tipoMovimiento,
            DateTime? desde,
            DateTime? hasta,
            int? idBodega,
            string? laboratorio)
        {
            var query = _context.Kardex.AsNoTracking().AsQueryable();

            if (!string.IsNullOrEmpty(idProducto))
            {
                query = query.Where(k => k.Id_Producto == idProducto);
            }

            if (!string.IsNullOrEmpty(laboratorio))
            {
                var matchingProdIds = await _context.Productos.AsNoTracking()
                    .Where(p => _context.Proveedores.Any(prov => prov.Id == p.Id_Proveedor && prov.Nombre.Contains(laboratorio.Trim())))
                    .Select(p => p.Id.ToString())
                    .ToListAsync();
                
                query = query.Where(k => matchingProdIds.Contains(k.Id_Producto));
            }

            if (!string.IsNullOrEmpty(codBarra))
            {
                var prod = await _context.Productos.AsNoTracking().FirstOrDefaultAsync(p => p.CodigoBarra == codBarra.Trim());
                if (prod != null)
                {
                    query = query.Where(k => k.Id_Producto == prod.Id.ToString());
                }
                else
                {
                    query = query.Where(k => k.Id_Producto == "0");
                }
            }

            if (!string.IsNullOrEmpty(tipoMovimiento) && tipoMovimiento != "TODOS")
            {
                query = query.Where(k => k.TipoMovimiento == tipoMovimiento);
            }

            if (desde.HasValue)
            {
                query = query.Where(k => k.FechaRegistro >= desde.Value.Date);
            }

            if (hasta.HasValue)
            {
                query = query.Where(k => k.FechaRegistro < hasta.Value.Date.AddDays(1));
            }

            if (idBodega.HasValue)
            {
                query = query.Where(k => k.Id_Bodega == idBodega);
            }

            var movimientos = await query.OrderByDescending(k => k.FechaRegistro).ToListAsync();

            ViewBag.Productos = await _context.Productos.AsNoTracking().OrderBy(p => p.Nombre).ToListAsync();
            ViewBag.Bodegas = await _context.Bodegas.AsNoTracking().Where(b => b.Estado).ToListAsync();
            ViewBag.Usuarios = await _context.Usuarios.AsNoTracking().ToDictionaryAsync(u => u.Id, u => u.Login ?? "N/A");
            ViewBag.StockPorBodega = (await _context.StockBodegas.AsNoTracking().ToListAsync())
                .GroupBy(sb => sb.Id_Producto)
                .ToDictionary(g => g.Key ?? "", g => g.ToList());
            ViewBag.Proveedores = await _context.Proveedores.AsNoTracking().ToDictionaryAsync(pv => pv.Id, pv => pv.Nombre ?? "N/A");
            ViewBag.ProveedoresList = await _context.Proveedores.AsNoTracking().OrderBy(pv => pv.Nombre).ToListAsync();

            // Pre-populate filter names for display in print header
            if (!string.IsNullOrEmpty(idProducto))
            {
                var p = await _context.Productos.FindAsync(long.Parse(idProducto));
                ViewBag.FilteredProducto = p?.Nombre;
            }
            if (idBodega.HasValue)
            {
                var b = await _context.Bodegas.FindAsync(idBodega.Value);
                ViewBag.FilteredBodega = b?.Nombre;
            }
            ViewBag.FilteredTipoMovimiento = tipoMovimiento;
            ViewBag.FilteredDesde = desde?.ToString("dd/MM/yyyy");
            ViewBag.FilteredHasta = hasta?.ToString("dd/MM/yyyy");
            ViewBag.FilteredLaboratorio = laboratorio;
            ViewBag.FilterLaboratorio = laboratorio;

            // Load company parameters for report header
            var parametros = await _context.ParametrosSistema.AsNoTracking().FirstOrDefaultAsync();
            ViewBag.Parametros = parametros;

            return View(movimientos);
        }


        public async Task<IActionResult> Ticket(long? id)
        {
            if (id == null) return NotFound();

            var venta = await _context.Ventas.FindAsync(id);
            if (venta == null) return NotFound();

            var cliente = await _context.Clientes.FindAsync((long?)venta.Id_Cliente);
            ViewBag.Cliente = cliente;

            var detalles = await _context.VentasDetalle
                .Where(d => d.Id_Venta == id)
                .ToListAsync();

            ViewBag.Detalles = detalles;

            // Load system parameters for ticket customization
            var parametros = await _context.ParametrosSistema.AsNoTracking().FirstOrDefaultAsync();
            ViewBag.Parametros = parametros;

            // Load cashier name
            var userId = venta.Id_Usuario;
            var cajero = userId.HasValue
                ? (await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == userId.Value))?.Login
                : null;
            ViewBag.Cajero = cajero ?? User.Identity?.Name;

            return View(venta);
        }

        public async Task<IActionResult> POS()
        {
            var idUser = await GetCurrentUserId();
            var localToday = GetUserLocalDate();
            var nextDay = localToday.AddDays(1);
            var apertura = await _context.AperturasCaja
                .FirstOrDefaultAsync(a => a.UsuarioId == idUser && a.FechaApertura >= localToday && a.FechaApertura < nextDay);

            var tieneCaja = apertura != null;
            var cierreExistente = apertura != null ? await _context.CierresCaja.AsNoTracking().FirstOrDefaultAsync(c => c.AperturaCajaId == apertura.Id) : null;
            var cajaCerrada = cierreExistente != null;

            var user = await _context.Usuarios.FindAsync(idUser);
            var isAdmin = user?.Tipo_Usuario == "ADMINISTRADOR" || user?.Tipo_Usuario == "ADMIN";

            var sriSettings = await _context.ConfiguracionSRI.AsNoTracking().FirstOrDefaultAsync();
            var parametros = await _context.ParametrosSistema.AsNoTracking().FirstOrDefaultAsync();
            ViewBag.MonedaSimbolo        = await GetMonedaSimbolo(parametros?.MonedaOficial);
            ViewBag.PosLayout            = parametros?.PosLayout ?? "imagenes";
            ViewBag.StockMinimoCargar    = parametros?.StockMinimoCargar ?? 1;

            ViewBag.CajaAbierta          = tieneCaja;
            ViewBag.CajaCerrada          = cajaCerrada;
            ViewBag.IsAdmin              = isAdmin;
            ViewBag.CierreId             = cierreExistente?.Id;
            ViewBag.MontoMinimoCF        = sriSettings?.MontoMinimoConsumidorFinal ?? 50m;
            ViewBag.ActivarEnvioSRI      = sriSettings?.ActivarEnvioDesdeVentas ?? false;
            ViewBag.Clientes             = await _context.Clientes.ToListAsync();
            ViewBag.Proveedores          = await _context.Proveedores.ToDictionaryAsync(p => p.Id, p => p.Nombre);
            ViewBag.Bodegas              = await _context.Bodegas.Where(b => b.Estado).ToListAsync();
            ViewBag.Presentaciones       = await _context.Presentaciones.AsNoTracking().ToListAsync();

            // Cargar reglas de descuentos activas y vigentes
            var hoy = DateTime.Today;
            var lineasActivas = await _context.DescuentosLinea
                .Where(l => l.Estado && l.Vence_Desde <= hoy && l.Vence_Hasta >= hoy)
                .ToListAsync();
            var lineasActivasIds = lineasActivas.Select(l => l.Id).ToList();
            var lineasDetallesActivos = await _context.DescuentosLineaDetalle
                .Where(d => lineasActivasIds.Contains(d.Id_Descuento_Linea))
                .ToListAsync();

            var combosActivos = await _context.DescuentosCombo
                .Where(c => c.Estado && c.Vence_Desde <= hoy && c.Vence_Hasta >= hoy)
                .ToListAsync();
            var combosActivosIds = combosActivos.Select(c => c.Id).ToList();
            var combosDetallesActivos = await _context.DescuentosComboProductos
                .Where(d => combosActivosIds.Contains(d.Id_Descuento_Combo))
                .ToListAsync();

            var proveedoresDescuentosActivos = await _context.DescuentosProveedor
                .Where(dp => dp.Estado && dp.Vence_Desde <= hoy && dp.Vence_Hasta >= hoy)
                .ToListAsync();

            ViewBag.LineasActivas = lineasActivas;
            ViewBag.LineasDetallesActivos = lineasDetallesActivos;
            ViewBag.CombosActivos = combosActivos;
            ViewBag.CombosDetallesActivos = combosDetallesActivos;
            ViewBag.DescuentosProveedorActivos = proveedoresDescuentosActivos;

            var stockMinimo = parametros?.StockMinimoCargar ?? 1;
            ViewBag.Productos = await _context.Productos
                .Where(p => p.Estado == "Activo" && p.Disponible >= stockMinimo)
                .ToListAsync();

            ViewBag.DiasAlertaVencimiento = parametros?.DiasAlertaVencimiento ?? 30;
            ViewBag.ClaveAprobacionVencido = parametros?.ClaveAprobacionVencido;

            // Cargar stock por bodega para validación en tiempo real en frontend
            ViewBag.StockBodegas = await _context.StockBodegas.AsNoTracking().ToListAsync();

            // Cargar bancos para formulario de cobro
            ViewBag.Bancos = await _context.Bancos.AsNoTracking().ToListAsync();

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> FinalizarVenta([FromBody] VentaRequest request)
        {
            if (request == null || !request.Detalles.Any())
                return Json(new { success = false, message = "La venta no tiene productos." });

            var localNow = GetUserLocalDateTime();
            var localToday = localNow.Date;

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 0. Validar Caja y Usuario
                var idUser = await GetCurrentUserId();
                var nextDay = localToday.AddDays(1);
                var apertura = await _context.AperturasCaja
                    .FirstOrDefaultAsync(a => a.UsuarioId == idUser && a.FechaApertura >= localToday && a.FechaApertura < nextDay);

                if (apertura == null)
                {
                    return Json(new { success = false, message = "No tiene una apertura de caja activa para hoy." });
                }

                var cajaCerrada = await _context.CierresCaja.AnyAsync(c => c.AperturaCajaId == apertura.Id);
                if (cajaCerrada)
                {
                    return Json(new { success = false, message = "Su caja ya fue cerrada. No puede facturar hasta el día siguiente." });
                }

                // 1. Cabecera de Venta (ID Manual ya que la DB no es IDENTITY)
                long nextVentaId = (await _context.Ventas.AnyAsync()) ? await _context.Ventas.MaxAsync(v => v.Id) + 1 : 1;

                var venta = new VentaOperacion {
                    Id = nextVentaId,
                    Id_Cliente = request.IdCliente,
                    FechaHora_Registro = localNow,
                    Procesado = 1,
                    NumDoc = "Ticket-" + localNow.Ticks.ToString().Substring(10),
                    Tipo_Venta = 1,
                    Id_Usuario = idUser,
                    // Datos del comprador ingresados manualmente (Consumidor Final > monto mínimo)
                    NameClienteTem = request.ClienteExtra?.Nombre,
                    CF_Cedula      = request.ClienteExtra?.Cedula,
                    CF_Direccion   = request.ClienteExtra?.Direccion
                };

                _context.Ventas.Add(venta);
                await _context.SaveChangesAsync(); 

                decimal totalVenta = 0;
                var detallesAgrupados = new Dictionary<string, Venta_Producto>();

                foreach (var item in request.Detalles)
                {
                    var producto = await _context.Productos.FindAsync((long)item.IdProducto);
                    
                    // Cálculo de cantidad real basada en si es caja o detalle
                    int cantidadReal = item.EsCaja && (producto?.Contiene ?? 0) > 0 ? item.Cantidad * (producto!.Contiene ?? 1) : item.Cantidad;

                    if (producto == null)
                    {
                        await transaction.RollbackAsync();
                        return Json(new { success = false, message = $"Producto no encontrado." });
                    }

                    if (producto.Disponible < cantidadReal)
                    {
                        await transaction.RollbackAsync();
                        return Json(new { success = false, message = $"Stock insuficiente para {item.NombreProducto}." });
                    }

                    // Validar Vencimiento (30 días)
                    if (producto.Vencimiento.HasValue && producto.Vencimiento.Value < localToday.AddDays(30))
                    {
                        await transaction.RollbackAsync();
                        return Json(new { success = false, message = $"El producto {item.NombreProducto} no se puede vender porque está próximo a vencer o ya venció." });
                    }

                    // Obtener Stock por Bodega y Validar estrictamente que haya suficiente stock en la bodega seleccionada
                    var stockBodega = _context.StockBodegas.Local.FirstOrDefault(sb => sb.Id_Bodega == item.IdBodega && sb.Id_Producto == item.IdProducto.ToString());
                    if (stockBodega == null)
                    {
                        stockBodega = await _context.StockBodegas.FirstOrDefaultAsync(sb => sb.Id_Bodega == item.IdBodega && sb.Id_Producto == item.IdProducto.ToString());
                    }

                    long stockActualEnBodega = stockBodega?.Stock ?? 0;
                    if (stockActualEnBodega < cantidadReal)
                    {
                        var bodegaNom = (await _context.Bodegas.FindAsync(item.IdBodega))?.Nombre ?? $"Bodega #{item.IdBodega}";
                        await transaction.RollbackAsync();
                        return Json(new { success = false, message = $"Stock insuficiente en la bodega '{bodegaNom}' para {item.NombreProducto} (Requerido: {cantidadReal}, Disponible: {stockActualEnBodega})." });
                    }

                    if (stockBodega == null)
                    {
                        stockBodega = new StockBodega { Id_Bodega = item.IdBodega, Id_Producto = item.IdProducto.ToString(), Stock = 0 };
                        _context.StockBodegas.Add(stockBodega);
                    }
                    stockBodega.Stock -= cantidadReal;

                    // Actualizar Stock General (indiferente a la bodega)
                    producto.Disponible -= cantidadReal;
                    _context.Productos.Update(producto);

                    // Kardex
                    _context.Kardex.Add(new Stock_Producto {
                        IdVenta = (int)venta.Id,
                        Id_Producto = item.IdProducto.ToString(),
                        Producto = (item.EsCaja ? "[CAJA] " : "[DETALLE] ") + item.NombreProducto,
                        Cantidad = -cantidadReal,
                        PVF = producto.PVF,
                        TipoMovimiento = "VENTA (FACTURA)",
                        Estado = true,
                        FechaRegistro = localNow,
                        Stock = (int?)stockBodega.Stock,
                        Id_Bodega = item.IdBodega,
                        Id_Usuario = idUser
                    });

                    // Agrupar los detalles para evitar la colisión de clave primaria compuesta en Ventas_Productos
                    string key = item.IdProducto.ToString();
                    if (!detallesAgrupados.TryGetValue(key, out var detalleExistente))
                    {
                        detalleExistente = new Venta_Producto {
                            Id_Venta = venta.Id,
                            Id_Producto = key,
                            Producto = (item.EsCaja ? "[CAJA] " : "[DETALLE] ") + producto.Nombre,
                            Cantidad = (long)item.Cantidad,
                            PVP = item.Precio,
                            Subtotal = item.Subtotal,
                            Total_Pagar = item.Total,
                            IVA = item.Iva,
                            Descuento = 0,
                            Ganancia = (item.Precio - (producto.PVF ?? 0)) * item.Cantidad,
                            Contiene = (long?)(producto.Contiene ?? 1),
                            Disponible = (long?)producto.Disponible,
                            Parcial = item.Total,
                            Lote = producto.Lote
                        };
                        detallesAgrupados.Add(key, detalleExistente);
                    }
                    else
                    {
                        detalleExistente.Cantidad += (long)item.Cantidad;
                        detalleExistente.Subtotal += item.Subtotal;
                        detalleExistente.Total_Pagar += item.Total;
                        detalleExistente.IVA += item.Iva;
                        detalleExistente.Ganancia += (item.Precio - (producto.PVF ?? 0)) * item.Cantidad;
                        detalleExistente.Parcial += item.Total;
                        
                        bool containsCaja = (detalleExistente.Producto ?? "").StartsWith("[CAJA]") || item.EsCaja;
                        bool containsDetalle = (detalleExistente.Producto ?? "").StartsWith("[DETALLE]") || !item.EsCaja;
                        string prefix = (containsCaja && containsDetalle) ? "[MIXTO] " : (containsCaja ? "[CAJA] " : "[DETALLE] ");
                        detalleExistente.Producto = prefix + producto.Nombre;

                        if (detalleExistente.Cantidad > 0)
                        {
                            detalleExistente.PVP = detalleExistente.Total_Pagar / (decimal)detalleExistente.Cantidad;
                        }
                        
                        detalleExistente.Disponible = (long?)producto.Disponible;
                    }

                    totalVenta += item.Total;
                }

                // Agregar los detalles agrupados al contexto
                foreach (var det in detallesAgrupados.Values)
                {
                    _context.VentasDetalle.Add(det);
                }

                await _context.SaveChangesAsync();

                // 2. Registrar en Cuentas por Cobrar (CxC) y/o Abonos de Pago
                if (request.Pagos != null && request.Pagos.Any())
                {
                    foreach (var p in request.Pagos)
                    {
                        if (p.Monto <= 0) continue;

                        if (p.Metodo.Equals("CREDITO", StringComparison.OrdinalIgnoreCase))
                        {
                            int diasPago = p.DiasCredito > 0 ? p.DiasCredito.Value : 30;
                            var cxc = new FacturasPorCobrar
                            {
                                Num_Venta = venta.Id,
                                Factura = venta.NumDoc,
                                Id_Cliente = request.IdCliente,
                                Fecha_Emision = localToday,
                                DiasPago = diasPago,
                                Fecha_Vencimiento = localToday.AddDays(diasPago),
                                Monto_Fact = p.Monto,
                                Monto_NC = 0,
                                Monto_RET = 0,
                                Monto_TotalPagar = p.Monto,
                                Monto_Abonado = 0,
                                Monto_Saldo = p.Monto,
                                Estado = "PENDIENTE",
                                Retencion_Renta = false,
                                Retencion_IVA = false,
                                ORIGEN_FACTURA = "POS",
                                Monto_RET_IVA = 0
                            };
                            _context.FacturasPorCobrar.Add(cxc);
                        }
                        else
                        {
                            int tipoPago = 1; // 1 = Efectivo
                            if (p.Metodo.Equals("TARJETA DE CREDITO", StringComparison.OrdinalIgnoreCase)) tipoPago = 2;
                            if (p.Metodo.Equals("TRANSFERENCIA", StringComparison.OrdinalIgnoreCase)) tipoPago = 3;
                            if (p.Metodo.Equals("CHEQUE", StringComparison.OrdinalIgnoreCase)) tipoPago = 4;

                            var abono = new FacturasPorCobrarAbono
                            {
                                Id_Usuario = idUser,
                                Id_Venta = venta.Id,
                                Fecha_Cobro = localToday,
                                Monto_Efectivo = p.Monto,
                                FechaHora_Reg = localNow,
                                Tipo_Pago = tipoPago,
                                Num_Doc = p.Referencia,
                                Id_Banco = p.IdBanco,
                                Monto_Multipago = p.Monto,
                                Imagen_Doc = p.ImagenCheque ?? "",
                                Adquirente = p.Adquirente ?? "",
                                Interes_MultiPago = p.Interes
                            };
                            _context.FacturasPorCobrarAbonos.Add(abono);
                        }
                    }
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // Fallback para pago único tradicional
                    if (request.Pago != null && request.Pago.MontoCredito > 0)
                    {
                        int diasPago = request.Pago.DiasCredito > 0 ? request.Pago.DiasCredito : 30;

                        var cxc = new FacturasPorCobrar
                        {
                            Num_Venta = venta.Id,
                            Factura = venta.NumDoc,
                            Id_Cliente = request.IdCliente,
                            Fecha_Emision = localToday,
                            DiasPago = diasPago,
                            Fecha_Vencimiento = localToday.AddDays(diasPago),
                            Monto_Fact = request.Pago.MontoCredito,
                            Monto_NC = 0,
                            Monto_RET = 0,
                            Monto_TotalPagar = request.Pago.MontoCredito,
                            Monto_Abonado = 0,
                            Monto_Saldo = request.Pago.MontoCredito,
                            Estado = "PENDIENTE",
                            Retencion_Renta = false,
                            Retencion_IVA = false,
                            ORIGEN_FACTURA = "POS",
                            Monto_RET_IVA = 0
                        };
                        _context.FacturasPorCobrar.Add(cxc);
                        await _context.SaveChangesAsync();
                    }

                    if (request.Pago != null && request.Pago.Recibido > request.Pago.Cambio)
                    {
                        decimal montoPagado = request.Pago.Recibido - request.Pago.Cambio;
                        int tipoPago = 1; // 1=Efectivo
                        if (request.Pago.Metodo == "19") tipoPago = 2; // Tarjeta (Sri 19)
                        if (request.Pago.Metodo == "20") tipoPago = 3; // Transferencia (Sri 20)
                        
                        var abono = new FacturasPorCobrarAbono
                        {
                            Id_Usuario = idUser,
                            Id_Venta = venta.Id,
                            Fecha_Cobro = DateTime.Today,
                            Monto_Efectivo = montoPagado,
                            FechaHora_Reg = DateTime.Now,
                            Tipo_Pago = tipoPago,
                            Num_Doc = request.Pago.Referencia,
                            Id_Banco = null,
                            Monto_Multipago = montoPagado,
                            Imagen_Doc = "",
                            Adquirente = "",
                            Interes_MultiPago = 0
                        };
                        _context.FacturasPorCobrarAbonos.Add(abono);
                        await _context.SaveChangesAsync();
                    }
                }

                await transaction.CommitAsync();

                return Json(new { success = true, message = "Venta procesada con éxito.", idVenta = venta.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                var msg = ex.Message;
                if (ex.InnerException != null) msg += " | " + ex.InnerException.Message;
                return Json(new { success = false, message = "Error en venta: " + msg });
            }
            }); // fin ExecutionStrategy
        }

        public class VentaRequest {
            public long IdCliente { get; set; }
            public ClienteExtraData? ClienteExtra { get; set; }
            public List<VentaDetalleRequest> Detalles { get; set; } = new();
            public PagoRequest? Pago { get; set; } // Información del cobro
            public List<PagoMetodoRequest> Pagos { get; set; } = new(); // Multi-pagos!
        }

        public class PagoMetodoRequest {
            public string Metodo { get; set; } = "CONTADO"; // CONTADO, TARJETA DE CREDITO, CHEQUE, CREDITO, TRANSFERENCIA
            public decimal Monto { get; set; }
            public string? Referencia { get; set; } // Num_Doc
            public int? IdBanco { get; set; } // Banco
            public string? Adquirente { get; set; }
            public decimal Interes { get; set; }
            public int? DiasCredito { get; set; }
            public string? ImagenCheque { get; set; } // base64 representation of cheque image if any
        }

        public class PagoRequest {
            public string Metodo { get; set; } = "01";
            public decimal Recibido { get; set; }
            public decimal Cambio { get; set; }
            public string? Referencia { get; set; } // Número de voucher o transferencia
            public decimal MontoCredito { get; set; } // Monto a crédito
            public int DiasCredito { get; set; } // Plazo en días del crédito
        }

        public class ClienteExtraData {
            public string? Cedula    { get; set; }
            public string? Nombre    { get; set; }
            public string? Direccion { get; set; }
        }

        public class VentaDetalleRequest {
            public long IdProducto { get; set; }
            public string? NombreProducto { get; set; }
            public int Cantidad { get; set; }
            public decimal Precio { get; set; }
            public decimal Subtotal { get; set; }
            public decimal Iva { get; set; }
            public decimal Total { get; set; }
            public bool EsCaja { get; set; }
            public int IdBodega { get; set; }
        }
        [HttpGet]
        public async Task<IActionResult> GetStockPorBodega(long idProducto)
        {
            var bodegas = await _context.Bodegas.Where(b => b.Estado).ToListAsync();
            var stocks = await _context.StockBodegas
                .Where(sb => sb.Id_Producto == idProducto.ToString())
                .ToListAsync();

            var data = bodegas.Select(b => new {
                Nombre = b.Nombre,
                EsPrincipal = b.EsPrincipal,
                Stock = stocks.Where(s => s.Id_Bodega == b.Id).Sum(s => s.Stock)
            });

            return Json(data);
        }
        [HttpGet]
        public async Task<IActionResult> GetStockByProduct(long id)
        {
            string idStr = id.ToString();
            var stock = await (from s in _context.StockBodegas
                               join b in _context.Bodegas on s.Id_Bodega equals b.Id
                               where s.Id_Producto == idStr
                               group s by new { b.Nombre } into g
                               select new {
                                   bodega = g.Key.Nombre,
                                   stock = g.Sum(x => x.Stock)
                               }).ToListAsync();
            return Json(stock);
        }

        [HttpPost]
        public async Task<IActionResult> ValidarPasswordPrecio([FromBody] ValidarClaveRequest req)
        {
            try
            {
                var settings = await _context.ConfiguracionSRI.AsNoTracking().FirstOrDefaultAsync();
                if (settings == null || string.IsNullOrWhiteSpace(settings.ClaveMaestra))
                    return Json(new { success = false, message = "No hay clave maestra configurada. Configure la Clave Maestra en Ajustes SRI." });

                bool ok = settings.ClaveMaestra.Trim() == (req.Clave ?? "").Trim();
                return Json(new { success = ok, message = ok ? "Autorizado" : "Clave incorrecta" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ValidarPasswordVencido([FromBody] ValidarClaveRequest req)
        {
            try
            {
                var parametros = await _context.ParametrosSistema.AsNoTracking().FirstOrDefaultAsync();
                var claveConfigurada = parametros?.ClaveAprobacionVencido ?? "";

                if (string.IsNullOrWhiteSpace(claveConfigurada))
                {
                    var settings = await _context.ConfiguracionSRI.AsNoTracking().FirstOrDefaultAsync();
                    claveConfigurada = settings?.ClaveMaestra ?? "";
                }

                if (string.IsNullOrWhiteSpace(claveConfigurada))
                    return Json(new { success = false, message = "No hay clave de aprobación configurada en Parámetros del Sistema." });

                bool ok = claveConfigurada.Trim() == (req.Clave ?? "").Trim();
                return Json(new { success = ok, message = ok ? "Autorizado" : "Clave incorrecta" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetProductDetails(long id)
        {
            try
            {
                var producto = await _context.Productos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
                if (producto == null)
                {
                    return Json(new { success = false, message = "Producto no encontrado." });
                }

                // Obtener datos adicionales del producto (JSON)
                var datosAdicionales = _camposService.ParseDatosAdicionales(producto.DatosAdicionales);

                // Resolver EmpresaId y RubroId del usuario autenticado
                var empresaIdClaim = User.FindFirst("EmpresaId")?.Value;
                int? empresaId = string.IsNullOrEmpty(empresaIdClaim) ? null : int.Parse(empresaIdClaim);

                int? rubroId = null;
                if (empresaId.HasValue)
                {
                    var empresa = await _adminContext.Empresas.FindAsync(empresaId.Value);
                    rubroId = empresa?.RubroId;
                }

                // Cargar campos personalizados de tipo 'Producto' configurados para la empresa o rubro actual
                var campos = await _camposService.GetCamposActivosAsync("Producto", rubroId, empresaId);

                // Mapear campos dinámicamente usando una proyección LINQ segura
                var especificaciones = campos
                    .Select(campo => {
                        if (datosAdicionales.TryGetValue(campo.NombreCampo, out var valorObj) && valorObj != null)
                        {
                            var valorStr = valorObj.ToString();
                            if (!string.IsNullOrWhiteSpace(valorStr))
                            {
                                if (campo.TipoDato.Equals("Booleano", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (bool.TryParse(valorStr, out bool boolVal))
                                    {
                                        valorStr = boolVal ? "Sí" : "No";
                                    }
                                    else if (valorStr == "1" || valorStr.Equals("true", StringComparison.OrdinalIgnoreCase))
                                    {
                                        valorStr = "Sí";
                                    }
                                    else
                                    {
                                        valorStr = "No";
                                    }
                                }
                                return new { Etiqueta = campo.Etiqueta, Valor = valorStr, Orden = campo.OrdenVisualizacion };
                            }
                        }
                        return null;
                    })
                    .Where(e => e != null)
                    .OrderBy(e => e!.Orden)
                    .Select(e => new { etiqueta = e!.Etiqueta, valor = e!.Valor })
                    .ToList();

                // Obtener información de la presentación y proveedor
                var presentacion = producto.Id_Presentacion.HasValue 
                    ? (await _context.Presentaciones.AsNoTracking().FirstOrDefaultAsync(pr => pr.Id == producto.Id_Presentacion.Value))?.Nombre 
                    : null;
                var proveedor = producto.Id_Proveedor.HasValue 
                    ? (await _context.Proveedores.AsNoTracking().FirstOrDefaultAsync(pv => pv.Id == producto.Id_Proveedor.Value))?.Nombre 
                    : null;

                // Obtener productos similares (misma presentación/categoría, activos, excluyendo el actual)
                object similares;
                if (producto.Id_Presentacion.HasValue)
                {
                    similares = await _context.Productos.AsNoTracking()
                        .Where(p => p.Id != id && p.Id_Presentacion == producto.Id_Presentacion.Value && p.Estado == "Activo")
                        .Take(5)
                        .Select(p => new {
                            id = p.Id,
                            nombre = p.Nombre ?? "",
                            pvp = p.PVP ?? 0m,
                            codigoBarra = p.CodigoBarra ?? ""
                        })
                        .ToListAsync();
                }
                else
                {
                    similares = new List<object>();
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        id = producto.Id,
                        nombre = producto.Nombre,
                        codigoBarra = producto.CodigoBarra ?? "N/A",
                        presentacion = presentacion ?? "N/A",
                        proveedor = proveedor ?? "N/A",
                        registroSanitario = producto.RegistroSanitario ?? "N/A",
                        pvp = producto.PVP ?? 0m,
                        pvf = producto.PVF ?? 0m,
                        disponible = producto.Disponible ?? 0,
                        contiene = producto.Contiene ?? 1,
                        especificaciones = especificaciones,
                        similares = similares
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al obtener detalles: " + ex.Message });
            }
        }

        public class ValidarClaveRequest
        {
            public string? Clave { get; set; }
        }
    }
}
