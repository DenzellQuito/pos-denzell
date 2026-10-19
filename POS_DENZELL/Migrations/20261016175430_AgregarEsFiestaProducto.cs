using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS_DENZELL.Migrations
{
    /// <inheritdoc />
    public partial class AgregarEsFiestaProducto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EsFiesta",
                table: "Productos",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EsFiesta",
                table: "Productos");
        }
    }
}
