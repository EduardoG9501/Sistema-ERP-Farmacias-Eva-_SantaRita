$conn = New-Object System.Data.SqlClient.SqlConnection('Server=38.247.140.172,10433;Database=FERRETERIA_ROMO;User Id=sa;Password=Farc0inf$info$;TrustServerCertificate=True;')
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = 'SELECT COUNT(*) FROM Denominaciones'
$count = $cmd.ExecuteScalar()
Write-Host "Total Denominaciones in FERRETERIA_ROMO: $count"

$cmd.CommandText = 'SELECT COUNT(*) FROM CierresCaja'
$cCount = $cmd.ExecuteScalar()
Write-Host "Total CierresCaja in FERRETERIA_ROMO: $cCount"

$cmd.CommandText = 'SELECT TOP 10 Id, Nombre, Valor, Tipo FROM Denominaciones'
$reader = $cmd.ExecuteReader()
while ($reader.Read()) {
    Write-Host "Denominacion: Id=$($reader['Id']), Nombre=$($reader['Nombre']), Valor=$($reader['Valor']), Tipo=$($reader['Tipo'])"
}
$reader.Close()

$conn.Close()
