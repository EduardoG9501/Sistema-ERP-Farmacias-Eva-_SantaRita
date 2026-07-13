using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using SistemaERP.Models;
using SistemaERP.Models.Entities;
using SistemaERP.Areas.Admin.Models;
using SistemaERP.Areas.Admin.Services;
using Microsoft.AspNetCore.Authorization;

namespace SistemaERP.Controllers
{
    [Authorize]
    public class ImportacionMasivaController : Controller
    {
        private readonly ScmContext _context;
        private readonly AdminContext _adminContext;
        private readonly ICamposPersonalizadosService _camposService;

        public ImportacionMasivaController(
            ScmContext context,
            AdminContext adminContext,
            ICamposPersonalizadosService camposService)
        {
            _context = context;
            _adminContext = adminContext;
            _camposService = camposService;
        }

        private async Task<Empresa?> GetCurrentEmpresaAsync()
        {
            var empresaIdClaim = User.FindFirst("EmpresaId")?.Value;
            if (int.TryParse(empresaIdClaim, out int empresaId))
            {
                return await _adminContext.Empresas.FindAsync(empresaId);
            }
            var ruc = await _context.ParametrosSistema.Select(p => p.RUC).FirstOrDefaultAsync();
            if (!string.IsNullOrEmpty(ruc))
            {
                return await _adminContext.Empresas.FirstOrDefaultAsync(e => e.RUC == ruc);
            }
            return null;
        }

        private async Task<List<CampoPersonalizado>> GetCustomFieldsAsync(string entidad)
        {
            var empresa = await GetCurrentEmpresaAsync();
            if (empresa != null)
            {
                return await _camposService.GetCamposActivosAsync(entidad, empresa.RubroId, empresa.Id);
            }
            return new List<CampoPersonalizado>();
        }

        private async Task<bool> ValidarTablaAccesoAsync(string tabla)
        {
            if (string.IsNullOrWhiteSpace(tabla)) return false;
            var sanitizedInput = Regex.Replace(tabla, @"[^a-zA-Z0-9_]", "");
            if (sanitizedInput != tabla) return false;

            var empresa = await GetCurrentEmpresaAsync();
            if (empresa == null) return false;

            int currentEmpresaId = empresa.Id;
            int? currentRubroId = empresa.RubroId;

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
                                    return true;
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
            return false;
        }

        [HttpGet]
        public async Task<IActionResult> DescargarPlantilla(string catalogo, string? tabla = null)
        {
            using var workbook = new XLWorkbook();
            string nombreArchivo = $"Plantilla_{catalogo}";
            var ws = workbook.Worksheets.Add("Plantilla");

            int col = 1;

            if (string.Equals(catalogo, "bancos", StringComparison.OrdinalIgnoreCase))
            {
                ws.Cell(1, col).Value = "Banco";
                AddHeaderComment(ws, col++, "Nombre del Banco (Obligatorio)");

                ws.Cell(1, col).Value = "Telefono";
                AddHeaderComment(ws, col++, "Teléfono de contacto");

                ws.Cell(1, col).Value = "Correo";
                AddHeaderComment(ws, col++, "Correo de contacto");

                ws.Cell(1, col).Value = "Direccion";
                AddHeaderComment(ws, col++, "Dirección física");

                // Fila de ejemplo
                ws.Cell(2, 1).Value = "BANCO INTERNACIONAL";
                ws.Cell(2, 2).Value = "022999888";
                ws.Cell(2, 3).Value = "info@bancointernacional.com";
                ws.Cell(2, 4).Value = "Av. Patria y Amazonas";
            }
            else if (string.Equals(catalogo, "presentaciones", StringComparison.OrdinalIgnoreCase))
            {
                ws.Cell(1, col).Value = "Presentacion";
                AddHeaderComment(ws, col++, "Categoría o Presentación del Producto (Obligatorio)");

                ws.Cell(2, 1).Value = "UNIDADES";
            }
            else if (string.Equals(catalogo, "bodegas", StringComparison.OrdinalIgnoreCase))
            {
                ws.Cell(1, col).Value = "Nombre";
                AddHeaderComment(ws, col++, "Nombre de la Bodega (Obligatorio)");

                ws.Cell(1, col).Value = "Direccion";
                AddHeaderComment(ws, col++, "Dirección física de la bodega");

                ws.Cell(1, col).Value = "EsPrincipal";
                AddHeaderComment(ws, col++, "¿Es la bodega principal? (Sí / No)");

                ws.Cell(1, col).Value = "Estado";
                AddHeaderComment(ws, col++, "Estado de la bodega (Activo / Inactivo)");

                ws.Cell(2, 1).Value = "Bodega Norte";
                ws.Cell(2, 2).Value = "Av. Galo Plaza Lasso N54";
                ws.Cell(2, 3).Value = "No";
                ws.Cell(2, 4).Value = "Activo";
            }
            else if (string.Equals(catalogo, "clientes", StringComparison.OrdinalIgnoreCase))
            {
                ws.Cell(1, col).Value = "Nombres_Apellidos";
                AddHeaderComment(ws, col++, "Nombres y Apellidos del Cliente (Obligatorio)");

                ws.Cell(1, col).Value = "TipoDocumento";
                AddHeaderComment(ws, col++, "Tipo Documento (Opciones: CÉDULA, RUC, PASAPORTE, CONSUMIDOR FINAL, IDENTIFICACIÓN DEL EXTERIOR)");

                ws.Cell(1, col).Value = "Cedula";
                AddHeaderComment(ws, col++, "Cédula del cliente (Obligatorio si TipoDocumento es CÉDULA, 10 dígitos)");

                ws.Cell(1, col).Value = "RUC";
                AddHeaderComment(ws, col++, "RUC del cliente (Obligatorio si TipoDocumento es RUC, 13 dígitos)");

                ws.Cell(1, col).Value = "Telefono";
                AddHeaderComment(ws, col++, "Teléfono de contacto");

                ws.Cell(1, col).Value = "Correo";
                AddHeaderComment(ws, col++, "Correo electrónico");

                ws.Cell(1, col).Value = "Ciudad";
                AddHeaderComment(ws, col++, "Ciudad de residencia");

                ws.Cell(1, col).Value = "Direccion";
                AddHeaderComment(ws, col++, "Dirección domiciliaria");

                // Campos personalizados
                var customFields = await GetCustomFieldsAsync("Cliente");
                foreach (var f in customFields)
                {
                    ws.Cell(1, col).Value = f.Etiqueta;
                    string reqText = f.Requerido ? " (Obligatorio)" : "";
                    AddHeaderComment(ws, col++, $"Campo personalizado. Tipo: {f.TipoDato}{reqText}. {f.Tooltip}");
                }

                // Fila de ejemplo
                ws.Cell(2, 1).Value = "JUAN PEREZ ESPINOZA";
                ws.Cell(2, 2).Value = "CÉDULA";
                ws.Cell(2, 3).Value = "0999999999";
                ws.Cell(2, 4).Value = "";
                ws.Cell(2, 5).Value = "0987654321";
                ws.Cell(2, 6).Value = "juan.perez@example.com";
                ws.Cell(2, 7).Value = "Quito";
                ws.Cell(2, 8).Value = "Av. 10 de Agosto N23";
                
                int cCol = 9;
                foreach (var f in customFields)
                {
                    if (f.TipoDato.Equals("Booleano", StringComparison.OrdinalIgnoreCase))
                        ws.Cell(2, cCol++).Value = "No";
                    else if (f.TipoDato.Equals("Numero", StringComparison.OrdinalIgnoreCase))
                        ws.Cell(2, cCol++).Value = 0;
                    else if (f.TipoDato.Equals("Fecha", StringComparison.OrdinalIgnoreCase))
                        ws.Cell(2, cCol++).Value = DateTime.Now.ToString("yyyy-MM-dd");
                    else
                        ws.Cell(2, cCol++).Value = "Ejemplo";
                }
            }
            else if (string.Equals(catalogo, "proveedores", StringComparison.OrdinalIgnoreCase))
            {
                ws.Cell(1, col).Value = "Nombre";
                AddHeaderComment(ws, col++, "Nombre / Razón Social del Proveedor (Obligatorio)");

                ws.Cell(1, col).Value = "NombreComercial";
                AddHeaderComment(ws, col++, "Nombre comercial o de fantasía");

                ws.Cell(1, col).Value = "RUC";
                AddHeaderComment(ws, col++, "RUC legal (13 dígitos)");

                ws.Cell(1, col).Value = "Gerente";
                AddHeaderComment(ws, col++, "Representante legal / Gerente");

                ws.Cell(1, col).Value = "Correo";
                AddHeaderComment(ws, col++, "Correo electrónico");

                ws.Cell(1, col).Value = "Telefono";
                AddHeaderComment(ws, col++, "Teléfono de contacto");

                ws.Cell(1, col).Value = "Direccion";
                AddHeaderComment(ws, col++, "Dirección física");

                ws.Cell(1, col).Value = "AgenteRetencion";
                AddHeaderComment(ws, col++, "¿Es agente de retención? (Sí / No)");

                ws.Cell(1, col).Value = "Tipocontribuyente";
                AddHeaderComment(ws, col++, "Tipo de Contribuyente (Opciones: RIMPE, REGIMEN GENERAL, CONTRIBUYENTE ESPECIAL)");

                // Campos personalizados
                var customFields = await GetCustomFieldsAsync("Proveedor");
                foreach (var f in customFields)
                {
                    ws.Cell(1, col).Value = f.Etiqueta;
                    string reqText = f.Requerido ? " (Obligatorio)" : "";
                    AddHeaderComment(ws, col++, $"Campo personalizado. Tipo: {f.TipoDato}{reqText}. {f.Tooltip}");
                }

                // Fila de ejemplo
                ws.Cell(2, 1).Value = "IMPORTADORA INDUSTRIAL S.A.";
                ws.Cell(2, 2).Value = "Imp Industrial";
                ws.Cell(2, 3).Value = "1799999999001";
                ws.Cell(2, 4).Value = "Ing. Carlos Villacis";
                ws.Cell(2, 5).Value = "ventas@impindustrial.com";
                ws.Cell(2, 6).Value = "022999777";
                ws.Cell(2, 7).Value = "Vía a Daule Km 14";
                ws.Cell(2, 8).Value = "No";
                ws.Cell(2, 9).Value = "REGIMEN GENERAL";

                int cCol = 10;
                foreach (var f in customFields)
                {
                    if (f.TipoDato.Equals("Booleano", StringComparison.OrdinalIgnoreCase))
                        ws.Cell(2, cCol++).Value = "No";
                    else if (f.TipoDato.Equals("Numero", StringComparison.OrdinalIgnoreCase))
                        ws.Cell(2, cCol++).Value = 0;
                    else if (f.TipoDato.Equals("Fecha", StringComparison.OrdinalIgnoreCase))
                        ws.Cell(2, cCol++).Value = DateTime.Now.ToString("yyyy-MM-dd");
                    else
                        ws.Cell(2, cCol++).Value = "Ejemplo";
                }
            }
            else if (string.Equals(catalogo, "productos", StringComparison.OrdinalIgnoreCase))
            {
                ws.Cell(1, col).Value = "Nombre";
                AddHeaderComment(ws, col++, "Nombre descriptivo del producto (Obligatorio)");

                ws.Cell(1, col).Value = "CodigoBarra";
                AddHeaderComment(ws, col++, "Código de barras único");

                ws.Cell(1, col).Value = "Presentacion";
                AddHeaderComment(ws, col++, "Nombre de la Presentación / Categoría");

                ws.Cell(1, col).Value = "Proveedor";
                AddHeaderComment(ws, col++, "Nombre del Proveedor / Marca");

                ws.Cell(1, col).Value = "PVF";
                AddHeaderComment(ws, col++, "Precio de Venta Fracción (Distribuidor / Costo)");

                ws.Cell(1, col).Value = "PVP";
                AddHeaderComment(ws, col++, "Precio de Venta Público (PVP)");

                ws.Cell(1, col).Value = "Contiene";
                AddHeaderComment(ws, col++, "Cantidad de unidades por caja/empaque");

                ws.Cell(1, col).Value = "IVA";
                AddHeaderComment(ws, col++, "Porcentaje de IVA (Opciones comunes: 0, 15)");

                ws.Cell(1, col).Value = "Pvp_Iva";
                AddHeaderComment(ws, col++, "¿El PVP incluye IVA? (Sí / No)");

                ws.Cell(1, col).Value = "RegistroSanitario";
                AddHeaderComment(ws, col++, "Registro sanitario del producto");

                // Campos personalizados
                var customFields = await GetCustomFieldsAsync("Producto");
                foreach (var f in customFields)
                {
                    ws.Cell(1, col).Value = f.Etiqueta;
                    string reqText = f.Requerido ? " (Obligatorio)" : "";
                    AddHeaderComment(ws, col++, $"Campo personalizado. Tipo: {f.TipoDato}{reqText}. {f.Tooltip}");
                }

                // Fila de ejemplo
                ws.Cell(2, 1).Value = "RODAMIENTO SKF 6204";
                ws.Cell(2, 2).Value = "7316571987654";
                ws.Cell(2, 3).Value = "UNIDADES";
                ws.Cell(2, 4).Value = "IMPORTADORA INDUSTRIAL S.A.";
                ws.Cell(2, 5).Value = 4.50;
                ws.Cell(2, 6).Value = 7.50;
                ws.Cell(2, 7).Value = 1;
                ws.Cell(2, 8).Value = 15;
                ws.Cell(2, 9).Value = "Sí";
                ws.Cell(2, 10).Value = "N/A";

                int cCol = 11;
                foreach (var f in customFields)
                {
                    if (f.TipoDato.Equals("Booleano", StringComparison.OrdinalIgnoreCase))
                        ws.Cell(2, cCol++).Value = "No";
                    else if (f.TipoDato.Equals("Numero", StringComparison.OrdinalIgnoreCase))
                        ws.Cell(2, cCol++).Value = 0;
                    else if (f.TipoDato.Equals("Fecha", StringComparison.OrdinalIgnoreCase))
                        ws.Cell(2, cCol++).Value = DateTime.Now.ToString("yyyy-MM-dd");
                    else
                        ws.Cell(2, cCol++).Value = "Ejemplo";
                }
            }
            else if (string.Equals(catalogo, "dinamico", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(tabla) || !await ValidarTablaAccesoAsync(tabla))
                {
                    return BadRequest("Acceso denegado o tabla no válida para catálogo dinámico.");
                }

                nombreArchivo = $"Plantilla_Catalogo_{tabla}";

                ws.Cell(1, col).Value = "Nombre";
                AddHeaderComment(ws, col++, "Nombre descriptivo del elemento (Obligatorio)");

                ws.Cell(2, 1).Value = "Item Ejemplo A";
            }
            else if (string.Equals(catalogo, "ajustes", StringComparison.OrdinalIgnoreCase))
            {
                ws.Cell(1, col).Value = "CodigoBarra";
                AddHeaderComment(ws, col++, "Código de barras del producto (Opcional si se especifica el Nombre del Producto).");

                ws.Cell(1, col).Value = "Producto";
                AddHeaderComment(ws, col++, "Nombre del producto (Obligatorio si no se especifica el Código de Barra).");

                ws.Cell(1, col).Value = "Cantidad";
                AddHeaderComment(ws, col++, "Cantidad a ajustar (entero mayor a 0).");

                ws.Cell(1, col).Value = "TipoMovimiento";
                AddHeaderComment(ws, col++, "INGRESO o EGRESO (Obligatorio).");

                ws.Cell(1, col).Value = "Bodega";
                AddHeaderComment(ws, col++, "Nombre de la bodega. Ej: Bodega Principal (Si se deja vacío, se asume la bodega principal).");

                ws.Cell(1, col).Value = "PVF";
                AddHeaderComment(ws, col++, "Precio de costo/fracción (Opcional. Para ingresos, si no se envía se usa el actual).");

                ws.Cell(1, col).Value = "PVP";
                AddHeaderComment(ws, col++, "Precio de venta al público (Opcional. Si no se envía se usa el actual).");

                ws.Cell(1, col).Value = "Lote";
                AddHeaderComment(ws, col++, "Número de lote (Opcional).");

                ws.Cell(1, col).Value = "Vencimiento";
                AddHeaderComment(ws, col++, "Fecha de vencimiento en formato AAAA-MM-DD (Opcional).");

                // Fila de ejemplo
                ws.Cell(2, 1).Value = "7316571987654";
                ws.Cell(2, 2).Value = "RODAMIENTO SKF 6204";
                ws.Cell(2, 3).Value = 10;
                ws.Cell(2, 4).Value = "INGRESO";
                ws.Cell(2, 5).Value = "Bodega Principal";
                ws.Cell(2, 6).Value = 4.50;
                ws.Cell(2, 7).Value = 7.50;
                ws.Cell(2, 8).Value = "L-1002";
                ws.Cell(2, 9).Value = "2028-12-31";
            }
            else
            {
                return BadRequest("Catálogo especificado no válido.");
            }

            // Aplicar estilos premium a cabecera
            var headerRange = ws.Range(1, 1, 1, col - 1);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Font.FontSize = 11;
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E293B"); // Slate Gray elegante
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Row(1).Height = 26;

            // Bordes para los encabezados
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // Autoajustar columnas
            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{nombreArchivo}.xlsx");
        }

        private void AddHeaderComment(IXLWorksheet ws, int col, string commentText)
        {
            try
            {
                var comment = ws.Cell(1, col).GetComment();
                comment.AddText(commentText);
            }
            catch { }
        }

        [HttpPost]
        public async Task<IActionResult> Importar(string catalogo, IFormFile file, string? tabla = null)
        {
            if (file == null || file.Length == 0)
            {
                return Json(new { success = false, message = "Por favor seleccione un archivo Excel válido." });
            }

            try
            {
                using var stream = file.OpenReadStream();
                using var workbook = new XLWorkbook(stream);
                var ws = workbook.Worksheets.FirstOrDefault();
                if (ws == null)
                {
                    return Json(new { success = false, message = "El archivo Excel no contiene hojas de cálculo." });
                }

                // Generar mapa de cabeceras
                var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 1;
                for (int col = 1; col <= lastCol; col++)
                {
                    var headerVal = ws.Cell(1, col).GetString().Trim();
                    if (!string.IsNullOrEmpty(headerVal))
                    {
                        headerMap[headerVal] = col;
                    }
                }

                int totalRows = 0;
                int importedCount = 0;
                int failedCount = 0;
                var errors = new List<string>();

                var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
                if (lastRow < 2)
                {
                    return Json(new { success = false, message = "El archivo Excel no contiene filas de datos para importar." });
                }

                if (string.Equals(catalogo, "bancos", StringComparison.OrdinalIgnoreCase))
                {
                    for (int rowNum = 2; rowNum <= lastRow; rowNum++)
                    {
                        totalRows++;
                        var row = ws.Row(rowNum);
                        var nombre = GetCellValue(row, headerMap, "Banco");

                        if (string.IsNullOrEmpty(nombre))
                        {
                            failedCount++;
                            errors.Add($"Fila {rowNum}: El nombre del Banco es obligatorio.");
                            continue;
                        }

                        try
                        {
                            var existing = await _context.Bancos.FirstOrDefaultAsync(b => b.Nombre == nombre);
                            if (existing != null)
                            {
                                existing.Telefono = GetCellValue(row, headerMap, "Telefono");
                                existing.Correo = GetCellValue(row, headerMap, "Correo");
                                existing.Direccion = GetCellValue(row, headerMap, "Direccion");
                                _context.Update(existing);
                            }
                            else
                            {
                                var banco = new Banco
                                {
                                    Nombre = nombre,
                                    Telefono = GetCellValue(row, headerMap, "Telefono"),
                                    Correo = GetCellValue(row, headerMap, "Correo"),
                                    Direccion = GetCellValue(row, headerMap, "Direccion")
                                };
                                _context.Add(banco);
                            }
                            await _context.SaveChangesAsync();
                            importedCount++;
                        }
                        catch (Exception ex)
                        {
                            failedCount++;
                            errors.Add($"Fila {rowNum} ({nombre}): {ex.Message}");
                        }
                    }
                }
                else if (string.Equals(catalogo, "presentaciones", StringComparison.OrdinalIgnoreCase))
                {
                    for (int rowNum = 2; rowNum <= lastRow; rowNum++)
                    {
                        totalRows++;
                        var row = ws.Row(rowNum);
                        var nombre = GetCellValue(row, headerMap, "Presentacion");

                        if (string.IsNullOrEmpty(nombre))
                        {
                            failedCount++;
                            errors.Add($"Fila {rowNum}: El nombre de la Presentación es obligatorio.");
                            continue;
                        }

                        try
                        {
                            var existing = await _context.Presentaciones.FirstOrDefaultAsync(p => p.Nombre == nombre);
                            if (existing == null)
                            {
                                var pres = new Presentacion { Nombre = nombre };
                                _context.Add(pres);
                                await _context.SaveChangesAsync();
                            }
                            importedCount++;
                        }
                        catch (Exception ex)
                        {
                            failedCount++;
                            errors.Add($"Fila {rowNum} ({nombre}): {ex.Message}");
                        }
                    }
                }
                else if (string.Equals(catalogo, "bodegas", StringComparison.OrdinalIgnoreCase))
                {
                    for (int rowNum = 2; rowNum <= lastRow; rowNum++)
                    {
                        totalRows++;
                        var row = ws.Row(rowNum);
                        var nombre = GetCellValue(row, headerMap, "Nombre");

                        if (string.IsNullOrEmpty(nombre))
                        {
                            failedCount++;
                            errors.Add($"Fila {rowNum}: El nombre de la Bodega es obligatorio.");
                            continue;
                        }

                        try
                        {
                            bool esPrincipal = ResolveBoolean(GetCellValue(row, headerMap, "EsPrincipal"));
                            bool estado = ResolveBoolean(GetCellValue(row, headerMap, "Estado") ?? "Activo");

                            var existing = await _context.Bodegas.FirstOrDefaultAsync(b => b.Nombre == nombre);
                            if (existing != null)
                            {
                                existing.Direccion = GetCellValue(row, headerMap, "Direccion");
                                existing.EsPrincipal = esPrincipal;
                                existing.Estado = estado;
                                _context.Update(existing);
                            }
                            else
                            {
                                existing = new Bodega
                                {
                                    Nombre = nombre,
                                    Direccion = GetCellValue(row, headerMap, "Direccion"),
                                    EsPrincipal = esPrincipal,
                                    Estado = estado
                                };
                                _context.Add(existing);
                            }

                            await _context.SaveChangesAsync();

                            if (esPrincipal)
                            {
                                // Desmarcar otras principales
                                var otras = await _context.Bodegas.Where(b => b.Id != existing.Id && b.EsPrincipal).ToListAsync();
                                foreach (var o in otras)
                                {
                                    o.EsPrincipal = false;
                                    _context.Update(o);
                                }
                                await _context.SaveChangesAsync();
                            }

                            importedCount++;
                        }
                        catch (Exception ex)
                        {
                            failedCount++;
                            errors.Add($"Fila {rowNum} ({nombre}): {ex.Message}");
                        }
                    }
                }
                else if (string.Equals(catalogo, "clientes", StringComparison.OrdinalIgnoreCase))
                {
                    var customFields = await GetCustomFieldsAsync("Cliente");

                    for (int rowNum = 2; rowNum <= lastRow; rowNum++)
                    {
                        totalRows++;
                        var row = ws.Row(rowNum);
                        var nombres = GetCellValue(row, headerMap, "Nombres_Apellidos");

                        if (string.IsNullOrEmpty(nombres))
                        {
                            failedCount++;
                            errors.Add($"Fila {rowNum}: Los Nombres y Apellidos del cliente son obligatorios.");
                            continue;
                        }

                        try
                        {
                            string? tipoDocText = GetCellValue(row, headerMap, "TipoDocumento");
                            int tipoDoc = ResolveTipoDocumentoCliente(tipoDocText);
                            string? cedula = GetCellValue(row, headerMap, "Cedula");
                            string? ruc = GetCellValue(row, headerMap, "RUC");

                            // Validar obligatoriedad según TipoDocumento
                            if (tipoDoc == 5 && string.IsNullOrEmpty(cedula))
                            {
                                throw new Exception("La Cédula es obligatoria si el tipo de documento es CÉDULA.");
                            }
                            if (tipoDoc == 4 && string.IsNullOrEmpty(ruc))
                            {
                                throw new Exception("El RUC es obligatorio si el tipo de documento es RUC.");
                            }

                            // Buscar existente
                            Cliente? cliente = null;
                            if (tipoDoc == 5 && !string.IsNullOrEmpty(cedula))
                            {
                                cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.Cedula == cedula && c.TipoDocumento == 5);
                            }
                            else if (tipoDoc == 4 && !string.IsNullOrEmpty(ruc))
                            {
                                cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.RUC == ruc && c.TipoDocumento == 4);
                            }

                            if (cliente == null)
                            {
                                cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.Nombres_Apellidos == nombres);
                            }

                            long registroId = cliente?.Id ?? 0;

                            // Procesar campos personalizados
                            var customFieldsDict = new Dictionary<string, object>();
                            if (cliente != null && !string.IsNullOrEmpty(cliente.DatosAdicionales))
                            {
                                customFieldsDict = _camposService.ParseDatosAdicionales(cliente.DatosAdicionales);
                            }

                            foreach (var f in customFields)
                            {
                                if (headerMap.TryGetValue(f.Etiqueta, out int colIdx))
                                {
                                    var cellVal = row.Cell(colIdx).GetString().Trim();

                                    // Validar
                                    var (valido, msg) = await _camposService.ValidarCampoAsync(f, cellVal, _context, registroId);
                                    if (!valido)
                                    {
                                        throw new Exception($"Campo '{f.Etiqueta}' inválido: {msg}");
                                    }

                                    if (!string.IsNullOrEmpty(cellVal))
                                    {
                                        if (f.TipoDato.Equals("Lista", StringComparison.OrdinalIgnoreCase))
                                        {
                                            await RegistrarOpcionListaDinamicaAsync(f, cellVal);
                                        }
                                        customFieldsDict[f.NombreCampo] = cellVal;
                                    }
                                    else
                                    {
                                        customFieldsDict.Remove(f.NombreCampo);
                                    }
                                }
                            }

                            string jsonDatos = _camposService.SerializarDatosAdicionales(customFieldsDict);

                            if (cliente != null)
                            {
                                cliente.Nombres_Apellidos = nombres;
                                cliente.TipoDocumento = tipoDoc;
                                cliente.Cedula = cedula;
                                cliente.RUC = ruc;
                                cliente.Telefono = GetCellValue(row, headerMap, "Telefono");
                                cliente.Correo = GetCellValue(row, headerMap, "Correo");
                                cliente.Ciudad = GetCellValue(row, headerMap, "Ciudad");
                                cliente.Direccion = GetCellValue(row, headerMap, "Direccion");
                                cliente.DatosAdicionales = jsonDatos;
                                _context.Update(cliente);
                            }
                            else
                            {
                                cliente = new Cliente
                                {
                                    Nombres_Apellidos = nombres,
                                    TipoDocumento = tipoDoc,
                                    Cedula = cedula,
                                    RUC = ruc,
                                    Telefono = GetCellValue(row, headerMap, "Telefono"),
                                    Correo = GetCellValue(row, headerMap, "Correo"),
                                    Ciudad = GetCellValue(row, headerMap, "Ciudad"),
                                    Direccion = GetCellValue(row, headerMap, "Direccion"),
                                    DatosAdicionales = jsonDatos
                                };
                                _context.Add(cliente);
                            }

                            await _context.SaveChangesAsync();
                            importedCount++;
                        }
                        catch (Exception ex)
                        {
                            failedCount++;
                            errors.Add($"Fila {rowNum} ({nombres}): {ex.Message}");
                        }
                    }
                }
                else if (string.Equals(catalogo, "proveedores", StringComparison.OrdinalIgnoreCase))
                {
                    var customFields = await GetCustomFieldsAsync("Proveedor");

                    for (int rowNum = 2; rowNum <= lastRow; rowNum++)
                    {
                        totalRows++;
                        var row = ws.Row(rowNum);
                        var nombre = GetCellValue(row, headerMap, "Nombre");

                        if (string.IsNullOrEmpty(nombre))
                        {
                            failedCount++;
                            errors.Add($"Fila {rowNum}: El nombre o Razón Social del proveedor es obligatorio.");
                            continue;
                        }

                        try
                        {
                            string? ruc = GetCellValue(row, headerMap, "RUC");

                            Proveedor? proveedor = null;
                            if (!string.IsNullOrEmpty(ruc))
                            {
                                proveedor = await _context.Proveedores.FirstOrDefaultAsync(p => p.RUC == ruc);
                            }
                            if (proveedor == null)
                            {
                                proveedor = await _context.Proveedores.FirstOrDefaultAsync(p => p.Nombre == nombre);
                            }

                            long registroId = proveedor?.Id ?? 0;

                            // Procesar campos personalizados
                            var customFieldsDict = new Dictionary<string, object>();
                            if (proveedor != null && !string.IsNullOrEmpty(proveedor.DatosAdicionales))
                            {
                                customFieldsDict = _camposService.ParseDatosAdicionales(proveedor.DatosAdicionales);
                            }

                            foreach (var f in customFields)
                            {
                                if (headerMap.TryGetValue(f.Etiqueta, out int colIdx))
                                {
                                    var cellVal = row.Cell(colIdx).GetString().Trim();

                                    // Validar
                                    var (valido, msg) = await _camposService.ValidarCampoAsync(f, cellVal, _context, registroId);
                                    if (!valido)
                                    {
                                        throw new Exception($"Campo '{f.Etiqueta}' inválido: {msg}");
                                    }

                                    if (!string.IsNullOrEmpty(cellVal))
                                    {
                                        if (f.TipoDato.Equals("Lista", StringComparison.OrdinalIgnoreCase))
                                        {
                                            await RegistrarOpcionListaDinamicaAsync(f, cellVal);
                                        }
                                        customFieldsDict[f.NombreCampo] = cellVal;
                                    }
                                    else
                                    {
                                        customFieldsDict.Remove(f.NombreCampo);
                                    }
                                }
                            }

                            string jsonDatos = _camposService.SerializarDatosAdicionales(customFieldsDict);

                            string retencionText = GetCellValue(row, headerMap, "AgenteRetencion") ?? "";
                            string retencion = ResolveBoolean(retencionText) ? "Si" : "";

                            string contribuyente = GetCellValue(row, headerMap, "Tipocontribuyente") ?? "REGIMEN GENERAL";
                            contribuyente = contribuyente.ToUpper().Trim();

                            if (proveedor != null)
                            {
                                proveedor.Nombre = nombre;
                                proveedor.NombreComercial = GetCellValue(row, headerMap, "NombreComercial");
                                proveedor.RUC = ruc;
                                proveedor.Gerente = GetCellValue(row, headerMap, "Gerente");
                                proveedor.Correo = GetCellValue(row, headerMap, "Correo");
                                proveedor.Telefono = GetCellValue(row, headerMap, "Telefono");
                                proveedor.Direccion = GetCellValue(row, headerMap, "Direccion");
                                proveedor.AgenteRetencion = retencion;
                                proveedor.Tipocontribuyente = contribuyente;
                                proveedor.DatosAdicionales = jsonDatos;
                                _context.Update(proveedor);
                            }
                            else
                            {
                                proveedor = new Proveedor
                                {
                                    Nombre = nombre,
                                    NombreComercial = GetCellValue(row, headerMap, "NombreComercial"),
                                    RUC = ruc,
                                    Gerente = GetCellValue(row, headerMap, "Gerente"),
                                    Correo = GetCellValue(row, headerMap, "Correo"),
                                    Telefono = GetCellValue(row, headerMap, "Telefono"),
                                    Direccion = GetCellValue(row, headerMap, "Direccion"),
                                    AgenteRetencion = retencion,
                                    Tipocontribuyente = contribuyente,
                                    DatosAdicionales = jsonDatos
                                };
                                _context.Add(proveedor);
                            }

                            await _context.SaveChangesAsync();
                            importedCount++;
                        }
                        catch (Exception ex)
                        {
                            failedCount++;
                            errors.Add($"Fila {rowNum} ({nombre}): {ex.Message}");
                        }
                    }
                }
                else if (string.Equals(catalogo, "productos", StringComparison.OrdinalIgnoreCase))
                {
                    var customFields = await GetCustomFieldsAsync("Producto");

                    for (int rowNum = 2; rowNum <= lastRow; rowNum++)
                    {
                        totalRows++;
                        var row = ws.Row(rowNum);
                        var nombre = GetCellValue(row, headerMap, "Nombre");

                        if (string.IsNullOrEmpty(nombre))
                        {
                            failedCount++;
                            errors.Add($"Fila {rowNum}: El nombre del producto es obligatorio.");
                            continue;
                        }

                        try
                        {
                            string? codigoBarra = GetCellValue(row, headerMap, "CodigoBarra");

                            // Buscar o crear la Presentacion (Categoría)
                            string? presName = GetCellValue(row, headerMap, "Presentacion");
                            long? presentacionId = null;
                            if (!string.IsNullOrEmpty(presName))
                            {
                                var pres = await _context.Presentaciones.FirstOrDefaultAsync(p => p.Nombre == presName);
                                if (pres != null)
                                {
                                    presentacionId = pres.Id;
                                }
                                else
                                {
                                    var newPres = new Presentacion { Nombre = presName };
                                    _context.Presentaciones.Add(newPres);
                                    await _context.SaveChangesAsync();
                                    presentacionId = newPres.Id;
                                }
                            }

                            // Buscar o crear el Proveedor (Marca)
                            string? provName = GetCellValue(row, headerMap, "Proveedor");
                            long? proveedorId = null;
                            if (!string.IsNullOrEmpty(provName))
                            {
                                var prov = await _context.Proveedores.FirstOrDefaultAsync(p => p.Nombre == provName);
                                if (prov != null)
                                {
                                    proveedorId = prov.Id;
                                }
                                else
                                {
                                    var newProv = new Proveedor
                                    {
                                        Nombre = provName,
                                        RUC = "ND",
                                        Direccion = "ND",
                                        Telefono = "ND"
                                    };
                                    _context.Proveedores.Add(newProv);
                                    await _context.SaveChangesAsync();
                                    proveedorId = newProv.Id;
                                }
                            }

                            // Buscar producto existente
                            Producto? producto = null;
                            if (!string.IsNullOrEmpty(codigoBarra))
                            {
                                producto = await _context.Productos.FirstOrDefaultAsync(p => p.CodigoBarra == codigoBarra);
                            }
                            if (producto == null)
                            {
                                producto = await _context.Productos.FirstOrDefaultAsync(p => p.Nombre == nombre);
                            }

                            long registroId = producto?.Id ?? 0;

                            // Procesar campos personalizados
                            var customFieldsDict = new Dictionary<string, object>();
                            if (producto != null && !string.IsNullOrEmpty(producto.DatosAdicionales))
                            {
                                customFieldsDict = _camposService.ParseDatosAdicionales(producto.DatosAdicionales);
                            }

                            foreach (var f in customFields)
                            {
                                if (headerMap.TryGetValue(f.Etiqueta, out int colIdx))
                                {
                                    var cellVal = row.Cell(colIdx).GetString().Trim();

                                    // Validar
                                    var (valido, msg) = await _camposService.ValidarCampoAsync(f, cellVal, _context, registroId);
                                    if (!valido)
                                    {
                                        throw new Exception($"Campo '{f.Etiqueta}' inválido: {msg}");
                                    }

                                    if (!string.IsNullOrEmpty(cellVal))
                                    {
                                        if (f.TipoDato.Equals("Lista", StringComparison.OrdinalIgnoreCase))
                                        {
                                            await RegistrarOpcionListaDinamicaAsync(f, cellVal);
                                        }
                                        customFieldsDict[f.NombreCampo] = cellVal;
                                    }
                                    else
                                    {
                                        customFieldsDict.Remove(f.NombreCampo);
                                    }
                                }
                            }

                            // Asegurar que FotosAdicionales exista en el JSON
                            if (!customFieldsDict.ContainsKey("FotosAdicionales"))
                            {
                                customFieldsDict["FotosAdicionales"] = new List<string>();
                            }

                            string jsonDatos = _camposService.SerializarDatosAdicionales(customFieldsDict);

                            decimal? pvf = ParseDecimal(GetCellValue(row, headerMap, "PVF"));
                            decimal? pvp = ParseDecimal(GetCellValue(row, headerMap, "PVP"));
                            int? contiene = ParseInt(GetCellValue(row, headerMap, "Contiene")) ?? 1;
                            decimal? iva = ParseDecimal(GetCellValue(row, headerMap, "IVA")) ?? 0;
                            bool pvpIva = ResolveBoolean(GetCellValue(row, headerMap, "Pvp_Iva") ?? "Sí");

                            if (producto != null)
                            {
                                producto.Nombre = nombre;
                                producto.CodigoBarra = codigoBarra;
                                producto.Id_Presentacion = presentacionId;
                                producto.Id_Proveedor = proveedorId;
                                producto.PVF = pvf;
                                producto.PVP = pvp;
                                producto.Contiene = contiene;
                                producto.IVA = iva;
                                producto.Pvp_Iva = pvpIva;
                                producto.RegistroSanitario = GetCellValue(row, headerMap, "RegistroSanitario");
                                producto.DatosAdicionales = jsonDatos;
                                _context.Update(producto);
                            }
                            else
                            {
                                producto = new Producto
                                {
                                    Nombre = nombre,
                                    CodigoBarra = codigoBarra,
                                    Id_Presentacion = presentacionId,
                                    Id_Proveedor = proveedorId,
                                    PVF = pvf,
                                    PVP = pvp,
                                    Contiene = contiene,
                                    IVA = iva,
                                    Pvp_Iva = pvpIva,
                                    RegistroSanitario = GetCellValue(row, headerMap, "RegistroSanitario"),
                                    Estado = "Activo",
                                    Disponible = 0,
                                    DatosAdicionales = jsonDatos
                                };
                                _context.Add(producto);
                            }

                            await _context.SaveChangesAsync();
                            importedCount++;
                        }
                        catch (Exception ex)
                        {
                            failedCount++;
                            errors.Add($"Fila {rowNum} ({nombre}): {ex.Message}");
                        }
                    }
                }
                else if (string.Equals(catalogo, "dinamico", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrEmpty(tabla) || !await ValidarTablaAccesoAsync(tabla))
                    {
                        return Json(new { success = false, message = "Acceso denegado o tabla no válida para catálogo dinámico." });
                    }

                    var sanitizedTable = Regex.Replace(tabla, @"[^a-zA-Z0-9_]", "");
                    var conn = _context.Database.GetDbConnection();
                    if (conn.State != ConnectionState.Open)
                    {
                        await _context.Database.OpenConnectionAsync();
                    }

                    for (int rowNum = 2; rowNum <= lastRow; rowNum++)
                    {
                        totalRows++;
                        var row = ws.Row(rowNum);
                        var nombre = GetCellValue(row, headerMap, "Nombre");

                        if (string.IsNullOrEmpty(nombre))
                        {
                            failedCount++;
                            errors.Add($"Fila {rowNum}: El campo Nombre es obligatorio.");
                            continue;
                        }

                        try
                        {
                            bool exists = false;
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.CommandText = $"SELECT COUNT(*) FROM [{sanitizedTable}] WHERE Nombre = @nombre";
                                var pNombre = cmd.CreateParameter();
                                pNombre.ParameterName = "@nombre";
                                pNombre.Value = nombre;
                                cmd.Parameters.Add(pNombre);
                                exists = Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
                            }

                            if (!exists)
                            {
                                using (var cmd = conn.CreateCommand())
                                {
                                    cmd.CommandText = $"INSERT INTO [{sanitizedTable}] (Nombre) VALUES (@nombre)";
                                    var pNombre = cmd.CreateParameter();
                                    pNombre.ParameterName = "@nombre";
                                    pNombre.Value = nombre;
                                    cmd.Parameters.Add(pNombre);
                                    await cmd.ExecuteNonQueryAsync();
                                }
                            }
                            importedCount++;
                        }
                        catch (Exception ex)
                        {
                            failedCount++;
                            errors.Add($"Fila {rowNum} ({nombre}): {ex.Message}");
                        }
                    }
                }
                else if (string.Equals(catalogo, "ajustes", StringComparison.OrdinalIgnoreCase))
                {
                    var rowsToProcess = new List<AjusteExcelRow>();
                    for (int rowNum = 2; rowNum <= lastRow; rowNum++)
                    {
                        totalRows++;
                        var row = ws.Row(rowNum);

                        var codigoBarra = GetCellValue(row, headerMap, "CodigoBarra");
                        var productoNombre = GetCellValue(row, headerMap, "Producto");
                        var cantidadStr = GetCellValue(row, headerMap, "Cantidad");
                        var tipoMov = GetCellValue(row, headerMap, "TipoMovimiento");
                        var bodegaNombre = GetCellValue(row, headerMap, "Bodega");
                        var pvfStr = GetCellValue(row, headerMap, "PVF");
                        var pvpStr = GetCellValue(row, headerMap, "PVP");
                        var lote = GetCellValue(row, headerMap, "Lote");
                        var vencimientoStr = GetCellValue(row, headerMap, "Vencimiento");

                        if (string.IsNullOrEmpty(codigoBarra) && string.IsNullOrEmpty(productoNombre))
                        {
                            failedCount++;
                            errors.Add($"Fila {rowNum}: Debe especificar Código de Barra o Nombre de Producto.");
                            continue;
                        }

                        // Buscar Producto
                        Producto? prod = null;
                        if (!string.IsNullOrEmpty(codigoBarra))
                        {
                            var trimmedBarra = codigoBarra.Trim();
                            prod = await _context.Productos.FirstOrDefaultAsync(p => p.CodigoBarra == trimmedBarra);
                        }
                        if (prod == null && !string.IsNullOrEmpty(productoNombre))
                        {
                            var trimmedNombre = productoNombre.Trim();
                            prod = await _context.Productos.FirstOrDefaultAsync(p => p.Nombre != null && p.Nombre.Trim() == trimmedNombre);
                        }

                        if (prod == null)
                        {
                            failedCount++;
                            string identificador = !string.IsNullOrEmpty(codigoBarra) ? $"Código: '{codigoBarra}'" : $"Nombre: '{productoNombre}'";
                            errors.Add($"Fila {rowNum}: Producto no encontrado ({identificador}).");
                            continue;
                        }

                        // Validar Cantidad
                        var cantOpt = ParseInt(cantidadStr);
                        if (cantOpt == null || cantOpt.Value <= 0)
                        {
                            failedCount++;
                            errors.Add($"Fila {rowNum}: Cantidad inválida '{cantidadStr}'. Debe ser un número entero mayor a 0.");
                            continue;
                        }
                        int cantidad = cantOpt.Value;

                        // Validar TipoMovimiento
                        if (string.IsNullOrEmpty(tipoMov) ||
                            (!string.Equals(tipoMov, "INGRESO", StringComparison.OrdinalIgnoreCase) &&
                             !string.Equals(tipoMov, "EGRESO", StringComparison.OrdinalIgnoreCase)))
                        {
                            failedCount++;
                            errors.Add($"Fila {rowNum}: Tipo de movimiento inválido '{tipoMov}'. Debe ser INGRESO o EGRESO.");
                            continue;
                        }
                        string tipoMovimiento = tipoMov.ToUpper();

                        // Buscar Bodega
                        Bodega? bodega = null;
                        if (!string.IsNullOrEmpty(bodegaNombre))
                        {
                            bodega = await _context.Bodegas.FirstOrDefaultAsync(b => b.Nombre == bodegaNombre);
                            if (bodega == null)
                            {
                                failedCount++;
                                errors.Add($"Fila {rowNum}: Bodega '{bodegaNombre}' no encontrada.");
                                continue;
                            }
                        }
                        else
                        {
                            bodega = await _context.Bodegas.FirstOrDefaultAsync(b => b.EsPrincipal);
                            if (bodega == null)
                            {
                                bodega = await _context.Bodegas.FirstOrDefaultAsync();
                            }
                        }

                        if (bodega == null)
                        {
                            failedCount++;
                            errors.Add($"Fila {rowNum}: No se encontró ninguna bodega configurada en el sistema.");
                            continue;
                        }

                        // Parsear PVF / PVP
                        decimal? pvf = ParseDecimal(pvfStr);
                        decimal? pvp = ParseDecimal(pvpStr);

                        // Parsear Vencimiento
                        DateTime? vencimiento = null;
                        if (!string.IsNullOrEmpty(vencimientoStr))
                        {
                            if (DateTime.TryParse(vencimientoStr, System.Globalization.CultureInfo.InvariantCulture, out DateTime vUtc))
                            {
                                vencimiento = vUtc;
                            }
                            else if (DateTime.TryParse(vencimientoStr, out DateTime vLocal))
                            {
                                vencimiento = vLocal;
                            }
                            else
                            {
                                failedCount++;
                                errors.Add($"Fila {rowNum}: Formato de fecha de vencimiento inválido '{vencimientoStr}'. Use AAAA-MM-DD.");
                                continue;
                            }
                        }

                        rowsToProcess.Add(new AjusteExcelRow
                        {
                            RowNum = rowNum,
                            Producto = prod,
                            Cantidad = cantidad,
                            TipoMovimiento = tipoMovimiento,
                            Bodega = bodega,
                            PVF = pvf,
                            PVP = pvp,
                            Lote = lote,
                            Vencimiento = vencimiento
                        });
                    }

                    if (errors.Any())
                    {
                        return Json(new
                        {
                            success = true,
                            total = totalRows,
                            imported = 0,
                            failed = failedCount,
                            errors = errors
                        });
                    }

                    var strategy = _context.Database.CreateExecutionStrategy();
                    var resultJson = await strategy.ExecuteAsync(async () =>
                    {
                        using var transaction = await _context.Database.BeginTransactionAsync();
                        try
                        {
                            var groups = rowsToProcess.GroupBy(r => new { r.TipoMovimiento, r.Bodega.Id });

                            foreach (var group in groups)
                            {
                                int bodegaId = group.Key.Id;
                                string tipo = group.Key.TipoMovimiento;
                                var bodega = group.First().Bodega;

                                if (tipo == "INGRESO")
                                {
                                    var ajuste = new CargoAjuste
                                    {
                                        Movimiento = "AJUSTE MASIVO EXCEL",
                                        Observacion = $"Importación Masiva Excel - {DateTime.Now:dd/MM/yyyy HH:mm}",
                                        FechaHora_Registro = DateTime.Now,
                                        Id_Resp = 1,
                                        Estado = "Procesado",
                                        Id_Bodega = bodegaId,
                                        Almacen = bodega.Nombre
                                    };
                                    _context.AjustesIngreso.Add(ajuste);
                                    await _context.SaveChangesAsync();

                                    foreach (var item in group)
                                    {
                                        var prod = item.Producto;
                                        int cantidad = item.Cantidad;

                                        _context.AjustesIngresoDetalle.Add(new CargoAjuste_Producto
                                        {
                                            Id_Factura = ajuste.Id,
                                            Id_Producto = prod.Id.ToString(),
                                            Producto = prod.Nombre,
                                            Cantidad = cantidad,
                                            Lote = item.Lote,
                                            FechaVence = item.Vencimiento,
                                            EsCaja = false,
                                            Contiene = prod.Contiene,
                                            PVF = item.PVF ?? prod.PVF ?? 0,
                                            PVP = item.PVP ?? prod.PVP ?? 0,
                                            Subtotal = cantidad * (item.PVF ?? prod.PVF ?? 0),
                                            Id_Bodega = bodegaId,
                                            Disponible = (prod.Disponible ?? 0) + cantidad
                                        });

                                        prod.Disponible = (prod.Disponible ?? 0) + cantidad;
                                        if (!string.IsNullOrEmpty(item.Lote)) prod.Lote = item.Lote;
                                        if (item.Vencimiento.HasValue) prod.Vencimiento = item.Vencimiento;
                                        _context.Update(prod);

                                        var stockBodega = _context.StockBodegas.Local.FirstOrDefault(sb => sb.Id_Bodega == bodegaId && sb.Id_Producto == prod.Id.ToString());
                                        if (stockBodega == null)
                                        {
                                            stockBodega = await _context.StockBodegas.FirstOrDefaultAsync(sb => sb.Id_Bodega == bodegaId && sb.Id_Producto == prod.Id.ToString());
                                        }
                                        if (stockBodega == null)
                                        {
                                            stockBodega = new StockBodega { Id_Bodega = bodegaId, Id_Producto = prod.Id.ToString(), Stock = 0 };
                                            _context.StockBodegas.Add(stockBodega);
                                        }
                                        stockBodega.Stock += cantidad;

                                        _context.Kardex.Add(new Stock_Producto
                                        {
                                            Id_Producto = prod.Id.ToString(),
                                            Producto = prod.Nombre,
                                            Cantidad = cantidad,
                                            TipoMovimiento = "INGRESO POR AJUSTE",
                                            Estado = true,
                                            FechaRegistro = DateTime.Now,
                                            Stock = (int?)stockBodega.Stock,
                                            Id_Bodega = bodegaId,
                                            Id_Usuario = 1
                                        });
                                    }
                                }
                                else // EGRESO
                                {
                                    var ajuste = new DescargoAjuste
                                    {
                                        Movimiento = "AJUSTE MASIVO EXCEL",
                                        Observacion = $"Importación Masiva Excel - {DateTime.Now:dd/MM/yyyy HH:mm}",
                                        FechaHora_Registro = DateTime.Now,
                                        Id_Resp = 1,
                                        Estado = "Procesado",
                                        Id_Bodega = bodegaId,
                                        Almacen = bodega.Nombre
                                    };
                                    _context.AjustesEgreso.Add(ajuste);
                                    await _context.SaveChangesAsync();

                                    foreach (var item in group)
                                    {
                                        var prod = item.Producto;
                                        int cantidad = item.Cantidad;

                                        _context.AjustesEgresoDetalle.Add(new DescargoAjuste_Producto
                                        {
                                            Id_Factura = ajuste.Id,
                                            Id_Producto = prod.Id.ToString(),
                                            Producto = prod.Nombre,
                                            Cantidad = cantidad,
                                            PVP = item.PVP ?? prod.PVP ?? 0,
                                            Subtotal = cantidad * (item.PVP ?? prod.PVP ?? 0),
                                            Disponible = (prod.Disponible ?? 0) - cantidad,
                                            Lote = item.Lote,
                                            FechaVence = item.Vencimiento,
                                            EsCaja = false
                                        });

                                        prod.Disponible = (prod.Disponible ?? 0) - cantidad;
                                        _context.Update(prod);

                                        var stockBodega = _context.StockBodegas.Local.FirstOrDefault(sb => sb.Id_Bodega == bodegaId && sb.Id_Producto == prod.Id.ToString());
                                        if (stockBodega == null)
                                        {
                                            stockBodega = await _context.StockBodegas.FirstOrDefaultAsync(sb => sb.Id_Bodega == bodegaId && sb.Id_Producto == prod.Id.ToString());
                                        }
                                        if (stockBodega == null)
                                        {
                                            stockBodega = new StockBodega { Id_Bodega = bodegaId, Id_Producto = prod.Id.ToString(), Stock = 0 };
                                            _context.StockBodegas.Add(stockBodega);
                                        }
                                        stockBodega.Stock -= cantidad;

                                        _context.Kardex.Add(new Stock_Producto
                                        {
                                            Id_Producto = prod.Id.ToString(),
                                            Producto = prod.Nombre,
                                            Cantidad = -cantidad,
                                            TipoMovimiento = "EGRESO POR AJUSTE",
                                            Estado = true,
                                            FechaRegistro = DateTime.Now,
                                            Stock = (int?)stockBodega.Stock,
                                            Id_Bodega = bodegaId,
                                            Id_Usuario = 1
                                        });
                                    }
                                }
                            }

                            await _context.SaveChangesAsync();
                            await transaction.CommitAsync();
                            importedCount = rowsToProcess.Count;

                            return Json(new
                            {
                                success = true,
                                total = totalRows,
                                imported = importedCount,
                                failed = 0,
                                errors = errors
                            });
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync();
                            return Json(new { success = false, message = "Error al guardar la importación: " + ex.Message });
                        }
                    });

                    return resultJson;
                }
                else
                {
                    return Json(new { success = false, message = "Catálogo especificado no válido." });
                }

                return Json(new
                {
                    success = true,
                    total = totalRows,
                    imported = importedCount,
                    failed = failedCount,
                    errors = errors
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al procesar el archivo Excel: " + ex.Message });
            }
        }

        private async Task RegistrarOpcionListaDinamicaAsync(CampoPersonalizado campo, string valorStr)
        {
            if (string.IsNullOrEmpty(valorStr)) return;

            bool esTabla = false;
            string tablaNombre = "";
            if (!string.IsNullOrEmpty(campo.OpcionesLista) && campo.OpcionesLista.Trim().StartsWith("{"))
            {
                try
                {
                    using (var doc = JsonDocument.Parse(campo.OpcionesLista))
                    {
                        if (doc.RootElement.TryGetProperty("EsTabla", out var esTablaProp) && esTablaProp.GetBoolean())
                        {
                            esTabla = true;
                            if (doc.RootElement.TryGetProperty("TablaNombre", out var tablaProp))
                            {
                                tablaNombre = tablaProp.GetString() ?? "";
                            }
                        }
                    }
                }
                catch { }
            }

            if (esTabla && !string.IsNullOrEmpty(tablaNombre))
            {
                try
                {
                    var conn = _context.Database.GetDbConnection();
                    if (conn.State != ConnectionState.Open)
                    {
                        await _context.Database.OpenConnectionAsync();
                    }

                    bool exists = false;
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = $"SELECT COUNT(*) FROM [{tablaNombre}] WHERE Nombre = @nombre";
                        var pNombre = cmd.CreateParameter();
                        pNombre.ParameterName = "@nombre";
                        pNombre.Value = valorStr;
                        cmd.Parameters.Add(pNombre);
                        exists = Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
                    }

                    if (!exists)
                    {
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = $"INSERT INTO [{tablaNombre}] (Nombre) VALUES (@nombre)";
                            var pNombre = cmd.CreateParameter();
                            pNombre.ParameterName = "@nombre";
                            pNombre.Value = valorStr;
                            cmd.Parameters.Add(pNombre);
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }
                catch { }
            }
        }

        private string? GetCellValue(IXLRow row, Dictionary<string, int> headerMap, string columnName)
        {
            if (headerMap.TryGetValue(columnName, out int colIndex))
            {
                return row.Cell(colIndex).GetString().Trim();
            }
            return null;
        }

        private bool ResolveBoolean(string? val)
        {
            if (string.IsNullOrEmpty(val)) return false;
            val = val.ToLower().Trim();
            return val == "sí" || val == "si" || val == "yes" || val == "true" || val == "1" || val == "activo";
        }

        private int ResolveTipoDocumentoCliente(string? val)
        {
            if (string.IsNullOrEmpty(val)) return 5; // Cédula por defecto
            val = val.ToUpper().Trim();
            if (val.Contains("RUC") || val == "4") return 4;
            if (val.Contains("CÉDULA") || val.Contains("CEDULA") || val == "5") return 5;
            if (val.Contains("PASAPORTE") || val == "7") return 7;
            if (val.Contains("CONSUMIDOR FINAL") || val.Contains("CONSUMIDOR_FINAL") || val == "9") return 9;
            if (val.Contains("EXTERIOR") || val == "8") return 8;
            return 5;
        }

        private decimal? ParseDecimal(string? val)
        {
            if (string.IsNullOrEmpty(val)) return null;
            if (decimal.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal d))
                return d;
            if (decimal.TryParse(val, out decimal dLocal))
                return dLocal;
            return null;
        }

        private int? ParseInt(string? val)
        {
            if (string.IsNullOrEmpty(val)) return null;
            if (int.TryParse(val, out int i))
                return i;
            return null;
        }

        private class AjusteExcelRow
        {
            public int RowNum { get; set; }
            public Producto Producto { get; set; } = null!;
            public int Cantidad { get; set; }
            public string TipoMovimiento { get; set; } = null!;
            public Bodega Bodega { get; set; } = null!;
            public decimal? PVF { get; set; }
            public decimal? PVP { get; set; }
            public string? Lote { get; set; }
            public DateTime? Vencimiento { get; set; }
        }
    }
}
