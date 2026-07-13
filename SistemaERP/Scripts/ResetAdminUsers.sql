/* --------------------------------------------------------------
   ResetAdminUsers.sql
   - Establece la contraseña permanente "SuperSecure2026!"
   - Si los usuarios existen, actualiza su hash; si no existen, los crea.
   -------------------------------------------------------------- */

USE AdminDB;
-- No usar GO aquí; queremos que la variable persista en todo el lote.

DECLARE @NewHash NVARCHAR(200) = N'$2a$12$KIXx4iUuX1fT5G6x9lYbUeU5F8c0c2zE5K6Yj4Q1b8F5uJbY6cW2';   -- hash de "SuperSecure2026!"

/* ------------------- admin ------------------------------------ */
IF EXISTS (SELECT 1 FROM dbo.AdminUsuarios WHERE Login = N'admin')
BEGIN
    UPDATE dbo.AdminUsuarios
    SET
        PasswordHash   = @NewHash,
        Activo         = 1
        -- Opcional: desbloquear la cuenta si existen columnas de lockout
        -- LockoutEnabled = 0,
        -- LockoutEnd     = NULL
    WHERE Login = N'admin';
    PRINT 'Usuario "admin" actualizado con la nueva contraseña.';
END
ELSE
BEGIN
    INSERT INTO dbo.AdminUsuarios
        (Login, PasswordHash, Nombre, Email, Activo, FechaCreacion)
    VALUES
        (N'admin', @NewHash,
         N'Super Administrador ERP',
         N'superadmin@sistemaerp.com',
         1, GETDATE());
    PRINT 'Usuario "admin" creado con la nueva contraseña.';
END

/* ------------------- superadmin ------------------------------ */
IF EXISTS (SELECT 1 FROM dbo.AdminUsuarios WHERE Login = N'superadmin')
BEGIN
    UPDATE dbo.AdminUsuarios
    SET
        PasswordHash   = @NewHash,
        Activo         = 1
        -- LockoutEnabled = 0,
        -- LockoutEnd     = NULL
    WHERE Login = N'superadmin';
    PRINT 'Usuario "superadmin" actualizado con la nueva contraseña.';
END
ELSE
BEGIN
    INSERT INTO dbo.AdminUsuarios
        (Login, PasswordHash, Nombre, Email, Activo, FechaCreacion)
    VALUES
        (N'superadmin', @NewHash,
         N'Super Administrador',
         N'admin@sistemaerp.com',
         1, GETDATE());
    PRINT 'Usuario "superadmin" creado con la nueva contraseña.';
END

PRINT '=== ResetAdminUsers.sql terminado correctamente ===';