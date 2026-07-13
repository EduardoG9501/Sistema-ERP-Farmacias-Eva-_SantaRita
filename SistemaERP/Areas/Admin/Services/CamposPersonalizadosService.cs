using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SistemaERP.Models;
using SistemaERP.Areas.Admin.Models;

namespace SistemaERP.Areas.Admin.Services
{
    public interface ICamposPersonalizadosService
    {
        Task<List<CampoPersonalizado>> GetCamposActivosAsync(string entidad, int? rubroId, int? empresaId);
        Dictionary<string, object> ParseDatosAdicionales(string? json);
        string SerializarDatosAdicionales(Dictionary<string, object> datos);
        Task<(bool EsValido, string Mensaje)> ValidarCampoAsync(CampoPersonalizado campo, object? valor, ScmContext dbContext, long idRegistroActual);
    }

    public class CamposPersonalizadosService : ICamposPersonalizadosService
    {
        private readonly AdminContext _adminContext;

        public CamposPersonalizadosService(AdminContext adminContext)
        {
            _adminContext = adminContext;
        }

        public async Task<List<CampoPersonalizado>> GetCamposActivosAsync(string entidad, int? rubroId, int? empresaId)
        {
            // Obtener los campos activos para la entidad especificada.
            // Primero, cargamos los campos globales (rubroId y empresaId son nulos).
            // Segundo, los de este rubro.
            // Tercero, los específicos de esta empresa.
            return await _adminContext.CamposPersonalizados
                .Where(c => c.Activo && 
                            c.Entidad.ToLower() == entidad.ToLower() &&
                            (c.EmpresaId == null || c.EmpresaId == empresaId) &&
                            (c.RubroId == null || c.RubroId == rubroId))
                .OrderBy(c => c.OrdenVisualizacion)
                .ToListAsync();
        }

        public Dictionary<string, object> ParseDatosAdicionales(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new Dictionary<string, object>();
            }

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(json, options);
                return dict ?? new Dictionary<string, object>();
            }
            catch
            {
                return new Dictionary<string, object>();
            }
        }

        public string SerializarDatosAdicionales(Dictionary<string, object> datos)
        {
            if (datos == null || !datos.Any())
            {
                return "{}";
            }

            try
            {
                return JsonSerializer.Serialize(datos);
            }
            catch
            {
                return "{}";
            }
        }

        public async Task<(bool EsValido, string Mensaje)> ValidarCampoAsync(
            CampoPersonalizado campo, 
            object? valor, 
            ScmContext dbContext, 
            long idRegistroActual)
        {
            // 1. Validar si es requerido
            if (campo.Requerido && (valor == null || string.IsNullOrWhiteSpace(valor.ToString())))
            {
                return (false, $"El campo '{campo.Etiqueta}' es obligatorio.");
            }

            var valorStr = valor?.ToString();
            if (string.IsNullOrWhiteSpace(valorStr))
            {
                return (true, string.Empty); // Si no tiene valor y no es requerido, es válido
            }

            // 2. Validaciones específicas para Texto
            if (campo.TipoDato.Equals("Texto", StringComparison.OrdinalIgnoreCase))
            {
                if (campo.MinLongitud.HasValue && valorStr.Length < campo.MinLongitud.Value)
                {
                    return (false, $"El campo '{campo.Etiqueta}' debe tener al menos {campo.MinLongitud.Value} caracteres.");
                }

                if (campo.MaxLongitud.HasValue && valorStr.Length > campo.MaxLongitud.Value)
                {
                    return (false, $"El campo '{campo.Etiqueta}' no puede superar los {campo.MaxLongitud.Value} caracteres.");
                }

                if (!string.IsNullOrEmpty(campo.ExpresionRegular))
                {
                    try
                    {
                        var regex = new Regex(campo.ExpresionRegular);
                        if (!regex.IsMatch(valorStr))
                        {
                            return (false, $"El campo '{campo.Etiqueta}' no cumple con el formato requerido.");
                        }
                    }
                    catch
                    {
                        // Expresión regular inválida configurada por el admin, omitimos
                    }
                }
            }

            // 3. Validaciones específicas para Número
            if (campo.TipoDato.Equals("Numero", StringComparison.OrdinalIgnoreCase))
            {
                if (!decimal.TryParse(valorStr, out decimal numValor))
                {
                    return (false, $"El campo '{campo.Etiqueta}' debe ser un número válido.");
                }

                if (campo.ValorMinimo.HasValue && numValor < campo.ValorMinimo.Value)
                {
                    return (false, $"El campo '{campo.Etiqueta}' no puede ser menor a {campo.ValorMinimo.Value}.");
                }

                if (campo.ValorMaximo.HasValue && numValor > campo.ValorMaximo.Value)
                {
                    return (false, $"El campo '{campo.Etiqueta}' no puede ser mayor a {campo.ValorMaximo.Value}.");
                }
            }

            // 4. Validación de Unicidad (EsUnico = 1)
            if (campo.EsUnico)
            {
                string tablaDestino = ObtenerNombreTabla(campo.Entidad);
                if (!string.IsNullOrEmpty(tablaDestino))
                {
                    // Query T-SQL dinámico ultra seguro usando JSON_VALUE
                    var sqlQuery = $@"
                        SELECT COUNT(*) 
                        FROM [{tablaDestino}] 
                        WHERE JSON_VALUE(DatosAdicionales, '$.{campo.NombreCampo}') = @valor 
                          AND Id != @id";

                    var valorParam = new SqlParameter("@valor", valorStr);
                    var idParam = new SqlParameter("@id", idRegistroActual);

                    try
                    {
                        var connection = dbContext.Database.GetDbConnection();
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = sqlQuery;
                            command.Parameters.Add(valorParam);
                            command.Parameters.Add(idParam);

                            if (connection.State != System.Data.ConnectionState.Open)
                            {
                                await connection.OpenAsync();
                            }

                            var countResult = await command.ExecuteScalarAsync();
                            int count = Convert.ToInt32(countResult);

                            if (count > 0)
                            {
                                return (false, $"El valor '{valorStr}' para el campo '{campo.Etiqueta}' ya existe y debe ser único.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // En caso de que falle por no existir la columna DatosAdicionales aún en la DB cliente
                        return (true, string.Empty);
                    }
                }
            }

            return (true, string.Empty);
        }

        private string ObtenerNombreTabla(string entidad)
        {
            return entidad.ToLower() switch
            {
                "producto" => "Productos",
                "cliente" => "Clientes",
                "proveedor" => "Proveedores",
                "venta" => "Ventas",
                "compra" => "CargoCompra",
                "traslado" => "Traslados",
                _ => string.Empty
            };
        }
    }
}
