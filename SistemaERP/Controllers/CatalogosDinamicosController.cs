using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaERP.Models;
using SistemaERP.Areas.Admin.Models;

namespace SistemaERP.Controllers
{
    public class CatalogosDinamicosController : Controller
    {
        private readonly ScmContext _context;
        private readonly AdminContext _adminContext;

        public CatalogosDinamicosController(ScmContext context, AdminContext adminContext)
        {
            _context = context;
            _adminContext = adminContext;
        }

        private async Task<(bool EsValido, string LabelVisual)> ValidarTablaAccesoAsync(string tabla)
        {
            if (string.IsNullOrWhiteSpace(tabla)) return (false, "");

            // Sanitizar nombre de tabla (solo caracteres alfanuméricos)
            var sanitizedInput = Regex.Replace(tabla, @"[^a-zA-Z0-9]", "");
            if (sanitizedInput != tabla) return (false, "");

            // Obtener el EmpresaId del usuario en sesión
            var empresaIdClaim = User.FindFirst("EmpresaId")?.Value;
            if (!int.TryParse(empresaIdClaim, out int currentEmpresaId))
            {
                return (false, "");
            }

            // Cargar la empresa para obtener su RubroId
            var empresa = await _adminContext.Empresas.FindAsync(currentEmpresaId);
            int? currentRubroId = empresa?.RubroId;

            // Consultar campos personalizados tipo Lista activos que puedan ser tablas
            var camposLista = await _adminContext.CamposPersonalizados
                .Where(c => c.Activo && c.TipoDato == "Lista" &&
                            (c.EmpresaId == null || c.EmpresaId == currentEmpresaId) &&
                            (c.RubroId == null || c.RubroId == currentRubroId))
                .ToListAsync();

            foreach (var campo in camposLista)
            {
                if (!string.IsNullOrEmpty(campo.OpcionesLista) && campo.OpcionesLista.Trim().StartsWith("{"))
                {
                    try
                    {
                        using (var doc = JsonDocument.Parse(campo.OpcionesLista))
                        {
                            if (doc.RootElement.TryGetProperty("EsTabla", out var esTablaProp) && esTablaProp.GetBoolean())
                            {
                                var tableName = doc.RootElement.GetProperty("TablaNombre").GetString();
                                if (string.Equals(tableName, tabla, StringComparison.OrdinalIgnoreCase))
                                {
                                    return (true, campo.Etiqueta);
                                }
                            }
                        }
                    }
                    catch { }
                }
            }

            return (false, "");
        }

        public async Task<IActionResult> Index(string tabla)
        {
            var validacion = await ValidarTablaAccesoAsync(tabla);
            if (!validacion.EsValido)
            {
                return BadRequest("Catálogo dinámico no válido o sin acceso autorizado.");
            }

            ViewBag.Tabla = tabla;
            ViewBag.LabelVisual = validacion.LabelVisual;

            var list = new List<Tuple<int, string>>();
            try
            {
                var conn = _context.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open)
                {
                    await _context.Database.OpenConnectionAsync();
                }

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $"SELECT Id, Nombre FROM [{tabla}] ORDER BY Nombre";
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            list.Add(Tuple.Create(reader.GetInt32(0), reader.GetString(1)));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar los datos del catálogo: {ex.Message}";
            }

            return View(list);
        }

        public async Task<IActionResult> Create(string tabla)
        {
            var validacion = await ValidarTablaAccesoAsync(tabla);
            if (!validacion.EsValido)
            {
                return BadRequest("Catálogo dinámico no válido.");
            }

            ViewBag.Tabla = tabla;
            ViewBag.LabelVisual = validacion.LabelVisual;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string tabla, string nombre)
        {
            var validacion = await ValidarTablaAccesoAsync(tabla);
            if (!validacion.EsValido)
            {
                return BadRequest("Catálogo dinámico no válido.");
            }

            if (string.IsNullOrWhiteSpace(nombre))
            {
                ModelState.AddModelError("Nombre", "El nombre es requerido.");
                ViewBag.Tabla = tabla;
                ViewBag.LabelVisual = validacion.LabelVisual;
                return View();
            }

            try
            {
                var conn = _context.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open)
                {
                    await _context.Database.OpenConnectionAsync();
                }

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $"INSERT INTO [{tabla}] (Nombre) VALUES (@nombre)";
                    var pNombre = cmd.CreateParameter();
                    pNombre.ParameterName = "@nombre";
                    pNombre.Value = nombre;
                    cmd.Parameters.Add(pNombre);

                    await cmd.ExecuteNonQueryAsync();
                }

                TempData["Success"] = "Elemento creado exitosamente en el catálogo.";
                return RedirectToAction(nameof(Index), new { tabla });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error al insertar el registro: {ex.Message}");
                ViewBag.Tabla = tabla;
                ViewBag.LabelVisual = validacion.LabelVisual;
                return View();
            }
        }

        public async Task<IActionResult> Edit(string tabla, int id)
        {
            var validacion = await ValidarTablaAccesoAsync(tabla);
            if (!validacion.EsValido)
            {
                return BadRequest("Catálogo dinámico no válido.");
            }

            ViewBag.Tabla = tabla;
            ViewBag.LabelVisual = validacion.LabelVisual;
            ViewBag.Id = id;

            string nombre = null;
            try
            {
                var conn = _context.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open)
                {
                    await _context.Database.OpenConnectionAsync();
                }

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $"SELECT Nombre FROM [{tabla}] WHERE Id = @id";
                    var pId = cmd.CreateParameter();
                    pId.ParameterName = "@id";
                    pId.Value = id;
                    cmd.Parameters.Add(pId);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            nombre = reader.GetString(0);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al buscar el registro: {ex.Message}";
                return RedirectToAction(nameof(Index), new { tabla });
            }

            if (nombre == null) return NotFound();

            ViewBag.Nombre = nombre;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string tabla, int id, string nombre)
        {
            var validacion = await ValidarTablaAccesoAsync(tabla);
            if (!validacion.EsValido)
            {
                return BadRequest("Catálogo dinámico no válido.");
            }

            if (string.IsNullOrWhiteSpace(nombre))
            {
                ModelState.AddModelError("Nombre", "El nombre es requerido.");
                ViewBag.Tabla = tabla;
                ViewBag.LabelVisual = validacion.LabelVisual;
                ViewBag.Id = id;
                ViewBag.Nombre = nombre;
                return View();
            }

            try
            {
                var conn = _context.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open)
                {
                    await _context.Database.OpenConnectionAsync();
                }

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $"UPDATE [{tabla}] SET Nombre = @nombre WHERE Id = @id";
                    
                    var pNombre = cmd.CreateParameter();
                    pNombre.ParameterName = "@nombre";
                    pNombre.Value = nombre;
                    cmd.Parameters.Add(pNombre);

                    var pId = cmd.CreateParameter();
                    pId.ParameterName = "@id";
                    pId.Value = id;
                    cmd.Parameters.Add(pId);

                    await cmd.ExecuteNonQueryAsync();
                }

                TempData["Success"] = "Elemento actualizado exitosamente en el catálogo.";
                return RedirectToAction(nameof(Index), new { tabla });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error al actualizar el registro: {ex.Message}");
                ViewBag.Tabla = tabla;
                ViewBag.LabelVisual = validacion.LabelVisual;
                ViewBag.Id = id;
                ViewBag.Nombre = nombre;
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string tabla, int id)
        {
            var validacion = await ValidarTablaAccesoAsync(tabla);
            if (!validacion.EsValido)
            {
                return BadRequest("Catálogo dinámico no válido.");
            }

            try
            {
                var conn = _context.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open)
                {
                    await _context.Database.OpenConnectionAsync();
                }

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $"DELETE FROM [{tabla}] WHERE Id = @id";
                    var pId = cmd.CreateParameter();
                    pId.ParameterName = "@id";
                    pId.Value = id;
                    cmd.Parameters.Add(pId);

                    await cmd.ExecuteNonQueryAsync();
                }

                TempData["Success"] = "Elemento eliminado del catálogo.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al eliminar el registro: {ex.Message}";
            }

            return RedirectToAction(nameof(Index), new { tabla });
        }
    }
}
