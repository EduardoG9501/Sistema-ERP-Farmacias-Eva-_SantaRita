-- Verificar columnas en tabla Ventas
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Ventas'
  AND COLUMN_NAME IN ('NameClienteTem','CF_Cedula','CF_Direccion')
ORDER BY COLUMN_NAME;

-- Ver datos del último ticket
SELECT TOP 3 Id, NumDoc, NameClienteTem, CF_Cedula, CF_Direccion, SRI_Estado
FROM Ventas ORDER BY Id DESC;
