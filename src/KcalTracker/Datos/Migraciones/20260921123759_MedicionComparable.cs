using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KcalTracker.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class MedicionComparable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EsComparable",
                table: "Mediciones",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EsComparable",
                table: "Mediciones");
        }
    }
}
