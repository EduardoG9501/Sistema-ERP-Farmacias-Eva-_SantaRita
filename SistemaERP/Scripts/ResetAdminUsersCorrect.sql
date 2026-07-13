USE AdminDB;
GO

UPDATE dbo.AdminUsuarios
SET PasswordHash = '$2a$11$wZDZaHJCtuMXqDYOa9yJnOqYqeCd/HPDKyeOMxyr8xPX6uRgriQDW',
    Activo = 1
WHERE Login IN ('admin', 'superadmin');
GO

PRINT 'Hashes actualizados correctamente a la contraseña SuperSecure2026!';
GO
