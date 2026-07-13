-- 01_AdminDB_Create.sql
-- Script de Creación de la Base de Datos Maestra (AdminDB)

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'AdminDB')
BEGIN
    CREATE DATABASE AdminDB;
END
GO

USE AdminDB;
GO

-- 1. Tabla de Rubros de Negocio
IF OBJECT_ID('dbo.Rubros', 'U') IS NULL
BEGIN
    CREATE TABLE Rubros (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Nombre NVARCHAR(100) NOT NULL UNIQUE,
        Descripcion NVARCHAR(500) NULL,
        Icono NVARCHAR(100) NULL,
        Activo BIT DEFAULT 1,
        FechaCreacion DATETIME DEFAULT GETDATE()
    );
END
GO

-- 2. Tabla de Empresas Clientes
IF OBJECT_ID('dbo.Empresas', 'U') IS NULL
BEGIN
    CREATE TABLE Empresas (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Nombre NVARCHAR(200) NOT NULL,
        RUC NVARCHAR(20) NOT NULL UNIQUE,
        RubroId INT NOT NULL,
        ConnectionStringEncriptado NVARCHAR(MAX) NOT NULL,
        ServidorDB NVARCHAR(200) NULL,
        NombreDB NVARCHAR(100) NULL,
        Logo NVARCHAR(500) NULL,
        Telefono NVARCHAR(50) NULL,
        Email NVARCHAR(200) NULL,
        Direccion NVARCHAR(500) NULL,
        Ciudad NVARCHAR(100) NULL,
        NombreContacto NVARCHAR(200) NULL,
        FechaAlta DATETIME DEFAULT GETDATE(),
        FechaVencimiento DATETIME NULL,
        Estado BIT DEFAULT 1,
        VersionSistema NVARCHAR(20) DEFAULT '1.0.0',
        UltimoAccesoAdmin DATETIME NULL,
        CONSTRAINT FK_Empresas_Rubros FOREIGN KEY (RubroId) REFERENCES Rubros(Id)
    );
END
GO

-- 3. Tabla de Usuarios del Super-Administrador
IF OBJECT_ID('dbo.AdminUsuarios', 'U') IS NULL
BEGIN
    CREATE TABLE AdminUsuarios (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Login NVARCHAR(100) NOT NULL UNIQUE,
        PasswordHash NVARCHAR(200) NOT NULL,
        Nombre NVARCHAR(200) NOT NULL,
        Email NVARCHAR(200) NOT NULL,
        Activo BIT DEFAULT 1,
        FechaCreacion DATETIME DEFAULT GETDATE()
    );
END
GO

-- 4. Tabla de Auditoría
IF OBJECT_ID('dbo.AdminAuditLog', 'U') IS NULL
BEGIN
    CREATE TABLE AdminAuditLog (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        AdminUsuarioId INT NOT NULL,
        EmpresaId INT NULL,
        Accion NVARCHAR(200) NOT NULL,
        Detalle NVARCHAR(MAX) NULL,
        FechaHora DATETIME DEFAULT GETDATE(),
        IP NVARCHAR(50) NULL,
        CONSTRAINT FK_AuditLog_AdminUsuarios FOREIGN KEY (AdminUsuarioId) REFERENCES AdminUsuarios(Id),
        CONSTRAINT FK_AuditLog_Empresas FOREIGN KEY (EmpresaId) REFERENCES Empresas(Id)
    );
END
GO

-- 5. Registro de Backups
IF OBJECT_ID('dbo.Backups', 'U') IS NULL
BEGIN
    CREATE TABLE Backups (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        EmpresaId INT NOT NULL,
        FechaBackup DATETIME DEFAULT GETDATE(),
        RutaArchivo NVARCHAR(500) NOT NULL,
        TamanoMB DECIMAL(10,2) NOT NULL,
        Estado NVARCHAR(50) NOT NULL, -- 'Exitoso', 'Fallido', 'En proceso'
        AdminUsuarioId INT NOT NULL,
        Observaciones NVARCHAR(MAX) NULL,
        CONSTRAINT FK_Backups_Empresas FOREIGN KEY (EmpresaId) REFERENCES Empresas(Id),
        CONSTRAINT FK_Backups_AdminUsuarios FOREIGN KEY (AdminUsuarioId) REFERENCES AdminUsuarios(Id)
    );
END
GO

-- 6. Tabla de Configuración de Campos Personalizados
IF OBJECT_ID('dbo.CamposPersonalizados', 'U') IS NULL
BEGIN
    CREATE TABLE CamposPersonalizados (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        EmpresaId INT NULL, -- NULL significa que aplica a todas las empresas del rubro
        RubroId INT NULL,   -- NULL significa que aplica a todos los rubros
        Entidad NVARCHAR(100) NOT NULL, -- 'Producto', 'Cliente', 'Venta', 'Proveedor', etc.
        NombreCampo NVARCHAR(100) NOT NULL, -- Nombre clave en el JSON (sin espacios, ej: 'PrincipioActivo')
        Etiqueta NVARCHAR(200) NOT NULL,    -- Texto para el UI (ej: 'Principio Activo')
        TipoDato NVARCHAR(50) NOT NULL,     -- 'Texto', 'Numero', 'Fecha', 'Booleano', 'Lista'
        OpcionesLista NVARCHAR(MAX) NULL,   -- Opciones separadas por coma o formato JSON para tipo 'Lista'
        Requerido BIT DEFAULT 0,
        
        -- Validaciones
        MinLongitud INT NULL,
        MaxLongitud INT NULL,
        ExpresionRegular NVARCHAR(500) NULL,
        EsUnico BIT DEFAULT 0,
        ValorMinimo DECIMAL(18,4) NULL,
        ValorMaximo DECIMAL(18,4) NULL,
        Decimales INT NULL,
        
        -- Interfaz y Ayuda
        ValorDefault NVARCHAR(500) NULL,
        Placeholder NVARCHAR(200) NULL,
        Tooltip NVARCHAR(500) NULL,
        
        -- Visibilidad y Destinos
        VisibleEnFormulario BIT DEFAULT 1,
        VisibleEnLista BIT DEFAULT 0,
        VisibleEnReporte BIT DEFAULT 1,
        VisibleEnEtiqueta BIT DEFAULT 0,
        VisibleEnFactura BIT DEFAULT 0, -- Si es verdadero, se incluye en el XML del SRI <infoAdicional>
        
        OrdenVisualizacion INT DEFAULT 0,
        Activo BIT DEFAULT 1,
        FechaCreacion DATETIME DEFAULT GETDATE(),
        CreadoPorAdmin INT NULL,
        CONSTRAINT FK_CamposPersonalizados_Empresas FOREIGN KEY (EmpresaId) REFERENCES Empresas(Id),
        CONSTRAINT FK_CamposPersonalizados_Rubros FOREIGN KEY (RubroId) REFERENCES Rubros(Id),
        CONSTRAINT FK_CamposPersonalizados_AdminUsuarios FOREIGN KEY (CreadoPorAdmin) REFERENCES AdminUsuarios(Id)
    );
END
GO

-- 7. Tabla de Plantilla/Catálogos predeterminados por Rubro
IF OBJECT_ID('dbo.PlantillasRubro', 'U') IS NULL
BEGIN
    CREATE TABLE PlantillasRubro (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        RubroId INT NOT NULL,
        TipoPlantilla NVARCHAR(100) NOT NULL, -- 'Catalogo', 'Presentacion', 'Parametros'
        Clave NVARCHAR(100) NOT NULL,         -- Codigo o Identificador
        Valor NVARCHAR(MAX) NOT NULL,         -- Descripcion, JSON de parametros, etc.
        Extras NVARCHAR(MAX) NULL,            -- Info adicional de apoyo
        FechaCreacion DATETIME DEFAULT GETDATE(),
        CONSTRAINT FK_PlantillasRubro_Rubros FOREIGN KEY (RubroId) REFERENCES Rubros(Id)
    );
END
GO
