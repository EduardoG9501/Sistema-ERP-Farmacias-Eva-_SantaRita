using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaERP.Models;
using SistemaERP.Models.Entities;

namespace SistemaERP.Controllers
{
    public class AjustesSRIController : Controller
    {
        private readonly ScmContext _context;

        public AjustesSRIController(ScmContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Siempre traer el primer registro ordenado por Id para ser consistente
            var config = await _context.ConfiguracionSRI
                .OrderBy(c => c.Id)
                .FirstOrDefaultAsync();

            if (config == null)
            {
                config = new ConfiguracionSRI { Entorno = "PRUEBAS" };
            }

            return View(config);
        }

        [HttpPost]
        public async Task<IActionResult> Guardar(ConfiguracionSRI model)
        {
            try
            {
                // Buscar siempre el registro real en la BD (por Id o el primero)
                ConfiguracionSRI? existing = null;

                if (model.Id > 0)
                {
                    existing = await _context.ConfiguracionSRI.FindAsync(model.Id);
                }

                // Fallback: tomar el primero disponible
                if (existing == null)
                {
                    existing = await _context.ConfiguracionSRI
                        .OrderBy(c => c.Id)
                        .FirstOrDefaultAsync();
                }

                if (existing == null)
                {
                    // No existe ningún registro — crear uno nuevo
                    model.UltimaActualizacion = DateTime.Now;
                    _context.ConfiguracionSRI.Add(model);
                }
                else
                {
                    // Actualizar campo por campo para no perder datos no incluidos en el form
                    existing.Entorno                      = model.Entorno;
                    existing.AgenteRetencion              = model.AgenteRetencion;
                    existing.NumeroResolucion             = model.NumeroResolucion;
                    existing.ContribuyenteEspecialNro     = model.ContribuyenteEspecialNro;
                    existing.TipoComprobanteRetencion     = model.TipoComprobanteRetencion;
                    existing.CodDocSustento               = model.CodDocSustento;
                    existing.RUC_Emisor                   = model.RUC_Emisor;
                    existing.RazonSocial                  = model.RazonSocial;
                    existing.NombreComercial               = model.NombreComercial;
                    existing.Establecimiento              = model.Establecimiento;
                    existing.PuntoEmision                 = model.PuntoEmision;
                    existing.CodigoNumerico               = model.CodigoNumerico;
                    existing.DireccionMatriz              = model.DireccionMatriz;
                    existing.DireccionEstablecimiento     = model.DireccionEstablecimiento;
                    existing.Regimen                      = model.Regimen;
                    existing.ObligadoContabilidad         = model.ObligadoContabilidad;
                    existing.MontoMinimoConsumidorFinal   = model.MontoMinimoConsumidorFinal;
                    existing.UrlSriLocal                  = model.UrlSriLocal;
                    existing.ImpresoraTickets             = model.ImpresoraTickets;
                    existing.ActivarServicioSRI           = model.ActivarServicioSRI;
                    existing.ActivarEnvioDesdeVentas      = model.ActivarEnvioDesdeVentas;
                    existing.PathFileCertificado          = model.PathFileCertificado;
                    existing.PathFileSaveXml              = model.PathFileSaveXml;
                    existing.SmtpHost                     = model.SmtpHost;
                    existing.SmtpPort                     = model.SmtpPort;
                    existing.SmtpUser                     = model.SmtpUser;
                    existing.SmtpSsl                      = model.SmtpSsl;
                    existing.SenderEmail                  = model.SenderEmail;
                    existing.UltimaActualizacion          = DateTime.Now;

                    // Solo actualizar contraseñas si vienen con valor (evitar borrarlas)
                    if (!string.IsNullOrWhiteSpace(model.PasswordCertificado))
                        existing.PasswordCertificado = model.PasswordCertificado;

                    if (!string.IsNullOrWhiteSpace(model.SmtpPassword))
                        existing.SmtpPassword = model.SmtpPassword;

                    // Clave maestra: solo actualizar si viene un valor
                    if (!string.IsNullOrWhiteSpace(model.ClaveMaestra))
                        existing.ClaveMaestra = model.ClaveMaestra;
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Configuración del SRI guardada correctamente.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al guardar: " + ex.Message;
            }

            return RedirectToAction("Index");
        }
    }
}
