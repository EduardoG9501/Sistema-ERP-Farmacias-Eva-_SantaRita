using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaERP.Models;
using SistemaERP.Models.Entities;
using SistemaERP.Areas.Admin.Models;
using SistemaERP.Areas.Admin.Services;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;

namespace SistemaERP.Controllers
{
    public class CatalogoPublicoController : Controller
    {
        private readonly AdminContext _adminContext;
        private readonly IEncryptionService _encryptionService;
        private readonly ICamposPersonalizadosService _camposService;
        private ScmContext? _scmContext;

        public CatalogoPublicoController(
            AdminContext adminContext,
            IEncryptionService encryptionService,
            ICamposPersonalizadosService camposService)
        {
            _adminContext = adminContext;
            _encryptionService = encryptionService;
            _camposService = camposService;
        }

        private async Task<bool> InitializeScmContextAsync(string alias)
        {
            if (string.IsNullOrEmpty(alias)) return false;

            var empresa = await _adminContext.Empresas
                .FirstOrDefaultAsync(e => e.Subdominio == alias);

            if (empresa == null || string.IsNullOrEmpty(empresa.ConnectionStringEncriptado))
                return false;

            var connStr = _encryptionService.Decrypt(empresa.ConnectionStringEncriptado);
            var opts = new DbContextOptionsBuilder<ScmContext>()
                .UseSqlServer(connStr)
                .Options;

            _scmContext = new ScmContext(opts);
            
            // Pasar información de la empresa a la vista
            ViewBag.EmpresaCentral = empresa;
            ViewBag.Alias = alias;
            
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _scmContext?.Dispose();
            }
            base.Dispose(disposing);
        }

        public async Task<IActionResult> Index(string alias, string? buscar, long? categoriaId, int pagina = 1)
        {
            if (!await InitializeScmContextAsync(alias))
            {
                return NotFound("Empresa no encontrada.");
            }

            var sysParams = await _scmContext!.ParametrosSistema.FirstOrDefaultAsync(p => p.Id == 1);
            if (sysParams == null || !sysParams.CatalogoPublicoActivo)
            {
                ViewBag.Titulo = sysParams?.CatalogoTitulo ?? "Catálogo Web";
                ViewBag.LogoPath = sysParams?.LogoPath;
                return View("NoDisponible");
            }

            ViewBag.SysParams = sysParams;
            ViewBag.MonedaSimbolo = GetMonedaSimbolo(sysParams.MonedaOficial);

            // Cargar Categorías (Presentaciones) que tengan productos activos
            var categoriasActivasIds = await _scmContext.Productos
                .Where(p => p.Estado == "Activo" && p.Id_Presentacion.HasValue)
                .Select(p => p.Id_Presentacion!.Value)
                .Distinct()
                .ToListAsync();

            var categorias = await _scmContext.Presentaciones
                .Where(p => categoriasActivasIds.Contains(p.Id))
                .OrderBy(p => p.Nombre)
                .ToListAsync();

            ViewBag.Categorias = categorias;

            // Cargar Diccionario de Marcas (Proveedores)
            var marcas = await _scmContext.Proveedores.ToDictionaryAsync(p => p.Id, p => p.Nombre ?? "Sin Marca");
            ViewBag.Marcas = marcas;

            // Query de productos activos
            var query = _scmContext.Productos.Where(p => p.Estado == "Activo");

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                var term = buscar.Trim().ToLower();
                query = query.Where(p => p.Nombre!.ToLower().Contains(term) || p.CodigoBarra!.ToLower().Contains(term));
                ViewBag.Buscar = buscar;
            }

            if (categoriaId.HasValue && categoriaId.Value > 0)
            {
                query = query.Where(p => p.Id_Presentacion == categoriaId.Value);
                ViewBag.CategoriaId = categoriaId;
                var catSeleccionada = categorias.FirstOrDefault(c => c.Id == categoriaId.Value);
                ViewBag.CategoriaNombre = catSeleccionada?.Nombre;
            }

            // Paginación
            int itemsPorPagina = 12;
            int totalItems = await query.CountAsync();
            int totalPaginas = (int)Math.Ceiling((double)totalItems / itemsPorPagina);
            pagina = pagina < 1 ? 1 : (pagina > totalPaginas && totalPaginas > 0 ? totalPaginas : pagina);

            var productos = await query
                .OrderBy(p => p.Nombre)
                .Skip((pagina - 1) * itemsPorPagina)
                .Take(itemsPorPagina)
                .ToListAsync();

            ViewBag.PaginaActual = pagina;
            ViewBag.TotalPaginas = totalPaginas;
            ViewBag.TotalItems = totalItems;

            // Mapeo de presentaciones a diccionario para renderizado rápido
            ViewBag.Presentaciones = await _scmContext.Presentaciones.ToDictionaryAsync(p => p.Id, p => p.Nombre ?? "Sin Categoría");

            return View(productos);
        }

        public async Task<IActionResult> Detalle(string alias, long id)
        {
            if (!await InitializeScmContextAsync(alias))
            {
                return NotFound("Empresa no encontrada.");
            }

            var sysParams = await _scmContext!.ParametrosSistema.FirstOrDefaultAsync(p => p.Id == 1);
            if (sysParams == null || !sysParams.CatalogoPublicoActivo)
            {
                return View("NoDisponible");
            }

            ViewBag.SysParams = sysParams;
            ViewBag.MonedaSimbolo = GetMonedaSimbolo(sysParams.MonedaOficial);

            var producto = await _scmContext.Productos.FindAsync(id);
            if (producto == null || producto.Estado != "Activo")
            {
                return NotFound("Producto no encontrado o no disponible.");
            }

            // Mapeo de Categoría (Presentación) y Marca (Proveedor)
            if (producto.Id_Presentacion.HasValue)
            {
                var pres = await _scmContext.Presentaciones.FindAsync(producto.Id_Presentacion.Value);
                ViewBag.CategoriaNombre = pres?.Nombre ?? "Sin Categoría";
            }
            else
            {
                ViewBag.CategoriaNombre = "Sin Categoría";
            }

            if (producto.Id_Proveedor.HasValue)
            {
                var prov = await _scmContext.Proveedores.FindAsync(producto.Id_Proveedor.Value);
                ViewBag.MarcaNombre = prov?.Nombre ?? "Sin Marca";
            }
            else
            {
                ViewBag.MarcaNombre = "Sin Marca";
            }

            // Cargar Campos Personalizados (Especificaciones Técnicas)
            var centralEmpresa = ViewBag.EmpresaCentral as Empresa;
            if (centralEmpresa != null)
            {
                var campos = await _camposService.GetCamposActivosAsync("Producto", centralEmpresa.RubroId, centralEmpresa.Id);
                var camposValores = _camposService.ParseDatosAdicionales(producto.DatosAdicionales);

                // Filtrar para mostrar solo los que tienen valor guardado en el producto
                var especificaciones = new List<Tuple<string, string>>();
                foreach (var campo in campos)
                {
                    if (camposValores.TryGetValue(campo.NombreCampo, out var val) && val != null && !string.IsNullOrWhiteSpace(val.ToString()))
                    {
                        especificaciones.Add(Tuple.Create(campo.Etiqueta, val.ToString()!));
                    }
                }
                ViewBag.Especificaciones = especificaciones;
            }

            // Cargar fotos del carrusel
            var fotosAdicionales = new List<string>();
            if (!string.IsNullOrEmpty(producto.DatosAdicionales))
            {
                try
                {
                    var doc = JsonDocument.Parse(producto.DatosAdicionales);
                    if (doc.RootElement.TryGetProperty("FotosAdicionales", out var prop) && prop.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in prop.EnumerateArray())
                        {
                            if (item.ValueKind == JsonValueKind.String)
                                fotosAdicionales.Add(item.GetString()!);
                        }
                    }
                }
                catch { }
            }

            // Asegurarnos de incluir la principal si no está en la lista
            if (!string.IsNullOrEmpty(producto.Foto) && !fotosAdicionales.Contains(producto.Foto))
            {
                fotosAdicionales.Insert(0, producto.Foto);
            }
            ViewBag.FotosAdicionales = fotosAdicionales;

            // Productos similares (misma categoría / presentación, excluyendo el actual)
            var similares = new List<Producto>();
            if (producto.Id_Presentacion.HasValue)
            {
                similares = await _scmContext.Productos
                    .Where(p => p.Estado == "Activo" && p.Id_Presentacion == producto.Id_Presentacion.Value && p.Id != producto.Id)
                    .Take(4)
                    .ToListAsync();
            }
            ViewBag.Similares = similares;
            ViewBag.Presentaciones = await _scmContext.Presentaciones.ToDictionaryAsync(p => p.Id, p => p.Nombre ?? "Sin Categoría");
            ViewBag.Marcas = await _scmContext.Proveedores.ToDictionaryAsync(p => p.Id, p => p.Nombre ?? "Sin Marca");

            return View(producto);
        }

        private string GetMonedaSimbolo(string? codigo)
        {
            if (string.IsNullOrEmpty(codigo)) return "$";
            return codigo.ToUpper() switch
            {
                "USD" => "$",
                "EUR" => "€",
                "GBP" => "£",
                "COP" => "Col$",
                "MXN" => "Mex$",
                _ => codigo + " "
            };
        }
    }
}
