using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SistemaERP.Models;
using SistemaERP.Models.Entities;
using SistemaERP.Areas.Admin.Models;

namespace SistemaERP.Areas.Admin.Services
{
    public interface IRubroService
    {
        Task<int> AplicarRubroAsync(int empresaId, int rubroId);
    }

    public class RubroService : IRubroService
    {
        private readonly AdminContext _adminContext;
        private readonly IEmpresaConnectionService _empresaConnService;

        public RubroService(AdminContext adminContext, IEmpresaConnectionService empresaConnService)
        {
            _adminContext = adminContext;
            _empresaConnService = empresaConnService;
        }

        public async Task<int> AplicarRubroAsync(int empresaId, int rubroId)
        {
            var empresa = await _adminContext.Empresas.FindAsync(empresaId);
            if (empresa == null)
            {
                throw new ArgumentException("Empresa no encontrada");
            }

            var rubro = await _adminContext.Rubros.FindAsync(rubroId);
            if (rubro == null)
            {
                throw new ArgumentException("Rubro no encontrado");
            }

            // Obtener las plantillas predefinidas en la DB maestra para este rubro
            var plantillas = await _adminContext.PlantillasRubros
                .Where(p => p.RubroId == rubroId)
                .ToListAsync();

            int elementosInsertados = 0;

            // Conectarse dinámicamente a la base de datos de la empresa
            using (var clientContext = _empresaConnService.GetContextForEmpresa(empresa))
            {
                var conn = clientContext.Database.GetDbConnection();
                
                // 1. Inicializar metadatos de migración si es una base de datos antigua pre-existente
                try
                {
                    await conn.OpenAsync();
                    bool tablasExisten = false;
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Traslados'";
                        var result = await cmd.ExecuteScalarAsync();
                        tablasExisten = Convert.ToInt32(result) > 0;
                    }

                    if (tablasExisten)
                    {
                        // Crear __EFMigrationsHistory si no existe
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = @"
                                IF OBJECT_ID('__EFMigrationsHistory') IS NULL
                                CREATE TABLE [__EFMigrationsHistory] (
                                    [MigrationId] nvarchar(150) NOT NULL,
                                    [ProductVersion] nvarchar(32) NOT NULL,
                                    CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
                                )";
                            await cmd.ExecuteNonQueryAsync();
                        }

                        // Registrar la migración inicial si no está ya
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = @"
                                IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] LIKE '%InitialCreate%')
                                INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) 
                                VALUES ('20260602020916_InitialCreate', '10.0.2')";
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }
                catch (Exception) { }
                finally
                {
                    if (conn.State == System.Data.ConnectionState.Open)
                    {
                        await conn.CloseAsync();
                    }
                }

                // 2. Ejecutar las migraciones estándar de EF Core
                try
                {
                    await clientContext.Database.MigrateAsync();
                }
                catch (Exception)
                {
                    // Si falla, intentamos continuar dado que las tablas principales podrían estar presentes
                }

                // 3. Sincronizar columnas y tablas manuales de inicio de aplicación (Program.cs) que no están en migraciones
                try
                {
                    await conn.OpenAsync();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                            -- Asegurar que las columnas de CatalogoPublico existan
                            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ParametrosSistema' AND COLUMN_NAME = 'CatalogoPublicoActivo')
                                ALTER TABLE ParametrosSistema ADD CatalogoPublicoActivo BIT NOT NULL DEFAULT 0;
                            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ParametrosSistema' AND COLUMN_NAME = 'CatalogoMostrarPrecios')
                                ALTER TABLE ParametrosSistema ADD CatalogoMostrarPrecios BIT NOT NULL DEFAULT 0;
                            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ParametrosSistema' AND COLUMN_NAME = 'CatalogoTitulo')
                                ALTER TABLE ParametrosSistema ADD CatalogoTitulo NVARCHAR(MAX) NULL;
                            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ParametrosSistema' AND COLUMN_NAME = 'CatalogoWhatsApp')
                                ALTER TABLE ParametrosSistema ADD CatalogoWhatsApp NVARCHAR(MAX) NULL;

                            -- Asegurar que la columna PosLayout exista
                            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ParametrosSistema' AND COLUMN_NAME = 'PosLayout')
                                ALTER TABLE ParametrosSistema ADD PosLayout NVARCHAR(MAX) NULL;

                            -- Asegurar que existan las columnas para la especificación del IVA
                            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ParametrosSistema' AND COLUMN_NAME = 'IvaCodigoSri')
                            BEGIN
                                ALTER TABLE ParametrosSistema ADD IvaCodigoSri NVARCHAR(MAX) NULL;
                                EXEC('UPDATE ParametrosSistema SET IvaCodigoSri = ''2'' WHERE IvaCodigoSri IS NULL');
                            END
                            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ParametrosSistema' AND COLUMN_NAME = 'IvaCodPorcentajeSri')
                            BEGIN
                                ALTER TABLE ParametrosSistema ADD IvaCodPorcentajeSri NVARCHAR(MAX) NULL;
                                EXEC('UPDATE ParametrosSistema SET IvaCodPorcentajeSri = ''4'' WHERE IvaCodPorcentajeSri IS NULL');
                            END
                            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ParametrosSistema' AND COLUMN_NAME = 'IvaTarifa')
                            BEGIN
                                ALTER TABLE ParametrosSistema ADD IvaTarifa DECIMAL(18,2) NOT NULL DEFAULT 15.00;
                            END

                            -- Asegurar que existan las tablas de CuentasPorPagar y CuentasPorPagar_Abonos
                            IF OBJECT_ID('CuentasPorPagar') IS NULL
                            CREATE TABLE [CuentasPorPagar] (
                                [Id] bigint IDENTITY(1,1) NOT NULL,
                                [Id_Compra] bigint NOT NULL,
                                [Factura] nvarchar(max) NULL,
                                [Id_Proveedor] bigint NOT NULL,
                                [Fecha_Emision] datetime2 NOT NULL,
                                [DiasPago] int NOT NULL,
                                [Fecha_Vencimiento] datetime2 NOT NULL,
                                [Monto_Fact] decimal(18,2) NOT NULL,
                                [Monto_NC] decimal(18,2) NOT NULL,
                                [Monto_RET] decimal(18,2) NOT NULL,
                                [Monto_TotalPagar] decimal(18,2) NOT NULL,
                                [Monto_Abonado] decimal(18,2) NOT NULL,
                                [Monto_Saldo] decimal(18,2) NOT NULL,
                                [Estado] nvarchar(max) NULL,
                                [Monto_RET_IVA] decimal(18,2) NOT NULL DEFAULT 0.00,
                                CONSTRAINT [PK_CuentasPorPagar] PRIMARY KEY ([Id])
                            );

                            IF OBJECT_ID('CuentasPorPagar_Abonos') IS NULL
                            CREATE TABLE [CuentasPorPagar_Abonos] (
                                [Id] bigint IDENTITY(1,1) NOT NULL,
                                [Id_Usuario] int NOT NULL,
                                [Id_Compra] bigint NOT NULL,
                                [Fecha_Pago] datetime2 NOT NULL,
                                [Monto_Efectivo] decimal(18,2) NOT NULL,
                                [FechaHora_Reg] datetime2 NOT NULL,
                                [Tipo_Pago] int NOT NULL,
                                [Num_Doc] nvarchar(max) NULL,
                                [Id_Banco] int NULL,
                                [Monto_Multipago] decimal(18,2) NOT NULL DEFAULT 0.00,
                                [Imagen_Doc] nvarchar(max) NULL,
                                [Adquirente] nvarchar(max) NULL,
                                CONSTRAINT [PK_CuentasPorPagar_Abonos] PRIMARY KEY ([Id])
                            );";
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                catch (Exception) { }
                finally
                {
                    if (conn.State == System.Data.ConnectionState.Open)
                    {
                        await conn.CloseAsync();
                    }
                }

                // ── 1. Procesar Catálogos Genéricos ───────────────────────────
                var catalogosPlantilla = plantillas.Where(p => p.TipoPlantilla == "Catalogo").ToList();
                if (catalogosPlantilla.Any())
                {
                    var catalogosExistentes = await clientContext.Catalogos.ToListAsync();

                    foreach (var p in catalogosPlantilla)
                    {
                        int idTipo = int.TryParse(p.Extras, out int val) ? val : 1;
                        
                        // Verificar si ya existe este catálogo con la misma descripción y tipo
                        bool existe = catalogosExistentes.Any(c => 
                            string.Equals(c.Descripcion, p.Valor, StringComparison.OrdinalIgnoreCase) && 
                            c.IdTipoCatalogo == idTipo);

                        if (!existe)
                        {
                            var nuevoCatalogo = new Catalogo
                            {
                                Descripcion = p.Valor,
                                IdTipoCatalogo = idTipo,
                                Estado = true,
                                Fecha_Registro = DateTime.Now,
                                Codigo = p.Clave
                            };
                            clientContext.Catalogos.Add(nuevoCatalogo);
                            elementosInsertados++;
                        }
                    }
                }

                // ── 2. Procesar Presentaciones de Productos ───────────────────
                var presentacionesPlantilla = plantillas.Where(p => p.TipoPlantilla == "Presentacion").ToList();
                if (presentacionesPlantilla.Any())
                {
                    var presentacionesExistentes = await clientContext.Presentaciones.ToListAsync();

                    foreach (var p in presentacionesPlantilla)
                    {
                        bool existe = presentacionesExistentes.Any(pr => 
                            string.Equals(pr.Nombre, p.Valor, StringComparison.OrdinalIgnoreCase));

                        if (!existe)
                        {
                            var nuevaPresentacion = new Presentacion
                            {
                                Nombre = p.Valor
                            };
                            clientContext.Presentaciones.Add(nuevaPresentacion);
                            elementosInsertados++;
                        }
                    }
                }

                // ── 3. Inicializar Parámetros del Sistema de la Empresa ───────
                var parametros = await clientContext.ParametrosSistema.FirstOrDefaultAsync();
                if (parametros == null)
                {
                    // Si no existen parámetros en la DB del cliente, creamos el registro inicial (ID=1)
                    var nuevosParametros = new ParametrosSistema
                    {
                        Id = 1,
                        NombreEmpresa = empresa.Nombre,
                        RUC = empresa.RUC,
                        Telefono = empresa.Telefono,
                        Correo = empresa.Email,
                        Direccion = empresa.Direccion,
                        NumItemsFactura = 14,
                        TicketAncho = 80,
                        TicketEncabezado = empresa.Nombre,
                        TicketPie = "Gracias por su compra!",
                        ReporteEncabezado = empresa.Nombre,
                        FechaVigenciaDesde = empresa.FechaAlta,
                        FechaVigenciaHasta = empresa.FechaVencimiento,
                        UltimaActualizacion = DateTime.Now,
                        MonedaOficial = "USD" // Predeterminado en Ecuador
                    };
                    clientContext.ParametrosSistema.Add(nuevosParametros);
                    elementosInsertados++;
                }
                else
                {
                    // Si ya existe, actualizamos los datos básicos si no están llenos
                    if (string.IsNullOrEmpty(parametros.NombreEmpresa)) parametros.NombreEmpresa = empresa.Nombre;
                    if (string.IsNullOrEmpty(parametros.RUC)) parametros.RUC = empresa.RUC;
                    if (string.IsNullOrEmpty(parametros.Correo)) parametros.Correo = empresa.Email;
                    if (string.IsNullOrEmpty(parametros.Telefono)) parametros.Telefono = empresa.Telefono;
                    if (string.IsNullOrEmpty(parametros.Direccion)) parametros.Direccion = empresa.Direccion;
                    
                    parametros.UltimaActualizacion = DateTime.Now;
                    clientContext.Entry(parametros).State = EntityState.Modified;
                }

                // ── 4. Seed: Usuario administrador por defecto ───────────────────
                var usuarioExistente = await clientContext.Usuarios.FirstOrDefaultAsync(u => u.Login == "admin");
                if (usuarioExistente == null)
                {
                    clientContext.Usuarios.Add(new Usuario
                    {
                        Login = "admin",
                        Password = "YQBkAG0AaQBuADEAMgAzAA==", // admin123 en Base64 UTF-16LE
                        Tipo_Usuario = "ADMINISTRADOR",
                        Estado = "ACTIVO"
                    });
                    elementosInsertados++;
                }

                // ── 5. Seed: Cliente "CONSUMIDOR FINAL" por defecto ────────────
                var clienteExistente = await clientContext.Clientes
                    .FirstOrDefaultAsync(c => c.Cedula == "9999999999999" || c.Nombres_Apellidos == "CONSUMIDOR FINAL");
                if (clienteExistente == null)
                {
                    clientContext.Clientes.Add(new Cliente
                    {
                        Cedula = "9999999999999",
                        Nombres_Apellidos = "CONSUMIDOR FINAL",
                        TipoDocumento = 1
                    });
                    elementosInsertados++;
                }

                // ── 6. Seed: Monedas / Divisas por defecto ────────────────────────
                var monedasPlantilla = new List<Moneda>
                {
                    new Moneda { Codigo = "USD", Nombre = "Dólar Estadounidense", Simbolo = "$", Pais = "Ecuador / Estados Unidos", Activo = true },
                    new Moneda { Codigo = "NIO", Nombre = "Córdoba Nicaragüense", Simbolo = "C$", Pais = "Nicaragua", Activo = true },
                    new Moneda { Codigo = "EUR", Nombre = "Euro", Simbolo = "€", Pais = "Unión Europea", Activo = true },
                    new Moneda { Codigo = "COP", Nombre = "Peso Colombiano", Simbolo = "$", Pais = "Colombia", Activo = true },
                    new Moneda { Codigo = "PEN", Nombre = "Sol Peruano", Simbolo = "S/.", Pais = "Perú", Activo = true },
                    new Moneda { Codigo = "MXN", Nombre = "Peso Mexicano", Simbolo = "$", Pais = "México", Activo = true }
                };

                var monedasExistentes = await clientContext.Monedas.ToListAsync();
                foreach (var m in monedasPlantilla)
                {
                    if (!monedasExistentes.Any(me => me.Codigo == m.Codigo))
                    {
                        clientContext.Monedas.Add(m);
                        elementosInsertados++;
                    }
                }

                // ── 7. Seed: Denominaciones por defecto (Ecuador / USD) ────────────
                var denominacionesExistentes = await clientContext.Denominaciones.AnyAsync();
                if (!denominacionesExistentes)
                {
                    var denominacionesPlantilla = new List<Denominacion>
                    {
                        new Denominacion { Nombre = "100 Dólares", Valor = 100.00m, Tipo = "Billete", Activo = true },
                        new Denominacion { Nombre = "50 Dólares",  Valor = 50.00m,  Tipo = "Billete", Activo = true },
                        new Denominacion { Nombre = "20 Dólares",  Valor = 20.00m,  Tipo = "Billete", Activo = true },
                        new Denominacion { Nombre = "10 Dólares",  Valor = 10.00m,  Tipo = "Billete", Activo = true },
                        new Denominacion { Nombre = "5 Dólares",   Valor = 5.00m,   Tipo = "Billete", Activo = true },
                        new Denominacion { Nombre = "2 Dólares",   Valor = 2.00m,   Tipo = "Billete", Activo = true },
                        new Denominacion { Nombre = "1 Dólar",     Valor = 1.00m,   Tipo = "Billete", Activo = true },
                        new Denominacion { Nombre = "1 Dólar (Moneda)", Valor = 1.00m, Tipo = "Moneda",  Activo = true },
                        new Denominacion { Nombre = "50 Centavos", Valor = 0.50m,  Tipo = "Moneda",  Activo = true },
                        new Denominacion { Nombre = "25 Centavos", Valor = 0.25m,  Tipo = "Moneda",  Activo = true },
                        new Denominacion { Nombre = "10 Centavos", Valor = 0.10m,  Tipo = "Moneda",  Activo = true },
                        new Denominacion { Nombre = "5 Centavos",  Valor = 0.05m,  Tipo = "Moneda",  Activo = true },
                        new Denominacion { Nombre = "1 Centavo",   Valor = 0.01m,  Tipo = "Moneda",  Activo = true }
                    };
                    clientContext.Denominaciones.AddRange(denominacionesPlantilla);
                    elementosInsertados += denominacionesPlantilla.Count;
                }

                // Guardar todos los cambios en la base de datos de la empresa
                if (elementosInsertados > 0)
                {
                    try
                    {
                        await clientContext.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        var msg = ex.Message;
                        if (ex.InnerException != null)
                        {
                            msg += " -> " + ex.InnerException.Message;
                        }
                        throw new Exception($"Error al guardar semillas en base de datos inquilino: {msg}", ex);
                    }
                }
            }

            // Actualizar estado de la empresa
            empresa.UltimoAccesoAdmin = DateTime.Now;
            _adminContext.Entry(empresa).State = EntityState.Modified;
            await _adminContext.SaveChangesAsync();

            // Registrar el usuario admin en UsuarioEmpresa (tabla de ruteo multi-tenant)
            var ueExistente = await _adminContext.UsuariosEmpresa
                .FirstOrDefaultAsync(u => u.Login == "admin" && u.EmpresaId == empresa.Id);
            if (ueExistente == null)
            {
                _adminContext.UsuariosEmpresa.Add(new UsuarioEmpresa
                {
                    Login = "admin",
                    EmpresaId = empresa.Id,
                    Activo = true
                });
                await _adminContext.SaveChangesAsync();
            }

            return elementosInsertados;
        }
    }
}
