using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaERP.Migrations
{
    /// <inheritdoc />
    public partial class AddPosLayoutOption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PosLayout",
                table: "ParametrosSistema",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PosLayout",
                table: "ParametrosSistema");
        }
    }
}
