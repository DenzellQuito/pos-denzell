using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS_DENZELL.Migrations
{
    /// <inheritdoc />
    public partial class AjustesSunat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClienteDocumento",
                table: "Ventas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClienteNombre",
                table: "Ventas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MensajeSunat",
                table: "Ventas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalIGV",
                table: "Ventas",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "SubTotal",
                table: "VentaDetalles",
                type: "decimal(10,3)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "PrecioUnitario",
                table: "VentaDetalles",
                type: "decimal(10,3)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClienteDocumento",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "ClienteNombre",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "MensajeSunat",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "TotalIGV",
                table: "Ventas");

            migrationBuilder.AlterColumn<decimal>(
                name: "SubTotal",
                table: "VentaDetalles",
                type: "decimal(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,3)");

            migrationBuilder.AlterColumn<decimal>(
                name: "PrecioUnitario",
                table: "VentaDetalles",
                type: "decimal(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,3)");
        }
    }
}
