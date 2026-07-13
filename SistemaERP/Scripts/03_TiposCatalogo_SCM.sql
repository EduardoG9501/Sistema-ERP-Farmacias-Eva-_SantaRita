-- =============================================================================
-- FILE: 03_TiposCatalogo_SCM.sql
-- DESC: Extends an empresa SCM database with the generic catalog type system.
--       Run this script on each EMPRESA database (SCM_EmpresaX), NOT on AdminDB.
--
--       USAGE:
--         1. Replace 'SCM' in the USE statement below with the actual empresa
--            database name (e.g. USE SCM_Empresa1).
--         2. Execute.
--
--       This script is IDEMPOTENT — it is safe to run more than once.
-- DATE: 2026-05-27
-- =============================================================================

-- IMPORTANT: Replace 'SCM' with the actual empresa database name before executing.
USE SCM;
GO

-- ============================================================================
-- TABLE: TiposCatalogo
-- Defines the catalog dimensions applicable to products (or clients/providers)
-- in this empresa. Rows are seeded from AdminDB.PlantillasRubro when the empresa
-- is onboarded for a specific rubro.
-- ============================================================================
IF OBJECT_ID('TiposCatalogo', 'U') IS NULL
BEGIN
    CREATE TABLE TiposCatalogo (
        Id                INT            IDENTITY(1,1) NOT NULL,
        Nombre            NVARCHAR(100)  NOT NULL,
        Codigo            NVARCHAR(50)   NULL,
        Descripcion       NVARCHAR(200)  NULL,
        -- Domain that this catalog type belongs to.
        -- Valid values: 'Producto' | 'Cliente' | 'Proveedor' | 'Gasto' | 'Banco'
        Dominio           NVARCHAR(50)   NOT NULL DEFAULT N'Producto',
        -- NULL means applicable to all rubros; otherwise restricts to a specific rubro.
        RubroId           INT            NULL,
        -- Controls UI visibility on the product maintenance form
        VisibleEnProducto BIT            NOT NULL DEFAULT 1,
        -- When true this catalog type appears as a column in product lists/grids
        VisibleEnLista    BIT            NOT NULL DEFAULT 0,
        Requerido         BIT            NOT NULL DEFAULT 0,
        Orden             INT            NOT NULL DEFAULT 0,
        Estado            BIT            NOT NULL DEFAULT 1,
        FechaCreacion     DATETIME       NOT NULL DEFAULT GETDATE(),
        CONSTRAINT PK_TiposCatalogo PRIMARY KEY CLUSTERED (Id ASC)
    );

    -- Unique constraint: one catalog type per (code, domain) combination
    CREATE UNIQUE INDEX UQ_TiposCatalogo_Codigo_Dominio
        ON TiposCatalogo (Codigo, Dominio)
        WHERE Codigo IS NOT NULL;

    PRINT 'Table TiposCatalogo created.';
END
ELSE
    PRINT 'Table TiposCatalogo already exists — skipped.';
GO

-- ============================================================================
-- TABLE: ProductoCatalogos
-- Junction table that links a Producto to selected Catalogo values for each
-- TipoCatalogo dimension (e.g. Línea=Electricidad, Marca=Bosch, etc.)
-- ============================================================================
IF OBJECT_ID('ProductoCatalogos', 'U') IS NULL
BEGIN
    CREATE TABLE ProductoCatalogos (
        Id               BIGINT   IDENTITY(1,1) NOT NULL,
        ProductoId       BIGINT   NOT NULL,          -- FK → Productos.Id (existing table)
        TipoCatalogoId   INT      NOT NULL,          -- FK → TiposCatalogo.Id
        CatalogoId       INT      NOT NULL,          -- FK → Catalogo.Id  (existing table)
        FechaRegistro    DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT PK_ProductoCatalogos PRIMARY KEY CLUSTERED (Id ASC),
        -- A product can only have one value per catalog-type dimension
        CONSTRAINT UQ_ProductoCatalogo UNIQUE (ProductoId, TipoCatalogoId),
        CONSTRAINT FK_ProdCat_TiposCatalogo FOREIGN KEY (TipoCatalogoId) REFERENCES TiposCatalogo (Id)
    );

    CREATE INDEX IX_ProductoCatalogos_ProductoId     ON ProductoCatalogos (ProductoId);
    CREATE INDEX IX_ProductoCatalogos_TipoCatalogoId ON ProductoCatalogos (TipoCatalogoId);
    CREATE INDEX IX_ProductoCatalogos_CatalogoId     ON ProductoCatalogos (CatalogoId);

    PRINT 'Table ProductoCatalogos created.';
END
ELSE
    PRINT 'Table ProductoCatalogos already exists — skipped.';
GO

-- ============================================================================
-- ALTER: ParametrosSistema — add RubroId and RubroNombre columns
-- These columns record which rubro this empresa belongs to so the app can load
-- the correct catalog templates automatically.
-- ============================================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'ParametrosSistema') AND name = N'RubroId'
)
BEGIN
    ALTER TABLE ParametrosSistema ADD RubroId INT NULL;
    PRINT 'Column ParametrosSistema.RubroId added.';
END
ELSE
    PRINT 'Column ParametrosSistema.RubroId already exists — skipped.';
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'ParametrosSistema') AND name = N'RubroNombre'
)
BEGIN
    ALTER TABLE ParametrosSistema ADD RubroNombre NVARCHAR(100) NULL;
    PRINT 'Column ParametrosSistema.RubroNombre added.';
END
ELSE
    PRINT 'Column ParametrosSistema.RubroNombre already exists — skipped.';
GO

-- ============================================================================
-- ALTER: Catalogo — add RubroId column
-- Allows catalog values to be filtered / restricted to a specific rubro so
-- that a multi-rubro installation can have rubro-specific catalog items while
-- keeping shared ones (RubroId IS NULL) visible to all.
-- ============================================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'Catalogo') AND name = N'RubroId'
)
BEGIN
    ALTER TABLE Catalogo ADD RubroId INT NULL;
    PRINT 'Column Catalogo.RubroId added.';
END
ELSE
    PRINT 'Column Catalogo.RubroId already exists — skipped.';
GO

-- ============================================================================
-- ALTER: Catalogo — add TipoCatalogoId column (optional linkage)
-- Links a Catalogo value to its TipoCatalogo row for easier querying.
-- ============================================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'Catalogo') AND name = N'TipoCatalogoId'
)
BEGIN
    ALTER TABLE Catalogo ADD TipoCatalogoId INT NULL;
    PRINT 'Column Catalogo.TipoCatalogoId added.';
END
ELSE
    PRINT 'Column Catalogo.TipoCatalogoId already exists — skipped.';
GO

-- Add index on Catalogo.TipoCatalogoId for fast filtered lookups
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Catalogo_TipoCatalogoId' AND object_id = OBJECT_ID(N'Catalogo')
)
BEGIN
    CREATE INDEX IX_Catalogo_TipoCatalogoId ON Catalogo (TipoCatalogoId) WHERE TipoCatalogoId IS NOT NULL;
    PRINT 'Index IX_Catalogo_TipoCatalogoId created.';
END
ELSE
    PRINT 'Index IX_Catalogo_TipoCatalogoId already exists — skipped.';
GO

-- Add index on Catalogo.RubroId for fast rubro-filtered lookups
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Catalogo_RubroId' AND object_id = OBJECT_ID(N'Catalogo')
)
BEGIN
    CREATE INDEX IX_Catalogo_RubroId ON Catalogo (RubroId) WHERE RubroId IS NOT NULL;
    PRINT 'Index IX_Catalogo_RubroId created.';
END
ELSE
    PRINT 'Index IX_Catalogo_RubroId already exists — skipped.';
GO

PRINT '=== 03_TiposCatalogo_SCM.sql completed successfully. ===';
GO
