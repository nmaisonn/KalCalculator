using KcalTracker.Calculo;
using KcalTracker.Datos.Plantillas;
using KcalTracker.Dominio;
using KcalTracker.Servicios;
using Microsoft.EntityFrameworkCore;

namespace KcalTracker.Tests;

public class DiaYValidacionTests
{
    [Fact]
    public async Task CA07_ElComboDesayunoSuma361()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, _) = await baseDatos.CrearAnaYBeto();
        await using (var db = baseDatos.Como(null))
            await ServicioCuentas.CompletarFilasPorCuentaAsync(db);

        await using var dbAna = baseDatos.Como(ana);
        await new ServicioPlantillas(dbAna).ImportarAsync(PlantillaExcel.Id,
            [PartePlantilla.Alimentos, PartePlantilla.Combos, PartePlantilla.TiposComida]);
        var dia = new ServicioDia(dbAna);
        var combo = await dbAna.Combos.SingleAsync(c => c.Nombre == "Desayuno completo");
        await dia.AgregarComboAsync(new DateOnly(2026, 9, 16), combo.Id);
        var vista = await dia.CargarAsync(new DateOnly(2026, 9, 16));
        Assert.Equal(361, Redondeo.Kcal(vista.Balance.Ingesta!.Kcal));
    }

    [Fact]
    public async Task MediaPorcionDejaLaMitadDeKcal()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, _) = await baseDatos.CrearAnaYBeto();
        await using (var db = baseDatos.Como(null))
            await ServicioCuentas.CompletarFilasPorCuentaAsync(db);

        await using var dbAna = baseDatos.Como(ana);
        await new ServicioPlantillas(dbAna).ImportarAsync(PlantillaExcel.Id,
            [PartePlantilla.Alimentos, PartePlantilla.Combos, PartePlantilla.TiposComida]);
        var dia = new ServicioDia(dbAna);
        var combo = await dbAna.Combos.SingleAsync(c => c.Nombre == "Desayuno completo");
        await dia.AgregarComboAsync(new DateOnly(2026, 9, 16), combo.Id);
        foreach (var item in await dbAna.ComidaItems.ToListAsync())
            Assert.Null(await dia.CambiarCantidadItemAsync(item.Id, 0.5));
        var vista = await dia.CargarAsync(new DateOnly(2026, 9, 16));
        Assert.Equal(181, Redondeo.Kcal(vista.Balance.Ingesta!.Kcal));
    }

    [Fact]
    public async Task ElComboDeDesayunoCargadoEnCenaQuedaEnCena()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, _) = await baseDatos.CrearAnaYBeto();
        await using (var db = baseDatos.Como(null))
            await ServicioCuentas.CompletarFilasPorCuentaAsync(db);

        await using var dbAna = baseDatos.Como(ana);
        await new ServicioPlantillas(dbAna).ImportarAsync(PlantillaExcel.Id,
            [PartePlantilla.Alimentos, PartePlantilla.Combos, PartePlantilla.TiposComida]);
        var dia = new ServicioDia(dbAna);
        var combo = await dbAna.Combos.SingleAsync(c => c.Nombre == "Desayuno completo");
        var desayuno = await dbAna.TiposComida.SingleAsync(t => t.Nombre == "Desayuno");
        var cena = await dbAna.TiposComida.SingleAsync(t => t.Nombre == "Cena");

        await dia.AgregarEleccionAsync(new DateOnly(2026, 9, 16), cena.Id, $"c:{combo.Id}");
        var vista = await dia.CargarAsync(new DateOnly(2026, 9, 16));

        Assert.DoesNotContain(vista.Comidas, g => g.Tipo.Id == desayuno.Id);
        var grupoCena = Assert.Single(vista.Comidas, g => g.Tipo.Id == cena.Id);
        Assert.Equal(361, Redondeo.Kcal(grupoCena.Subtotal));
    }

    [Fact]
    public async Task CA08_ElComboPolloSuma565()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, _) = await baseDatos.CrearAnaYBeto();
        await using (var db = baseDatos.Como(null))
            await ServicioCuentas.CompletarFilasPorCuentaAsync(db);

        await using var dbAna = baseDatos.Como(ana);
        await new ServicioPlantillas(dbAna).ImportarAsync(PlantillaExcel.Id,
            [PartePlantilla.Alimentos, PartePlantilla.Combos, PartePlantilla.TiposComida]);
        var dia = new ServicioDia(dbAna);
        var combo = await dbAna.Combos.SingleAsync(c => c.Nombre == "Almuerzo/cena pollo");
        await dia.AgregarComboAsync(new DateOnly(2026, 9, 16), combo.Id);
        var vista = await dia.CargarAsync(new DateOnly(2026, 9, 16));
        Assert.Equal(565, Redondeo.Kcal(vista.Balance.Ingesta!.Kcal));
    }

    [Fact]
    public async Task EditarUnAlimentoActualizaDiasCombosYTotales()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, _) = await baseDatos.CrearAnaYBeto();
        await using (var db = baseDatos.Como(null))
            await ServicioCuentas.CompletarFilasPorCuentaAsync(db);

        await using var dbAna = baseDatos.Como(ana);
        await new ServicioPlantillas(dbAna).ImportarAsync(PlantillaExcel.Id,
            [PartePlantilla.Alimentos, PartePlantilla.Combos, PartePlantilla.TiposComida]);
        var pan = await dbAna.Alimentos.SingleAsync(a => a.Nombre == "Pan Los Sorchantes");
        var tipo = await dbAna.TiposComida.FirstAsync();
        var dia = new ServicioDia(dbAna);
        await dia.AgregarAlimentoAsync(new DateOnly(2026, 9, 10), tipo.Id, pan.Id);
        await dia.AgregarAlimentoAsync(new DateOnly(2026, 9, 11), tipo.Id, pan.Id, 2);

        var (ok, _, aviso) = await new ServicioAlimentos(dbAna).GuardarAsync(new Alimento
        {
            Id = pan.Id, Nombre = pan.Nombre, Porcion = pan.Porcion,
            Kcal = 135, Proteina = pan.Proteina, Carbos = pan.Carbos, Grasa = pan.Grasa, Activo = true
        });

        Assert.True(ok);
        Assert.Contains("2 días", aviso);
        Assert.Contains("combos", aviso);

        var diez = await dia.CargarAsync(new DateOnly(2026, 9, 10));
        var once = await dia.CargarAsync(new DateOnly(2026, 9, 11));
        Assert.Equal(135, Redondeo.Kcal(diez.Balance.Ingesta!.Kcal));
        Assert.Equal(270, Redondeo.Kcal(once.Balance.Ingesta!.Kcal));

        var combo = await dbAna.Combos.Include(c => c.Items).ThenInclude(i => i.Alimento)
            .SingleAsync(c => c.Nombre == "Desayuno completo");
        var kcalCombo = combo.Items.Sum(i => (i.Alimento?.Kcal ?? 0) * i.Cantidad);
        Assert.Equal(369, Redondeo.Kcal(kcalCombo));
    }

    [Fact]
    public async Task EditarUnAlimentoNoTocaUnaCargaPuntual()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, _) = await baseDatos.CrearAnaYBeto();
        await using (var db = baseDatos.Como(null))
            await ServicioCuentas.CompletarFilasPorCuentaAsync(db);

        await using var dbAna = baseDatos.Como(ana);
        await new ServicioPlantillas(dbAna).ImportarAsync(PlantillaExcel.Id,
            [PartePlantilla.Alimentos, PartePlantilla.TiposComida]);
        var huevo = await dbAna.Alimentos.SingleAsync(a => a.Nombre == "Huevo");
        var tipo = await dbAna.TiposComida.FirstAsync();
        var dia = new ServicioDia(dbAna);
        var fecha = new DateOnly(2026, 9, 10);
        await dia.AgregarAlimentoAsync(fecha, tipo.Id, huevo.Id);
        await dia.AgregarKcalComidaAsync(fecha, tipo.Id, 850);

        await new ServicioAlimentos(dbAna).GuardarAsync(new Alimento
        {
            Id = huevo.Id, Nombre = huevo.Nombre, Porcion = huevo.Porcion,
            Kcal = 80, Proteina = huevo.Proteina, Carbos = huevo.Carbos, Grasa = huevo.Grasa, Activo = true
        });

        var vista = await dia.CargarAsync(fecha);
        Assert.Equal(930, Redondeo.Kcal(vista.Balance.Ingesta!.Kcal));
        Assert.Equal(850, Redondeo.Kcal(vista.Balance.Ingesta.KcalSinMacros));
    }

    [Fact]
    public void CA15_SugerenciaCon57PorCientoDeDesvioNoEsConfiable()
    {
        var r = ValidadorSugerencia.Validar(400, 20, 0, 10);
        Assert.Equal(NivelSugerencia.NoConfiable, r.Nivel);
        Assert.True(r.Desvio > 0.5);
    }

    [Fact]
    public void CA16_SugerenciaQueCierraSeOfreceSinAdvertencia()
    {
        var r = ValidadorSugerencia.Validar(290, 20, 30, 10);
        Assert.Equal(NivelSugerencia.Consistente, r.Nivel);
    }

    [Fact]
    public void CA37_ElProtocoloHiitDura19Minutos()
    {
        var bloques = new[]
        {
            new BloqueCardio { Minutos = 5, Repeticiones = 1 },
            new BloqueCardio { Minutos = 0.5, Repeticiones = 6 },
            new BloqueCardio { Minutos = 1, Repeticiones = 6 },
            new BloqueCardio { Minutos = 5, Repeticiones = 1 }
        };
        Assert.Equal(19, DuracionProtocolo.Minutos(bloques));
    }

    [Fact]
    public void CA36_ElPuntoMedioDelRangoEs190()
    {
        Assert.Equal(190, ServicioCardio.KcalEfectivas(null, 160, 220));
    }

    [Fact]
    public async Task MarcarFutbolSumaLasKcalAlGastoDelDia()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, _) = await baseDatos.CrearAnaYBeto();
        await using (var db = baseDatos.Como(null))
            await ServicioCuentas.CompletarFilasPorCuentaAsync(db);

        await using var dbAna = baseDatos.Como(ana);
        await new ServicioPlantillas(dbAna).ImportarAsync(PlantillaExcel.Id,
            [PartePlantilla.Actividades, PartePlantilla.Alimentos, PartePlantilla.TiposComida]);
        var dia = new ServicioDia(dbAna);
        var fecha = new DateOnly(2026, 9, 17);
        var futbol = await dbAna.Actividades.SingleAsync(a => a.Nombre == "Fútbol 5");
        var antes = await dia.CargarAsync(fecha);
        await dia.AlternarActividadAsync(fecha, futbol.Id);
        var despues = await dia.CargarAsync(fecha);

        Assert.Contains(futbol.Id, despues.ActividadesDelDia);
        Assert.Equal(futbol.Kcal, despues.Balance.Gasto.Actividades);
        Assert.Equal(550, futbol.Kcal);
        Assert.Equal(60, futbol.Minutos);
        Assert.Equal(
            Redondeo.Kcal(antes.Balance.Gasto.Total + futbol.Kcal),
            Redondeo.Kcal(despues.Balance.Gasto.Total));
    }

    [Fact]
    public async Task MediaPorcionDeDeporteDejaLaMitadDeKcal()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, _) = await baseDatos.CrearAnaYBeto();
        await using (var db = baseDatos.Como(null))
            await ServicioCuentas.CompletarFilasPorCuentaAsync(db);

        await using var dbAna = baseDatos.Como(ana);
        await new ServicioPlantillas(dbAna).ImportarAsync(PlantillaExcel.Id, [PartePlantilla.Actividades]);
        var dia = new ServicioDia(dbAna);
        var fecha = new DateOnly(2026, 9, 17);
        var futbol = await dbAna.Actividades.SingleAsync(a => a.Nombre == "Fútbol 5");
        await dia.AlternarActividadAsync(fecha, futbol.Id);
        await dia.CambiarCantidadActividadAsync(fecha, futbol.Id, 0.5);
        var vista = await dia.CargarAsync(fecha);
        Assert.Equal(275, Redondeo.Kcal(vista.Balance.Gasto.Actividades));
        Assert.Equal(0.5, vista.DeporteMarcas.Single().Cantidad);
    }

    [Fact]
    public async Task MediaDelComboRecortaTodasLasLineas()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, _) = await baseDatos.CrearAnaYBeto();
        await using (var db = baseDatos.Como(null))
            await ServicioCuentas.CompletarFilasPorCuentaAsync(db);

        await using var dbAna = baseDatos.Como(ana);
        await new ServicioPlantillas(dbAna).ImportarAsync(PlantillaExcel.Id,
            [PartePlantilla.Alimentos, PartePlantilla.Combos, PartePlantilla.TiposComida]);
        var dia = new ServicioDia(dbAna);
        var combo = await dbAna.Combos.SingleAsync(c => c.Nombre == "Desayuno completo");
        var fecha = new DateOnly(2026, 9, 16);
        await dia.AgregarComboAsync(fecha, combo.Id);
        var items = await dbAna.ComidaItems.ToListAsync();
        Assert.True(items.Count > 1);
        Assert.All(items, i => Assert.False(string.IsNullOrEmpty(i.GrupoId)));
        Assert.Single(items.Select(i => i.GrupoId).Distinct());

        Assert.Null(await dia.CambiarCantidadGrupoAsync(items[0].GrupoId!, 0.5));
        var vista = await dia.CargarAsync(fecha);
        Assert.Equal(181, Redondeo.Kcal(vista.Balance.Ingesta!.Kcal));
        Assert.All(await dbAna.ComidaItems.ToListAsync(), i =>
        {
            Assert.Equal(0.5, i.GrupoFactor);
        });
    }

    [Fact]
    public async Task UnIngredienteDelComboSePuedeRecortarSolo()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, _) = await baseDatos.CrearAnaYBeto();
        await using (var db = baseDatos.Como(null))
            await ServicioCuentas.CompletarFilasPorCuentaAsync(db);

        await using var dbAna = baseDatos.Como(ana);
        await new ServicioPlantillas(dbAna).ImportarAsync(PlantillaExcel.Id,
            [PartePlantilla.Alimentos, PartePlantilla.Combos, PartePlantilla.TiposComida]);
        var dia = new ServicioDia(dbAna);
        var combo = await dbAna.Combos.SingleAsync(c => c.Nombre == "Desayuno completo");
        var fecha = new DateOnly(2026, 9, 16);
        await dia.AgregarComboAsync(fecha, combo.Id);
        var entero = (await dia.CargarAsync(fecha)).Balance.Ingesta!.Kcal;
        var uno = await dbAna.ComidaItems.OrderByDescending(i => i.Kcal * i.Cantidad).FirstAsync();
        var kcalLinea = uno.Kcal * uno.Cantidad;
        Assert.Null(await dia.CambiarCantidadItemAsync(uno.Id, 0.5));
        var despues = await dia.CargarAsync(fecha);
        Assert.Equal(Redondeo.Kcal(entero - kcalLinea * 0.5), Redondeo.Kcal(despues.Balance.Ingesta!.Kcal));
        var recortado = await dbAna.ComidaItems.SingleAsync(i => i.Id == uno.Id);
        Assert.Equal(0.5, FraccionHecha.FactorDeItem(recortado.Cantidad, recortado.CantidadBase, recortado.GrupoFactor));
        Assert.All(await dbAna.ComidaItems.ToListAsync(), i => Assert.Equal(1, i.GrupoFactor));
    }

    [Fact]
    public void ElPorcentaje50EsLaMitad()
    {
        Assert.Equal(0.5, FraccionHecha.De(0, 50));
        Assert.Equal(1, FraccionHecha.De(1, 0));
        Assert.True(FraccionHecha.EsMitad(0.5));
        Assert.True(FraccionHecha.EsCompleta(1));
        Assert.Null(FraccionHecha.De(0, 0));
        Assert.Null(FraccionHecha.De(0, 5));
    }

    [Fact]
    public async Task MarcarCintaEstimaKcalConAcsmYSumaAlGasto()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, _) = await baseDatos.CrearAnaYBeto();
        await using (var db = baseDatos.Como(null))
            await ServicioCuentas.CompletarFilasPorCuentaAsync(db);

        await using var dbAna = baseDatos.Como(ana);
        await new ServicioPlantillas(dbAna).ImportarAsync(PlantillaExcel.Id,
            [PartePlantilla.Actividades, PartePlantilla.Mediciones]);
        var dia = new ServicioDia(dbAna);
        var fecha = new DateOnly(2026, 9, 17);
        var cinta = await dbAna.ProtocolosCardio.SingleAsync(p => p.Nombre.Contains("10 %"));
        var antes = await dia.CargarAsync(fecha);
        await dia.AlternarProtocoloAsync(fecha, cinta.Id);
        var despues = await dia.CargarAsync(fecha);

        Assert.Contains(cinta.Id, despues.CardioDelDia);
        Assert.InRange(despues.Balance.Gasto.Cardio, 280, 310);
        Assert.Equal(
            Redondeo.Kcal(antes.Balance.Gasto.Total + despues.Balance.Gasto.Cardio),
            Redondeo.Kcal(despues.Balance.Gasto.Total));
    }

    [Fact]
    public async Task KcalAManoEntranSinCrearCatalogo()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, _) = await baseDatos.CrearAnaYBeto();
        await using (var db = baseDatos.Como(null))
            await ServicioCuentas.CompletarFilasPorCuentaAsync(db);

        await using var dbAna = baseDatos.Como(ana);
        await new ServicioPlantillas(dbAna).ImportarAsync(PlantillaExcel.Id, [PartePlantilla.TiposComida]);
        var dia = new ServicioDia(dbAna);
        var fecha = new DateOnly(2026, 9, 17);
        var tipo = await dbAna.TiposComida.OrderBy(t => t.Orden).FirstAsync();

        Assert.Null(await dia.AgregarKcalComidaAsync(fecha, tipo.Id, 850));
        Assert.Null(await dia.AgregarKcalGymAsync(fecha, 320));
        Assert.Null(await dia.AgregarKcalCardioAsync(fecha, 180));
        var vista = await dia.CargarAsync(fecha);

        var item = Assert.Single(vista.Comidas.SelectMany(g => g.Comida.Items));
        Assert.True(item.EsAfuera);
        Assert.Null(item.AlimentoId);
        Assert.Equal("De afuera", item.Nombre);
        Assert.Equal(850, Redondeo.Kcal(item.Kcal));
        Assert.Equal(850, Redondeo.Kcal(vista.Balance.Ingesta!.KcalSinMacros));
        Assert.Empty(await dbAna.Alimentos.ToListAsync());

        var gym = Assert.Single(vista.GymSueltos);
        Assert.Equal("Gym", gym.Nombre);
        Assert.Equal(320, Redondeo.Kcal(gym.Kcal));
        Assert.Equal(320, Redondeo.Kcal(vista.Balance.Gasto.Gym));
        Assert.Empty(vista.SesionesGymDelDia);

        var cardio = Assert.Single(vista.CardioSueltos);
        Assert.Equal("Cardio", cardio.Nombre);
        Assert.Equal(180, Redondeo.Kcal(cardio.Kcal));
        Assert.Equal(180, Redondeo.Kcal(vista.Balance.Gasto.Cardio));
        Assert.Empty(vista.CardioDelDia);

        Assert.Null(await dia.AgregarKcalComidaAsync(fecha, tipo.Id, 430, "Empanada de la esquina"));
        Assert.Null(await dia.AgregarKcalGymAsync(fecha, 200, "Press banca extra"));
        var conNombre = await dia.CargarAsync(fecha);
        Assert.Contains(conNombre.Comidas.SelectMany(g => g.Comida.Items), i => i.Nombre == "Empanada de la esquina");
        Assert.Contains(conNombre.GymSueltos, s => s.Nombre == "Press banca extra");
        Assert.Empty(await dbAna.Alimentos.ToListAsync());
        Assert.Empty(await dbAna.Ejercicios.ToListAsync());

        await dia.QuitarGymSueltoAsync(gym.Id);
        await dia.QuitarCardioSueltoAsync(cardio.Id);
        var limpio = await dia.CargarAsync(fecha);
        Assert.Single(limpio.GymSueltos);
        Assert.Equal("Press banca extra", limpio.GymSueltos[0].Nombre);
        Assert.Empty(limpio.CardioSueltos);
    }

    [Fact]
    public async Task SumarUnExtraNoCargaElAlimentoDelDesplegable()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, _) = await baseDatos.CrearAnaYBeto();
        await using (var db = baseDatos.Como(null))
            await ServicioCuentas.CompletarFilasPorCuentaAsync(db);

        await using var dbAna = baseDatos.Como(ana);
        await new ServicioPlantillas(dbAna).ImportarAsync(PlantillaExcel.Id,
            [PartePlantilla.Alimentos, PartePlantilla.TiposComida]);
        var dia = new ServicioDia(dbAna);
        var fecha = new DateOnly(2026, 9, 17);
        var extra = await dbAna.TiposComida.SingleAsync(t => t.Nombre == "Extra");
        var aceite = await dbAna.Alimentos.FirstAsync(a => a.Nombre == "Aceite");

        Assert.Null(await dia.AgregarSlotAsync(fecha, extra.Id, $"a:{aceite.Id}", 90, "Alfajor"));

        var vista = await dia.CargarAsync(fecha);
        var item = Assert.Single(vista.Comidas.SelectMany(g => g.Comida.Items));
        Assert.Equal("Alfajor", item.Nombre);
        Assert.Equal(90, Redondeo.Kcal(item.Kcal));
        Assert.Null(item.AlimentoId);
    }

    [Fact]
    public async Task BorrarEnElDiaCopiadoNoTocaElOriginal()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, _) = await baseDatos.CrearAnaYBeto();
        await using (var db = baseDatos.Como(null))
            await ServicioCuentas.CompletarFilasPorCuentaAsync(db);

        await using var dbAna = baseDatos.Como(ana);
        await new ServicioPlantillas(dbAna).ImportarAsync(PlantillaExcel.Id,
            [PartePlantilla.Alimentos, PartePlantilla.Combos, PartePlantilla.TiposComida]);
        var dia = new ServicioDia(dbAna);
        var origen = new DateOnly(2026, 9, 9);
        var copia = new DateOnly(2026, 9, 10);
        var almuerzo = await dbAna.TiposComida.SingleAsync(t => t.Nombre == "Almuerzo");

        Assert.Null(await dia.AgregarKcalComidaAsync(origen, almuerzo.Id, 350, "Almuerzo"));
        Assert.Null(await dia.AgregarKcalComidaAsync(origen, almuerzo.Id, 50, "Extra"));
        Assert.Null(await dia.AgregarKcalGymAsync(origen, 220, "Press"));

        var combo = await dbAna.Combos.SingleAsync(c => c.Nombre == "Desayuno completo");
        Assert.Null(await dia.AgregarComboAsync(origen, combo.Id));

        Assert.Null(await dia.CopiarDiaAsync(origen, copia));

        var dest = await dia.CargarAsync(copia);
        var extraDest = dest.Comidas.SelectMany(g => g.Comida.Items).Single(i => i.Nombre == "Extra");
        await dia.QuitarItemAsync(extraDest.Id, copia);
        var gymDest = Assert.Single(dest.GymSueltos);
        await dia.QuitarGymSueltoAsync(gymDest.Id, copia);
        var grupoDest = dest.Comidas.SelectMany(g => g.Comida.Items)
            .First(i => i.GrupoId is { Length: > 0 }).GrupoId!;
        await dia.QuitarGrupoAsync(grupoDest, copia);

        var orig = await dia.CargarAsync(origen);
        var itemsOrig = orig.Comidas.SelectMany(g => g.Comida.Items).ToList();
        Assert.Contains(itemsOrig, i => i.Nombre == "Almuerzo" && Redondeo.Kcal(i.Kcal) == 350);
        Assert.Contains(itemsOrig, i => i.Nombre == "Extra" && Redondeo.Kcal(i.Kcal) == 50);
        Assert.Contains(itemsOrig, i => i.GrupoId is { Length: > 0 });
        Assert.Equal(220, Redondeo.Kcal(Assert.Single(orig.GymSueltos).Kcal));

        var destLimpio = await dia.CargarAsync(copia);
        Assert.DoesNotContain(destLimpio.Comidas.SelectMany(g => g.Comida.Items), i => i.Nombre == "Extra");
        Assert.Contains(destLimpio.Comidas.SelectMany(g => g.Comida.Items), i => i.Nombre == "Almuerzo");
        Assert.Empty(destLimpio.GymSueltos);
        Assert.DoesNotContain(destLimpio.Comidas.SelectMany(g => g.Comida.Items), i => i.GrupoId is { Length: > 0 });
    }

    [Fact]
    public async Task SePuedeCargarYCopiarUnDiaFuturo()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, _) = await baseDatos.CrearAnaYBeto();
        await using (var db = baseDatos.Como(null))
            await ServicioCuentas.CompletarFilasPorCuentaAsync(db);

        await using var dbAna = baseDatos.Como(ana);
        await new ServicioPlantillas(dbAna).ImportarAsync(PlantillaExcel.Id,
            [PartePlantilla.Alimentos, PartePlantilla.Combos, PartePlantilla.TiposComida]);
        var dia = new ServicioDia(dbAna);
        var maniana = DateOnly.FromDateTime(DateTime.Today).AddDays(1);
        var combo = await dbAna.Combos.SingleAsync(c => c.Nombre == "Desayuno completo");

        Assert.Null(await dia.AgregarComboAsync(maniana, combo.Id));
        Assert.Null(await dia.AgregarKcalGymAsync(maniana, 310, "Día 2"));
        var vista = await dia.CargarAsync(maniana);
        Assert.True(vista.FechaFutura);
        Assert.Equal(361, Redondeo.Kcal(vista.Balance.Ingesta!.Kcal));
        Assert.Equal(310, Redondeo.Kcal(Assert.Single(vista.GymSueltos).Kcal));

        var pasadoManiana = maniana.AddDays(1);
        Assert.Null(await dia.CopiarDiaAsync(maniana, pasadoManiana));
        var copia = await dia.CargarAsync(pasadoManiana);
        Assert.Equal(361, Redondeo.Kcal(copia.Balance.Ingesta!.Kcal));
        Assert.Equal(310, Redondeo.Kcal(Assert.Single(copia.GymSueltos).Kcal));
    }
}
