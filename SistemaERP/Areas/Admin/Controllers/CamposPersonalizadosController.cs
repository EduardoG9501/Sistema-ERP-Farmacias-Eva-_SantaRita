using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaERP.Areas.Admin.Models;
using SistemaERP.Areas.Admin.Services;

namespace SistemaERP.Areas.Admin.Controllers
{
    public class CamposPersonalizadosController : BaseAdminController
    {
        private readonly AdminContext _context;
        private readonly IAdminAuditService _auditService;
        private readonly IEmpresaConnectionService _empresaConnService;

        public CamposPersonalizadosController(AdminContext context, IAdminAuditService auditService, IEmpresaConnectionService empresaConnService)
        {
            _context = context;
            _auditService = auditService;
            _empresaConnService = empresaConnService;
        }

        public async Task<IActionResult> Index(int? rubroId, int? empresaId)
        {
            var query = _context.CamposPersonalizados
                .Include(c => c.Empresa)
                .Include(c => c.Rubro)
                .AsQueryable();

            if (rubroId.HasValue && rubroId.Value > 0)
            {
                query = query.Where(c => c.RubroId == rubroId.Value);
            }

            if (empresaId.HasValue && empresaId.Value > 0)
            {
                query = query.Where(c => c.EmpresaId == empresaId.Value);
            }

            var campos = await query.OrderBy(c => c.Entidad).ThenBy(c => c.OrdenVisualizacion).ToListAsync();

            ViewBag.Rubros = new SelectList(await _context.Rubros.Where(r => r.Activo).ToListAsync(), "Id", "Nombre", rubroId);
            ViewBag.Empresas = new SelectList(await _context.Empresas.Where(e => e.Estado).ToListAsync(), "Id", "Nombre", empresaId);

            return View(campos);
        }

        [HttpGet]
        public async Task<IActionResult> Crear()
        {
            ViewBag.RubroId = new SelectList(await _context.Rubros.Where(r => r.Activo).ToListAsync(), "Id", "Nombre");
            ViewBag.EmpresaId = new SelectList(await _context.Empresas.Where(e => e.Estado).ToListAsync(), "Id", "Nombre");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(CampoPersonalizado model, string OrigenOpciones, string OpcionesListaTablaNombre, bool OpcionesListaCrearTabla)
        {
            // Limpiar errores de ModelState que no aplican según el tipo de dato seleccionado
            if (model.TipoDato != "Lista")
            {
                ModelState.Remove("OpcionesLista");
                ModelState.Remove("OpcionesListaTablaNombre");
            }
            if (model.TipoDato != "Texto")
            {
                ModelState.Remove("MinLongitud");
                ModelState.Remove("MaxLongitud");
                ModelState.Remove("ExpresionRegular");
            }
            if (model.TipoDato != "Numero")
            {
                ModelState.Remove("ValorMinimo");
                ModelState.Remove("ValorMaximo");
                ModelState.Remove("Decimales");
            }

            // Validar que el NombreCampo sea una cadena PascalCase sin espacios
            if (!string.IsNullOrEmpty(model.NombreCampo))
            {
                if (model.NombreCampo.Contains(" ") || !System.Text.RegularExpressions.Regex.IsMatch(model.NombreCampo, "^[a-zA-Z0-9]+$"))
                {
                    ModelState.AddModelError("NombreCampo", "El nombre de la clave de campo debe ser alfanumérico sin espacios (PascalCase). Ej: 'PrincipioActivo'.");
                }
            }

            if (model.TipoDato == "Lista")
            {
                if (OrigenOpciones == "Tabla")
                {
                    if (string.IsNullOrWhiteSpace(OpcionesListaTablaNombre))
                    {
                        ModelState.AddModelError("OpcionesLista", "Debe especificar el nombre de la tabla.");
                    }
                    else
                    {
                        var sanitizedTableName = System.Text.RegularExpressions.Regex.Replace(OpcionesListaTablaNombre, @"[^a-zA-Z0-9]", "");
                        if (string.IsNullOrEmpty(sanitizedTableName))
                        {
                            ModelState.AddModelError("OpcionesLista", "El nombre de la tabla debe contener caracteres alfanuméricos válidos.");
                        }
                        else
                        {
                            var config = new { EsTabla = true, TablaNombre = sanitizedTableName, CrearTabla = OpcionesListaCrearTabla };
                            model.OpcionesLista = System.Text.Json.JsonSerializer.Serialize(config);
                        }
                    }
                }
            }

            if (ModelState.IsValid)
            {
                model.Activo = true;
                model.FechaCreacion = DateTime.Now;
                model.CreadoPorAdmin = CurrentAdminId;

                // Obtener orden visual automático
                var maxOrden = await _context.CamposPersonalizados
                    .Where(c => c.Entidad == model.Entidad && c.EmpresaId == model.EmpresaId)
                    .Select(c => (int?)c.OrdenVisualizacion)
                    .MaxAsync();
                
                model.OrdenVisualizacion = (maxOrden ?? 0) + 1;

                _context.CamposPersonalizados.Add(model);
                await _context.SaveChangesAsync();

                // Auto-crear tabla si es requerido
                if (model.TipoDato == "Lista" && OrigenOpciones == "Tabla" && OpcionesListaCrearTabla)
                {
                    try
                    {
                        using (var configJson = System.Text.Json.JsonDocument.Parse(model.OpcionesLista))
                        {
                            var tableName = configJson.RootElement.GetProperty("TablaNombre").GetString();
                            if (!string.IsNullOrEmpty(tableName))
                            {
                                await CrearTablaEnEmpresasAsync(tableName, model.EmpresaId);
                            }
                        }
                    }
                    catch { }
                }

                await _auditService.LogActionAsync(CurrentAdminId, model.EmpresaId, "CREAR_CAMPO_PERSONALIZADO", 
                    $"Se creó el campo dinámico '{model.Etiqueta}' ({model.TipoDato}) para la entidad '{model.Entidad}'.");

                TempData["SuccessMessage"] = $"Campo dinámico '{model.Etiqueta}' creado exitosamente.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.RubroId = new SelectList(await _context.Rubros.Where(r => r.Activo).ToListAsync(), "Id", "Nombre", model.RubroId);
            ViewBag.EmpresaId = new SelectList(await _context.Empresas.Where(e => e.Estado).ToListAsync(), "Id", "Nombre", model.EmpresaId);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var campo = await _context.CamposPersonalizados.FindAsync(id);
            if (campo == null) return NotFound();

            ViewBag.RubroId = new SelectList(await _context.Rubros.Where(r => r.Activo).ToListAsync(), "Id", "Nombre", campo.RubroId);
            ViewBag.EmpresaId = new SelectList(await _context.Empresas.Where(e => e.Estado).ToListAsync(), "Id", "Nombre", campo.EmpresaId);
            return View(campo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, CampoPersonalizado model, string OrigenOpciones, string OpcionesListaTablaNombre, bool OpcionesListaCrearTabla)
        {
            if (id != model.Id) return NotFound();

            // Limpiar errores de ModelState que no aplican según el tipo de dato seleccionado
            if (model.TipoDato != "Lista")
            {
                ModelState.Remove("OpcionesLista");
                ModelState.Remove("OpcionesListaTablaNombre");
            }
            if (model.TipoDato != "Texto")
            {
                ModelState.Remove("MinLongitud");
                ModelState.Remove("MaxLongitud");
                ModelState.Remove("ExpresionRegular");
            }
            if (model.TipoDato != "Numero")
            {
                ModelState.Remove("ValorMinimo");
                ModelState.Remove("ValorMaximo");
                ModelState.Remove("Decimales");
            }

            if (model.TipoDato == "Lista")
            {
                if (OrigenOpciones == "Tabla")
                {
                    if (string.IsNullOrWhiteSpace(OpcionesListaTablaNombre))
                    {
                        ModelState.AddModelError("OpcionesLista", "Debe especificar el nombre de la tabla.");
                    }
                    else
                    {
                        var sanitizedTableName = System.Text.RegularExpressions.Regex.Replace(OpcionesListaTablaNombre, @"[^a-zA-Z0-9]", "");
                        if (string.IsNullOrEmpty(sanitizedTableName))
                        {
                            ModelState.AddModelError("OpcionesLista", "El nombre de la tabla debe contener caracteres alfanuméricos válidos.");
                        }
                        else
                        {
                            var config = new { EsTabla = true, TablaNombre = sanitizedTableName, CrearTabla = OpcionesListaCrearTabla };
                            model.OpcionesLista = System.Text.Json.JsonSerializer.Serialize(config);
                        }
                    }
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var dbCampo = await _context.CamposPersonalizados.FindAsync(id);
                    if (dbCampo == null) return NotFound();

                    dbCampo.Etiqueta = model.Etiqueta;
                    dbCampo.TipoDato = model.TipoDato;
                    dbCampo.OpcionesLista = model.OpcionesLista;
                    dbCampo.Requerido = model.Requerido;
                    
                    dbCampo.MinLongitud = model.MinLongitud;
                    dbCampo.MaxLongitud = model.MaxLongitud;
                    dbCampo.ExpresionRegular = model.ExpresionRegular;
                    dbCampo.EsUnico = model.EsUnico;
                    
                    dbCampo.ValorMinimo = model.ValorMinimo;
                    dbCampo.ValorMaximo = model.ValorMaximo;
                    dbCampo.Decimales = model.Decimales;
                    
                    dbCampo.ValorDefault = model.ValorDefault;
                    dbCampo.Placeholder = model.Placeholder;
                    dbCampo.Tooltip = model.Tooltip;

                    dbCampo.VisibleEnFormulario = model.VisibleEnFormulario;
                    dbCampo.VisibleEnLista = model.VisibleEnLista;
                    dbCampo.VisibleEnReporte = model.VisibleEnReporte;
                    dbCampo.VisibleEnEtiqueta = model.VisibleEnEtiqueta;
                    dbCampo.VisibleEnFactura = model.VisibleEnFactura;

                    dbCampo.RubroId = model.RubroId;
                    dbCampo.EmpresaId = model.EmpresaId;

                    _context.Entry(dbCampo).State = EntityState.Modified;
                    await _context.SaveChangesAsync();

                    // Auto-crear tabla si es requerido
                    if (model.TipoDato == "Lista" && OrigenOpciones == "Tabla" && OpcionesListaCrearTabla)
                    {
                        try
                        {
                            using (var configJson = System.Text.Json.JsonDocument.Parse(model.OpcionesLista))
                            {
                                var tableName = configJson.RootElement.GetProperty("TablaNombre").GetString();
                                if (!string.IsNullOrEmpty(tableName))
                                {
                                    await CrearTablaEnEmpresasAsync(tableName, model.EmpresaId);
                                }
                            }
                        }
                        catch { }
                    }

                    await _auditService.LogActionAsync(CurrentAdminId, model.EmpresaId, "EDITAR_CAMPO_PERSONALIZADO", 
                        $"Se modificó la configuración del campo '{model.Etiqueta}'.");

                    TempData["SuccessMessage"] = "Configuración de campo dinámico actualizada.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error al guardar: {ex.Message}");
                }
            }

            ViewBag.RubroId = new SelectList(await _context.Rubros.Where(r => r.Activo).ToListAsync(), "Id", "Nombre", model.RubroId);
            ViewBag.EmpresaId = new SelectList(await _context.Empresas.Where(e => e.Estado).ToListAsync(), "Id", "Nombre", model.EmpresaId);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Eliminar(int id)
        {
            var campo = await _context.CamposPersonalizados.FindAsync(id);
            if (campo == null) return NotFound();

            campo.Activo = false;
            _context.Entry(campo).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            await _auditService.LogActionAsync(CurrentAdminId, campo.EmpresaId, "ELIMINAR_CAMPO_PERSONALIZADO", 
                $"Se desactivó el campo '{campo.Etiqueta}' para la entidad '{campo.Entidad}'.");

            return Json(new { success = true });
        }

        private async Task CrearTablaEnEmpresasAsync(string tableName, int? empresaId)
        {
            var query = $"IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[{tableName}]') AND type in (N'U')) " +
                        "BEGIN " +
                        $"    CREATE TABLE [dbo].[{tableName}] ( " +
                        "        [Id] INT IDENTITY(1,1) PRIMARY KEY, " +
                        "        [Nombre] NVARCHAR(150) NOT NULL " +
                        "    ) " +
                        "END";

            var empresas = new List<Empresa>();
            if (empresaId.HasValue && empresaId.Value > 0)
            {
                var emp = await _context.Empresas.FindAsync(empresaId.Value);
                if (emp != null && emp.Estado)
                {
                    empresas.Add(emp);
                }
            }
            else
            {
                // Global: todas las empresas activas
                empresas = await _context.Empresas.Where(e => e.Estado).ToListAsync();
            }

            foreach (var emp in empresas)
            {
                try
                {
                    using (var clientContext = _empresaConnService.GetContextForEmpresa(emp))
                    {
                        await clientContext.Database.ExecuteSqlRawAsync(query);
                    }
                }
                catch (Exception ex)
                {
                    await _auditService.LogActionAsync(CurrentAdminId, emp.Id, "ERROR_CREACION_TABLA_DINAMICA", 
                        $"No se pudo crear la tabla dinámica '{tableName}' en la base de datos de '{emp.Nombre}': {ex.Message}");
                }
            }
        }
    }
}
