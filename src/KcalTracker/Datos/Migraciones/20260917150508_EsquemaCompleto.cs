using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KcalTracker.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class EsquemaCompleto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Actividades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    TipoActividadId = table.Column<int>(type: "INTEGER", nullable: false),
                    Kcal = table.Column<double>(type: "REAL", nullable: false),
                    Minutos = table.Column<double>(type: "REAL", nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Actividades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Actividades_TiposActividad_TipoActividadId",
                        column: x => x.TipoActividadId,
                        principalTable: "TiposActividad",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Actividades_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Combos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    TipoComidaId = table.Column<int>(type: "INTEGER", nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Combos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Combos_TiposComida_TipoComidaId",
                        column: x => x.TipoComidaId,
                        principalTable: "TiposComida",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Combos_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Configuraciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    KcalPorKgGrasa = table.Column<double>(type: "REAL", nullable: false),
                    KcalPorKgMagro = table.Column<double>(type: "REAL", nullable: false),
                    MetodoBmr = table.Column<int>(type: "INTEGER", nullable: false),
                    BmrMedido = table.Column<double>(type: "REAL", nullable: true),
                    PctGastoMax = table.Column<double>(type: "REAL", nullable: false),
                    PctPesoSemanal = table.Column<double>(type: "REAL", nullable: false),
                    CobMinContraste = table.Column<double>(type: "REAL", nullable: false),
                    CobMinCalibrado = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configuraciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Configuraciones_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Ejercicios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    GrupoMuscularId = table.Column<int>(type: "INTEGER", nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ejercicios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ejercicios_GruposMusculares_GrupoMuscularId",
                        column: x => x.GrupoMuscularId,
                        principalTable: "GruposMusculares",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Ejercicios_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Mediciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    PesoKg = table.Column<double>(type: "REAL", nullable: false),
                    GrasaPct = table.Column<double>(type: "REAL", nullable: true),
                    MusculoPct = table.Column<double>(type: "REAL", nullable: true),
                    Visceral = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mediciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Mediciones_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Perfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    AlturaCm = table.Column<double>(type: "REAL", nullable: true),
                    FechaNacimiento = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Sexo = table.Column<int>(type: "INTEGER", nullable: true),
                    FactorActividad = table.Column<double>(type: "REAL", nullable: false),
                    GrasaObjetivoPct = table.Column<double>(type: "REAL", nullable: true),
                    PlazoSemanas = table.Column<int>(type: "INTEGER", nullable: false),
                    ProteinaObjetivoG = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Perfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Perfiles_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProtocolosCardio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProtocolosCardio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProtocolosCardio_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Rutinas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", nullable: true),
                    EsActiva = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rutinas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rutinas_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActividadLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ActividadId = table.Column<int>(type: "INTEGER", nullable: true),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", nullable: false),
                    Kcal = table.Column<double>(type: "REAL", nullable: false),
                    Minutos = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActividadLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActividadLogs_Actividades_ActividadId",
                        column: x => x.ActividadId,
                        principalTable: "Actividades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ActividadLogs_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComboItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ComboId = table.Column<int>(type: "INTEGER", nullable: false),
                    AlimentoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Cantidad = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComboItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComboItems_Alimentos_AlimentoId",
                        column: x => x.AlimentoId,
                        principalTable: "Alimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ComboItems_Combos_ComboId",
                        column: x => x.ComboId,
                        principalTable: "Combos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BloquesCardio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProtocoloId = table.Column<int>(type: "INTEGER", nullable: false),
                    Orden = table.Column<int>(type: "INTEGER", nullable: false),
                    Minutos = table.Column<double>(type: "REAL", nullable: false),
                    Velocidad = table.Column<double>(type: "REAL", nullable: true),
                    Inclinacion = table.Column<double>(type: "REAL", nullable: true),
                    Repeticiones = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BloquesCardio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BloquesCardio_ProtocolosCardio_ProtocoloId",
                        column: x => x.ProtocoloId,
                        principalTable: "ProtocolosCardio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SesionesCardio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ProtocoloId = table.Column<int>(type: "INTEGER", nullable: true),
                    Minutos = table.Column<double>(type: "REAL", nullable: false),
                    Velocidad = table.Column<double>(type: "REAL", nullable: true),
                    Inclinacion = table.Column<double>(type: "REAL", nullable: true),
                    Kcal = table.Column<double>(type: "REAL", nullable: false),
                    KcalMin = table.Column<double>(type: "REAL", nullable: true),
                    KcalMax = table.Column<double>(type: "REAL", nullable: true),
                    Nota = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SesionesCardio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SesionesCardio_ProtocolosCardio_ProtocoloId",
                        column: x => x.ProtocoloId,
                        principalTable: "ProtocolosCardio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SesionesCardio_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RutinaDias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RutinaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    Orden = table.Column<int>(type: "INTEGER", nullable: false),
                    KcalEstimadas = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RutinaDias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RutinaDias_Rutinas_RutinaId",
                        column: x => x.RutinaId,
                        principalTable: "Rutinas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RutinaEjercicios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RutinaDiaId = table.Column<int>(type: "INTEGER", nullable: false),
                    EjercicioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Orden = table.Column<int>(type: "INTEGER", nullable: false),
                    Series = table.Column<int>(type: "INTEGER", nullable: false),
                    RepsMin = table.Column<int>(type: "INTEGER", nullable: false),
                    RepsMax = table.Column<int>(type: "INTEGER", nullable: true),
                    PesoRef = table.Column<double>(type: "REAL", nullable: true),
                    PuedeMas = table.Column<bool>(type: "INTEGER", nullable: false),
                    Observacion = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RutinaEjercicios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RutinaEjercicios_Ejercicios_EjercicioId",
                        column: x => x.EjercicioId,
                        principalTable: "Ejercicios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RutinaEjercicios_RutinaDias_RutinaDiaId",
                        column: x => x.RutinaDiaId,
                        principalTable: "RutinaDias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SesionesGym",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    RutinaDiaId = table.Column<int>(type: "INTEGER", nullable: true),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    Kcal = table.Column<double>(type: "REAL", nullable: true),
                    Nota = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SesionesGym", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SesionesGym_RutinaDias_RutinaDiaId",
                        column: x => x.RutinaDiaId,
                        principalTable: "RutinaDias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SesionesGym_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SesionEjercicios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SesionGymId = table.Column<int>(type: "INTEGER", nullable: false),
                    EjercicioId = table.Column<int>(type: "INTEGER", nullable: true),
                    Orden = table.Column<int>(type: "INTEGER", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    GrupoMuscular = table.Column<string>(type: "TEXT", nullable: true),
                    Series = table.Column<int>(type: "INTEGER", nullable: false),
                    Reps = table.Column<int>(type: "INTEGER", nullable: true),
                    Peso = table.Column<double>(type: "REAL", nullable: true),
                    PuedeMas = table.Column<bool>(type: "INTEGER", nullable: false),
                    Observacion = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SesionEjercicios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SesionEjercicios_Ejercicios_EjercicioId",
                        column: x => x.EjercicioId,
                        principalTable: "Ejercicios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SesionEjercicios_SesionesGym_SesionGymId",
                        column: x => x.SesionGymId,
                        principalTable: "SesionesGym",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SeriesRealizadas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SesionEjercicioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Numero = table.Column<int>(type: "INTEGER", nullable: false),
                    Reps = table.Column<int>(type: "INTEGER", nullable: false),
                    Peso = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeriesRealizadas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeriesRealizadas_SesionEjercicios_SesionEjercicioId",
                        column: x => x.SesionEjercicioId,
                        principalTable: "SesionEjercicios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Actividades_TipoActividadId",
                table: "Actividades",
                column: "TipoActividadId");

            migrationBuilder.CreateIndex(
                name: "IX_Actividades_UsuarioId",
                table: "Actividades",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ActividadLogs_ActividadId",
                table: "ActividadLogs",
                column: "ActividadId");

            migrationBuilder.CreateIndex(
                name: "IX_ActividadLogs_UsuarioId_Fecha",
                table: "ActividadLogs",
                columns: new[] { "UsuarioId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_BloquesCardio_ProtocoloId",
                table: "BloquesCardio",
                column: "ProtocoloId");

            migrationBuilder.CreateIndex(
                name: "IX_ComboItems_AlimentoId",
                table: "ComboItems",
                column: "AlimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_ComboItems_ComboId",
                table: "ComboItems",
                column: "ComboId");

            migrationBuilder.CreateIndex(
                name: "IX_Combos_TipoComidaId",
                table: "Combos",
                column: "TipoComidaId");

            migrationBuilder.CreateIndex(
                name: "IX_Combos_UsuarioId",
                table: "Combos",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Configuraciones_UsuarioId",
                table: "Configuraciones",
                column: "UsuarioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ejercicios_GrupoMuscularId",
                table: "Ejercicios",
                column: "GrupoMuscularId");

            migrationBuilder.CreateIndex(
                name: "IX_Ejercicios_UsuarioId_Nombre",
                table: "Ejercicios",
                columns: new[] { "UsuarioId", "Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Mediciones_UsuarioId_Fecha",
                table: "Mediciones",
                columns: new[] { "UsuarioId", "Fecha" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Perfiles_UsuarioId",
                table: "Perfiles",
                column: "UsuarioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProtocolosCardio_UsuarioId",
                table: "ProtocolosCardio",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_RutinaDias_RutinaId",
                table: "RutinaDias",
                column: "RutinaId");

            migrationBuilder.CreateIndex(
                name: "IX_RutinaEjercicios_EjercicioId",
                table: "RutinaEjercicios",
                column: "EjercicioId");

            migrationBuilder.CreateIndex(
                name: "IX_RutinaEjercicios_RutinaDiaId",
                table: "RutinaEjercicios",
                column: "RutinaDiaId");

            migrationBuilder.CreateIndex(
                name: "IX_Rutinas_UnaActivaPorUsuario",
                table: "Rutinas",
                column: "UsuarioId",
                unique: true,
                filter: "EsActiva = 1");

            migrationBuilder.CreateIndex(
                name: "IX_SeriesRealizadas_SesionEjercicioId",
                table: "SeriesRealizadas",
                column: "SesionEjercicioId");

            migrationBuilder.CreateIndex(
                name: "IX_SesionEjercicios_EjercicioId",
                table: "SesionEjercicios",
                column: "EjercicioId");

            migrationBuilder.CreateIndex(
                name: "IX_SesionEjercicios_SesionGymId",
                table: "SesionEjercicios",
                column: "SesionGymId");

            migrationBuilder.CreateIndex(
                name: "IX_SesionesCardio_ProtocoloId",
                table: "SesionesCardio",
                column: "ProtocoloId");

            migrationBuilder.CreateIndex(
                name: "IX_SesionesCardio_UsuarioId_Fecha",
                table: "SesionesCardio",
                columns: new[] { "UsuarioId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_SesionesGym_RutinaDiaId",
                table: "SesionesGym",
                column: "RutinaDiaId");

            migrationBuilder.CreateIndex(
                name: "IX_SesionesGym_UsuarioId_Fecha",
                table: "SesionesGym",
                columns: new[] { "UsuarioId", "Fecha" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActividadLogs");

            migrationBuilder.DropTable(
                name: "BloquesCardio");

            migrationBuilder.DropTable(
                name: "ComboItems");

            migrationBuilder.DropTable(
                name: "Configuraciones");

            migrationBuilder.DropTable(
                name: "Mediciones");

            migrationBuilder.DropTable(
                name: "Perfiles");

            migrationBuilder.DropTable(
                name: "RutinaEjercicios");

            migrationBuilder.DropTable(
                name: "SeriesRealizadas");

            migrationBuilder.DropTable(
                name: "SesionesCardio");

            migrationBuilder.DropTable(
                name: "Actividades");

            migrationBuilder.DropTable(
                name: "Combos");

            migrationBuilder.DropTable(
                name: "SesionEjercicios");

            migrationBuilder.DropTable(
                name: "ProtocolosCardio");

            migrationBuilder.DropTable(
                name: "Ejercicios");

            migrationBuilder.DropTable(
                name: "SesionesGym");

            migrationBuilder.DropTable(
                name: "RutinaDias");

            migrationBuilder.DropTable(
                name: "Rutinas");
        }
    }
}
