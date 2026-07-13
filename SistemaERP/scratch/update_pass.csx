using System;
using System.Text;
using Microsoft.Data.SqlClient;

// Contraseña admin123 en Base64 UTF-16LE
var passwordBase64 = Convert.ToBase64String(Encoding.Unicode.GetBytes("admin123"));
Console.WriteLine($"Password Base64: {passwordBase64}");

// Conexión a FERRETERIA (empresa 7 - la que se sembró con Admin@2026)
var connStr = "Server=38.247.140.172,10433;Database=FERRETERIA;User Id=sa;Password=Farc0inf$info$;TrustServerCertificate=True;";

using var conn = new SqlConnection(connStr);
await conn.OpenAsync();

// Verificar usuarios existentes
using (var cmdCheck = new SqlCommand("SELECT [Id], [Usuario], [Contraseña], [Tipo_Usuario], [Estado] FROM [Usuarios]", conn))
{
    using var reader = await cmdCheck.ExecuteReaderAsync();
    Console.WriteLine("\n=== Usuarios actuales en FERRETERIA ===");
    while (await reader.ReadAsync())
    {
        Console.WriteLine($"  ID: {reader[0]}, Login: {reader[1]}, Pass: {reader[2]}, Tipo: {reader[3]}, Estado: {reader[4]}");
    }
}

// Actualizar contraseña
using (var cmdUpdate = new SqlCommand($"UPDATE [Usuarios] SET [Contraseña] = @pass WHERE [Usuario] = 'admin'", conn))
{
    cmdUpdate.Parameters.AddWithValue("@pass", passwordBase64);
    var rows = await cmdUpdate.ExecuteNonQueryAsync();
    Console.WriteLine($"\nFilas actualizadas en FERRETERIA: {rows}");
}

// También actualizar en AdminDB -> UsuarioEmpresa
var adminConnStr = "Server=38.247.140.172,10433;Database=AdminDB;User Id=sa;Password=Farc0inf$info$;TrustServerCertificate=True;";
using var adminConn = new SqlConnection(adminConnStr);
await adminConn.OpenAsync();

using (var cmdAdmin = new SqlCommand($"UPDATE [UsuarioEmpresa] SET [PasswordHash] = @pass WHERE [Login] = 'admin'", adminConn))
{
    cmdAdmin.Parameters.AddWithValue("@pass", passwordBase64);
    var rows = await cmdAdmin.ExecuteNonQueryAsync();
    Console.WriteLine($"Filas actualizadas en AdminDB.UsuarioEmpresa: {rows}");
}

// Verificar también qué empresas hay
using (var cmdEmpresas = new SqlCommand("SELECT [Id], [Nombre], [RUC], [Estado] FROM [Empresas]", adminConn))
{
    using var reader = await cmdEmpresas.ExecuteReaderAsync();
    Console.WriteLine("\n=== Empresas registradas ===");
    while (await reader.ReadAsync())
    {
        Console.WriteLine($"  ID: {reader[0]}, Nombre: {reader[1]}, RUC: {reader[2]}, Estado: {reader[3]}");
    }
}

Console.WriteLine("\n¡Contraseñas actualizadas exitosamente!");
