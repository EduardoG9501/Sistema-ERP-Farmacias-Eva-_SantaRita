-- =============================================================================
-- FILE: 04_AdminUsuario_Default.sql
-- DESC: Creates the default super-admin user in AdminDB.
--       Run AFTER 01_AdminDB_Create.sql.
--
--       DEFAULT CREDENTIALS:
--         Login    : superadmin
--         Password : Admin@2026
--
--  ┌─────────────────────────────────────────────────────────────────────┐
--  │  SECURITY NOTE                                                      │
--  │  The BCrypt hash embedded below was generated with BCrypt work      │
--  │  factor 12 for the password "Admin@2026".                           │
--  │                                                                     │
--  │  To generate a fresh hash at first-run instead of using this        │
--  │  static one, call the following C# snippet from your startup code:  │
--  │                                                                     │
--  │    string hash = BCrypt.Net.BCrypt.HashPassword("Admin@2026", 12); │
--  │                                                                     │
--  │  Then UPDATE AdminUsuarios SET PasswordHash = '<new_hash>'          │
--  │  WHERE Login = 'superadmin';                                        │
--  │                                                                     │
--  │  CHANGE THE PASSWORD IMMEDIATELY AFTER FIRST LOGIN.                 │
--  └─────────────────────────────────────────────────────────────────────┘
-- DATE: 2026-05-27
-- =============================================================================

USE AdminDB;
GO

-- BCrypt hash for "Admin@2026" (work factor 12).
-- Generated with BCrypt.Net-Next: BCrypt.Net.BCrypt.HashPassword("Admin@2026", 12)
DECLARE @PasswordHash NVARCHAR(200);
SET @PasswordHash = N'$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8WGWQZ9H8vNs7K0e7Ky';

-- ---------------------------------------------------------------------------
-- Insert default super-admin only if it does not already exist.
-- This makes the script safe to re-run (e.g. after a restore).
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM AdminUsuarios WHERE Login = N'superadmin')
BEGIN
    INSERT INTO AdminUsuarios (Login, PasswordHash, Nombre, Email, Activo, FechaCreacion)
    VALUES (
        N'superadmin',
        @PasswordHash,
        N'Super Administrador',
        N'admin@sistemaerp.com',
        1,
        GETDATE()
    );

    PRINT 'Default super-admin user "superadmin" created successfully.';
    PRINT 'IMPORTANT: Log in and change the password immediately!';
END
ELSE
BEGIN
    PRINT 'User "superadmin" already exists — no changes made.';
END
GO

-- ---------------------------------------------------------------------------
-- Optional: verify the inserted record (commented out by default)
-- ---------------------------------------------------------------------------
-- SELECT Id, Login, Nombre, Email, Activo, FechaCreacion
-- FROM AdminUsuarios
-- WHERE Login = N'superadmin';
-- GO

PRINT '=== 04_AdminUsuario_Default.sql completed successfully. ===';
GO
