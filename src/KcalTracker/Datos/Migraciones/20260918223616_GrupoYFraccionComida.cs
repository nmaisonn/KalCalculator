using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KcalTracker.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class GrupoYFraccionComida : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "CantidadBase",
                table: "ComidaItems",
                type: "REAL",
                nullable: false,
                defaultValue: 1.0);

            migrationBuilder.AddColumn<double>(
                name: "GrupoFactor",
                table: "ComidaItems",
                type: "REAL",
                nullable: false,
                defaultValue: 1.0);

            migrationBuilder.AddColumn<string>(
                name: "GrupoId",
                table: "ComidaItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GrupoNombre",
                table: "ComidaItems",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CantidadBase",
                table: "ComidaItems");

            migrationBuilder.DropColumn(
                name: "GrupoFactor",
                table: "ComidaItems");

            migrationBuilder.DropColumn(
                name: "GrupoId",
                table: "ComidaItems");

            migrationBuilder.DropColumn(
                name: "GrupoNombre",
                table: "ComidaItems");
        }
    }
}
