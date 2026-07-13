using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaERP.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogoPublico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CatalogoMostrarPrecios",
                table: "ParametrosSistema",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CatalogoPublicoActivo",
                table: "ParametrosSistema",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CatalogoTitulo",
                table: "ParametrosSistema",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CatalogoWhatsApp",
                table: "ParametrosSistema",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CatalogoMostrarPrecios",
                table: "ParametrosSistema");

            migrationBuilder.DropColumn(
                name: "CatalogoPublicoActivo",
                table: "ParametrosSistema");

            migrationBuilder.DropColumn(
                name: "CatalogoTitulo",
                table: "ParametrosSistema");

            migrationBuilder.DropColumn(
                name: "CatalogoWhatsApp",
                table: "ParametrosSistema");
        }
    }
}
