using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KcalTracker.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class AlimentoTipoComida : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TipoComidaId",
                table: "Alimentos",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Alimentos_TipoComidaId",
                table: "Alimentos",
                column: "TipoComidaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Alimentos_TiposComida_TipoComidaId",
                table: "Alimentos",
                column: "TipoComidaId",
                principalTable: "TiposComida",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Alimentos_TiposComida_TipoComidaId",
                table: "Alimentos");

            migrationBuilder.DropIndex(
                name: "IX_Alimentos_TipoComidaId",
                table: "Alimentos");

            migrationBuilder.DropColumn(
                name: "TipoComidaId",
                table: "Alimentos");
        }
    }
}
