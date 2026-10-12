using System.Reflection;
using KcalTracker.Dominio;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace KcalTracker.Datos;

/// <summary>
/// Acceso a datos con aislamiento por cuenta incorporado (D-13). Toda entidad que
/// implemente <see cref="EntidadDeUsuario"/> queda filtrada por el usuario actual sin
/// que las consultas tengan que recordarlo, y recibe su UsuarioId al insertarse.
/// </summary>
public class KcalDbContext : DbContext
{
    private readonly IUsuarioActual _usuarioActual;
    private readonly CatalogoCompartido _catalogo;

    public KcalDbContext(
        DbContextOptions<KcalDbContext> opciones,
        IUsuarioActual usuarioActual,
        CatalogoCompartido? catalogo = null)
        : base(opciones)
    {
        _usuarioActual = usuarioActual;
        _catalogo = catalogo ?? new CatalogoCompartido();
    }

    /// <summary>
    /// Cero cuando nadie ingresó, de modo que el filtro global no encuentre nada en lugar
    /// de devolver datos de otra cuenta. Es una propiedad y no un valor capturado porque
    /// EF Core la convierte en parámetro de la consulta y la reevalúa en cada una.
    /// </summary>
    public int UsuarioIdActual => _usuarioActual.UsuarioId ?? 0;

    /// <summary>
    /// Cuenta dueña del catálogo de la app. Cero si todavía no se sembró.
    /// El filtro de alimentos y categorías la incluye junto a la cuenta actual.
    /// </summary>
    public int UsuarioIdCatalogo => _catalogo.Id;

    public bool EsDelCatalogo(int usuarioId)
        => UsuarioIdCatalogo != 0 && usuarioId == UsuarioIdCatalogo;

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Configuracion> Configuraciones => Set<Configuracion>();
    public DbSet<Perfil> Perfiles => Set<Perfil>();
    public DbSet<TipoComida> TiposComida => Set<TipoComida>();
    public DbSet<TipoActividad> TiposActividad => Set<TipoActividad>();
    public DbSet<CategoriaAlimento> CategoriasAlimento => Set<CategoriaAlimento>();
    public DbSet<GrupoMuscular> GruposMusculares => Set<GrupoMuscular>();
    public DbSet<Alimento> Alimentos => Set<Alimento>();
    public DbSet<Comida> Comidas => Set<Comida>();
    public DbSet<ComidaItem> ComidaItems => Set<ComidaItem>();
    public DbSet<Combo> Combos => Set<Combo>();
    public DbSet<ComboItem> ComboItems => Set<ComboItem>();
    public DbSet<Actividad> Actividades => Set<Actividad>();
    public DbSet<ActividadLog> ActividadLogs => Set<ActividadLog>();
    public DbSet<Ejercicio> Ejercicios => Set<Ejercicio>();
    public DbSet<Rutina> Rutinas => Set<Rutina>();
    public DbSet<RutinaDia> RutinaDias => Set<RutinaDia>();
    public DbSet<RutinaEjercicio> RutinaEjercicios => Set<RutinaEjercicio>();
    public DbSet<SesionGym> SesionesGym => Set<SesionGym>();
    public DbSet<SesionEjercicio> SesionEjercicios => Set<SesionEjercicio>();
    public DbSet<SerieRealizada> SeriesRealizadas => Set<SerieRealizada>();
    public DbSet<ProtocoloCardio> ProtocolosCardio => Set<ProtocoloCardio>();
    public DbSet<BloqueCardio> BloquesCardio => Set<BloqueCardio>();
    public DbSet<SesionCardio> SesionesCardio => Set<SesionCardio>();
    public DbSet<Medicion> Mediciones => Set<Medicion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Usuario>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Email).IsRequired();
            e.Property(u => u.HashClave).IsRequired();
        });

        modelBuilder.Entity<Configuracion>(e =>
        {
            e.ToTable("Configuraciones");
            e.HasIndex(c => c.UsuarioId).IsUnique();
        });

        modelBuilder.Entity<Perfil>(e =>
        {
            e.ToTable("Perfiles");
            e.HasIndex(p => p.UsuarioId).IsUnique();
        });

        ConfigurarCatalogo<TipoComida>(modelBuilder, "TiposComida");
        ConfigurarCatalogo<TipoActividad>(modelBuilder, "TiposActividad");
        ConfigurarCatalogo<CategoriaAlimento>(modelBuilder, "CategoriasAlimento");
        ConfigurarCatalogo<GrupoMuscular>(modelBuilder, "GruposMusculares");

        modelBuilder.Entity<Alimento>(e =>
        {
            e.Property(a => a.Nombre).IsRequired();
            e.Property(a => a.Porcion).IsRequired();
            e.HasIndex(a => new { a.UsuarioId, a.Nombre });
            e.HasOne(a => a.CategoriaAlimento)
                .WithMany()
                .HasForeignKey(a => a.CategoriaAlimentoId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(a => a.TipoComida)
                .WithMany()
                .HasForeignKey(a => a.TipoComidaId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Comida>(e =>
        {
            e.HasIndex(c => new { c.UsuarioId, c.Fecha, c.TipoComidaId }).IsUnique();
            e.HasOne(c => c.TipoComida)
                .WithMany()
                .HasForeignKey(c => c.TipoComidaId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasMany(c => c.Items)
                .WithOne(i => i.Comida!)
                .HasForeignKey(i => i.ComidaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ComidaItem>(e =>
        {
            e.Property(i => i.Nombre).IsRequired();
            e.Property(i => i.Porcion).IsRequired();
            e.HasOne(i => i.Alimento)
                .WithMany()
                .HasForeignKey(i => i.AlimentoId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasQueryFilter(i => i.Comida!.UsuarioId == UsuarioIdActual);
            e.Property(i => i.Cantidad).HasDefaultValue(1.0);
            e.Property(i => i.CantidadBase).HasDefaultValue(1.0);
            e.Property(i => i.GrupoFactor).HasDefaultValue(1.0);
        });

        modelBuilder.Entity<Combo>(e =>
        {
            e.Property(c => c.Nombre).IsRequired();
            e.HasOne(c => c.TipoComida)
                .WithMany()
                .HasForeignKey(c => c.TipoComidaId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasMany(c => c.Items)
                .WithOne(i => i.Combo!)
                .HasForeignKey(i => i.ComboId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ComboItem>(e =>
        {
            e.HasOne(i => i.Alimento)
                .WithMany()
                .HasForeignKey(i => i.AlimentoId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(i => i.Combo!.UsuarioId == UsuarioIdActual);
        });

        modelBuilder.Entity<Actividad>(e =>
        {
            e.ToTable("Actividades");
            e.Property(a => a.Nombre).IsRequired();
            e.HasOne(a => a.TipoActividad)
                .WithMany()
                .HasForeignKey(a => a.TipoActividadId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ActividadLog>(e =>
        {
            e.Property(l => l.Nombre).IsRequired();
            e.Property(l => l.Tipo).IsRequired();
            e.HasIndex(l => new { l.UsuarioId, l.Fecha });
            e.HasOne(l => l.Actividad)
                .WithMany()
                .HasForeignKey(l => l.ActividadId)
                .OnDelete(DeleteBehavior.SetNull);
            e.Property(l => l.Cantidad).HasDefaultValue(1.0);
        });

        modelBuilder.Entity<Ejercicio>(e =>
        {
            e.Property(x => x.Nombre).IsRequired();
            e.HasIndex(x => new { x.UsuarioId, x.Nombre }).IsUnique();
            e.HasOne(x => x.GrupoMuscular)
                .WithMany()
                .HasForeignKey(x => x.GrupoMuscularId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Rutina>(e =>
        {
            e.Property(r => r.Nombre).IsRequired();
            e.HasMany(r => r.Dias)
                .WithOne(d => d.Rutina!)
                .HasForeignKey(d => d.RutinaId)
                .OnDelete(DeleteBehavior.Cascade);
            // A lo sumo una rutina activa por cuenta (RF-J4).
            e.HasIndex(r => r.UsuarioId)
                .IsUnique()
                .HasFilter("EsActiva = 1")
                .HasDatabaseName("IX_Rutinas_UnaActivaPorUsuario");
        });

        modelBuilder.Entity<RutinaDia>(e =>
        {
            e.Property(d => d.Nombre).IsRequired();
            e.HasMany(d => d.Ejercicios)
                .WithOne(x => x.RutinaDia!)
                .HasForeignKey(x => x.RutinaDiaId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(d => d.Rutina!.UsuarioId == UsuarioIdActual);
        });

        modelBuilder.Entity<RutinaEjercicio>(e =>
        {
            e.HasOne(x => x.Ejercicio)
                .WithMany()
                .HasForeignKey(x => x.EjercicioId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(x => x.RutinaDia!.Rutina!.UsuarioId == UsuarioIdActual);
        });

        modelBuilder.Entity<SesionGym>(e =>
        {
            e.ToTable("SesionesGym");
            e.Property(s => s.Nombre).IsRequired();
            e.HasIndex(s => new { s.UsuarioId, s.Fecha });
            e.HasOne(s => s.RutinaDia)
                .WithMany()
                .HasForeignKey(s => s.RutinaDiaId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasMany(s => s.Ejercicios)
                .WithOne(x => x.SesionGym!)
                .HasForeignKey(x => x.SesionGymId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Property(s => s.Cantidad).HasDefaultValue(1.0);
        });

        modelBuilder.Entity<SesionEjercicio>(e =>
        {
            e.Property(x => x.Nombre).IsRequired();
            e.HasOne(x => x.Ejercicio)
                .WithMany()
                .HasForeignKey(x => x.EjercicioId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasMany(x => x.SeriesDetalle)
                .WithOne(s => s.SesionEjercicio!)
                .HasForeignKey(s => s.SesionEjercicioId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(x => x.SesionGym!.UsuarioId == UsuarioIdActual);
        });

        modelBuilder.Entity<SerieRealizada>(e =>
        {
            e.ToTable("SeriesRealizadas");
            e.HasQueryFilter(s => s.SesionEjercicio!.SesionGym!.UsuarioId == UsuarioIdActual);
        });

        modelBuilder.Entity<ProtocoloCardio>(e =>
        {
            e.ToTable("ProtocolosCardio");
            e.Property(p => p.Nombre).IsRequired();
            e.HasMany(p => p.Bloques)
                .WithOne(b => b.Protocolo!)
                .HasForeignKey(b => b.ProtocoloId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BloqueCardio>(e =>
        {
            e.ToTable("BloquesCardio");
            e.HasQueryFilter(b => b.Protocolo!.UsuarioId == UsuarioIdActual);
        });

        modelBuilder.Entity<SesionCardio>(e =>
        {
            e.ToTable("SesionesCardio");
            e.HasIndex(s => new { s.UsuarioId, s.Fecha });
            e.HasOne(s => s.Protocolo)
                .WithMany()
                .HasForeignKey(s => s.ProtocoloId)
                .OnDelete(DeleteBehavior.SetNull);
            e.Property(s => s.Cantidad).HasDefaultValue(1.0);
        });

        modelBuilder.Entity<Medicion>(e =>
        {
            e.ToTable("Mediciones");
            e.HasIndex(m => new { m.UsuarioId, m.Fecha }).IsUnique();
            e.Property(m => m.EsComparable).HasDefaultValue(true);
        });

        AplicarFiltroDeUsuarioATodo(modelBuilder);
        AplicarIndicesPorUsuario(modelBuilder);
    }

    private static void ConfigurarCatalogo<T>(ModelBuilder modelBuilder, string tabla)
        where T : class, EntidadDeUsuario
    {
        modelBuilder.Entity<T>(e =>
        {
            e.ToTable(tabla);
            e.Property("Nombre").IsRequired();
            e.HasIndex("UsuarioId", "Nombre").IsUnique();
        });
    }

    /// <summary>
    /// El aislamiento se declara una vez para todo el modelo en lugar de repetirse en
    /// cada consulta (D-13). Una consulta que olvide el filtro devuelve vacío, no datos
    /// ajenos (CA-24).
    /// </summary>
    private void AplicarFiltroDeUsuarioATodo(ModelBuilder modelBuilder)
    {
        var aplicar = typeof(KcalDbContext)
            .GetMethod(nameof(AplicarFiltroDeUsuario), BindingFlags.NonPublic | BindingFlags.Instance)!;

        foreach (var tipo in modelBuilder.Model.GetEntityTypes())
        {
            if (tipo.BaseType is not null) continue;
            if (!typeof(EntidadDeUsuario).IsAssignableFrom(tipo.ClrType)) continue;

            if (tipo.ClrType == typeof(Alimento) || tipo.ClrType == typeof(CategoriaAlimento))
            {
                typeof(KcalDbContext)
                    .GetMethod(nameof(AplicarFiltroConCatalogo), BindingFlags.NonPublic | BindingFlags.Instance)!
                    .MakeGenericMethod(tipo.ClrType)
                    .Invoke(this, [modelBuilder]);
                continue;
            }

            aplicar.MakeGenericMethod(tipo.ClrType).Invoke(this, [modelBuilder]);
        }
    }

    private void AplicarFiltroDeUsuario<T>(ModelBuilder modelBuilder) where T : class, EntidadDeUsuario
    {
        modelBuilder.Entity<T>(e =>
        {
            e.HasQueryFilter(x => x.UsuarioId == UsuarioIdActual);
            e.HasOne<Usuario>()
                .WithMany()
                .HasForeignKey(x => x.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>
    /// Alimentos y tipos de alimento del catálogo de la app se ven junto a los de la
    /// cuenta, para poder usarlos en Hoy sin copiarlos. El resto de tablas sigue
    /// estrictamente por usuario.
    /// </summary>
    private void AplicarFiltroConCatalogo<T>(ModelBuilder modelBuilder) where T : class, EntidadDeUsuario
    {
        modelBuilder.Entity<T>(e =>
        {
            e.HasQueryFilter(x =>
                x.UsuarioId == UsuarioIdActual
                || (UsuarioIdActual != 0 && UsuarioIdCatalogo != 0 && x.UsuarioId == UsuarioIdCatalogo));
            e.HasOne<Usuario>()
                .WithMany()
                .HasForeignKey(x => x.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>
    /// Todo índice de una tabla por usuario empieza por UsuarioId (RNF-18), porque toda
    /// consulta lleva ese predicado adelante por el filtro global.
    /// </summary>
    private static void AplicarIndicesPorUsuario(ModelBuilder modelBuilder)
    {
        foreach (var tipo in modelBuilder.Model.GetEntityTypes())
        {
            if (tipo.BaseType is not null) continue;
            if (!typeof(EntidadDeUsuario).IsAssignableFrom(tipo.ClrType)) continue;

            var yaIndexado = tipo.GetIndexes()
                .Any(i => i.Properties[0].Name == nameof(EntidadDeUsuario.UsuarioId));
            if (yaIndexado) continue;

            tipo.AddIndex(tipo.FindProperty(nameof(EntidadDeUsuario.UsuarioId))!);
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        AsignarUsuarioALoNuevo();
        VerificarReferenciasDelMismoUsuarioAsync(CancellationToken.None).GetAwaiter().GetResult();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        AsignarUsuarioALoNuevo();
        await VerificarReferenciasDelMismoUsuarioAsync(cancellationToken);
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// El filtro global cubre las lecturas, no las inserciones. El UsuarioId se asigna acá
    /// y no en cada alta, para que olvidarlo sea imposible en lugar de improbable.
    /// </summary>
    private void AsignarUsuarioALoNuevo()
    {
        foreach (var entrada in ChangeTracker.Entries<EntidadDeUsuario>())
        {
            if (entrada.State == EntityState.Added)
            {
                if (UsuarioIdActual != 0)
                {
                    entrada.Entity.UsuarioId = UsuarioIdActual;
                }
                else if (entrada.Entity.UsuarioId == 0)
                {
                    throw new InvalidOperationException(
                        "No se puede guardar datos de usuario sin una cuenta en sesión.");
                }
                // Si no hay sesión pero UsuarioId ya viene cargado, es el alta de la cuenta:
                // se insertan Configuracion y Perfil junto con el Usuario recién creado.
            }
            else if (entrada.State == EntityState.Modified)
            {
                entrada.Property(nameof(EntidadDeUsuario.UsuarioId)).IsModified = false;
                entrada.Property(nameof(EntidadDeUsuario.UsuarioId)).CurrentValue =
                    entrada.Property(nameof(EntidadDeUsuario.UsuarioId)).OriginalValue;
            }
        }
    }

    /// <summary>
    /// El filtro global impide leer datos de otra cuenta, pero no impide referenciarlos a
    /// ciegas por su identificador. Esta verificación cierra ese hueco (CA-25), también
    /// cuando la referencia apunta a una tabla hija sin UsuarioId propio (un día de rutina,
    /// un item de combo).
    /// </summary>
    private async Task VerificarReferenciasDelMismoUsuarioAsync(CancellationToken ct)
    {
        foreach (var entrada in ChangeTracker.Entries().ToList())
        {
            if (entrada.State is not (EntityState.Added or EntityState.Modified)) continue;

            foreach (var fk in entrada.Metadata.GetForeignKeys())
            {
                var principal = fk.PrincipalEntityType.ClrType;
                if (principal == typeof(Usuario)) continue;

                var clave = LeerClaveForanea(entrada, fk.Properties);
                if (clave is null) continue;

                var dueno = await BuscarUsuarioDuenoAsync(principal, clave, ct);

                if (dueno is null)
                {
                    throw new ReferenciaAjenaException(
                        $"{entrada.Metadata.ClrType.Name} referencia un {principal.Name} inexistente.");
                }

                if (dueno != UsuarioIdActual && !EsDelCatalogo(dueno.Value))
                {
                    throw new ReferenciaAjenaException(
                        $"{entrada.Metadata.ClrType.Name} referencia un {principal.Name} de otra cuenta.");
                }
            }
        }
    }

    /// <summary>
    /// Devuelve el valor de la clave foránea, o null si no hay nada que verificar: porque
    /// es opcional y quedó vacía, o porque el padre se está insertando en este mismo
    /// guardado y su identificador todavía no existe.
    /// </summary>
    private static object? LeerClaveForanea(EntityEntry entrada, IReadOnlyList<IProperty> propiedades)
    {
        if (propiedades.Count != 1) return null;

        var valor = entrada.Property(propiedades[0].Name).CurrentValue;

        return valor switch
        {
            null => null,
            int i when i == 0 => null,
            _ => valor
        };
    }

    private async Task<int?> BuscarUsuarioDuenoAsync(Type tipo, object clave, CancellationToken ct)
        => await BuscarUsuarioDuenoAsync(tipo, clave, [], ct);

    private async Task<int?> BuscarUsuarioDuenoAsync(
        Type tipo, object clave, HashSet<string> visitados, CancellationToken ct)
    {
        if (!visitados.Add(tipo.Name)) return null;

        var enMemoria = ChangeTracker.Entries()
            .FirstOrDefault(e => e.Metadata.ClrType == tipo
                && e.State != EntityState.Deleted
                && Equals(e.Property("Id").CurrentValue, clave));

        if (enMemoria is not null)
            return await UsuarioDuenoDesdeEntradaAsync(enMemoria, visitados, ct);

        var cargada = await CargarPorIdSinFiltroAsync(tipo, clave, ct);
        return cargada is null ? null : await UsuarioDuenoDesdeEntradaAsync(cargada, visitados, ct);
    }

    private async Task<int?> UsuarioDuenoDesdeEntradaAsync(
        EntityEntry entrada, HashSet<string> visitados, CancellationToken ct)
    {
        if (entrada.Entity is EntidadDeUsuario deUsuario)
            return deUsuario.UsuarioId;

        foreach (var fk in entrada.Metadata.GetForeignKeys())
        {
            var principal = fk.PrincipalEntityType.ClrType;
            if (principal == typeof(Usuario)) continue;

            var clave = LeerClaveForanea(entrada, fk.Properties);
            if (clave is null) continue;

            var dueno = await BuscarUsuarioDuenoAsync(principal, clave, visitados, ct);
            if (dueno is not null) return dueno;
        }

        return null;
    }

    private async Task<EntityEntry?> CargarPorIdSinFiltroAsync(Type tipo, object clave, CancellationToken ct)
    {
        var metodo = typeof(KcalDbContext)
            .GetMethod(nameof(CargarPorIdSinFiltroTipadoAsync), BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod(tipo);

        var entidad = await (Task<object?>)metodo.Invoke(this, [clave, ct])!;
        return entidad is null ? null : Entry(entidad);
    }

    private async Task<object?> CargarPorIdSinFiltroTipadoAsync<T>(object clave, CancellationToken ct)
        where T : class
    {
        var id = (int)clave;
        return await Set<T>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id, ct);
    }
}

/// <summary>Se intentó guardar algo que apunta a datos de otra cuenta (RN-12, CA-25).</summary>
public class ReferenciaAjenaException(string mensaje) : InvalidOperationException(mensaje);
