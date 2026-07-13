using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaERP.Migrations
{
    /// <inheritdoc />
    public partial class AddClaveAprobacionVencido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClaveAprobacionVencido",
                table: "ParametrosSistema",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Gastos_CuentaContableId",
                table: "Gastos",
                column: "CuentaContableId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionEstadoResultados_CuentaContableId",
                table: "ConfiguracionEstadoResultados",
                column: "CuentaContableId");

            migrationBuilder.AddForeignKey(
                name: "FK_ConfiguracionEstadoResultados_CuentasContables_CuentaContableId",
                table: "ConfiguracionEstadoResultados",
                column: "CuentaContableId",
                principalTable: "CuentasContables",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Gastos_CuentasContables_CuentaContableId",
                table: "Gastos",
                column: "CuentaContableId",
                principalTable: "CuentasContables",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConfiguracionEstadoResultados_CuentasContables_CuentaContableId",
                table: "ConfiguracionEstadoResultados");

            migrationBuilder.DropForeignKey(
                name: "FK_Gastos_CuentasContables_CuentaContableId",
                table: "Gastos");

            migrationBuilder.DropIndex(
                name: "IX_Gastos_CuentaContableId",
                table: "Gastos");

            migrationBuilder.DropIndex(
                name: "IX_ConfiguracionEstadoResultados_CuentaContableId",
                table: "ConfiguracionEstadoResultados");

            migrationBuilder.DropColumn(
                name: "ClaveAprobacionVencido",
                table: "ParametrosSistema");
        }
    }
}
