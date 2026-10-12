using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KcalTracker.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    HashClave = table.Column<string>(type: "TEXT", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", nullable: true),
                    CreadoEl = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CategoriasAlimento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    Orden = table.Column<int>(type: "INTEGER", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoriasAlimento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CategoriasAlimento_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GruposMusculares",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    Orden = table.Column<int>(type: "INTEGER", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GruposMusculares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GruposMusculares_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TiposActividad",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    Orden = table.Column<int>(type: "INTEGER", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposActividad", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TiposActividad_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TiposComida",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    Orden = table.Column<int>(type: "INTEGER", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposComida", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TiposComida_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Alimentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    Porcion = table.Column<string>(type: "TEXT", nullable: false),
                    Kcal = table.Column<double>(type: "REAL", nullable: false),
                    Proteina = table.Column<double>(type: "REAL", nullable: true),
                    Carbos = table.Column<double>(type: "REAL", nullable: true),
                    Grasa = table.Column<double>(type: "REAL", nullable: true),
                    CategoriaAlimentoId = table.Column<int>(type: "INTEGER", nullable: true),
                    EsAfuera = table.Column<bool>(type: "INTEGER", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    Origen = table.Column<int>(type: "INTEGER", nullable: false),
                    ObtenidoEl = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alimentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Alimentos_CategoriasAlimento_CategoriaAlimentoId",
                        column: x => x.CategoriaAlimentoId,
                        principalTable: "CategoriasAlimento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Alimentos_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Comidas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    TipoComidaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nota = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comidas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comidas_TiposComida_TipoComidaId",
                        column: x => x.TipoComidaId,
                        principalTable: "TiposComida",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Comidas_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComidaItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ComidaId = table.Column<int>(type: "INTEGER", nullable: false),
                    AlimentoId = table.Column<int>(type: "INTEGER", nullable: true),
                    Cantidad = table.Column<double>(type: "REAL", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    Porcion = table.Column<string>(type: "TEXT", nullable: false),
                    Kcal = table.Column<double>(type: "REAL", nullable: false),
                    Proteina = table.Column<double>(type: "REAL", nullable: true),
                    Carbos = table.Column<double>(type: "REAL", nullable: true),
                    Grasa = table.Column<double>(type: "REAL", nullable: true),
                    EsAfuera = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComidaItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComidaItems_Alimentos_AlimentoId",
                        column: x => x.AlimentoId,
                        principalTable: "Alimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ComidaItems_Comidas_ComidaId",
                        column: x => x.ComidaId,
                        principalTable: "Comidas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alimentos_CategoriaAlimentoId",
                table: "Alimentos",
                column: "CategoriaAlimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Alimentos_UsuarioId_Nombre",
                table: "Alimentos",
                columns: new[] { "UsuarioId", "Nombre" });

            migrationBuilder.CreateIndex(
                name: "IX_CategoriasAlimento_UsuarioId_Nombre",
                table: "CategoriasAlimento",
                columns: new[] { "UsuarioId", "Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComidaItems_AlimentoId",
                table: "ComidaItems",
                column: "AlimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_ComidaItems_ComidaId",
                table: "ComidaItems",
                column: "ComidaId");

            migrationBuilder.CreateIndex(
                name: "IX_Comidas_TipoComidaId",
                table: "Comidas",
                column: "TipoComidaId");

            migrationBuilder.CreateIndex(
                name: "IX_Comidas_UsuarioId_Fecha_TipoComidaId",
                table: "Comidas",
                columns: new[] { "UsuarioId", "Fecha", "TipoComidaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GruposMusculares_UsuarioId_Nombre",
                table: "GruposMusculares",
                columns: new[] { "UsuarioId", "Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TiposActividad_UsuarioId_Nombre",
                table: "TiposActividad",
                columns: new[] { "UsuarioId", "Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TiposComida_UsuarioId_Nombre",
                table: "TiposComida",
                columns: new[] { "UsuarioId", "Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComidaItems");

            migrationBuilder.DropTable(
                name: "GruposMusculares");

            migrationBuilder.DropTable(
                name: "TiposActividad");

            migrationBuilder.DropTable(
                name: "Alimentos");

            migrationBuilder.DropTable(
                name: "Comidas");

            migrationBuilder.DropTable(
                name: "CategoriasAlimento");

            migrationBuilder.DropTable(
                name: "TiposComida");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
