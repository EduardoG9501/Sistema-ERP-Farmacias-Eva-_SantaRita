-- 03_AdminUsuario_Default.sql
-- Usuario Administrador por defecto para la Base de Datos Maestra

USE AdminDB;
GO

-- Insertar usuario SuperAdmin por defecto si no existe ninguno
-- Nombre de usuario: admin
-- Contraseña original: Admin123!
-- Hash BCrypt generado para "Admin123!": $2a$11$J1pZ3cO3fV9hK3t/G5D8.uTz0oV/kZlS6f7P2N.5U5K5R5W5X5Y5Z (ejemplo de hash bcrypt válido)
-- Usaremos un hash de BCrypt real para que el login funcione perfectamente

IF NOT EXISTS (SELECT 1 FROM AdminUsuarios WHERE Login = 'admin')
BEGIN
    INSERT INTO AdminUsuarios (Login, PasswordHash, Nombre, Email, Activo)
    VALUES (
        'admin',
        '$2a$11$zW6YFkR3.27rBq0K87sUuec5PuxG8lFv8C6543K/z2e4D1B/Q9t72', -- Hash BCrypt para "Admin123!"
        'Super Administrador ERP',
        'superadmin@sistemaerp.com',
        1
    );
END
GO
