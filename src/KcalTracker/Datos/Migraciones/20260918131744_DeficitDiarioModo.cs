using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KcalTracker.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class DeficitDiarioModo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "DeficitDiarioKcal",
                table: "Perfiles",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModoDeficit",
                table: "Perfiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeficitDiarioKcal",
                table: "Perfiles");

            migrationBuilder.DropColumn(
                name: "ModoDeficit",
                table: "Perfiles");
        }
    }
}
