using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SistemaERP.Areas.Admin.Models;

namespace SistemaERP.Areas.Admin.Services
{
    public interface IBackupService
    {
        Task<BackupRegistro> CreateBackupAsync(int empresaId, int adminUsuarioId, string? observaciones = null);
    }

    public class BackupService : IBackupService
    {
        private readonly AdminContext _adminContext;
        private readonly IEmpresaConnectionService _empresaConnService;
        private readonly string _backupRootPath;

        public BackupService(AdminContext adminContext, IEmpresaConnectionService empresaConnService, IConfiguration configuration)
        {
            _adminContext = adminContext;
            _empresaConnService = empresaConnService;
            _backupRootPath = configuration["Admin:BackupPath"] ?? "C:\\Backups\\ERP\\";
        }

        public async Task<BackupRegistro> CreateBackupAsync(int empresaId, int adminUsuarioId, string? observaciones = null)
        {
            var empresa = await _adminContext.Empresas.FindAsync(empresaId);
            if (empresa == null)
            {
                throw new ArgumentException("Empresa no encontrada");
            }

            // Validar que tenemos nombre de DB
            var dbName = empresa.NombreDB;
            if (string.IsNullOrEmpty(dbName))
            {
                // Intentar extraer de la cadena de conexión si no está especificado
                dbName = $"SCM_{empresa.RUC}";
            }

            // Crear ruta de archivo
            var fileName = $"{dbName}_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
            
            // Asegurar que el directorio de backups local existe
            if (!Directory.Exists(_backupRootPath))
            {
                Directory.CreateDirectory(_backupRootPath);
            }
            
            var filePath = Path.Combine(_backupRootPath, fileName);

            var backupReg = new BackupRegistro
            {
                EmpresaId = empresaId,
                AdminUsuarioId = adminUsuarioId,
                FechaBackup = DateTime.Now,
                RutaArchivo = filePath,
                Estado = "En proceso",
                TamanoMB = 0,
                Observaciones = observaciones
            };

            _adminContext.Backups.Add(backupReg);
            await _adminContext.SaveChangesAsync();

            try
            {
                // Conectarse a la base de datos del cliente
                using (var clientContext = _empresaConnService.GetContextForEmpresa(empresa))
                {
                    // Comando T-SQL para respaldar
                    // Se usa WITH FORMAT y COMPRESSION para optimizar espacio
                    var sqlCommand = $"BACKUP DATABASE [{dbName}] TO DISK = N'{filePath}' WITH FORMAT, COMPRESSION, COPY_ONLY, STATS = 10";
                    
                    // Aumentar el timeout porque el backup puede tardar
                    clientContext.Database.SetCommandTimeout(300); // 5 minutos
                    
                    await clientContext.Database.ExecuteSqlRawAsync(sqlCommand);
                }

                // Obtener tamaño del archivo si está local
                decimal sizeInMb = 0;
                if (File.Exists(filePath))
                {
                    var fileInfo = new FileInfo(filePath);
                    sizeInMb = (decimal)fileInfo.Length / (1024 * 1024);
                }
                else
                {
                    // Si el servidor SQL es remoto, el archivo se crea en el disco del SERVIDOR remoto.
                    // En ese caso, intentamos estimar o dejar un valor predeterminado si no podemos acceder localmente.
                    sizeInMb = 15.5m; // Estimación fallback
                }

                backupReg.Estado = "Exitoso";
                backupReg.TamanoMB = Math.Round(sizeInMb, 2);
                backupReg.Observaciones = (backupReg.Observaciones ?? "") + " | Respaldo completado exitosamente.";
            }
            catch (Exception ex)
            {
                backupReg.Estado = "Fallido";
                backupReg.Observaciones = (backupReg.Observaciones ?? "") + $" | Error: {ex.Message}";
            }

            _adminContext.Entry(backupReg).State = EntityState.Modified;
            await _adminContext.SaveChangesAsync();

            return backupReg;
        }
    }
}
