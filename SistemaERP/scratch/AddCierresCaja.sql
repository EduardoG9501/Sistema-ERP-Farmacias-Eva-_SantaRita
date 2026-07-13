-- Create CierresCaja table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CierresCaja]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[CierresCaja] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [AperturaCajaId] INT NOT NULL,
        [UsuarioCierraId] INT NOT NULL,
        [FechaCierre] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [MontoFinal] DECIMAL(18,2) NOT NULL,
        [Diferencia] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [DetalleDenominaciones] NVARCHAR(MAX) NULL,
        [MetodosPago] NVARCHAR(MAX) NULL,
        CONSTRAINT [PK_CierresCaja] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CierresCaja_AperturasCaja] FOREIGN KEY ([AperturaCajaId]) REFERENCES [AperturasCaja]([Id]),
        CONSTRAINT [FK_CierresCaja_Usuarios] FOREIGN KEY ([UsuarioCierraId]) REFERENCES [Usuarios]([Id])
    );
END;

-- Record the migration in EF Core history
IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = N'20260526230000_AddCierresCaja')
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260526230000_AddCierresCaja', N'10.0.2');
END;
