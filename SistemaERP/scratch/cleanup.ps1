$connString = 'Server=38.247.140.172,10433;Database=AdminDB;User Id=sa;Password=Farc0inf$info$;TrustServerCertificate=True;'
$conn = New-Object System.Data.SqlClient.SqlConnection($connString)
$conn.Open()

$cmd = $conn.CreateCommand()
$cmd.CommandText = "UPDATE AdminAuditLog SET EmpresaId = NULL WHERE EmpresaId = 5; DELETE FROM Empresas WHERE Id = 5;"
$rows = $cmd.ExecuteNonQuery()
Write-Host "Cleanup completed successfully. Deleted records."

$conn.Close()
