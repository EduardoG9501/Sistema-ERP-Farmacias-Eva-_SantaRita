-- Drop existing table (old schema with only Codigo/Nombre)
DROP TABLE [dbo].[Monedas];

-- Create Monedas table with full schema
CREATE TABLE [dbo].[Monedas] (
    [Codigo] NVARCHAR(450) NOT NULL,
    [Nombre] NVARCHAR(MAX) NULL,
    [Simbolo] NVARCHAR(MAX) NULL,
    [Pais] NVARCHAR(MAX) NULL,
    [Activo] BIT NOT NULL DEFAULT 1,
    CONSTRAINT [PK_Monedas] PRIMARY KEY ([Codigo])
);

-- Seed data for Latin American currencies and major forex
INSERT INTO [dbo].[Monedas] ([Codigo], [Activo], [Nombre], [Pais], [Simbolo]) VALUES
    (N'USD', 1, N'Dólar Americano', N'Estados Unidos / Ecuador', N'$'),
    (N'EUR', 1, N'Euro', N'Unión Europea', N'€'),
    (N'MXN', 1, N'Peso Mexicano', N'México', N'$'),
    (N'NIO', 1, N'Córdoba Nicaragüense', N'Nicaragua', N'C$'),
    (N'COP', 1, N'Peso Colombiano', N'Colombia', N'$'),
    (N'PEN', 1, N'Sol Peruano', N'Perú', N'S/'),
    (N'BRL', 1, N'Real Brasileño', N'Brasil', N'R$'),
    (N'ARS', 1, N'Peso Argentino', N'Argentina', N'$'),
    (N'CLP', 1, N'Peso Chileno', N'Chile', N'$'),
    (N'BOB', 1, N'Boliviano', N'Bolivia', N'Bs'),
    (N'PYG', 1, N'Guaraní Paraguayo', N'Paraguay', N'₲'),
    (N'UYU', 1, N'Peso Uruguayo', N'Uruguay', N'$U'),
    (N'CRC', 1, N'Colón Costarricense', N'Costa Rica', N'₡'),
    (N'GTQ', 1, N'Quetzal Guatemalteco', N'Guatemala', N'Q');

-- Record the migration in EF Core history
IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = N'20260526215755_AddMonedasTable')
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260526215755_AddMonedasTable', N'10.0.2');
END;
