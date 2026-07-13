using System;
using Microsoft.EntityFrameworkCore;
using SistemaERP.Models;
using SistemaERP.Areas.Admin.Models;

namespace SistemaERP.Areas.Admin.Services
{
    public interface IEmpresaConnectionService
    {
        ScmContext GetContextForEmpresa(Empresa empresa);
        ScmContext GetContextForEmpresa(int empresaId);
        bool TestConnection(string connectionString);
    }

    public class EmpresaConnectionService : IEmpresaConnectionService
    {
        private readonly AdminContext _adminContext;
        private readonly IEncryptionService _encryptionService;

        public EmpresaConnectionService(AdminContext adminContext, IEncryptionService encryptionService)
        {
            _adminContext = adminContext;
            _encryptionService = encryptionService;
        }

        public ScmContext GetContextForEmpresa(Empresa empresa)
        {
            var connString = _encryptionService.Decrypt(empresa.ConnectionStringEncriptado);
            if (string.IsNullOrEmpty(connString))
            {
                throw new InvalidOperationException("La cadena de conexión no pudo ser descifrada.");
            }

            var optionsBuilder = new DbContextOptionsBuilder<ScmContext>();
            optionsBuilder.UseSqlServer(connString, sqlServerOptionsAction: sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null);
            });

            return new ScmContext(optionsBuilder.Options);
        }

        public ScmContext GetContextForEmpresa(int empresaId)
        {
            var empresa = _adminContext.Empresas.Find(empresaId);
            if (empresa == null)
            {
                throw new ArgumentException($"Empresa con ID {empresaId} no encontrada.");
            }
            return GetContextForEmpresa(empresa);
        }

        public bool TestConnection(string connectionString)
        {
            try
            {
                var optionsBuilder = new DbContextOptionsBuilder<ScmContext>();
                optionsBuilder.UseSqlServer(connectionString);
                using (var context = new ScmContext(optionsBuilder.Options))
                {
                    // Intentamos abrir la conexión para verificar validez
                    return context.Database.CanConnect();
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
