using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

namespace SistemaERP.Controllers
{
    public class ProductosController : Controller
    {
        private readonly ScmContext _context;
        private readonly AdminContext _adminContext;
        private readonly ICamposPersonalizadosService _camposService;
        private readonly IWebHostEnvironment _env;

        public ProductosController(
            ScmContext context,
            AdminContext adminContext,
            ICamposPersonalizadosService camposService,
            IWebHostEnvironment env)
        {
            _context = context;
            _adminContext = adminContext;
            _camposService = camposService;
            _env = env;
        }

        public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 15)
        {
            try
            {
                var query = _context.Productos.AsNoTracking();

                if (!string.IsNullOrEmpty(search) && search != "*")
                {
                    search = search.Trim();
                    long.TryParse(search, out long parsedId);

                    // Buscar proveedores que coincidan con el nombre
                    var matchingSupplierIds = await _context.Proveedores
                        .AsNoTracking()
                        .Where(p => p.Nombre != null && p.Nombre.Contains(search))
                        .Select(p => p.Id)
                        .ToListAsync();

                    query = query.Where(p =>
                        (p.Nombre != null && p.Nombre.Contains(search)) ||
                        (p.CodigoBarra != null && p.CodigoBarra.Contains(search)) ||
                        (parsedId > 0 && p.Id == parsedId) ||
                        (p.Id_Proveedor.HasValue && matchingSupplierIds.Contains(p.Id_Proveedor.Value))
                    );
                }

                // Paginación
                var totalRecords = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

                if (page < 1) page = 1;
                if (totalPages > 0 && page > totalPages) page = totalPages;

                var list = await query
                    .OrderBy(p => p.Nombre)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Mapeo manual simple para la vista
                ViewBag.Presentaciones = await _context.Presentaciones.AsNoTracking().ToDictionaryAsync(p => p.Id, p => p.Nombre ?? "N/A");
                ViewBag.Proveedores = await _context.Proveedores.AsNoTracking().ToDictionaryAsync(p => p.Id, p => p.Nombre ?? "Sin Proveedor");

                // Cargar campos personalizados que son visibles en listado
                var ruc = await _context.ParametrosSistema.Select(p => p.RUC).FirstOrDefaultAsync();
                var empresa = await _adminContext.Empresas.FirstOrDefaultAsync(e => e.RUC == ruc);
                if (empresa != null)
                {
                    ViewBag.CustomFields = await _camposService.GetCamposActivosAsync("Producto", empresa.RubroId, empresa.Id);
                    ViewBag.CamposService = _camposService;
                }

                // Datos de paginación
                ViewBag.CurrentPage = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalRecords = totalRecords;
                ViewBag.TotalPages = totalPages;
                ViewBag.SearchQuery = search;

                return View(list);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error al obtener productos: " + ex.Message;
                return View(new List<Producto>());
            }
        }

        public async Task<IActionResult> Create()
        {
            await LoadFormData();
            await LoadCustomFields();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Producto producto, List<IFormFile>? FotoFiles, int FotoPrincipalIndex = 0)
        {
            // Procesar y Validar Campos Dinámicos
            var customFieldsDict = new Dictionary<string, object>();
            var ruc = await _context.ParametrosSistema.Select(p => p.RUC).FirstOrDefaultAsync();
            var empresa = await _adminContext.Empresas.FirstOrDefaultAsync(e => e.RUC == ruc);
            if (empresa != null)
            {
                var campos = await _camposService.GetCamposActivosAsync("Producto", empresa.RubroId, empresa.Id);
                foreach (var campo in campos.Where(c => c.VisibleEnFormulario))
                {
                    var formKey = $"custom_fields[{campo.NombreCampo}]";
                    var formVal = Request.Form[formKey].ToString();

                    var (valido, msg) = await _camposService.ValidarCampoAsync(campo, formVal, _context, 0);
                    if (!valido)
                    {
                        ModelState.AddModelError("", msg);
                    }
                    else if (!string.IsNullOrEmpty(formVal))
                    {
                        customFieldsDict[campo.NombreCampo] = formVal;
                    }
                }
            }

            // Procesar subida de múltiples imágenes
            var uploadedPaths = new List<string>();
            if (FotoFiles != null && FotoFiles.Any())
            {
                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "productos");
                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                }

                foreach (var file in FotoFiles)
                {
                    if (file.Length > 0)
                    {
                        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                        var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                        if (allowed.Contains(ext))
                        {
                            var filename = Guid.NewGuid().ToString() + ext;
                            var physicalPath = Path.Combine(uploadsDir, filename);
                            using (var stream = new FileStream(physicalPath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }
                            uploadedPaths.Add("/uploads/productos/" + filename);
                        }
                    }
                }
            }

            if (uploadedPaths.Any())
            {
                var pIdx = (FotoPrincipalIndex >= 0 && FotoPrincipalIndex < uploadedPaths.Count) ? FotoPrincipalIndex : 0;
                producto.Foto = uploadedPaths[pIdx];
                customFieldsDict["FotosAdicionales"] = uploadedPaths;
            }
            else
            {
                customFieldsDict["FotosAdicionales"] = new List<string>();
            }

            if (empresa != null)
            {
                producto.DatosAdicionales = _camposService.SerializarDatosAdicionales(customFieldsDict);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    producto.Estado = "Activo";
                    _context.Add(producto);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Producto registrado exitosamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error al registrar: " + ex.Message;
                }
            }
            await LoadFormData();
            await LoadCustomFields(producto.DatosAdicionales);
            return View(producto);
        }

        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null) return NotFound();

            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return NotFound();

            await LoadFormData();
            await LoadCustomFields(producto.DatosAdicionales);
            return View(producto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, Producto producto, List<IFormFile>? FotoFiles, int FotoPrincipalIndex = 0, string? FotoPrincipalExistente = null, string? FotosEliminadas = null)
        {
            if (id != producto.Id) return NotFound();

            // Campos calculados por transacciones — no deben ser validados ni modificados desde el formulario
            ModelState.Remove("Disponible");
            ModelState.Remove("Lote");
            ModelState.Remove("Vencimiento");
            ModelState.Remove("Foto");
            ModelState.Remove("Descuento");
            ModelState.Remove("Descuento_Desde");
            ModelState.Remove("Descuento_Hasta");
            ModelState.Remove("PUR");
            ModelState.Remove("Utilidad");
            ModelState.Remove("EsFraccionable");
            ModelState.Remove("Id_Especificacion");
            ModelState.Remove("Id_Composicion");

            // Procesar y Validar Campos Dinámicos
            var customFieldsDict = new Dictionary<string, object>();
            var ruc = await _context.ParametrosSistema.Select(p => p.RUC).FirstOrDefaultAsync();
            var empresa = await _adminContext.Empresas.FirstOrDefaultAsync(e => e.RUC == ruc);
            if (empresa != null)
            {
                var campos = await _camposService.GetCamposActivosAsync("Producto", empresa.RubroId, empresa.Id);
                foreach (var campo in campos.Where(c => c.VisibleEnFormulario))
                {
                    var formKey = $"custom_fields[{campo.NombreCampo}]";
                    var formVal = Request.Form[formKey].ToString();

                    var (valido, msg) = await _camposService.ValidarCampoAsync(campo, formVal, _context, producto.Id);
                    if (!valido)
                    {
                        ModelState.AddModelError("", msg);
                    }
                    else if (!string.IsNullOrEmpty(formVal))
                    {
                        customFieldsDict[campo.NombreCampo] = formVal;
                    }
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var dbProd = await _context.Productos.FindAsync(id);
                    if (dbProd == null) return NotFound();

                    // Parsear Datos Adicionales Existentes
                    var existingDict = _camposService.ParseDatosAdicionales(dbProd.DatosAdicionales);

                    // Copiar los nuevos valores de campos personalizados a existingDict
                    foreach (var kvp in customFieldsDict)
                    {
                        existingDict[kvp.Key] = kvp.Value;
                    }

                    // Obtener lista actual de fotos
                    var fotosList = new List<string>();
                    if (existingDict.TryGetValue("FotosAdicionales", out var fotosObj) && fotosObj != null)
                    {
                        if (fotosObj is JsonElement jsonElem && jsonElem.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in jsonElem.EnumerateArray())
                            {
                                if (item.ValueKind == JsonValueKind.String)
                                    fotosList.Add(item.GetString()!);
                            }
                        }
                        else if (fotosObj is List<string> list)
                        {
                            fotosList = list;
                        }
                    }

                    // Procesar fotos eliminadas
                    if (!string.IsNullOrEmpty(FotosEliminadas))
                    {
                        var eliminadas = FotosEliminadas.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var path in eliminadas)
                        {
                            var trimmedPath = path.Trim();
                            fotosList.Remove(trimmedPath);

                            // Intentar borrar archivo físico
                            var physicalPath = Path.Combine(_env.WebRootPath, trimmedPath.TrimStart('/'));
                            if (System.IO.File.Exists(physicalPath))
                            {
                                try { System.IO.File.Delete(physicalPath); } catch { }
                            }
                        }
                    }

                    // Subir nuevas fotos
                    var newUploadedPaths = new List<string>();
                    if (FotoFiles != null && FotoFiles.Any())
                    {
                        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "productos");
                        if (!Directory.Exists(uploadsDir))
                        {
                            Directory.CreateDirectory(uploadsDir);
                        }

                        foreach (var file in FotoFiles)
                        {
                            if (file.Length > 0)
                            {
                                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                                var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                                if (allowed.Contains(ext))
                                {
                                    var filename = Guid.NewGuid().ToString() + ext;
                                    var physicalPath = Path.Combine(uploadsDir, filename);
                                    using (var stream = new FileStream(physicalPath, FileMode.Create))
                                    {
                                        await file.CopyToAsync(stream);
                                    }
                                    newUploadedPaths.Add("/uploads/productos/" + filename);
                                }
                            }
                        }
                    }

                    // Agregar nuevas fotos a la lista general
                    fotosList.AddRange(newUploadedPaths);

                    // Definir la foto principal
                    if (!string.IsNullOrEmpty(FotoPrincipalExistente) && fotosList.Contains(FotoPrincipalExistente))
                    {
                        dbProd.Foto = FotoPrincipalExistente;
                    }
                    else if (newUploadedPaths.Any())
                    {
                        var pIdx = (FotoPrincipalIndex >= 0 && FotoPrincipalIndex < newUploadedPaths.Count) ? FotoPrincipalIndex : 0;
                        dbProd.Foto = newUploadedPaths[pIdx];
                    }
                    else if (fotosList.Any() && (string.IsNullOrEmpty(dbProd.Foto) || !fotosList.Contains(dbProd.Foto)))
                    {
                        dbProd.Foto = fotosList.First();
                    }
                    else if (!fotosList.Any())
                    {
                        dbProd.Foto = null;
                    }

                    existingDict["FotosAdicionales"] = fotosList;

                    if (empresa != null)
                    {
                        dbProd.DatosAdicionales = _camposService.SerializarDatosAdicionales(existingDict);
                    }

                    // Actualizar campos normales del producto
                    dbProd.Nombre = producto.Nombre;
                    dbProd.CodigoBarra = producto.CodigoBarra;
                    dbProd.PVF = producto.PVF;
                    dbProd.PVP = producto.PVP;
                    dbProd.IVA = producto.IVA;
                    dbProd.Id_Presentacion = producto.Id_Presentacion;
                    dbProd.Id_Proveedor = producto.Id_Proveedor;
                    dbProd.Contiene = producto.Contiene;
                    dbProd.RegistroSanitario = producto.RegistroSanitario;
                    dbProd.Pvp_Iva = producto.Pvp_Iva;

                    if (!string.IsNullOrEmpty(producto.Estado))
                    {
                        dbProd.Estado = producto.Estado;
                    }

                    _context.Update(dbProd);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Producto actualizado exitosamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error al actualizar: " + ex.Message;
                }
            }
            await LoadFormData();
            await LoadCustomFields(producto.DatosAdicionales);
            return View(producto);
        }

        [HttpPost]
        public async Task<IActionResult> CrearRapido([FromBody] ProductoRapidoRequest req)
        {
            if (string.IsNullOrWhiteSpace(req?.Nombre))
                return Json(new { success = false, message = "El nombre del producto es obligatorio." });
            try
            {
                var producto = new Producto
                {
                    Nombre = req.Nombre,
                    CodigoBarra = req.CodigoBarra,
                    PVF = req.PVF,
                    PVP = req.PVP,
                    IVA = req.IVA,
                    Id_Presentacion = req.IdPresentacion > 0 ? req.IdPresentacion : null,
                    Estado = "Activo",
                    Disponible = 0
                };
                _context.Productos.Add(producto);
                await _context.SaveChangesAsync();
                return Json(new { success = true, id = producto.Id, nombre = producto.Nombre, codigoBarra = producto.CodigoBarra ?? "", pvf = producto.PVF ?? 0 });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al crear producto: " + ex.Message });
            }
        }

        public class ProductoRapidoRequest
        {
            public string? Nombre { get; set; }
            public string? CodigoBarra { get; set; }
            public decimal? PVF { get; set; }
            public decimal? PVP { get; set; }
            public decimal? IVA { get; set; }
            public long IdPresentacion { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(long id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return Json(new { success = false, message = "Registro no encontrado." });

            try
            {
                _context.Productos.Remove(producto);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Producto eliminado exitosamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al eliminar: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> BatchIva()
        {
            var proveedores = await _context.Proveedores.AsNoTracking().ToListAsync();
            var presentaciones = await _context.Presentaciones.AsNoTracking().ToDictionaryAsync(p => p.Id, p => p.Nombre ?? "Sin Categoría");
            var paramsSistema = await _context.ParametrosSistema.AsNoTracking().FirstOrDefaultAsync();

            ViewBag.ProveedoresList = proveedores;
            ViewBag.PresentacionesMap = presentaciones;
            ViewBag.IvaDefault = paramsSistema?.IvaTarifa ?? 15.00m;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetProductosByProveedor(long idProveedor)
        {
            var productos = await _context.Productos
                .Where(p => p.Id_Proveedor == idProveedor && p.Estado == "Activo")
                .AsNoTracking()
                .ToListAsync();
            return Json(productos);
        }

        [HttpPost]
        public async Task<IActionResult> ApplyBatchIva([FromBody] BatchIvaRequest request)
        {
            if (request == null)
                return Json(new { success = false, message = "Datos de solicitud incorrectos." });

            try
            {
                var query = _context.Productos.Where(p => p.Id_Proveedor == request.IdProveedor && p.Estado == "Activo");

                if (request.Scope == "producto" && request.IdProducto.HasValue)
                {
                    query = query.Where(p => p.Id == request.IdProducto.Value);
                }

                var list = await query.ToListAsync();

                foreach (var p in list)
                {
                    if (request.ActionType == "iva_apply")
                    {
                        var paramsSistema = await _context.ParametrosSistema.AsNoTracking().FirstOrDefaultAsync();
                        p.IVA = paramsSistema?.IvaTarifa ?? 15.00m;
                    }
                    else if (request.ActionType == "iva_remove")
                    {
                        p.IVA = 0.00m;
                    }
                    else if (request.ActionType == "pvp_iva_apply")
                    {
                        p.Pvp_Iva = true;
                    }
                    else if (request.ActionType == "pvp_iva_remove")
                    {
                        p.Pvp_Iva = false;
                    }
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = $"Operación aplicada correctamente a {list.Count} productos." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        private async Task LoadFormData()
        {
            var presentaciones = await _context.Presentaciones.ToListAsync();
            var proveedores = await _context.Proveedores.ToListAsync();
            var parametros = await _context.ParametrosSistema.FirstOrDefaultAsync();

            ViewBag.Presentaciones = new SelectList(presentaciones, "Id", "Nombre");
            ViewBag.Proveedores = new SelectList(proveedores, "Id", "Nombre");
            ViewBag.DefaultIva = parametros?.IvaTarifa ?? 15.00m;
        }

        private async Task LoadCustomFields(string? datosAdicionalesJson = null)
        {
            var ruc = await _context.ParametrosSistema.Select(p => p.RUC).FirstOrDefaultAsync();
            var empresa = await _adminContext.Empresas.FirstOrDefaultAsync(e => e.RUC == ruc);

            if (empresa != null)
            {
                ViewBag.CustomFields = await _camposService.GetCamposActivosAsync("Producto", empresa.RubroId, empresa.Id);
                ViewBag.CustomFieldValues = _camposService.ParseDatosAdicionales(datosAdicionalesJson);
            }
            else
            {
                ViewBag.CustomFields = new List<CampoPersonalizado>();
                ViewBag.CustomFieldValues = new Dictionary<string, object>();
            }
        }
    }

    public class BatchIvaRequest
    {
        public long IdProveedor { get; set; }
        public string Scope { get; set; } = "todos"; // "todos" o "producto"
        public long? IdProducto { get; set; }
        public string ActionType { get; set; } = ""; // "iva_apply", "iva_remove", "pvp_iva_apply", "pvp_iva_remove"
    }
}
