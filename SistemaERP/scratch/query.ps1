$connString = 'Server=38.247.140.172,10433;Database=FERRETERIA;User Id=sa;Password=Farc0inf$info$;TrustServerCertificate=True;'
$conn = New-Object System.Data.SqlClient.SqlConnection($connString)
$conn.Open()

Write-Host "=== MODULOS WEB EN FERRETERIA ==="
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT Id, ControllerName, DisplayName, Estado FROM ModulosWeb"
$adapter = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
$dt = New-Object System.Data.DataTable
$adapter.Fill($dt) | Out-Null
$dt | Format-Table -AutoSize

Write-Host "=== PRIVILEGIOS WEB EN FERRETERIA ==="
$cmd.CommandText = "SELECT Id, Id_Modulo, ActionName, DisplayName, Estado FROM PrivilegiosWeb"
$dt2 = New-Object System.Data.DataTable
$adapter.Fill($dt2) | Out-Null
$dt2 | Format-Table -AutoSize

$conn.Close()
