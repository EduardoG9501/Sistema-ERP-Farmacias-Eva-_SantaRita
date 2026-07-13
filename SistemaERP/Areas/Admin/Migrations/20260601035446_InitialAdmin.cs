using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaERP.Areas.Admin.Migrations
{
    /// <inheritdoc />
    public partial class InitialAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminUsuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Login = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminUsuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rubros",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Icono = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rubros", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Empresas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RUC = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RubroId = table.Column<int>(type: "int", nullable: false),
                    ConnectionStringEncriptado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ServidorDB = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NombreDB = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Logo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Direccion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Ciudad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NombreContacto = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FechaAlta = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaVencimiento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Estado = table.Column<bool>(type: "bit", nullable: false),
                    VersionSistema = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UltimoAccesoAdmin = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empresas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Empresas_Rubros_RubroId",
                        column: x => x.RubroId,
                        principalTable: "Rubros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlantillasRubro",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RubroId = table.Column<int>(type: "int", nullable: false),
                    TipoPlantilla = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Clave = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Valor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Extras = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantillasRubro", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlantillasRubro_Rubros_RubroId",
                        column: x => x.RubroId,
                        principalTable: "Rubros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdminAuditLog",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdminUsuarioId = table.Column<int>(type: "int", nullable: false),
                    EmpresaId = table.Column<int>(type: "int", nullable: true),
                    Accion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Detalle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaHora = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IP = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminAuditLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdminAuditLog_AdminUsuarios_AdminUsuarioId",
                        column: x => x.AdminUsuarioId,
                        principalTable: "AdminUsuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdminAuditLog_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Backups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    FechaBackup = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RutaArchivo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TamanoMB = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AdminUsuarioId = table.Column<int>(type: "int", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Backups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Backups_AdminUsuarios_AdminUsuarioId",
                        column: x => x.AdminUsuarioId,
                        principalTable: "AdminUsuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Backups_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CamposPersonalizados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: true),
                    RubroId = table.Column<int>(type: "int", nullable: true),
                    Entidad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NombreCampo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Etiqueta = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TipoDato = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OpcionesLista = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Requerido = table.Column<bool>(type: "bit", nullable: false),
                    MinLongitud = table.Column<int>(type: "int", nullable: true),
                    MaxLongitud = table.Column<int>(type: "int", nullable: true),
                    ExpresionRegular = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EsUnico = table.Column<bool>(type: "bit", nullable: false),
                    ValorMinimo = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    ValorMaximo = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Decimales = table.Column<int>(type: "int", nullable: true),
                    ValorDefault = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Placeholder = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Tooltip = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    VisibleEnFormulario = table.Column<bool>(type: "bit", nullable: false),
                    VisibleEnLista = table.Column<bool>(type: "bit", nullable: false),
                    VisibleEnReporte = table.Column<bool>(type: "bit", nullable: false),
                    VisibleEnEtiqueta = table.Column<bool>(type: "bit", nullable: false),
                    VisibleEnFactura = table.Column<bool>(type: "bit", nullable: false),
                    OrdenVisualizacion = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreadoPorAdmin = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CamposPersonalizados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CamposPersonalizados_AdminUsuarios_CreadoPorAdmin",
                        column: x => x.CreadoPorAdmin,
                        principalTable: "AdminUsuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CamposPersonalizados_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CamposPersonalizados_Rubros_RubroId",
                        column: x => x.RubroId,
                        principalTable: "Rubros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "UsuarioEmpresa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Login = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioEmpresa", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsuarioEmpresa_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditLog_AdminUsuarioId",
                table: "AdminAuditLog",
                column: "AdminUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditLog_EmpresaId",
                table: "AdminAuditLog",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Backups_AdminUsuarioId",
                table: "Backups",
                column: "AdminUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Backups_EmpresaId",
                table: "Backups",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_CamposPersonalizados_CreadoPorAdmin",
                table: "CamposPersonalizados",
                column: "CreadoPorAdmin");

            migrationBuilder.CreateIndex(
                name: "IX_CamposPersonalizados_EmpresaId",
                table: "CamposPersonalizados",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_CamposPersonalizados_RubroId",
                table: "CamposPersonalizados",
                column: "RubroId");

            migrationBuilder.CreateIndex(
                name: "IX_Empresas_RubroId",
                table: "Empresas",
                column: "RubroId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantillasRubro_RubroId",
                table: "PlantillasRubro",
                column: "RubroId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioEmpresa_EmpresaId",
                table: "UsuarioEmpresa",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioEmpresa_Login_EmpresaId",
                table: "UsuarioEmpresa",
                columns: new[] { "Login", "EmpresaId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminAuditLog");

            migrationBuilder.DropTable(
                name: "Backups");

            migrationBuilder.DropTable(
                name: "CamposPersonalizados");

            migrationBuilder.DropTable(
                name: "PlantillasRubro");

            migrationBuilder.DropTable(
                name: "UsuarioEmpresa");

            migrationBuilder.DropTable(
                name: "AdminUsuarios");

            migrationBuilder.DropTable(
                name: "Empresas");

            migrationBuilder.DropTable(
                name: "Rubros");
        }
    }
}
