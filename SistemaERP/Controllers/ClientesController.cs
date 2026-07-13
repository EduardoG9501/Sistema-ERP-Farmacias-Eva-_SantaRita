using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaERP.Models;
using SistemaERP.Models.Entities;
using SistemaERP.Areas.Admin.Models;
using SistemaERP.Areas.Admin.Services;

namespace SistemaERP.Controllers
{
    public class ClientesController : Controller
    {
        private readonly ScmContext _context;
        private readonly AdminContext _adminContext;
        private readonly ICamposPersonalizadosService _camposService;

        public ClientesController(
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
            try
            {
                var list = await _context.Clientes.ToListAsync();

                // Cargar campos personalizados que son visibles en listado
                var ruc = await _context.ParametrosSistema.Select(p => p.RUC).FirstOrDefaultAsync();
                var empresa = await _adminContext.Empresas.FirstOrDefaultAsync(e => e.RUC == ruc);
                if (empresa != null)
                {
                    ViewBag.CustomFields = await _camposService.GetCamposActivosAsync("Cliente", empresa.RubroId, empresa.Id);
                    ViewBag.CamposService = _camposService;
                }

                return View(list);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error al obtener clientes: " + ex.Message;
                return View(new List<Cliente>());
            }
        }

        public async Task<IActionResult> Create()
        {
            await LoadCustomFields();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Cliente cliente)
        {
            // Procesar y Validar Campos Dinámicos
            var customFieldsDict = new Dictionary<string, object>();
            var ruc = await _context.ParametrosSistema.Select(p => p.RUC).FirstOrDefaultAsync();
            var empresa = await _adminContext.Empresas.FirstOrDefaultAsync(e => e.RUC == ruc);
            if (empresa != null)
            {
                var campos = await _camposService.GetCamposActivosAsync("Cliente", empresa.RubroId, empresa.Id);
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
                cliente.DatosAdicionales = _camposService.SerializarDatosAdicionales(customFieldsDict);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(cliente);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cliente registrado exitosamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error al registrar: " + ex.Message;
                }
            }
            await LoadCustomFields(cliente.DatosAdicionales);
            return View(cliente);
        }

        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null) return NotFound();

            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null) return NotFound();

            await LoadCustomFields(cliente.DatosAdicionales);
            return View(cliente);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, Cliente cliente)
        {
            if (id != cliente.Id) return NotFound();

            // Procesar y Validar Campos Dinámicos
            var customFieldsDict = new Dictionary<string, object>();
            var ruc = await _context.ParametrosSistema.Select(p => p.RUC).FirstOrDefaultAsync();
            var empresa = await _adminContext.Empresas.FirstOrDefaultAsync(e => e.RUC == ruc);
            if (empresa != null)
            {
                var campos = await _camposService.GetCamposActivosAsync("Cliente", empresa.RubroId, empresa.Id);
                foreach (var campo in campos.Where(c => c.VisibleEnFormulario))
                {
                    var formKey = $"custom_fields[{campo.NombreCampo}]";
                    var formVal = Request.Form[formKey].ToString();

                    var (valido, msg) = await _camposService.ValidarCampoAsync(campo, formVal, _context, cliente.Id);
                    if (!valido)
                    {
                        ModelState.AddModelError("", msg);
                    }
                    else if (!string.IsNullOrEmpty(formVal))
                    {
                        customFieldsDict[campo.NombreCampo] = formVal;
                    }
                }
                cliente.DatosAdicionales = _camposService.SerializarDatosAdicionales(customFieldsDict);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var dbCliente = await _context.Clientes.FindAsync(id);
                    if (dbCliente == null) return NotFound();

                    dbCliente.Cedula = cliente.Cedula;
                    dbCliente.RUC = cliente.RUC;
                    dbCliente.Nombres_Apellidos = cliente.Nombres_Apellidos;
                    dbCliente.Telefono = cliente.Telefono;
                    dbCliente.Direccion = cliente.Direccion;
                    dbCliente.Correo = cliente.Correo;
                    dbCliente.Ciudad = cliente.Ciudad;
                    dbCliente.TipoDocumento = cliente.TipoDocumento;
                    dbCliente.DatosAdicionales = cliente.DatosAdicionales;

                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cliente actualizado exitosamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error al actualizar: " + ex.Message;
                }
            }
            await LoadCustomFields(cliente.DatosAdicionales);
            return View(cliente);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(long id)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null) return Json(new { success = false, message = "Registro no encontrado." });

            try
            {
                _context.Clientes.Remove(cliente);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Cliente eliminado exitosamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al eliminar: " + ex.Message });
            }
        }

        private async Task LoadCustomFields(string? datosAdicionalesJson = null)
        {
            var ruc = await _context.ParametrosSistema.Select(p => p.RUC).FirstOrDefaultAsync();
            var empresa = await _adminContext.Empresas.FirstOrDefaultAsync(e => e.RUC == ruc);

            if (empresa != null)
            {
                ViewBag.CustomFields = await _camposService.GetCamposActivosAsync("Cliente", empresa.RubroId, empresa.Id);
                ViewBag.CustomFieldValues = _camposService.ParseDatosAdicionales(datosAdicionalesJson);
            }
            else
            {
                ViewBag.CustomFields = new List<CampoPersonalizado>();
                ViewBag.CustomFieldValues = new Dictionary<string, object>();
            }
        }
    }
}
