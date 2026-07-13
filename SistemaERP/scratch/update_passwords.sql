-- Actualizar contraseña del usuario admin en la BD de FERRETERIA (empresa 7)
-- Password: admin123 en Base64 UTF-16LE = YQBkAG0AaQBuADEAMgAzAA==
UPDATE [Usuarios] SET [Contraseña] = 'YQBkAG0AaQBuADEAMgAzAA==' WHERE [Usuario] = 'admin';
