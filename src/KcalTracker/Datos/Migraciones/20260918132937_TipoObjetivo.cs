using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KcalTracker.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class TipoObjetivo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Perfiles SET ModoDeficit = 0 WHERE ModoDeficit = 2;");
            migrationBuilder.RenameColumn(
                name: "ModoDeficit",
                table: "Perfiles",
                newName: "TipoObjetivo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TipoObjetivo",
                table: "Perfiles",
                newName: "ModoDeficit");
        }
    }
}
