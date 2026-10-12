using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KcalTracker.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class PlazoInicioYCantidadEntreno : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Cantidad",
                table: "SesionesGym",
                type: "REAL",
                nullable: false,
                defaultValue: 1.0);

            migrationBuilder.AddColumn<double>(
                name: "Cantidad",
                table: "SesionesCardio",
                type: "REAL",
                nullable: false,
                defaultValue: 1.0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "FechaInicioObjetivo",
                table: "Perfiles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Cantidad",
                table: "ComidaItems",
                type: "REAL",
                nullable: false,
                defaultValue: 1.0,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AddColumn<double>(
                name: "Cantidad",
                table: "ActividadLogs",
                type: "REAL",
                nullable: false,
                defaultValue: 1.0);

            // El plazo empieza en la medición de referencia (p. ej. 16/08 del demo).
            migrationBuilder.Sql("""
                UPDATE Perfiles
                SET FechaInicioObjetivo = (
                    SELECT m.Fecha FROM Mediciones m
                    WHERE m.UsuarioId = Perfiles.UsuarioId AND m.EsReferencia = 1
                    LIMIT 1
                )
                WHERE FechaInicioObjetivo IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cantidad",
                table: "SesionesGym");

            migrationBuilder.DropColumn(
                name: "Cantidad",
                table: "SesionesCardio");

            migrationBuilder.DropColumn(
                name: "FechaInicioObjetivo",
                table: "Perfiles");

            migrationBuilder.DropColumn(
                name: "Cantidad",
                table: "ActividadLogs");

            migrationBuilder.AlterColumn<double>(
                name: "Cantidad",
                table: "ComidaItems",
                type: "REAL",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "REAL",
                oldDefaultValue: 1.0);
        }
    }
}
