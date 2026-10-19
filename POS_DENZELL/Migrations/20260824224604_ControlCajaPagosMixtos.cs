using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS_DENZELL.Migrations
{
    /// <inheritdoc />
    public partial class ControlCajaPagosMixtos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ventas_Usuarios_IdUsuario",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "MetodoPago",
                table: "Ventas");

            migrationBuilder.AddColumn<int>(
                name: "IdTurno",
                table: "Ventas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "PagoEfectivo",
                table: "Ventas",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PagoYape",
                table: "Ventas",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "RutaFotoComprobante",
                table: "Ventas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Vuelto",
                table: "Ventas",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "TurnosCaja",
                columns: table => new
                {
                    IdTurno = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdUsuarioApertura = table.Column<int>(type: "int", nullable: false),
                    FechaHoraApertura = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaHoraCierre = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MontoInicial = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    MontoFinalCalculado = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    MontoFinalReal = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TurnosCaja", x => x.IdTurno);
                    table.ForeignKey(
                        name: "FK_TurnosCaja_Usuarios_IdUsuarioApertura",
                        column: x => x.IdUsuarioApertura,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MovimientosCaja",
                columns: table => new
                {
                    IdMovimiento = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdTurno = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    FechaHora = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientosCaja", x => x.IdMovimiento);
                    table.ForeignKey(
                        name: "FK_MovimientosCaja_TurnosCaja_IdTurno",
                        column: x => x.IdTurno,
                        principalTable: "TurnosCaja",
                        principalColumn: "IdTurno",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_IdTurno",
                table: "Ventas",
                column: "IdTurno");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosCaja_IdTurno",
                table: "MovimientosCaja",
                column: "IdTurno");

            migrationBuilder.CreateIndex(
                name: "IX_TurnosCaja_IdUsuarioApertura",
                table: "TurnosCaja",
                column: "IdUsuarioApertura");

            migrationBuilder.AddForeignKey(
                name: "FK_Ventas_TurnosCaja_IdTurno",
                table: "Ventas",
                column: "IdTurno",
                principalTable: "TurnosCaja",
                principalColumn: "IdTurno",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Ventas_Usuarios_IdUsuario",
                table: "Ventas",
                column: "IdUsuario",
                principalTable: "Usuarios",
                principalColumn: "IdUsuario",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ventas_TurnosCaja_IdTurno",
                table: "Ventas");

            migrationBuilder.DropForeignKey(
                name: "FK_Ventas_Usuarios_IdUsuario",
                table: "Ventas");

            migrationBuilder.DropTable(
                name: "MovimientosCaja");

            migrationBuilder.DropTable(
                name: "TurnosCaja");

            migrationBuilder.DropIndex(
                name: "IX_Ventas_IdTurno",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "IdTurno",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "PagoEfectivo",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "PagoYape",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "RutaFotoComprobante",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "Vuelto",
                table: "Ventas");

            migrationBuilder.AddColumn<string>(
                name: "MetodoPago",
                table: "Ventas",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_Ventas_Usuarios_IdUsuario",
                table: "Ventas",
                column: "IdUsuario",
                principalTable: "Usuarios",
                principalColumn: "IdUsuario",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
