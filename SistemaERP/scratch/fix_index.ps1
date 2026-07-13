$connString = 'Server=38.247.140.172,10433;Database=AdminDB;User Id=sa;Password=Farc0inf$info$;TrustServerCertificate=True;'
$conn = New-Object System.Data.SqlClient.SqlConnection($connString)
$conn.Open()

$sql = @"
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UsuarioEmpresa_Login' AND object_id = OBJECT_ID('UsuarioEmpresa'))
BEGIN
    DROP INDEX IX_UsuarioEmpresa_Login ON UsuarioEmpresa;
    PRINT 'Dropped index IX_UsuarioEmpresa_Login successfully.';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UsuarioEmpresa_Login_EmpresaId' AND object_id = OBJECT_ID('UsuarioEmpresa'))
BEGIN
    CREATE UNIQUE INDEX IX_UsuarioEmpresa_Login_EmpresaId ON UsuarioEmpresa (Login, EmpresaId);
    PRINT 'Created unique index IX_UsuarioEmpresa_Login_EmpresaId successfully.';
END
"@

$cmd = $conn.CreateCommand()
$cmd.CommandText = $sql
$cmd.ExecuteNonQuery() | Out-Null
Write-Host "Index fix script completed successfully."
$conn.Close()
