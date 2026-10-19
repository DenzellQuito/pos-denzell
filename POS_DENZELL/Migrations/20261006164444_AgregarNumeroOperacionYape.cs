using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS_DENZELL.Migrations
{
    /// <inheritdoc />
    public partial class AgregarNumeroOperacionYape : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NumeroOperacion",
                table: "Ventas",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NumeroOperacion",
                table: "Ventas");
        }
    }
}
