using Microsoft.EntityFrameworkCore;
using SistemaERP.Models;
using SistemaERP.Models.Entities;
using SistemaERP.Areas.Admin.Models;
using SistemaERP.Areas.Admin.Services;

var builder = WebApplication.CreateBuilder(args);

// Configurar Antiforgery para soportar peticiones AJAX con encabezado personalizado
builder.Services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");

// Limpiar proveedores de log por defecto para evitar que intente escribir en el EventLog de Windows
// (esto causa un crash fatal por falta de permisos de administrador en hostings compartidos de Plesk)
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<SistemaERP.Filters.DynamicAuthorizationFilter>();
    options.Filters.Add<SistemaERP.Filters.SetCurrencySymbolFilter>();
});
builder.Services.AddResponseCompression();
builder.Services.AddResponseCaching();
builder.Services.AddHttpContextAccessor();

// Registro de Servicios del ERP
builder.Services.AddScoped<SistemaERP.Services.ISriService, SistemaERP.Services.SriService>();

// Registro de Servicios del Área Admin
builder.Services.AddSingleton<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<IEmpresaConnectionService, EmpresaConnectionService>();
builder.Services.AddScoped<IBackupService, BackupService>();
builder.Services.AddScoped<IRubroService, RubroService>();
builder.Services.AddScoped<ICamposPersonalizadosService, CamposPersonalizadosService>();
builder.Services.AddScoped<IAdminAuditService, AdminAuditService>();

// ScmContext multi-tenant: resuelve la BD según la empresa del usuario autenticado
// Si no hay usuario autenticado o falla, usa DefaultConnection (SCM)
builder.Services.AddDbContext<ScmContext>((sp, options) =>
{
    string connStr = builder.Configuration.GetConnectionString("DefaultConnection")!;

    try
    {
        var httpContext = sp.GetRequiredService<IHttpContextAccessor>().HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            var empresaId = httpContext.User.FindFirst("EmpresaId")?.Value;
            if (empresaId != null && int.TryParse(empresaId, out var eid))
            {
                var adminCtx = sp.GetRequiredService<AdminContext>();
                var enc = sp.GetRequiredService<IEncryptionService>();
                var empresa = adminCtx.Empresas.Find(eid);
                if (empresa?.ConnectionStringEncriptado != null)
                {
                    var decrypted = enc.Decrypt(empresa.ConnectionStringEncriptado);
                    if (!string.IsNullOrEmpty(decrypted))
                        connStr = decrypted;
                }
            }
        }
    }
    catch { /* fallback a DefaultConnection */ }

    options.UseSqlServer(connStr, sqlOptions =>
    {
        sqlOptions.CommandTimeout(30);
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 2,
            maxRetryDelay: TimeSpan.FromSeconds(3),
            errorNumbersToAdd: null);
    });
});

// Configurar AdminContext con SQL Server
builder.Services.AddDbContext<AdminContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AdminConnection"),
        sqlServerOptionsAction: sqlOptions =>
        {
            sqlOptions.CommandTimeout(30);
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 2,
                maxRetryDelay: TimeSpan.FromSeconds(3),
                errorNumbersToAdd: null);
        }));

// Configurar Autenticación por Cookies (ERP y Admin por separado)
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(6);
        options.SlidingExpiration = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    })
    .AddCookie("AdminCookies", options =>
    {
        options.LoginPath = "/Admin/AdminAccount/Login";
        options.AccessDeniedPath = "/Admin/AdminAccount/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(4);
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseStaticFiles();
app.UseRouting();
app.UseResponseCaching();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// Ruta para Áreas (incluido /Admin)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Monitor}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "catalogo_publico",
    pattern: "catalogo/{alias}/{action=Index}/{id?}",
    defaults: new { controller = "CatalogoPublico" })
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();
// Migración automática de todas las bases de datos al iniciar la aplicación (Multi-Tenant)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        logger.LogInformation("Iniciando migración automática de bases de datos...");

        // 1. Migrar AdminContext (Base central de administración)
        var adminCtx = services.GetRequiredService<AdminContext>();
        await adminCtx.Database.MigrateAsync();
        logger.LogInformation("Base de datos de administración (AdminDB) migrada con éxito.");

        // Sembrar rubros predeterminados si la tabla está vacía
        if (!await adminCtx.Rubros.AnyAsync())
        {
            logger.LogInformation("Sembrando rubros predeterminados en AdminDB...");
            adminCtx.Rubros.AddRange(
                new Rubro { Nombre = "Ferretería", Descripcion = "Negocios de venta de materiales de construcción, herramientas, tornillería, plomería, electricidad y acabados.", Icono = "fa-hammer", Activo = true },
                new Rubro { Nombre = "Farmacia", Descripcion = "Farmacias, boticas, venta de medicamentos, insumos médicos, cosméticos y servicios de salud integrados.", Icono = "fa-prescription-bottle-medical", Activo = true },
                new Rubro { Nombre = "Despensa", Descripcion = "Supermercados, minimarkets, tiendas de abarrotes, despensas y negocios de venta de alimentos y artículos de primera necesidad.", Icono = "fa-basket-shopping", Activo = true },
                new Rubro { Nombre = "Taller de Repuestos", Descripcion = "Taller mecánico, venta de autopartes, repuestos automotrices, lubricantes y mantenimiento vehicular.", Icono = "fa-wrench", Activo = true }
            );
            await adminCtx.SaveChangesAsync();
            logger.LogInformation("Rubros predeterminados sembrados con éxito.");
        }

        // Sembrar usuario superadmin por defecto si la tabla está vacía
        if (!await adminCtx.AdminUsuarios.AnyAsync())
        {
            logger.LogInformation("Sembrando usuario superadmin por defecto en AdminDB...");
            adminCtx.AdminUsuarios.Add(new AdminUsuario
            {
                Login = "superadmin",
                PasswordHash = "$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8WGWQZ9H8vNs7K0e7Ky", // Hash de "Admin@2026"
                Nombre = "Super Administrador",
                Email = "admin@sistemaerp.com",
                Activo = true,
                FechaCreacion = DateTime.Now
            });
            await adminCtx.SaveChangesAsync();
            logger.LogInformation("Usuario superadmin sembrado con éxito.");
        }

        // 2. Recuperar y migrar todas las bases de datos de inquilinos (ScmContext)
        var enc = services.GetRequiredService<IEncryptionService>();
        var empresas = await adminCtx.Empresas.ToListAsync();
        
        foreach (var emp in empresas)
        {
            if (!string.IsNullOrEmpty(emp.ConnectionStringEncriptado))
            {
                try
                {
                    string decryptedConn = enc.Decrypt(emp.ConnectionStringEncriptado);
                    logger.LogInformation($"Migrando base de datos de empresa ID: {emp.Id} ({emp.Nombre})...");
                    
                    var opts = new DbContextOptionsBuilder<ScmContext>()
                        .UseSqlServer(decryptedConn)
                        .Options;
                        
                    using (var scmCtx = new ScmContext(opts))
                    {
                        // Lógica robusta: detectar si la BD tiene tablas de una versión anterior
                        // y registrar TODAS las migraciones en el historial si es necesario.
                        var conn = scmCtx.Database.GetDbConnection();
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
                            // Crear tabla de historial de migraciones si no existe
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

                            // Registrar TODAS las migraciones conocidas en el historial
                            // para evitar que MigrateAsync intente aplicarlas de nuevo
                            var migrationsToRegister = new[]
                            {
                                "20260602020916_InitialCreate",
                                "20260608194521_AddCatalogoPublico",
                                "20260610143818_AddPosLayoutOption"
                            };

                            foreach (var migrationId in migrationsToRegister)
                            {
                                using (var cmd = conn.CreateCommand())
                                {
                                    cmd.CommandText = $@"
                                        IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '{migrationId}')
                                        INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) 
                                        VALUES ('{migrationId}', '10.0.2')";
                                    await cmd.ExecuteNonQueryAsync();
                                }
                            }

                            // Asegurar que si por error se registró la migración AddAccountingModule sin haberse aplicado realmente,
                            // la eliminemos de la historia de migraciones para que MigrateAsync la ejecute y cree los campos/tablas contables.
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.CommandText = @"
                                    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ParametrosSistema' AND COLUMN_NAME = 'CuentaContableVentasId')
                                    BEGIN
                                        DELETE FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260626195837_AddAccountingModule';
                                    END";
                                await cmd.ExecuteNonQueryAsync();
                            }

                            // Asegurar que las columnas de CatalogoPublico existan
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.CommandText = @"
                                    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ParametrosSistema' AND COLUMN_NAME = 'CatalogoPublicoActivo')
                                        ALTER TABLE ParametrosSistema ADD CatalogoPublicoActivo BIT NOT NULL DEFAULT 0;
                                    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ParametrosSistema' AND COLUMN_NAME = 'CatalogoMostrarPrecios')
                                        ALTER TABLE ParametrosSistema ADD CatalogoMostrarPrecios BIT NOT NULL DEFAULT 0;
                                    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ParametrosSistema' AND COLUMN_NAME = 'CatalogoTitulo')
                                        ALTER TABLE ParametrosSistema ADD CatalogoTitulo NVARCHAR(MAX) NULL;
                                    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ParametrosSistema' AND COLUMN_NAME = 'CatalogoWhatsApp')
                                        ALTER TABLE ParametrosSistema ADD CatalogoWhatsApp NVARCHAR(MAX) NULL;";
                                await cmd.ExecuteNonQueryAsync();
                            }

                            // Asegurar que la columna PosLayout exista
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.CommandText = @"
                                    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ParametrosSistema' AND COLUMN_NAME = 'PosLayout')
                                        ALTER TABLE ParametrosSistema ADD PosLayout NVARCHAR(MAX) NULL;";
                                await cmd.ExecuteNonQueryAsync();
                            }

                            // Asegurar que existan las columnas para la especificación del IVA
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.CommandText = @"
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
                                    END";
                                await cmd.ExecuteNonQueryAsync();
                            }

                            // Asegurar que existan las tablas de CuentasPorPagar y CuentasPorPagar_Abonos
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.CommandText = @"
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

                        await conn.CloseAsync();
                        await scmCtx.Database.MigrateAsync();

                        // Sembrar usuario administrador por defecto si la tabla está vacía
                        if (!await scmCtx.Usuarios.AnyAsync())
                        {
                            logger.LogInformation($"Sembrando usuario admin por defecto para la empresa ID: {emp.Id} ({emp.Nombre})...");
                            // Contraseña "admin123" codificada en Base64 UTF-16LE (formato del sistema)
                            var passwordBytes = System.Text.Encoding.Unicode.GetBytes("admin123");
                            var passwordBase64 = Convert.ToBase64String(passwordBytes);
                            scmCtx.Usuarios.Add(new SistemaERP.Models.Entities.Usuario
                            {
                                Login = "admin",
                                Password = passwordBase64,
                                Tipo_Usuario = "ADMINISTRADOR",
                                Estado = "ACTIVO"
                            });
                            await scmCtx.SaveChangesAsync();
                            logger.LogInformation($"Usuario admin sembrado con éxito para la empresa ID: {emp.Id}.");

                            // También registrar en UsuariosEmpresa de AdminDB
                            if (!await adminCtx.UsuariosEmpresa.AnyAsync(u => u.Login == "admin" && u.EmpresaId == emp.Id))
                            {
                                adminCtx.UsuariosEmpresa.Add(new UsuarioEmpresa
                                {
                                    Login = "admin",
                                    EmpresaId = emp.Id,
                                    PasswordHash = passwordBase64,
                                    Activo = true,
                                    FechaCreacion = DateTime.Now
                                });
                                await adminCtx.SaveChangesAsync();
                                logger.LogInformation($"Usuario admin registrado en UsuariosEmpresa para empresa ID: {emp.Id}.");
                            }
                        }

                        // Sembrar denominaciones por defecto si la tabla está vacía
                        if (!await scmCtx.Denominaciones.AnyAsync())
                        {
                            logger.LogInformation($"Sembrando denominaciones por defecto para la empresa ID: {emp.Id} ({emp.Nombre})...");
                            scmCtx.Denominaciones.AddRange(
                                // Monedas
                                new Denominacion { Nombre = "1 Centavo", Valor = 0.01m, Tipo = "Moneda", Activo = true },
                                new Denominacion { Nombre = "5 Centavos", Valor = 0.05m, Tipo = "Moneda", Activo = true },
                                new Denominacion { Nombre = "10 Centavos", Valor = 0.10m, Tipo = "Moneda", Activo = true },
                                new Denominacion { Nombre = "25 Centavos", Valor = 0.25m, Tipo = "Moneda", Activo = true },
                                new Denominacion { Nombre = "50 Centavos", Valor = 0.50m, Tipo = "Moneda", Activo = true },
                                new Denominacion { Nombre = "1 Dólar Moneda", Valor = 1.00m, Tipo = "Moneda", Activo = true },
                                // Billetes
                                new Denominacion { Nombre = "1 Dólar Billete", Valor = 1.00m, Tipo = "Billete", Activo = true },
                                new Denominacion { Nombre = "2 Dólares", Valor = 2.00m, Tipo = "Billete", Activo = true },
                                new Denominacion { Nombre = "5 Dólares", Valor = 5.00m, Tipo = "Billete", Activo = true },
                                new Denominacion { Nombre = "10 Dólares", Valor = 10.00m, Tipo = "Billete", Activo = true },
                                new Denominacion { Nombre = "20 Dólares", Valor = 20.00m, Tipo = "Billete", Activo = true },
                                new Denominacion { Nombre = "50 Dólares", Valor = 50.00m, Tipo = "Billete", Activo = true },
                                new Denominacion { Nombre = "100 Dólares", Valor = 100.00m, Tipo = "Billete", Activo = true }
                            );
                            await scmCtx.SaveChangesAsync();
                            logger.LogInformation($"Denominaciones sembradas con éxito para la empresa ID: {emp.Id}.");
                        }

                        // Escanear dinámicamente todos los controladores y acciones mediante reflexión
                        // para registrarlos automáticamente en las tablas ModulosWeb y PrivilegiosWeb
                        logger.LogInformation($"Sincronizando módulos y privilegios dinámicamente para la empresa ID: {emp.Id}...");
                        var currentAssembly = System.Reflection.Assembly.GetExecutingAssembly();
                        var controllerTypes = currentAssembly.GetTypes()
                            .Where(t => typeof(Microsoft.AspNetCore.Mvc.Controller).IsAssignableFrom(t) && !t.IsAbstract)
                            .ToList();

                        foreach (var controllerType in controllerTypes)
                        {
                            // Omitir controladores del área de administración o controladores especiales/básicos de inicio
                            var areaAttr = controllerType.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.AreaAttribute), true)
                                .FirstOrDefault() as Microsoft.AspNetCore.Mvc.AreaAttribute;
                            if (areaAttr != null && string.Equals(areaAttr.RouteValue, "Admin", StringComparison.OrdinalIgnoreCase))
                                continue;

                            string controllerName = controllerType.Name;
                            if (controllerName.EndsWith("Controller"))
                                controllerName = controllerName.Substring(0, controllerName.Length - "Controller".Length);

                            if (string.Equals(controllerName, "Account", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(controllerName, "Home", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(controllerName, "AdminCheck", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(controllerName, "BaseAdmin", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            // Asegurar que el módulo está registrado
                            var modulo = await scmCtx.ModulosWeb.FirstOrDefaultAsync(m => m.ControllerName == controllerName);
                            if (modulo == null)
                            {
                                string displayName = controllerName;
                                if (string.Equals(controllerName, "CatGastos", StringComparison.OrdinalIgnoreCase)) displayName = "Catálogos Gastos";
                                else if (string.Equals(controllerName, "AjustesSRI", StringComparison.OrdinalIgnoreCase)) displayName = "Parámetros SRI";
                                else if (string.Equals(controllerName, "Facturacion", StringComparison.OrdinalIgnoreCase)) displayName = "Facturación / POS";
                                else if (string.Equals(controllerName, "Ajustes", StringComparison.OrdinalIgnoreCase)) displayName = "Ajustes Inventario";
                                else if (string.Equals(controllerName, "Reportes", StringComparison.OrdinalIgnoreCase)) displayName = "Centro de Reportes";
                                else if (string.Equals(controllerName, "Traslados", StringComparison.OrdinalIgnoreCase)) displayName = "Traslados Inventario";
                                else if (string.Equals(controllerName, "Bodegas", StringComparison.OrdinalIgnoreCase)) displayName = "Bodegas / Almacén";
                                else if (string.Equals(controllerName, "Cobros", StringComparison.OrdinalIgnoreCase)) displayName = "Cuentas por Cobrar";
                                else if (string.Equals(controllerName, "Descuentos", StringComparison.OrdinalIgnoreCase)) displayName = "Módulo Descuentos";
                                else if (string.Equals(controllerName, "ParametrosSistema", StringComparison.OrdinalIgnoreCase)) displayName = "Parámetros del Sistema";

                                modulo = new ModuloWeb
                                {
                                    ControllerName = controllerName,
                                    DisplayName = displayName,
                                    Estado = true
                                };
                                scmCtx.ModulosWeb.Add(modulo);
                                await scmCtx.SaveChangesAsync();
                            }

                            // Obtener todos los métodos públicos del controlador que retornan un IActionResult o Task<IActionResult>
                            var actionMethods = controllerType.GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.DeclaredOnly)
                                .Where(m => !m.IsSpecialName && 
                                           (typeof(Microsoft.AspNetCore.Mvc.IActionResult).IsAssignableFrom(m.ReturnType) || 
                                            typeof(System.Threading.Tasks.Task).IsAssignableFrom(m.ReturnType)))
                                .Select(m => m.Name)
                                .Distinct()
                                .ToList();

                            foreach (var actionName in actionMethods)
                            {
                                // Omitir acciones con [AllowAnonymous]
                                // Usamos GetMethods() en lugar de GetMethod(actionName) para evitar AmbiguousMatchException por sobrecargas (ej. Create [GET] y [POST])
                                var methods = controllerType.GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.DeclaredOnly)
                                    .Where(m => m.Name == actionName);
                                
                                bool hasAllowAnonymous = false;
                                foreach (var mInfo in methods)
                                {
                                    if (mInfo.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute), true).Any())
                                    {
                                        hasAllowAnonymous = true;
                                        break;
                                    }
                                }
                                if (hasAllowAnonymous)
                                    continue;

                                var privilegio = await scmCtx.PrivilegiosWeb
                                    .FirstOrDefaultAsync(p => p.Id_Modulo == modulo.Id && p.ActionName == actionName);

                                if (privilegio == null)
                                {
                                    string actionDisplayName = actionName;
                                    if (string.Equals(actionName, "Index", StringComparison.OrdinalIgnoreCase)) actionDisplayName = "Ver";
                                    else if (string.Equals(actionName, "Create", StringComparison.OrdinalIgnoreCase)) actionDisplayName = "Crear";
                                    else if (string.Equals(actionName, "Edit", StringComparison.OrdinalIgnoreCase)) actionDisplayName = "Editar";
                                    else if (string.Equals(actionName, "Delete", StringComparison.OrdinalIgnoreCase)) actionDisplayName = "Eliminar";
                                    else if (string.Equals(actionName, "POS", StringComparison.OrdinalIgnoreCase)) actionDisplayName = "Punto de Venta (POS)";
                                    else if (string.Equals(actionName, "Kardex", StringComparison.OrdinalIgnoreCase)) actionDisplayName = "Kardex Inventario";
                                    else if (string.Equals(actionName, "StockReport", StringComparison.OrdinalIgnoreCase)) actionDisplayName = "Reporte de Stock";
                                    else if (string.Equals(actionName, "KardexExportExcel", StringComparison.OrdinalIgnoreCase)) actionDisplayName = "Exportar Kardex a Excel";
                                    else if (string.Equals(actionName, "KardexPrint", StringComparison.OrdinalIgnoreCase)) actionDisplayName = "Imprimir/Guardar PDF Kardex";
                                    else if (string.Equals(actionName, "DescargarXml", StringComparison.OrdinalIgnoreCase)) actionDisplayName = "Descargar XML Factura SRI";
                                    else if (string.Equals(actionName, "Ticket", StringComparison.OrdinalIgnoreCase)) actionDisplayName = "Imprimir Ticket de Venta";
                                    else if (string.Equals(actionName, "TransmitirSRI", StringComparison.OrdinalIgnoreCase)) actionDisplayName = "Transmitir Factura al SRI";
                                    else if (string.Equals(actionName, "StockReportExportExcel", StringComparison.OrdinalIgnoreCase)) actionDisplayName = "Exportar Stock a Excel";

                                    // Usando EF add
                                    scmCtx.PrivilegiosWeb.Add(new PrivilegioWeb
                                    {
                                        Id_Modulo = modulo.Id,
                                        ActionName = actionName,
                                        DisplayName = actionDisplayName,
                                        Estado = true
                                    });
                                    await scmCtx.SaveChangesAsync();
                                }
                            }
                        }
                    }
                    logger.LogInformation($"Base de datos de empresa ID: {emp.Id} migrada y sembrada con éxito.");
                }
                catch (Exception empEx)
                {
                    logger.LogError(empEx, $"Error al migrar la base de datos de la empresa ID: {emp.Id} ({emp.Nombre}).");
                }
            }
        }
        logger.LogInformation("Migración automática de bases de datos finalizada.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error crítico durante la migración automática de bases de datos al iniciar.");
    }
}

app.Run();
