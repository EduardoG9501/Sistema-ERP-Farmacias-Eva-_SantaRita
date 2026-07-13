using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaERP.Models;
using SistemaERP.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SistemaERP.Areas.Admin.Models;
using SistemaERP.Areas.Admin.Services;

namespace SistemaERP.Controllers
{
    public class ReportesController : Controller
    {
        private readonly ScmContext _context;
        private readonly AdminContext _adminContext;
        private readonly ICamposPersonalizadosService _camposService;

        public ReportesController(
            ScmContext context,
            AdminContext adminContext,
            ICamposPersonalizadosService camposService)
        {
            _context = context;
            _adminContext = adminContext;
            _camposService = camposService;
        }

        public async Task<IActionResult> Index()
        {
            var parametros = await _context.ParametrosSistema.AsNoTracking().FirstOrDefaultAsync(p => p.Id == 1);
            ViewBag.Parametros = parametros;
            ViewBag.MonedaSimbolo = await GetMonedaSimbolo(parametros?.MonedaOficial);

            return View();
        }

        public async Task<IActionResult> VentasDiarias(DateTime? desde, DateTime? hasta, bool multiPago = false, bool detalleProducto = false)
        {
            if (desde == null && hasta == null)
            {
                ViewBag.InitialLoad = true;
                ViewBag.Desde = DateTime.Today.ToString("yyyy-MM-dd");
                ViewBag.Hasta = DateTime.Today.ToString("yyyy-MM-dd");
                var emptyModel = new VentasDiariasReportViewModel
                {
                    Desde = DateTime.Today,
                    Hasta = DateTime.Today,
                    MultiPago = false,
                    DetalleProducto = false
                };
                var emptyParams = await _context.ParametrosSistema.AsNoTracking().FirstOrDefaultAsync(p => p.Id == 1);
                ViewBag.Parametros = emptyParams;
                ViewBag.MonedaSimbolo = await GetMonedaSimbolo(emptyParams?.MonedaOficial);
                return View(emptyModel);
            }

            ViewBag.InitialLoad = false;
            var dateDesde = desde.Value;
            var dateHasta = hasta.Value;
            
            var start = dateDesde.Date;
            var end = dateHasta.Date.AddDays(1);
            
            var ventas = await _context.Ventas
                .AsNoTracking()
                .Where(v => v.FechaHora_Registro >= start && v.FechaHora_Registro < end)
                .OrderBy(v => v.FechaHora_Registro)
                .ToListAsync();

            var ventaIds = ventas.Select(v => v.Id).ToList();
            
            var detalles = await _context.VentasDetalle
                .AsNoTracking()
                .Where(d => ventaIds.Contains(d.Id_Venta))
                .ToListAsync();

            var userIds = ventas.Where(v => v.Id_Usuario.HasValue).Select(v => v.Id_Usuario!.Value).Distinct().ToList();
            var usuarios = await _context.Usuarios.AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Login ?? "Desconocido");

            var clienteIds = ventas.Where(v => v.Id_Cliente.HasValue).Select(v => v.Id_Cliente!.Value).Distinct().ToList();
            var clientes = await _context.Clientes.AsNoTracking()
                .Where(c => clienteIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Nombres_Apellidos ?? "Consumidor Final");

            var abonos = await _context.FacturasPorCobrarAbonos
                .AsNoTracking()
                .Where(a => ventaIds.Contains(a.Id_Venta))
                .ToListAsync();

            var creditos = await _context.FacturasPorCobrar
                .AsNoTracking()
                .Where(c => ventaIds.Contains(c.Num_Venta))
                .ToListAsync();

            var detallesGrouped = detalles.GroupBy(d => d.Id_Venta).ToDictionary(
                g => g.Key,
                g => new {
                    Total = g.Sum(d => d.Total_Pagar ?? 0),
                    Ganancia = g.Sum(d => d.Ganancia ?? 0),
                    Productos = g.ToList()
                }
            );

            var rows = new List<VentaReportRowViewModel>();
            foreach (var v in ventas)
            {
                string clienteNom = "Consumidor Final";
                if (v.Id_Cliente.HasValue && clientes.TryGetValue(v.Id_Cliente.Value, out var cli))
                {
                    clienteNom = cli;
                }
                else if (!string.IsNullOrEmpty(v.NameClienteTem))
                {
                    clienteNom = v.NameClienteTem;
                }

                detallesGrouped.TryGetValue(v.Id, out var det);

                var saleAbonos = abonos.Where(a => a.Id_Venta == v.Id).ToList();
                var saleCredits = creditos.Where(c => c.Num_Venta == v.Id).ToList();

                if (multiPago && (saleAbonos.Count + saleCredits.Count) > 1)
                {
                    // Split into multiple rows for each payment method
                    foreach (var ab in saleAbonos)
                    {
                        string tPago = ab.Tipo_Pago switch
                        {
                            1 => "EFECTIVO",
                            2 => "TARJETA",
                            3 => "TRANSFERENCIA",
                            4 => "CHEQUE",
                            _ => "CONTADO"
                        };

                        decimal propGanancia = 0;
                        if (det?.Total > 0)
                        {
                            propGanancia = (ab.Monto_Efectivo / det.Total) * det.Ganancia;
                        }

                        rows.Add(new VentaReportRowViewModel
                        {
                            Id = v.Id,
                            NumDoc = v.NumDoc ?? "N/A",
                            Fecha = v.FechaHora_Registro ?? DateTime.Today,
                            Cliente = clienteNom,
                            TipoPago = tPago,
                            TotalPagado = ab.Monto_Efectivo,
                            Ganancia = propGanancia,
                            DetalleProductos = det?.Productos ?? new List<Venta_Producto>()
                        });
                    }

                    foreach (var cr in saleCredits)
                    {
                        decimal propGanancia = 0;
                        if (det?.Total > 0)
                        {
                            propGanancia = (cr.Monto_Fact / det.Total) * det.Ganancia;
                        }

                        rows.Add(new VentaReportRowViewModel
                        {
                            Id = v.Id,
                            NumDoc = v.NumDoc ?? "N/A",
                            Fecha = v.FechaHora_Registro ?? DateTime.Today,
                            Cliente = clienteNom,
                            TipoPago = "CREDITO",
                            TotalPagado = cr.Monto_Fact,
                            Ganancia = propGanancia,
                            DetalleProductos = det?.Productos ?? new List<Venta_Producto>()
                        });
                    }
                }
                else
                {
                    // Single row per sale
                    string tipoPago = "CONTADO";
                    if (saleCredits.Any())
                    {
                        tipoPago = "CREDITO";
                    }
                    else if (saleAbonos.Count > 1)
                    {
                        tipoPago = "MULTI-PAGO";
                    }
                    else
                    {
                        var ab = saleAbonos.FirstOrDefault();
                        if (ab != null)
                        {
                            tipoPago = ab.Tipo_Pago switch
                            {
                                1 => "EFECTIVO",
                                2 => "TARJETA",
                                3 => "TRANSFERENCIA",
                                4 => "CHEQUE",
                                _ => "CONTADO"
                            };
                        }
                    }

                    rows.Add(new VentaReportRowViewModel
                    {
                        Id = v.Id,
                        NumDoc = v.NumDoc ?? "N/A",
                        Fecha = v.FechaHora_Registro ?? DateTime.Today,
                        Cliente = clienteNom,
                        TipoPago = tipoPago,
                        TotalPagado = det?.Total ?? 0,
                        Ganancia = det?.Ganancia ?? 0,
                        DetalleProductos = det?.Productos ?? new List<Venta_Producto>()
                    });
                }
            }

            // Agrupar por despachador
            var ventasPorDespachador = new List<VentaGrupoDespachadorViewModel>();
            var agrupado = rows.GroupBy(r => {
                var sale = ventas.FirstOrDefault(v => v.Id == r.Id);
                if (sale?.Id_Usuario != null && usuarios.TryGetValue(sale.Id_Usuario.Value, out var login))
                {
                    return login;
                }
                return "Desconocido";
            });

            foreach (var g in agrupado)
            {
                ventasPorDespachador.Add(new VentaGrupoDespachadorViewModel
                {
                    Despachador = g.Key,
                    Ventas = g.OrderBy(v => v.Fecha).ToList()
                });
            }

            // Resumen por día
            var resumenPorDia = rows.GroupBy(r => r.Fecha.Date)
                .Select(g => new ResumenDiaViewModel
                {
                    Fecha = g.Key,
                    SeVendio = g.Sum(r => r.TotalPagado),
                    Ganancia = g.Sum(r => r.Ganancia)
                })
                .OrderBy(r => r.Fecha)
                .ToList();

            // Resumen por despachador
            var resumenPorDespachador = ventasPorDespachador.Select(v => new ResumenDespachadorViewModel
            {
                Despachador = v.Despachador,
                SeVendio = v.TotalCobrado,
                Ganancia = v.Ventas.Sum(r => r.Ganancia)
            })
            .OrderByDescending(r => r.SeVendio)
            .ToList();

            var parametros = await _context.ParametrosSistema.AsNoTracking().FirstOrDefaultAsync(p => p.Id == 1);
            ViewBag.Parametros = parametros;
            ViewBag.MonedaSimbolo = await GetMonedaSimbolo(parametros?.MonedaOficial);
            ViewBag.Desde = dateDesde.ToString("yyyy-MM-dd");
            ViewBag.Hasta = dateHasta.ToString("yyyy-MM-dd");

            var model = new VentasDiariasReportViewModel
            {
                Desde = dateDesde,
                Hasta = dateHasta,
                MultiPago = multiPago,
                DetalleProducto = detalleProducto,
                VentasPorDespachador = ventasPorDespachador,
                ResumenPorDia = resumenPorDia,
                ResumenPorDespachador = resumenPorDespachador,
                TotalVentas = rows.Sum(r => r.TotalPagado),
                TotalGanancia = rows.Sum(r => r.Ganancia),
                TotalTransacciones = rows.Count
            };

            return View(model);
        }

        public async Task<IActionResult> VentasDiariasExcel(DateTime desde, DateTime hasta, bool multiPago = false, bool detalleProducto = false)
        {
            var start = desde.Date;
            var end = hasta.Date.AddDays(1);
            
            var ventas = await _context.Ventas
                .AsNoTracking()
                .Where(v => v.FechaHora_Registro >= start && v.FechaHora_Registro < end)
                .OrderBy(v => v.FechaHora_Registro)
                .ToListAsync();

            var ventaIds = ventas.Select(v => v.Id).ToList();
            
            var detalles = await _context.VentasDetalle
                .AsNoTracking()
                .Where(d => ventaIds.Contains(d.Id_Venta))
                .ToListAsync();

            var userIds = ventas.Where(v => v.Id_Usuario.HasValue).Select(v => v.Id_Usuario!.Value).Distinct().ToList();
            var usuarios = await _context.Usuarios.AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Login ?? "Desconocido");

            var clienteIds = ventas.Where(v => v.Id_Cliente.HasValue).Select(v => v.Id_Cliente!.Value).Distinct().ToList();
            var clientes = await _context.Clientes.AsNoTracking()
                .Where(c => clienteIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Nombres_Apellidos ?? "Consumidor Final");

            var abonos = await _context.FacturasPorCobrarAbonos
                .AsNoTracking()
                .Where(a => ventaIds.Contains(a.Id_Venta))
                .ToListAsync();

            var creditos = await _context.FacturasPorCobrar
                .AsNoTracking()
                .Where(c => ventaIds.Contains(c.Num_Venta))
                .ToListAsync();

            var detallesGrouped = detalles.GroupBy(d => d.Id_Venta).ToDictionary(
                g => g.Key,
                g => new {
                    Total = g.Sum(d => d.Total_Pagar ?? 0),
                    Ganancia = g.Sum(d => d.Ganancia ?? 0),
                    Productos = g.ToList()
                }
            );

            var rows = new List<VentaReportRowViewModel>();
            foreach (var v in ventas)
            {
                string clienteNom = "Consumidor Final";
                if (v.Id_Cliente.HasValue && clientes.TryGetValue(v.Id_Cliente.Value, out var cli))
                {
                    clienteNom = cli;
                }
                else if (!string.IsNullOrEmpty(v.NameClienteTem))
                {
                    clienteNom = v.NameClienteTem;
                }

                detallesGrouped.TryGetValue(v.Id, out var det);

                var saleAbonos = abonos.Where(a => a.Id_Venta == v.Id).ToList();
                var saleCredits = creditos.Where(c => c.Num_Venta == v.Id).ToList();

                if (multiPago && (saleAbonos.Count + saleCredits.Count) > 1)
                {
                    // Split into multiple rows for each payment method
                    foreach (var ab in saleAbonos)
                    {
                        string tPago = ab.Tipo_Pago switch
                        {
                            1 => "EFECTIVO",
                            2 => "TARJETA",
                            3 => "TRANSFERENCIA",
                            4 => "CHEQUE",
                            _ => "CONTADO"
                        };

                        decimal propGanancia = 0;
                        if (det?.Total > 0)
                        {
                            propGanancia = (ab.Monto_Efectivo / det.Total) * det.Ganancia;
                        }

                        rows.Add(new VentaReportRowViewModel
                        {
                            Id = v.Id,
                            NumDoc = v.NumDoc ?? "N/A",
                            Fecha = v.FechaHora_Registro ?? DateTime.Today,
                            Cliente = clienteNom,
                            TipoPago = tPago,
                            TotalPagado = ab.Monto_Efectivo,
                            Ganancia = propGanancia,
                            DetalleProductos = det?.Productos ?? new List<Venta_Producto>()
                        });
                    }

                    foreach (var cr in saleCredits)
                    {
                        decimal propGanancia = 0;
                        if (det?.Total > 0)
                        {
                            propGanancia = (cr.Monto_Fact / det.Total) * det.Ganancia;
                        }

                        rows.Add(new VentaReportRowViewModel
                        {
                            Id = v.Id,
                            NumDoc = v.NumDoc ?? "N/A",
                            Fecha = v.FechaHora_Registro ?? DateTime.Today,
                            Cliente = clienteNom,
                            TipoPago = "CREDITO",
                            TotalPagado = cr.Monto_Fact,
                            Ganancia = propGanancia,
                            DetalleProductos = det?.Productos ?? new List<Venta_Producto>()
                        });
                    }
                }
                else
                {
                    // Single row per sale
                    string tipoPago = "CONTADO";
                    if (saleCredits.Any())
                    {
                        tipoPago = "CREDITO";
                    }
                    else if (saleAbonos.Count > 1)
                    {
                        tipoPago = "MULTI-PAGO";
                    }
                    else
                    {
                        var ab = saleAbonos.FirstOrDefault();
                        if (ab != null)
                        {
                            tipoPago = ab.Tipo_Pago switch
                            {
                                1 => "EFECTIVO",
                                2 => "TARJETA",
                                3 => "TRANSFERENCIA",
                                4 => "CHEQUE",
                                _ => "CONTADO"
                            };
                        }
                    }

                    rows.Add(new VentaReportRowViewModel
                    {
                        Id = v.Id,
                        NumDoc = v.NumDoc ?? "N/A",
                        Fecha = v.FechaHora_Registro ?? DateTime.Today,
                        Cliente = clienteNom,
                        TipoPago = tipoPago,
                        TotalPagado = det?.Total ?? 0,
                        Ganancia = det?.Ganancia ?? 0,
                        DetalleProductos = det?.Productos ?? new List<Venta_Producto>()
                    });
                }
            }

            var ventasPorDespachador = new List<VentaGrupoDespachadorViewModel>();
            var agrupado = rows.GroupBy(r => {
                var sale = ventas.FirstOrDefault(v => v.Id == r.Id);
                if (sale?.Id_Usuario != null && usuarios.TryGetValue(sale.Id_Usuario.Value, out var login))
                {
                    return login;
                }
                return "Desconocido";
            });

            foreach (var g in agrupado)
            {
                ventasPorDespachador.Add(new VentaGrupoDespachadorViewModel
                {
                    Despachador = g.Key,
                    Ventas = g.OrderBy(v => v.Fecha).ToList()
                });
            }

            var resumenPorDia = rows.GroupBy(r => r.Fecha.Date)
                .Select(g => new ResumenDiaViewModel
                {
                    Fecha = g.Key,
                    SeVendio = g.Sum(r => r.TotalPagado),
                    Ganancia = g.Sum(r => r.Ganancia)
                })
                .OrderBy(r => r.Fecha)
                .ToList();

            var resumenPorDespachador = ventasPorDespachador.Select(v => new ResumenDespachadorViewModel
            {
                Despachador = v.Despachador,
                SeVendio = v.TotalCobrado,
                Ganancia = v.Ventas.Sum(r => r.Ganancia)
            })
            .OrderByDescending(r => r.SeVendio)
            .ToList();

            var parametros = await _context.ParametrosSistema.AsNoTracking().FirstOrDefaultAsync(p => p.Id == 1);
            ViewBag.Parametros = parametros;
            ViewBag.MonedaSimbolo = await GetMonedaSimbolo(parametros?.MonedaOficial);
            ViewBag.Desde = desde.ToString("yyyy-MM-dd");
            ViewBag.Hasta = hasta.ToString("yyyy-MM-dd");

            var model = new VentasDiariasReportViewModel
            {
                Desde = desde,
                Hasta = hasta,
                MultiPago = multiPago,
                DetalleProducto = detalleProducto,
                VentasPorDespachador = ventasPorDespachador,
                ResumenPorDia = resumenPorDia,
                ResumenPorDespachador = resumenPorDespachador,
                TotalVentas = rows.Sum(r => r.TotalPagado),
                TotalGanancia = rows.Sum(r => r.Ganancia),
                TotalTransacciones = rows.Count
            };

            Response.Headers.Append("Content-Disposition", $"attachment; filename=ReporteVentasDiarias_{desde:yyyyMMdd}_{hasta:yyyyMMdd}.xls");
            Response.ContentType = "application/vnd.ms-excel";
            return View("VentasDiariasExcel", model);
        }

        public async Task<IActionResult> VentasMensuales(DateTime? desde, DateTime? hasta)
        {
            if (desde == null && hasta == null)
            {
                ViewBag.InitialLoad = true;
                ViewBag.Desde = DateTime.Today.ToString("yyyy-MM-dd");
                ViewBag.Hasta = DateTime.Today.ToString("yyyy-MM-dd");
                var emptyModel = new VentasMensualesReportViewModel
                {
                    Desde = DateTime.Today,
                    Hasta = DateTime.Today
                };
                var emptyParams = await _context.ParametrosSistema.AsNoTracking().FirstOrDefaultAsync(p => p.Id == 1);
                ViewBag.Parametros = emptyParams;
                ViewBag.MonedaSimbolo = await GetMonedaSimbolo(emptyParams?.MonedaOficial);
                return View(emptyModel);
            }

            ViewBag.InitialLoad = false;
            var dateDesde = desde.Value;
            var dateHasta = hasta.Value;
            
            var start = dateDesde.Date;
            var end = dateHasta.Date.AddDays(1);
            
            var ventas = await _context.Ventas
                .AsNoTracking()
                .Where(v => v.FechaHora_Registro >= start && v.FechaHora_Registro < end)
                .ToListAsync();

            var ventaIds = ventas.Select(v => v.Id).ToList();
            
            var detalles = await _context.VentasDetalle
                .AsNoTracking()
                .Where(d => ventaIds.Contains(d.Id_Venta))
                .GroupBy(d => d.Id_Venta)
                .Select(g => new { Id_Venta = g.Key, Total = g.Sum(d => d.Total_Pagar ?? 0) })
                .ToDictionaryAsync(x => x.Id_Venta, x => x.Total);

            var ventasConTotal = ventas.Select(v => new {
                Venta = v,
                Total = detalles.TryGetValue(v.Id, out var tot) ? tot : 0
            });

            var agrupadoMes = ventasConTotal
                .GroupBy(v => new { Year = v.Venta.FechaHora_Registro.Value.Year, Month = v.Venta.FechaHora_Registro.Value.Month })
                .Select(g => new {
                    g.Key.Year,
                    g.Key.Month,
                    Total = g.Sum(x => x.Total)
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .Select(x => new VentaMensualGrupoViewModel
                {
                    MesAnio = ObtenerNombreMes(x.Month) + " " + x.Year,
                    TotalCobrado = x.Total
                })
                .ToList();

            var parametros = await _context.ParametrosSistema.AsNoTracking().FirstOrDefaultAsync(p => p.Id == 1);
            ViewBag.Parametros = parametros;
            ViewBag.MonedaSimbolo = await GetMonedaSimbolo(parametros?.MonedaOficial);
            ViewBag.Desde = dateDesde.ToString("yyyy-MM-dd");
            ViewBag.Hasta = dateHasta.ToString("yyyy-MM-dd");

            var model = new VentasMensualesReportViewModel
            {
                Desde = dateDesde,
                Hasta = dateHasta,
                GruposMensuales = agrupadoMes,
                TotalVentas = agrupadoMes.Sum(g => g.TotalCobrado)
            };

            return View(model);
        }

        public async Task<IActionResult> VentasMensualesExcel(DateTime desde, DateTime hasta)
        {
            var dateDesde = desde;
            var dateHasta = hasta;
            
            var start = dateDesde.Date;
            var end = dateHasta.Date.AddDays(1);
            
            var ventas = await _context.Ventas
                .AsNoTracking()
                .Where(v => v.FechaHora_Registro >= start && v.FechaHora_Registro < end)
                .ToListAsync();

            var ventaIds = ventas.Select(v => v.Id).ToList();
            
            var detalles = await _context.VentasDetalle
                .AsNoTracking()
                .Where(d => ventaIds.Contains(d.Id_Venta))
                .GroupBy(d => d.Id_Venta)
                .Select(g => new { Id_Venta = g.Key, Total = g.Sum(d => d.Total_Pagar ?? 0) })
                .ToDictionaryAsync(x => x.Id_Venta, x => x.Total);

            var ventasConTotal = ventas.Select(v => new {
                Venta = v,
                Total = detalles.TryGetValue(v.Id, out var tot) ? tot : 0
            });

            var agrupadoMes = ventasConTotal
                .GroupBy(v => new { Year = v.Venta.FechaHora_Registro.Value.Year, Month = v.Venta.FechaHora_Registro.Value.Month })
                .Select(g => new {
                    g.Key.Year,
                    g.Key.Month,
                    Total = g.Sum(x => x.Total)
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .Select(x => new VentaMensualGrupoViewModel
                {
                    MesAnio = ObtenerNombreMes(x.Month) + " " + x.Year,
                    TotalCobrado = x.Total
                })
                .ToList();

            var parametros = await _context.ParametrosSistema.AsNoTracking().FirstOrDefaultAsync(p => p.Id == 1);
            ViewBag.Parametros = parametros;
            ViewBag.MonedaSimbolo = await GetMonedaSimbolo(parametros?.MonedaOficial);

            var model = new VentasMensualesReportViewModel
            {
                Desde = dateDesde,
                Hasta = dateHasta,
                GruposMensuales = agrupadoMes,
                TotalVentas = agrupadoMes.Sum(g => g.TotalCobrado)
            };

            Response.Headers.Append("Content-Disposition", $"attachment; filename=ReporteVentasMensuales_{desde:yyyyMMdd}_{hasta:yyyyMMdd}.xls");
            Response.ContentType = "application/vnd.ms-excel";
            return View("VentasMensualesExcel", model);
        }

        private static string ObtenerNombreMes(int month)
        {
            return month switch
            {
                1 => "ENERO",
                2 => "FEBRERO",
                3 => "MARZO",
                4 => "ABRIL",
                5 => "MAYO",
                6 => "JUNIO",
                7 => "JULIO",
                8 => "AGOSTO",
                9 => "SEPTIEMBRE",
                10 => "OCTUBRE",
                11 => "NOVIEMBRE",
                12 => "DICIEMBRE",
                _ => ""
            };
        }

        public async Task<IActionResult> ProductosVencidos()
        {
            var query = _context.Productos.AsNoTracking()
                .Where(p => p.Vencimiento.HasValue && p.Vencimiento.Value < DateTime.Today);
                
            var parametros = await _context.ParametrosSistema.AsNoTracking().FirstOrDefaultAsync(p => p.Id == 1);
            ViewBag.Parametros = parametros;
            ViewBag.MonedaSimbolo = await GetMonedaSimbolo(parametros?.MonedaOficial);
            var productos = await query.OrderByDescending(p => p.Vencimiento).ToListAsync();
            return View(productos);
        }

        public async Task<IActionResult> StockReport(string? producto, string? laboratorio, bool? stockNuevo)
        {
            bool filterStockNuevo = stockNuevo ?? true;

            var query = _context.Productos.AsNoTracking().AsQueryable();

            if (filterStockNuevo)
            {
                query = query.Where(p => p.Disponible > 0);
            }

            var prodsList = await query.OrderBy(p => p.Nombre).ToListAsync();
            var proveMap = await _context.Proveedores.AsNoTracking().ToDictionaryAsync(p => p.Id, p => p.Nombre ?? "N/A");

            var rows = new List<StockReportRowViewModel>();

            foreach (var p in prodsList)
            {
                proveMap.TryGetValue(p.Id_Proveedor ?? 0, out var proveedorNom);
                proveedorNom = proveedorNom ?? "N/A";

                if (!string.IsNullOrEmpty(laboratorio) && !proveedorNom.Contains(laboratorio.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(producto) && 
                    !p.Nombre.Contains(producto.Trim(), StringComparison.OrdinalIgnoreCase) && 
                    !(p.CodigoBarra ?? "").Contains(producto.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                decimal pvpx = p.Pvp_Iva ? (p.PVP ?? 0) : (p.PVP ?? 0) * (1 + (p.IVA ?? 0) / 100);
                long stockUnits = p.Disponible ?? 0;

                decimal pvf = p.PVF ?? 0;
                decimal pvp = p.PVP ?? 0;

                decimal toPvf = pvf * stockUnits;
                decimal toPvp = pvp * stockUnits;
                decimal toPvpx = pvpx * stockUnits;
                decimal utilidad = toPvp - toPvf;

                rows.Add(new StockReportRowViewModel
                {
                    Id = p.Id,
                    CodigoBarra = p.CodigoBarra ?? "N/A",
                    Nombre = p.Nombre ?? "N/A",
                    Proveedor = proveedorNom,
                    PVF = pvf,
                    PVP = pvp,
                    PVPX = pvpx,
                    Disponible = stockUnits,
                    ToPVF = toPvf,
                    ToPVP = toPvp,
                    ToPVPX = toPvpx,
                    Utilidad = utilidad,
                    Vencimiento = p.Vencimiento?.ToString("yyyy-MM-dd") ?? "N/A",
                    DatosAdicionales = p.DatosAdicionales
                });
            }

            // Cargar campos personalizados que son visibles en reportes
            var ruc = await _context.ParametrosSistema.Select(p => p.RUC).FirstOrDefaultAsync();
            var empresa = await _adminContext.Empresas.FirstOrDefaultAsync(e => e.RUC == ruc);
            if (empresa != null)
            {
                ViewBag.CustomFields = await _camposService.GetCamposActivosAsync("Producto", empresa.RubroId, empresa.Id);
                ViewBag.CamposService = _camposService;
            }

            ViewBag.TotalPVF = rows.Sum(r => r.ToPVF);
            ViewBag.TotalPVP = rows.Sum(r => r.ToPVP);
            ViewBag.TotalPVPX = rows.Sum(r => r.ToPVPX);
            ViewBag.TotalUtilidad = rows.Sum(r => r.Utilidad);
            ViewBag.TotalItems = rows.Sum(r => r.Disponible);

            ViewBag.FilterProducto = producto;
            ViewBag.FilterLaboratorio = laboratorio;
            ViewBag.FilterStockNuevo = filterStockNuevo;

            var parametros = await _context.ParametrosSistema.AsNoTracking().FirstOrDefaultAsync(p => p.Id == 1);
            ViewBag.Parametros = parametros;
            ViewBag.MonedaSimbolo = await GetMonedaSimbolo(parametros?.MonedaOficial);

            return View(rows);
        }

        public async Task<IActionResult> StockReportPrint(string? producto, string? laboratorio, bool? stockNuevo)
        {
            // Default "Filtro Stock Nuevo" to true if not specified to show only active stock items
            bool filterStockNuevo = stockNuevo ?? true;

            var query = _context.Productos.AsNoTracking().AsQueryable();

            if (filterStockNuevo)
            {
                query = query.Where(p => p.Disponible > 0);
            }

            var prodsList = await query.OrderBy(p => p.Nombre).ToListAsync();
            var proveMap = await _context.Proveedores.AsNoTracking().ToDictionaryAsync(p => p.Id, p => p.Nombre ?? "N/A");

            var rows = new List<StockReportRowViewModel>();

            foreach (var p in prodsList)
            {
                proveMap.TryGetValue(p.Id_Proveedor ?? 0, out var proveedorNom);
                proveedorNom = proveedorNom ?? "N/A";

                // Filter in-memory for supplier name (Laboratory filter)
                if (!string.IsNullOrEmpty(laboratorio) && !proveedorNom.Contains(laboratorio.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Filter in-memory for product name or barcode
                if (!string.IsNullOrEmpty(producto) && 
                    !p.Nombre.Contains(producto.Trim(), StringComparison.OrdinalIgnoreCase) && 
                    !(p.CodigoBarra ?? "").Contains(producto.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                decimal pvpx = p.Pvp_Iva ? (p.PVP ?? 0) : (p.PVP ?? 0) * (1 + (p.IVA ?? 0) / 100);
                long stockUnits = p.Disponible ?? 0;

                decimal pvf = p.PVF ?? 0;
                decimal pvp = p.PVP ?? 0;

                decimal toPvf = pvf * stockUnits;
                decimal toPvp = pvp * stockUnits;
                decimal toPvpx = pvpx * stockUnits;
                decimal utilidad = toPvp - toPvf;

                rows.Add(new StockReportRowViewModel
                {
                    Id = p.Id,
                    CodigoBarra = p.CodigoBarra ?? "N/A",
                    Nombre = p.Nombre ?? "N/A",
                    Proveedor = proveedorNom,
                    PVF = pvf,
                    PVP = pvp,
                    PVPX = pvpx,
                    Disponible = stockUnits,
                    ToPVF = toPvf,
                    ToPVP = toPvp,
                    ToPVPX = toPvpx,
                    Utilidad = utilidad,
                    Vencimiento = p.Vencimiento?.ToString("yyyy-MM-dd") ?? "N/A",
                    DatosAdicionales = p.DatosAdicionales
                });
            }

            // Cargar campos personalizados que son visibles en reportes
            var ruc = await _context.ParametrosSistema.Select(p => p.RUC).FirstOrDefaultAsync();
            var empresa = await _adminContext.Empresas.FirstOrDefaultAsync(e => e.RUC == ruc);
            if (empresa != null)
            {
                ViewBag.CustomFields = await _camposService.GetCamposActivosAsync("Producto", empresa.RubroId, empresa.Id);
                ViewBag.CamposService = _camposService;
            }

            // Calculations for report summary widget
            ViewBag.TotalPVF = rows.Sum(r => r.ToPVF);
            ViewBag.TotalPVP = rows.Sum(r => r.ToPVP);
            ViewBag.TotalPVPX = rows.Sum(r => r.ToPVPX);
            ViewBag.TotalUtilidad = rows.Sum(r => r.Utilidad);
            ViewBag.TotalItems = rows.Sum(r => r.Disponible);

            ViewBag.FilterProducto = producto;
            ViewBag.FilterLaboratorio = laboratorio;
            ViewBag.FilterStockNuevo = filterStockNuevo;

            // Load company parameters for report header
            var parametros = await _context.ParametrosSistema.AsNoTracking().FirstOrDefaultAsync(p => p.Id == 1);
            ViewBag.Parametros = parametros;
            ViewBag.MonedaSimbolo = await GetMonedaSimbolo(parametros?.MonedaOficial);

            return View(rows);
        }

        [HttpGet]
        public async Task<IActionResult> StockReportExportExcel(string? producto, string? laboratorio, bool? stockNuevo)
        {
            bool filterStockNuevo = stockNuevo ?? true;

            var query = _context.Productos.AsNoTracking().AsQueryable();

            if (filterStockNuevo)
            {
                query = query.Where(p => p.Disponible > 0);
            }

            var prodsList = await query.ToListAsync();
            var proveMap = await _context.Proveedores.AsNoTracking().ToDictionaryAsync(p => p.Id, p => p.Nombre ?? "N/A");

            // Cargar campos personalizados para Producto
            var ruc = await _context.ParametrosSistema.Select(p => p.RUC).FirstOrDefaultAsync();
            var empresa = await _adminContext.Empresas.FirstOrDefaultAsync(e => e.RUC == ruc);
            List<CampoPersonalizado> customFields = new List<CampoPersonalizado>();
            if (empresa != null)
            {
                var allFields = await _camposService.GetCamposActivosAsync("Producto", empresa.RubroId, empresa.Id);
                customFields = allFields.Where(c => c.VisibleEnReporte).ToList();
            }

            var sb = new System.Text.StringBuilder();
            
            // Construir encabezados
            var headers = "ID/COD.BARRA;Producto;Proveedor;PVF;PVP;PVPX;Disponible;ToPVF;ToPVP;ToPVPX;Utilidad;Vencimiento";
            foreach (var campo in customFields)
            {
                headers += $";{campo.Etiqueta}";
            }
            sb.AppendLine(headers);

            foreach (var p in prodsList)
            {
                proveMap.TryGetValue(p.Id_Proveedor ?? 0, out var proveedorNom);
                proveedorNom = proveedorNom ?? "N/A";

                if (!string.IsNullOrEmpty(laboratorio) && !proveedorNom.Contains(laboratorio.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(producto) && 
                    !p.Nombre.Contains(producto.Trim(), StringComparison.OrdinalIgnoreCase) && 
                    !(p.CodigoBarra ?? "").Contains(producto.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                decimal pvpx = p.Pvp_Iva ? (p.PVP ?? 0) : (p.PVP ?? 0) * (1 + (p.IVA ?? 0) / 100);
                long stockUnits = p.Disponible ?? 0;

                decimal pvf = p.PVF ?? 0;
                decimal pvp = p.PVP ?? 0;

                decimal toPvf = pvf * stockUnits;
                decimal toPvp = pvp * stockUnits;
                decimal toPvpx = pvpx * stockUnits;
                decimal utilidad = toPvp - toPvf;

                var idCodBarra = string.IsNullOrEmpty(p.CodigoBarra) ? p.Id.ToString() : p.CodigoBarra;
                var vencStr = p.Vencimiento?.ToString("dd/MM/yyyy") ?? "N/A";

                var rowData = $"{idCodBarra};{p.Nombre};{proveedorNom};${pvf:N2};${pvp:N2};${pvpx:N2};{stockUnits};${toPvf:N2};${toPvp:N2};${toPvpx:N2};${utilidad:N2};{vencStr}";
                
                var dict = _camposService.ParseDatosAdicionales(p.DatosAdicionales);
                foreach (var campo in customFields)
                {
                    dict.TryGetValue(campo.NombreCampo, out var val);
                    rowData += $";{val?.ToString() ?? "N/A"}";
                }
                sb.AppendLine(rowData);
            }

            var fileBytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(fileBytes, "text/csv; charset=utf-8", $"ReporteStock_{DateTime.Now:yyyyMMddHHmmss}.csv");
        }

        private async Task<string> GetMonedaSimbolo(string? monedaCodigo)
        {
            if (string.IsNullOrEmpty(monedaCodigo)) return "$";
            var moneda = await _context.Monedas.AsNoTracking().FirstOrDefaultAsync(m => m.Codigo == monedaCodigo);
            return moneda?.Simbolo ?? "$";
        }

        public async Task<IActionResult> ReporteCierresCaja(DateTime? desde, DateTime? hasta)
        {
            var fromDate = desde ?? DateTime.Today;
            var toDate = hasta ?? DateTime.Today;
            var nextDay = toDate.Date.AddDays(1);

            var cierres = await _context.CierresCaja
                .Include(c => c.UsuarioCierra)
                .Include(c => c.Apertura)
                .Where(c => c.FechaCierre >= fromDate.Date && c.FechaCierre < nextDay)
                .OrderBy(c => c.FechaCierre)
                .ToListAsync();

            var denominaciones = await _context.Denominaciones
                .Where(d => d.Activo)
                .OrderByDescending(d => d.Valor)
                .ThenBy(d => d.Tipo)
                .ToListAsync();
            ViewBag.Denominaciones = denominaciones;

            var parametros = await _context.ParametrosSistema.AsNoTracking().FirstOrDefaultAsync(p => p.Id == 1);
            ViewBag.Parametros = parametros;
            ViewBag.MonedaSimbolo = await GetMonedaSimbolo(parametros?.MonedaOficial);
            ViewBag.Desde = fromDate.ToString("yyyy-MM-dd");
            ViewBag.Hasta = toDate.ToString("yyyy-MM-dd");

            return View(cierres);
        }

        public async Task<IActionResult> ResumenDetalleCierre(DateTime? fecha, int? usuarioId)
        {
            var date = fecha ?? DateTime.Today;
            var nextDay = date.Date.AddDays(1);

            var usuarios = await _context.Usuarios.Where(u => u.Estado == "ACTIVO").OrderBy(u => u.Login).ToListAsync();
            ViewBag.Usuarios = usuarios;

            int selectedUser = usuarioId ?? 0;
            if (selectedUser == 0)
            {
                var username = User.Identity?.Name;
                if (!string.IsNullOrEmpty(username))
                {
                    var u = usuarios.FirstOrDefault(x => string.Equals(x.Login?.Trim(), username.Trim(), StringComparison.OrdinalIgnoreCase));
                    if (u != null) selectedUser = u.Id;
                }
                if (selectedUser == 0 && usuarios.Any())
                {
                    selectedUser = usuarios.First().Id;
                }
            }
            ViewBag.SelectedUsuarioId = selectedUser;
            ViewBag.Fecha = date.ToString("yyyy-MM-dd");

            var apertura = await _context.AperturasCaja
                .FirstOrDefaultAsync(a => a.UsuarioId == selectedUser && a.FechaApertura >= date.Date && a.FechaApertura < nextDay);
            ViewBag.Apertura = apertura;

            var cierre = await _context.CierresCaja
                .Include(c => c.Apertura)
                .Include(c => c.UsuarioCierra)
                .FirstOrDefaultAsync(c => c.UsuarioCierraId == selectedUser && c.FechaCierre >= date.Date && c.FechaCierre < nextDay);

            ViewBag.Cierre = cierre;

            var denominaciones = await _context.Denominaciones
                .Where(d => d.Activo)
                .OrderByDescending(d => d.Valor)
                .ThenBy(d => d.Tipo)
                .ToListAsync();
            ViewBag.Denominaciones = denominaciones;

            var pagos = await _context.FacturasPorCobrarAbonos
                .Where(p => p.Fecha_Cobro >= date.Date && p.Fecha_Cobro < nextDay && p.Id_Usuario == selectedUser)
                .OrderBy(p => p.FechaHora_Reg)
                .ToListAsync();

            var bancos = await _context.Bancos.AsNoTracking().ToDictionaryAsync(b => b.Id, b => b.Nombre ?? "N/A");
            ViewBag.BancosMap = bancos;

            var transList = new List<CierreDetalleTransViewModel>();
            foreach (var p in pagos)
            {
                bancos.TryGetValue(p.Id_Banco ?? 0, out var bancoNombre);
                bancoNombre = bancoNombre ?? "N/A";

                string tipoPagoDesc = "OTRO";
                if (p.Tipo_Pago == 1) tipoPagoDesc = "EFECTIVO";
                else if (p.Tipo_Pago == 2) tipoPagoDesc = "TARJETA";
                else if (p.Tipo_Pago == 3) tipoPagoDesc = "TRANSFERENCIA";
                else if (p.Tipo_Pago == 4) tipoPagoDesc = "CHEQUE";

                var venta = await _context.Ventas.AsNoTracking().FirstOrDefaultAsync(v => v.Id == p.Id_Venta);
                string clienteNombre = "CONSUMIDOR FINAL";
                if (venta != null)
                {
                    var cli = await _context.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == venta.Id_Cliente);
                    if (cli != null) clienteNombre = cli.Nombres_Apellidos ?? "CONSUMIDOR FINAL";
                }

                transList.Add(new CierreDetalleTransViewModel
                {
                    Banco = bancoNombre,
                    AdquirenteCliente = string.IsNullOrEmpty(p.Adquirente) ? clienteNombre : $"{p.Adquirente} / {clienteNombre}",
                    Fecha = p.Fecha_Cobro.ToString("yyyy-MM-dd"),
                    Hora = p.FechaHora_Reg.ToString("hh:mm tt"),
                    DocVenta = p.Id_Venta.ToString(),
                    Ref = p.Num_Doc ?? "N/A",
                    TotalPagar = p.Monto_Efectivo,
                    InteresBancario = p.Interes_MultiPago,
                    TipoPago = tipoPagoDesc
                });
            }

            ViewBag.Transacciones = transList;

            var parametros = await _context.ParametrosSistema.AsNoTracking().FirstOrDefaultAsync(p => p.Id == 1);
            ViewBag.Parametros = parametros;
            ViewBag.MonedaSimbolo = await GetMonedaSimbolo(parametros?.MonedaOficial);

            return View();
        }

        public class CierreDetalleTransViewModel
        {
            public string Banco { get; set; } = "";
            public string AdquirenteCliente { get; set; } = "";
            public string Fecha { get; set; } = "";
            public string Hora { get; set; } = "";
            public string DocVenta { get; set; } = "";
            public string Ref { get; set; } = "";
            public decimal TotalPagar { get; set; }
            public decimal InteresBancario { get; set; }
            public string TipoPago { get; set; } = "";
        }

        public class StockReportRowViewModel
        {
            public long Id { get; set; }
            public string CodigoBarra { get; set; } = "";
            public string Nombre { get; set; } = "";
            public string Proveedor { get; set; } = "";
            public decimal PVF { get; set; }
            public decimal PVP { get; set; }
            public decimal PVPX { get; set; }
            public long Disponible { get; set; }
            public decimal ToPVF { get; set; }
            public decimal ToPVP { get; set; }
            public decimal ToPVPX { get; set; }
            public decimal Utilidad { get; set; }
            public string Vencimiento { get; set; } = "";
            public string? DatosAdicionales { get; set; }
        }

        public class VentasDiariasReportViewModel
        {
            public DateTime Desde { get; set; }
            public DateTime Hasta { get; set; }
            public bool MultiPago { get; set; }
            public bool DetalleProducto { get; set; }
            public List<VentaGrupoDespachadorViewModel> VentasPorDespachador { get; set; } = new();
            public List<ResumenDiaViewModel> ResumenPorDia { get; set; } = new();
            public List<ResumenDespachadorViewModel> ResumenPorDespachador { get; set; } = new();
            public decimal TotalVentas { get; set; }
            public decimal TotalGanancia { get; set; }
            public int TotalTransacciones { get; set; }
        }

        public class VentaGrupoDespachadorViewModel
        {
            public string Despachador { get; set; } = "";
            public List<VentaReportRowViewModel> Ventas { get; set; } = new();
            public decimal TotalCobrado => Ventas.Sum(v => v.TotalPagado);
        }

        public class VentaReportRowViewModel
        {
            public long Id { get; set; }
            public string NumDoc { get; set; } = "";
            public DateTime Fecha { get; set; }
            public string Cliente { get; set; } = "";
            public string TipoPago { get; set; } = "";
            public decimal TotalPagado { get; set; }
            public decimal Ganancia { get; set; }
            public List<Venta_Producto> DetalleProductos { get; set; } = new();
        }

        public class ResumenDiaViewModel
        {
            public DateTime Fecha { get; set; }
            public decimal SeVendio { get; set; }
            public decimal Ganancia { get; set; }
        }

        public class ResumenDespachadorViewModel
        {
            public string Despachador { get; set; } = "";
            public decimal SeVendio { get; set; }
            public decimal Ganancia { get; set; }
        }

        public class VentasMensualesReportViewModel
        {
            public DateTime Desde { get; set; }
            public DateTime Hasta { get; set; }
            public List<VentaMensualGrupoViewModel> GruposMensuales { get; set; } = new();
            public decimal TotalVentas { get; set; }
        }

        public class VentaMensualGrupoViewModel
        {
            public string MesAnio { get; set; } = "";
            public decimal TotalCobrado { get; set; }
        }

        public async Task<IActionResult> EstadoResultados(DateTime? desde, DateTime? hasta)
        {
            var dateDesde = desde ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var dateHasta = hasta ?? DateTime.Today;
            var start = dateDesde.Date;
            var end = dateHasta.Date.AddDays(1); // Hasta el fin del día seleccionado

            var paramsSistema = await _context.ParametrosSistema.AsNoTracking().FirstOrDefaultAsync(p => p.Id == 1);
            ViewBag.Parametros = paramsSistema;
            ViewBag.MonedaSimbolo = await GetMonedaSimbolo(paramsSistema?.MonedaOficial);

            // Obtener el diseño configurado del Estado de Resultados
            var configRows = await _context.ConfiguracionEstadoResultados
                .Include(r => r.CuentaContable)
                .OrderBy(r => r.Orden)
                .ToListAsync();

            var result = new List<EstadoResultadosRowViewModel>();
            var valoresPorOrden = new Dictionary<int, decimal>();

            foreach (var row in configRows)
            {
                decimal monto = 0;

                if (row.TipoFila == "Cuenta" && row.CuentaContableId.HasValue)
                {
                    var ccId = row.CuentaContableId.Value;

                    // Caso 1: Cuenta de Ventas (Ingresos)
                    if (paramsSistema?.CuentaContableVentasId == ccId)
                    {
                        monto = await _context.VentasDetalle
                            .AsNoTracking()
                            .Where(d => _context.Ventas
                                .AsNoTracking()
                                .Where(v => v.FechaHora_Registro >= start && v.FechaHora_Registro < end)
                                .Select(v => v.Id)
                                .Contains(d.Id_Venta))
                            .SumAsync(d => d.Total_Pagar ?? 0);
                    }
                    // Caso 2: Cuenta de Costos (Costo de Venta)
                    else if (paramsSistema?.CuentaContableCostosId == ccId)
                    {
                        monto = await _context.ComprasDetalle
                            .AsNoTracking()
                            .Where(d => _context.Compras
                                .AsNoTracking()
                                .Where(c => (c.Fecha_Documento ?? c.FechaHora_Reg) >= start && (c.Fecha_Documento ?? c.FechaHora_Reg) < end)
                                .Select(c => c.Id.ToString())
                                .Contains(d.Id_Factura))
                            .SumAsync(d => d.TotalPagar ?? 0);
                    }
                    // Caso 3: Cuentas de Gasto
                    else
                    {
                        var descIds = await GetAccountAndDescendantsIds(ccId);
                        monto = await _context.Gastos
                            .AsNoTracking()
                            .Where(g => g.Fecha >= start && g.Fecha <= dateHasta.Date && descIds.Contains(g.CuentaContableId))
                            .SumAsync(g => g.Monto);
                    }
                }
                else if (row.TipoFila == "Formula")
                {
                    monto = EvaluarFormula(row.Formula ?? "", valoresPorOrden);
                }

                valoresPorOrden[row.Orden] = monto;

                result.Add(new EstadoResultadosRowViewModel
                {
                    Orden = row.Orden,
                    Codigo = row.CuentaContable?.Codigo ?? "",
                    Descripcion = row.Descripcion,
                    Monto = monto,
                    EsNegrita = row.EsNegrita
                });
            }

            var model = new EstadoResultadosViewModel
            {
                Desde = dateDesde,
                Hasta = dateHasta,
                Filas = result
            };

            ViewBag.Desde = dateDesde.ToString("yyyy-MM-dd");
            ViewBag.Hasta = dateHasta.ToString("yyyy-MM-dd");

            return View(model);
        }

        private async Task<List<int>> GetAccountAndDescendantsIds(int parentId)
        {
            var ids = new List<int> { parentId };
            var childIds = await _context.CuentasContables
                .AsNoTracking()
                .Where(c => c.IdPadre == parentId && c.Activo)
                .Select(c => c.Id)
                .ToListAsync();

            foreach (var childId in childIds)
            {
                ids.AddRange(await GetAccountAndDescendantsIds(childId));
            }
            return ids;
        }

        private decimal EvaluarFormula(string formula, Dictionary<int, decimal> valoresPorOrden)
        {
            if (string.IsNullOrWhiteSpace(formula)) return 0;

            try
            {
                string expresion = formula;
                foreach (var kvp in valoresPorOrden)
                {
                    expresion = expresion.Replace($"{{{kvp.Key}}}", kvp.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                }

                var dt = new System.Data.DataTable();
                var val = dt.Compute(expresion, "");
                return Convert.ToDecimal(val);
            }
            catch
            {
                return 0; 
            }
        }

        public class EstadoResultadosViewModel
        {
            public DateTime Desde { get; set; }
            public DateTime Hasta { get; set; }
            public List<EstadoResultadosRowViewModel> Filas { get; set; } = new();
        }

        public class EstadoResultadosRowViewModel
        {
            public int Orden { get; set; }
            public string Codigo { get; set; } = "";
            public string Descripcion { get; set; } = "";
            public decimal Monto { get; set; }
            public bool EsNegrita { get; set; }
        }
    }
}
