using KcalTracker.Calculo;
using KcalTracker.Datos.Plantillas;
using KcalTracker.Dominio;
using KcalTracker.Servicios;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KcalTracker.Tests;

public class PlantillasTests
{
    [Fact]
    public async Task CA26_UnaCuentaNuevaArrancaVaciaHastaQueSeImporta()
    {
        using var baseDatos = new BaseDePrueba();
        await using (var dbAlta = baseDatos.Como(null))
        {
            var cuentas = new ServicioCuentas(dbAlta, new PasswordHasher<Usuario>());
            var (_, usuario) = await cuentas.RegistrarAsync("ana@test.com", "clave-segura", "Ana");
            Assert.True(usuario!.BienvenidaPendiente);
        }

        int ana;
        await using (var db = baseDatos.Como(null))
            ana = (await db.Usuarios.SingleAsync(u => u.Email == "ana@test.com")).Id;

        await using (var db = baseDatos.Como(ana))
        {
            Assert.Empty(await db.Alimentos.ToListAsync());
            Assert.Empty(await db.Actividades.ToListAsync());
            Assert.Empty(await db.Rutinas.ToListAsync());
            Assert.Empty(await db.TiposComida.ToListAsync());
            Assert.Empty(await db.Combos.ToListAsync());
            Assert.Empty(await db.Ejercicios.ToListAsync());
            Assert.Empty(await db.Mediciones.ToListAsync());
        }

        await using (var db = baseDatos.Como(ana))
        {
            var plantillas = new ServicioPlantillas(db);
            await plantillas.ImportarAsync(PlantillaExcel.Id, [PartePlantilla.TiposComida]);
        }

        await using (var db = baseDatos.Como(ana))
        {
            Assert.Equal(PlantillaExcel.TiposComida.Count, await db.TiposComida.CountAsync());
            Assert.Empty(await db.Alimentos.ToListAsync());
            Assert.Empty(await db.Actividades.ToListAsync());
            Assert.Empty(await db.Rutinas.ToListAsync());
        }
    }

    [Fact]
    public async Task CA13_ElPanDeLaPlantillaTieneProteina4_3()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, _) = await baseDatos.CrearAnaYBeto();
        await using var db = baseDatos.Como(ana);
        await new ServicioPlantillas(db).ImportarAsync(PlantillaExcel.Id, [PartePlantilla.Alimentos]);

        var pan = await db.Alimentos.SingleAsync(a => a.Nombre == "Pan Los Sorchantes");
        Assert.Equal(4.3, pan.Proteina);
        Assert.Equal(1.1, pan.Grasa);
        Assert.Equal(127, pan.Kcal);
        Assert.Equal(OrigenDato.Plantilla, pan.Origen);
    }

    [Fact]
    public async Task CA14_LosAlimentosConMacrosCierranContraLasKcal()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, _) = await baseDatos.CrearAnaYBeto();
        await using var db = baseDatos.Como(ana);
        await new ServicioPlantillas(db).ImportarAsync(PlantillaExcel.Id, [PartePlantilla.Alimentos]);

        var conMacros = await db.Alimentos
            .Where(a => a.Proteina != null && a.Carbos != null && a.Grasa != null)
            .ToListAsync();
        Assert.NotEmpty(conMacros);

        // El origen de CA-14 es la tabla de verificación del anexo: los valores
        // reconstruidos de P-01 cierran con menos del 3 %.
        string[] verificados = ["Pan Los Sorchantes", "Queso magro", "Yogur Viva Light", "Pechuga s/piel cruda", "Cuadril magro crudo", "Huevo", "Arroz Saman crudo", "Requeson light", "Leche descremada", "Manzana verde", "Mandarina", "Aceite"];
        foreach (var a in conMacros.Where(a => verificados.Contains(a.Nombre)))
        {
            var desvio = CoherenciaMacros.Desvio(a.Kcal, a.Proteina, a.Carbos, a.Grasa);
            Assert.True(desvio <= 0.03, $"{a.Nombre} ({a.Porcion}): desvío {desvio:P1}");
        }

        // El resto (hígado USDA vs Atwater ~4 %) cierra holgado. Un decimal
        // convertido a fecha reventaría este umbral.
        foreach (var a in conMacros)
        {
            var desvio = CoherenciaMacros.Desvio(a.Kcal, a.Proteina, a.Carbos, a.Grasa);
            Assert.True(desvio <= 0.20, $"{a.Nombre} ({a.Porcion}): desvío {desvio:P1}");
        }
    }

    [Fact]
    public async Task ImportarCopiaSinVincularYNoMeteElGymEnElCatalogoDeActividades()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, _) = await baseDatos.CrearAnaYBeto();
        await using var db = baseDatos.Como(ana);
        await new ServicioPlantillas(db).ImportarAsync(PlantillaExcel.Id,
        [
            PartePlantilla.Alimentos,
            PartePlantilla.Combos,
            PartePlantilla.Actividades,
            PartePlantilla.EjerciciosYRutinas
        ]);

        Assert.Equal(["Fútbol 5"], await db.Actividades.Select(a => a.Nombre).ToListAsync());
        Assert.DoesNotContain(await db.Actividades.Select(a => a.Nombre).ToListAsync(), n => n.Contains("Día 1"));

        var dia1 = await db.RutinaDias.SingleAsync(d => d.Nombre.StartsWith("Día 1"));
        Assert.Equal(330, dia1.KcalEstimadas);

        var muerto = await db.RutinaEjercicios
            .Include(e => e.Ejercicio)
            .SingleAsync(e => e.Ejercicio!.Nombre == "Peso muerto rumano");
        Assert.Equal(27.5, muerto.PesoRef);
        Assert.True(muerto.PuedeMas);

        var hiit = await db.ProtocolosCardio.Include(p => p.Bloques)
            .SingleAsync(p => p.Nombre == "HIIT jueves");
        var sprint = hiit.Bloques.OrderBy(b => b.Orden).ElementAt(1);
        Assert.Equal(9.0, sprint.Velocidad);
        Assert.Equal(4.8, hiit.Bloques.OrderBy(b => b.Orden).ElementAt(2).Velocidad);

        var desayuno = await db.Combos.Include(c => c.Items).SingleAsync(c => c.Nombre == "Desayuno completo");
        Assert.Equal(5, desayuno.Items.Count);
        var ids = desayuno.Items.Select(i => i.AlimentoId).ToList();
        var kcalDesayuno = await db.Alimentos.Where(a => ids.Contains(a.Id)).SumAsync(a => a.Kcal);
        Assert.Equal(361, Redondeo.Kcal(kcalDesayuno));
    }

    [Fact]
    public async Task SincronizarActualizaValoresDePlantillaSinDuplicar()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, _) = await baseDatos.CrearAnaYBeto();
        await using var db = baseDatos.Como(ana);
        var plantillas = new ServicioPlantillas(db);
        await plantillas.ImportarAsync(PlantillaExcel.Id, [PartePlantilla.Alimentos, PartePlantilla.Actividades, PartePlantilla.EjerciciosYRutinas]);

        var requeson = await db.Alimentos.SingleAsync(a => a.Nombre == "Requeson light");
        requeson.Kcal = 46;
        var dia1 = await db.RutinaDias.SingleAsync(d => d.Nombre.StartsWith("Día 1"));
        dia1.KcalEstimadas = 300;
        await db.SaveChangesAsync();

        await plantillas.SincronizarValoresAsync();

        Assert.Equal(14, (await db.Alimentos.SingleAsync(a => a.Nombre == "Requeson light")).Kcal);
        Assert.Equal(330, (await db.RutinaDias.SingleAsync(d => d.Nombre.StartsWith("Día 1"))).KcalEstimadas);
        Assert.Equal(PlantillaExcel.Alimentos.Count, await db.Alimentos.CountAsync());
    }

    [Fact]
    public async Task ReimportarNoDuplicaYNoCruzaCuentas()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, beto) = await baseDatos.CrearAnaYBeto();

        await using (var db = baseDatos.Como(ana))
        {
            var plantillas = new ServicioPlantillas(db);
            var primero = await plantillas.ImportarAsync(PlantillaExcel.Id, [PartePlantilla.Alimentos, PartePlantilla.EjerciciosYRutinas]);
            var segundo = await plantillas.ImportarAsync(PlantillaExcel.Id, [PartePlantilla.Alimentos, PartePlantilla.EjerciciosYRutinas]);
            Assert.True(primero.Creados > 0);
            Assert.Equal(0, segundo.Creados);
            Assert.Equal(primero.Creados, segundo.OmitidosPorYaExistir);
        }

        await using (var dbAna = baseDatos.Como(ana))
            Assert.Equal(PlantillaExcel.Alimentos.Count, await dbAna.Alimentos.CountAsync());

        await using var dbBeto = baseDatos.Como(beto);
        Assert.Empty(await dbBeto.Alimentos.ToListAsync());
        Assert.Empty(await dbBeto.Rutinas.ToListAsync());
    }
}
