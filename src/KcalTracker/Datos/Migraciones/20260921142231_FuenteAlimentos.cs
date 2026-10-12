using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KcalTracker.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class FuenteAlimentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FuenteAlimentos",
                table: "Perfiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FuenteAlimentos",
                table: "Perfiles");
        }
    }
}
