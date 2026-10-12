using KcalTracker.Calculo;
using KcalTracker.Dominio;

namespace KcalTracker.Tests;

/// <summary>
/// Caso de referencia de los requerimientos: medición 2026-09-15, 72.2 kg, 17.2 % de grasa.
/// </summary>
public class CalculoTests
{
    private const double Peso = 72.2;
    private const double GrasaPct = 17.2;
    private const double Factor = 1.2;

    [Fact]
    public void CA01_KatchMcArdleConLaMedicionDeReferencia()
    {
        var bmr = Metabolismo.KatchMcArdle(Peso, GrasaPct);
        var magra = Peso * (1 - GrasaPct / 100);

        Assert.Equal(59.78, Redondeo.Kg(magra));
        Assert.Equal(1661, Redondeo.Kcal(bmr));
    }

    [Fact]
    public void CA02_GastoSinDeporteUsaElFactorYNo2250()
    {
        var bmr = Metabolismo.Calcular(MetodoBmr.KatchMcArdle, null, Peso, GrasaPct, null, null, null);
        var gasto = Metabolismo.GastoSinDeporte(bmr, Factor);

        Assert.Equal(1994, Redondeo.Kcal(gasto!.Value));
        Assert.NotEqual(2250, Redondeo.Kcal(gasto.Value));
    }

    [Fact]
    public void CA03_EstaCubiertoEnBalanceDiario()
    {
        var gasto = BalanceDiario.CalcularGasto(
            Metabolismo.GastoSinDeporte(
                Metabolismo.Calcular(MetodoBmr.KatchMcArdle, null, Peso, GrasaPct, null, null, null),
                Factor)!.Value,
            kcalGym: [330]);

        Assert.Equal(2324, Redondeo.Kcal(gasto.Total));
    }

    [Fact]
    public void CA04_PlanCon15PorCientoUsaLaConstanteDeLaConfiguracion()
    {
        var bmr = Metabolismo.Calcular(MetodoBmr.KatchMcArdle, null, Peso, GrasaPct, null, null, null);
        var gasto = Metabolismo.GastoSinDeporte(bmr, Factor)!.Value;
        var plan = PlanObjetivo.Calcular(
            Peso, GrasaPct, 15, 8, ValoresDeFabrica.KcalPorKgGrasa, gasto,
            ValoresDeFabrica.PctGastoMax, ValoresDeFabrica.PctPesoSemanal)!;

        Assert.Equal(70.3, Redondeo.Kg(plan.PesoObjetivoKg, 1));
        Assert.Equal(1.87, Redondeo.Kg(plan.GrasaAPerderKg));
        Assert.Equal(Redondeo.Kcal(plan.GrasaAPerderKg * ValoresDeFabrica.KcalPorKgGrasa), Redondeo.Kcal(plan.KcalTotales));
        Assert.Equal(ValoresDeFabrica.KcalPorKgGrasa, plan.KcalPorKgGrasaAplicadas);
    }

    [Fact]
    public void CA05_OchoSemanasEsSostenibleConTecho498()
    {
        var gasto = Metabolismo.GastoSinDeporte(
            Metabolismo.Calcular(MetodoBmr.KatchMcArdle, null, Peso, GrasaPct, null, null, null),
            Factor)!.Value;
        var plan = PlanObjetivo.Calcular(
            Peso, GrasaPct, 15, 8, ValoresDeFabrica.KcalPorKgGrasa, gasto,
            ValoresDeFabrica.PctGastoMax, ValoresDeFabrica.PctPesoSemanal)!;

        Assert.Equal(Redondeo.Kcal(plan.KcalTotales / (8 * 7)), Redondeo.Kcal(plan.DeficitDiario));
        Assert.Equal(498, Redondeo.Kcal(plan.TechoDiario));
        Assert.True(plan.EsSostenible);
    }

    [Fact]
    public void CA06_CuatroSemanasSuperaElTecho()
    {
        var gasto = Metabolismo.GastoSinDeporte(
            Metabolismo.Calcular(MetodoBmr.KatchMcArdle, null, Peso, GrasaPct, null, null, null),
            Factor)!.Value;
        var plan = PlanObjetivo.Calcular(
            Peso, GrasaPct, 15, 4, ValoresDeFabrica.KcalPorKgGrasa, gasto,
            ValoresDeFabrica.PctGastoMax, ValoresDeFabrica.PctPesoSemanal)!;

        Assert.True(Redondeo.Kcal(plan.DeficitDiario) > Redondeo.Kcal(plan.TechoDiario));
        Assert.False(plan.EsSostenible);
    }

    [Fact]
    public void CA28_ElPlanUsa8000SiElUsuarioLoConfiguro()
    {
        var gasto = Metabolismo.GastoSinDeporte(
            Metabolismo.Calcular(MetodoBmr.KatchMcArdle, null, Peso, GrasaPct, null, null, null),
            Factor)!.Value;
        var plan7700 = PlanObjetivo.Calcular(
            Peso, GrasaPct, 15, 8, 7700, gasto, 0.25, 0.0075)!;
        var plan8000 = PlanObjetivo.Calcular(
            Peso, GrasaPct, 15, 8, 8000, gasto, 0.25, 0.0075)!;

        Assert.Equal(8000, plan8000.KcalPorKgGrasaAplicadas);
        Assert.True(plan8000.KcalTotales > plan7700.KcalTotales);
        Assert.Equal(Redondeo.Kcal(plan8000.GrasaAPerderKg * 8000), Redondeo.Kcal(plan8000.KcalTotales));
        Assert.NotEqual(Redondeo.Kcal(plan7700.KcalTotales), Redondeo.Kcal(plan8000.KcalTotales));
    }

    [Fact]
    public void OmronPor1_2DaGetCotidianoDe2070()
    {
        var bmr = Metabolismo.Calcular(MetodoBmr.Medido, 1724, 74.1, 18.8, 172, 25, Sexo.Masculino);
        var gasto = Metabolismo.GastoSinDeporte(bmr, 1.2);

        Assert.Equal(MetodoBmr.Medido, bmr.Aplicado);
        Assert.Equal(1724, bmr.Kcal);
        Assert.Equal(2069, Redondeo.Kcal(gasto!.Value));
        Assert.InRange(Redondeo.Kcal(gasto.Value), 2065, 2075);
    }

    [Fact]
    public void OnceSemanasConLaMedicionDeReferenciaDan352KcalAlDia()
    {
        var once = PlanObjetivo.Calcular(
            74.1, 18.8, 15, 11, ValoresDeFabrica.KcalPorKgGrasa, 2069, 0.25, 0.008)!;
        var ocho = PlanObjetivo.Calcular(
            74.1, 18.8, 15, 8, ValoresDeFabrica.KcalPorKgGrasa, 2069, 0.25, 0.008)!;

        Assert.Equal(352, Redondeo.Kcal(once.DeficitDiario));
        Assert.Equal(484, Redondeo.Kcal(ocho.DeficitDiario));
        Assert.Equal(Redondeo.Kcal(once.KcalTotales / 56), Redondeo.Kcal(ocho.DeficitDiario));
    }

    [Fact]
    public void ElDeficitDiarioSeFijaAlDiaCeroAunquePaseElTiempo()
    {
        var inicio = new DateOnly(2026, 8, 16);
        var hoy = new DateOnly(2026, 9, 20);
        var dia0 = PlanObjetivo.Calcular(
            74.1, 18.8, 16, 10, ValoresDeFabrica.KcalPorKgGrasa, 2069, 0.25, 0.008)!;
        var luego = PlanObjetivo.Calcular(
            74.1, 18.8, 16, 10, ValoresDeFabrica.KcalPorKgGrasa, 2069, 0.25, 0.008,
            inicio, hoy)!;

        Assert.Equal(Redondeo.Kcal(dia0.DeficitDiario), Redondeo.Kcal(luego.DeficitDiario));
        Assert.Equal(Redondeo.Kcal(luego.KcalTotales / 70), Redondeo.Kcal(luego.DeficitDiario));
        Assert.Equal(5, luego.Plazo!.SemanasTranscurridas, 1);
        Assert.True(luego.Plazo.SemanasRestantes < 10);

        var recomp = DeficitObjetivo.De(luego, 2069, 18.8, TipoObjetivo.Recomposicion, null);
        var corte = DeficitObjetivo.De(luego, 2069, 18.8, TipoObjetivo.Corte, null);
        var alcanceRecomp = AlcanceObjetivo.De(luego, recomp, 18.8, 16, TipoObjetivo.Recomposicion)!;
        var alcanceCorte = AlcanceObjetivo.De(luego, corte, 18.8, 16, TipoObjetivo.Corte)!;
        Assert.False(alcanceRecomp.Llega);
        Assert.Equal(TipoObjetivo.Corte, alcanceRecomp.TipoSugerido);
        Assert.True(alcanceCorte.Llega);
    }

    [Fact]
    public void ElPlazoQueCorreMarcaElRelojSinAcelerarElRecorte()
    {
        var inicio = new DateOnly(2026, 8, 16);
        var hoy = new DateOnly(2026, 9, 6); // 3 semanas
        var entero = PlanObjetivo.Calcular(
            74.1, 18.8, 15, 11, ValoresDeFabrica.KcalPorKgGrasa, 2069, 0.25, 0.008)!;
        var corrido = PlanObjetivo.Calcular(
            74.1, 18.8, 15, 11, ValoresDeFabrica.KcalPorKgGrasa, 2069, 0.25, 0.008, inicio, hoy)!;

        Assert.Equal(352, Redondeo.Kcal(entero.DeficitDiario));
        Assert.Equal(Redondeo.Kcal(entero.DeficitDiario), Redondeo.Kcal(corrido.DeficitDiario));
        Assert.Equal(3, corrido.Plazo!.SemanasTranscurridas, 1);
        Assert.True(corrido.Plazo.SemanasRestantes < 11);

        var ritmo = RitmoObjetivo.De(
            corrido.Plazo,
            corrido,
            new ResultadoCalibracion(
                inicio, hoy, 21, 18, 0.85, 0.4, 0.1, 3000, 2200, 1900, null));
        Assert.True(ritmo.VaMasLento);
        Assert.True(ritmo.PlazoSugerido > 11);
        Assert.Contains("Van 3", ritmo.Texto);
    }

    [Fact]
    public void UnPlazoQueDesdeElDiaCeroSuperaElTechoProponeAlargar()
    {
        var inicio = new DateOnly(2026, 8, 16);
        var hoy = new DateOnly(2026, 9, 18);
        var plan = PlanObjetivo.Calcular(
            74.1, 18.8, 15, 4, ValoresDeFabrica.KcalPorKgGrasa, 2069, 0.25, 0.008, inicio, hoy)!;

        Assert.False(plan.EsSostenible);
        var ritmo = RitmoObjetivo.De(plan.Plazo!, plan, null);
        Assert.True(ritmo.VaMasLento);
        Assert.True(ritmo.PlazoSugerido > 4);
        Assert.Contains("Alargar", ritmo.Texto, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RecompBajoElTechoNoMarcaElPlazoComoInsostenible()
    {
        var inicio = new DateOnly(2026, 9, 7);
        var hoy = new DateOnly(2026, 9, 18);
        var plan = PlanObjetivo.Calcular(
            74.1, 18.8, 15, 4, ValoresDeFabrica.KcalPorKgGrasa, 2069, 0.25, 0.008, inicio, hoy)!;

        Assert.False(plan.EsSostenible);
        var recomp = DeficitObjetivo.De(plan, 2069, 18.8, TipoObjetivo.Recomposicion, null);
        Assert.True(recomp.Diario <= plan.TechoDiario + 0.5);

        var ritmo = RitmoObjetivo.De(plan.Plazo!, plan, null, recomp);
        Assert.False(ritmo.VaMasLento);
        Assert.Null(ritmo.PlazoSugerido);
        Assert.Contains("por debajo del techo", ritmo.Texto);
    }

    [Fact]
    public void ProgresoResumeElDiaYLaSemanaContraElObjetivo()
    {
        var lun = new DateOnly(2026, 9, 14);
        var mar = lun.AddDays(1);
        var mie = lun.AddDays(2);
        DiaDePeriodo Dia(DateOnly f, bool hay, double ingesta, double gasto) => new(
            f, hay, hay ? ingesta : null, null, null, null, 0, gasto,
            hay ? gasto - ingesta : null, 0, 0, 0);

        var faltaDia = ProgresoObjetivo.De(
            [Dia(lun, true, 1800, 2100), Dia(mar, false, 0, 2100), Dia(mie, true, 1900, 2100)],
            mie, 380, false, "la semana");
        Assert.Equal("warn", faltaDia.ClaseDia);
        Assert.Contains("Te faltan", faltaDia.TextoDia);
        Assert.Contains("Te faltan", faltaDia.TextoPeriodo);
        Assert.Contains("1 día sin comida", faltaDia.TextoPeriodo);

        var completo = ProgresoObjetivo.De(
            [Dia(mie, true, 1600, 2100)],
            mie, 380, false, "la semana");
        Assert.Equal("ok", completo.ClaseDia);
        Assert.Contains("completaste el déficit del día", completo.TextoDia);
        Assert.Equal("ok", completo.ClasePeriodo);
        Assert.Contains("Completaste el déficit de la semana", completo.TextoPeriodo);
    }

    [Fact]
    public void SeguimientoComparaLoHechoConLoEsperadoDelPlazo()
    {
        var inicio = new DateOnly(2026, 8, 16);
        var hoy = new DateOnly(2026, 9, 19);
        var plazo = PlazoCorrido.De(15, inicio, hoy);
        DiaDePeriodo Dia(DateOnly f, bool hay, double def) => new(
            f, hay, hay ? 1800 : null, null, null, null, 0, 1800 + def,
            hay ? def : null, 0, 0, 0);

        var dias = new List<DiaDePeriodo>();
        for (var f = inicio; f <= hoy; f = f.AddDays(1))
            dias.Add(Dia(f, f.Day % 2 == 0, 500));

        var atras = SeguimientoObjetivo.De(plazo, dias, 600, false)!;
        Assert.Equal("warn", atras.Clase);
        Assert.Contains("abajo", atras.Texto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("no es posible", atras.Texto, StringComparison.OrdinalIgnoreCase);

        var adelante = SeguimientoObjetivo.De(plazo, dias, 100, false)!;
        Assert.Equal("ok", adelante.Clase);
        Assert.Contains("por encima", adelante.Texto, StringComparison.OrdinalIgnoreCase);

        var sinFecha = SeguimientoObjetivo.De(PlazoCorrido.De(15, null, hoy), dias, 400, false)!;
        Assert.Contains("arranque", sinFecha.Texto, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SeguimientoComparaContraLosDiasCargadosNoContraElCalendarioVacio()
    {
        var inicio = new DateOnly(2026, 8, 16);
        var hoy = new DateOnly(2026, 9, 20);
        var plan = PlanObjetivo.Calcular(
            74.1, 18.8, 16, 10, ValoresDeFabrica.KcalPorKgGrasa, 2069, 0.25, 0.008,
            inicio, hoy)!;
        var original = plan.KcalTotales / (10 * 7.0);
        Assert.Equal(Redondeo.Kcal(original), Redondeo.Kcal(plan.DeficitDiario));

        DiaDePeriodo Dia(DateOnly f, double def) => new(
            f, true, 1800, null, null, null, 0, 1800 + def, def, 0, 0, 0);
        var dias = new[] { Dia(inicio, 400), Dia(inicio.AddDays(2), 400) };

        var bien = SeguimientoObjetivo.De(plan.Plazo, dias, original, false)!;
        var esperadoCargados = Redondeo.Kcal(original) * 2;
        Assert.Equal("ok", bien.Clase);
        Assert.Contains(Formato.Kcal(esperadoCargados).Replace(" ", ""), bien.Texto.Replace(" ", ""), StringComparison.Ordinal);
        Assert.Contains("2 de", bien.Texto, StringComparison.Ordinal);
        Assert.DoesNotContain("10.100", bien.Texto.Replace(" ", ""));
        Assert.DoesNotContain("20.200", bien.Texto.Replace(" ", ""));
        Assert.DoesNotContain("no es posible", bien.Texto, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SeguimientoConHuecosNoPideElDeficitDeLosDiasSinCarga()
    {
        var inicio = new DateOnly(2026, 8, 16);
        var hoy = new DateOnly(2026, 9, 20);
        var plazo = PlazoCorrido.De(10, inicio, hoy);
        DiaDePeriodo Dia(DateOnly f, double def) => new(
            f, true, 1800, null, null, null, 0, 1800 + def, def, 0, 0, 0);

        var dias = Enumerable.Range(0, 13).Select(i => Dia(inicio.AddDays(i), 516))
            .Append(Dia(inicio.AddDays(13), 520)).ToList();
        var lectura = SeguimientoObjetivo.De(plazo, dias, 289, false)!;

        Assert.Equal("ok", lectura.Clase);
        Assert.Contains("4.046", lectura.Texto.Replace(" ", ""), StringComparison.Ordinal);
        Assert.Contains("7.228", lectura.Texto.Replace(" ", ""), StringComparison.Ordinal);
        Assert.Contains("por encima", lectura.Texto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("10.100", lectura.Texto.Replace(" ", ""));
        Assert.DoesNotContain("no es posible", lectura.Texto, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RecomposicionEsMasChicaQueElCorteDelPlazo()
    {
        var plan = PlanObjetivo.Calcular(
            74.1, 18.8, 15, 8, ValoresDeFabrica.KcalPorKgGrasa, 2069, 0.25, 0.008)!;
        var segun = DeficitObjetivo.De(plan, 2069, 18.8, TipoObjetivo.Corte, null);
        var recomp = DeficitObjetivo.De(plan, 2069, 18.8, TipoObjetivo.Recomposicion, null);
        var manual = DeficitObjetivo.De(plan, 2069, 18.8, TipoObjetivo.Corte, 220);
        var ganancia = DeficitObjetivo.De(plan, 2069, 18.8, TipoObjetivo.GananciaMusculo, null);

        Assert.Equal(Redondeo.Kcal(plan.DeficitDiario), Redondeo.Kcal(segun.Diario));
        Assert.False(segun.EsSuperavit);
        Assert.True(recomp.Diario < segun.Diario);
        Assert.Equal(220, Redondeo.Kcal(manual.Diario));
        Assert.True(manual.EsManual);
        Assert.True(recomp.SemanasAEsteRitmo > 8);
        Assert.True(ganancia.EsSuperavit);
        Assert.Equal("Ganancia de músculo", ganancia.Nombre);
        Assert.True(ganancia.Diario < 180);
    }

    [Fact]
    public void AlcanceAvisaSiElPlazoElPorcentajeOElTipoNoCierran()
    {
        var plan8 = PlanObjetivo.Calcular(
            74.1, 18.8, 15, 8, ValoresDeFabrica.KcalPorKgGrasa, 2069, 0.25, 0.008)!;
        var corte8 = DeficitObjetivo.De(plan8, 2069, 18.8, TipoObjetivo.Corte, null);
        var recomp8 = DeficitObjetivo.De(plan8, 2069, 18.8, TipoObjetivo.Recomposicion, null);
        var gana8 = DeficitObjetivo.De(plan8, 2069, 18.8, TipoObjetivo.GananciaMusculo, null);

        var okCorte = AlcanceObjetivo.De(plan8, corte8, 18.8, 15, TipoObjetivo.Corte)!;
        Assert.True(okCorte.Llega);
        Assert.Equal("ok", okCorte.Clase);

        var noRecomp = AlcanceObjetivo.De(plan8, recomp8, 18.8, 15, TipoObjetivo.Recomposicion)!;
        Assert.False(noRecomp.Llega);
        Assert.Equal("warn", noRecomp.Clase);
        Assert.Equal(TipoObjetivo.Corte, noRecomp.TipoSugerido);
        Assert.Contains("no es posible", noRecomp.Texto, StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(noRecomp.Cuenta));
        Assert.Contains("kg de grasa", noRecomp.Cuenta, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("kcal/kg", noRecomp.Cuenta, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("kcal/día", noRecomp.Cuenta, StringComparison.OrdinalIgnoreCase);

        var noGana = AlcanceObjetivo.De(plan8, gana8, 18.8, 15, TipoObjetivo.GananciaMusculo)!;
        Assert.False(noGana.Llega);
        Assert.Equal("bad", noGana.Clase);
        Assert.Contains("no baja", noGana.Texto, StringComparison.OrdinalIgnoreCase);

        var plan4 = PlanObjetivo.Calcular(
            74.1, 18.8, 15, 4, ValoresDeFabrica.KcalPorKgGrasa, 2069, 0.25, 0.008)!;
        var corte4 = DeficitObjetivo.De(plan4, 2069, 18.8, TipoObjetivo.Corte, null);
        var noPlazo = AlcanceObjetivo.De(plan4, corte4, 18.8, 15, TipoObjetivo.Corte)!;
        Assert.False(noPlazo.Llega);
        Assert.Equal("bad", noPlazo.Clase);
        Assert.True(noPlazo.PlazoSugerido > 4);
        Assert.Contains("plazo", noPlazo.Texto, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("kcal/día", noPlazo.Cuenta, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("techo", noRecomp.Texto, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("hace falta", noRecomp.Texto, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RecompCierraCuandoElObjetivoYaEntraEnUnRecorteChico()
    {
        var plan = PlanObjetivo.Calcular(
            74.1, 18.8, 17, 18, ValoresDeFabrica.KcalPorKgGrasa, 2069, 0.25, 0.008)!;
        var recomp = DeficitObjetivo.De(plan, 2069, 18.8, TipoObjetivo.Recomposicion, null);
        Assert.True(recomp.Diario + 0.5 >= recomp.Corte);
        var alcance = AlcanceObjetivo.De(plan, recomp, 18.8, 17, TipoObjetivo.Recomposicion)!;
        Assert.True(alcance.Llega);
        Assert.Equal("ok", alcance.Clase);

        var corrido = PlanObjetivo.Calcular(
            74.1, 18.8, 17, 18, ValoresDeFabrica.KcalPorKgGrasa, 2069, 0.25, 0.008,
            new DateOnly(2026, 8, 16), new DateOnly(2026, 9, 19))!;
        var recompCorrido = DeficitObjetivo.De(corrido, 2069, 18.8, TipoObjetivo.Recomposicion, null);
        var alcanceCorrido = AlcanceObjetivo.De(corrido, recompCorrido, 18.8, 17, TipoObjetivo.Recomposicion)!;
        Assert.True(alcanceCorrido.Llega);
    }

    [Fact]
    public void RecompEsLaMitadDelTechoDeCorteSegunLaGrasa()
    {
        var suave = PlanObjetivo.Calcular(
            74.1, 18.8, 17, 18, ValoresDeFabrica.KcalPorKgGrasa, 2069, 0.25, 0.008)!;
        var recompSuave = DeficitObjetivo.De(suave, 2069, 18.8, TipoObjetivo.Recomposicion, null);
        Assert.True(recompSuave.Diario + 0.5 >= recompSuave.Corte);

        var agresivo = PlanObjetivo.Calcular(
            74.1, 18.8, 15, 8, ValoresDeFabrica.KcalPorKgGrasa, 2069, 0.25, 0.008)!;
        var recompAgresivo = DeficitObjetivo.De(agresivo, 2069, 18.8, TipoObjetivo.Recomposicion, null);
        var cap = 2069 * ConstantesNutricion.PctRecomp(18.8);
        Assert.InRange(recompAgresivo.Diario, cap - 15, cap + 15);
        Assert.True(recompAgresivo.Diario < recompAgresivo.Corte - 50);
        var alcanceAgresivo = AlcanceObjetivo.De(agresivo, recompAgresivo, 18.8, 15, TipoObjetivo.Recomposicion)!;
        Assert.False(alcanceAgresivo.Llega);
    }

    [Fact]
    public void RecompYGananciaEscalanConElPlazoYLaGrasa()
    {
        ResultadoPlan Plan(int semanas, double grasaObj) => PlanObjetivo.Calcular(
            74.1, 18.8, grasaObj, semanas, ValoresDeFabrica.KcalPorKgGrasa, 2069, 0.25, 0.008)!;

        var recomp8 = DeficitObjetivo.De(Plan(8, 15), 2069, 18.8, TipoObjetivo.Recomposicion, null);
        var recomp11 = DeficitObjetivo.De(Plan(11, 15), 2069, 18.8, TipoObjetivo.Recomposicion, null);
        var recomp16 = DeficitObjetivo.De(Plan(16, 15), 2069, 18.8, TipoObjetivo.Recomposicion, null);
        Assert.True(recomp8.Corte > recomp11.Corte);
        Assert.True(recomp11.Corte > recomp16.Corte);
        var capRecomp = DeficitObjetivo.TopeRecompDe(2069, double.PositiveInfinity, recomp8.Corte, 18.8);
        Assert.Equal(capRecomp, Redondeo.Kcal(recomp8.Recomposicion));
        Assert.Equal(capRecomp, Redondeo.Kcal(recomp11.Recomposicion));
        Assert.Equal(352, Redondeo.Kcal(recomp11.Corte));
        Assert.True(Math.Abs(recomp8.Recomposicion - capRecomp) < 15);
        Assert.True(recomp16.Recomposicion <= recomp11.Recomposicion + 0.5);

        var recomp12pct = DeficitObjetivo.De(Plan(8, 12), 2069, 18.8, TipoObjetivo.Recomposicion, null);
        Assert.True(recomp12pct.Corte > recomp8.Corte);
        Assert.Equal(Redondeo.Kcal(recomp8.Recomposicion), Redondeo.Kcal(recomp12pct.Recomposicion));

        var gana8 = DeficitObjetivo.De(Plan(8, 15), 2069, 18.8, TipoObjetivo.GananciaMusculo, null);
        var gana16 = DeficitObjetivo.De(Plan(16, 15), 2069, 18.8, TipoObjetivo.GananciaMusculo, null);
        Assert.True(gana8.Ganancia < gana16.Ganancia);
    }

    [Fact]
    public void ProteinaDeGananciaUsaElPesoYNoLaMagra()
    {
        var corte = ProteinaNecesaria.Calcular(74.1, 18.8, TipoObjetivo.Corte)!;
        var gana = ProteinaNecesaria.Calcular(74.1, 18.8, TipoObjetivo.GananciaMusculo)!;

        Assert.Equal(180, corte.SugeridaG);
        Assert.True(corte.UsaMasaMagra);
        Assert.Equal(150, gana.SugeridaG);
        Assert.False(gana.UsaMasaMagra);
        Assert.True(gana.SugeridaG < corte.SugeridaG);
    }

    [Fact]
    public void CA29_ValorMedidoNoAplicaKatchMcArdle()
    {
        var bmr = Metabolismo.Calcular(MetodoBmr.Medido, 1720, Peso, GrasaPct, 178, 36, Sexo.Masculino);
        var gasto = Metabolismo.GastoSinDeporte(bmr, 1.2);

        Assert.Equal(MetodoBmr.Medido, bmr.Aplicado);
        Assert.Equal(1720, bmr.Kcal);
        Assert.Equal(2064, Redondeo.Kcal(gasto!.Value));
        Assert.NotEqual(1661, Redondeo.Kcal(bmr.Kcal!.Value));
    }

    [Fact]
    public void CA30_SinPorcentajeDeGrasaCaeAMifflinYLoInforma()
    {
        var bmr = Metabolismo.Calcular(
            MetodoBmr.KatchMcArdle, null, Peso, grasaPct: null, 178, 36, Sexo.Masculino);

        Assert.True(bmr.UsoRespaldo);
        Assert.Equal(MetodoBmr.Mifflin, bmr.Aplicado);
        Assert.Contains("Mifflin", bmr.Aviso);
        Assert.Equal(Redondeo.Kcal(Metabolismo.MifflinStJeor(Peso, 178, 36, Sexo.Masculino)), Redondeo.Kcal(bmr.Kcal!.Value));
    }

    [Fact]
    public void SiElCatalogoTieneKcalSeUsanEsas()
    {
        var proto = new ProtocoloCardio
        {
            Nombre = "Caminata",
            Kcal = 180,
            Bloques = [new BloqueCardio { Minutos = 40, Velocidad = 5, Inclinacion = 0 }]
        };
        Assert.Equal(180, EstimacionCardio.DeProtocolo(proto, Peso));
    }

    [Fact]
    public void Cinta30minAl10PorCientoElBrutoRonda290()
    {
        var bruto = EstimacionCardio.BrutoKcal(30, 5, 10, 74.1);
        Assert.InRange(Redondeo.Kcal(bruto), 285, 305);

        var neto = EstimacionCardio.NetoKcal(bruto, 30, 2250);
        Assert.True(neto < bruto);
    }

    [Fact]
    public void HiitDe19MinutosConTroteSumaMasQueCaminarPlano()
    {
        var bloques = new[]
        {
            new BloqueCardio { Minutos = 5, Velocidad = 5, Inclinacion = 5, Repeticiones = 1 },
            new BloqueCardio { Minutos = 0.5, Velocidad = 9, Inclinacion = 5, Repeticiones = 6 },
            new BloqueCardio { Minutos = 1, Velocidad = 4.8, Inclinacion = 5, Repeticiones = 6 },
            new BloqueCardio { Minutos = 5, Velocidad = 5, Inclinacion = 5, Repeticiones = 1 }
        };
        var bmr = Metabolismo.Calcular(MetodoBmr.KatchMcArdle, null, Peso, GrasaPct, null, null, null);
        var gastoCot = Metabolismo.GastoSinDeporte(bmr, ValoresDeFabrica.FactorActividad)!.Value;
        var kcal = EstimacionCardio.DeBloques(bloques, Peso, gastoCot);
        Assert.InRange(Redondeo.Kcal(kcal), 100, 160);
    }

    [Fact]
    public void ProteinaSugeridaConMedicionDeReferenciaEs180()
    {
        var r = ProteinaNecesaria.Calcular(Peso, GrasaPct)!;
        Assert.Equal(59.78, Redondeo.Kg(r.MasaMagraKg!.Value));
        Assert.Equal(180, r.SugeridaG);
        Assert.True(r.UsaMasaMagra);
        Assert.InRange(r.MinG, 135, 140);
        Assert.InRange(r.MaxG, 183, 187);
    }

    [Fact]
    public void ProteinaSinGrasaUsaElPesoTotal()
    {
        var r = ProteinaNecesaria.Calcular(74, null)!;
        Assert.False(r.UsaMasaMagra);
        Assert.Equal(165, r.SugeridaG);
        Assert.Equal(165, ProteinaNecesaria.Objetivo(null, 74, null));
        Assert.Equal(180, ProteinaNecesaria.Objetivo(180, 74, null));
    }

    [Fact]
    public void SinPorcentajeDeGrasaElPlanSaleDelGastoCotidiano()
    {
        var bmr = Metabolismo.Calcular(
            MetodoBmr.Mifflin, null, 80, grasaPct: null, 175, 30, Sexo.Masculino);
        var gasto = Metabolismo.GastoSinDeporte(bmr, 1.2)!.Value;
        var plan = PlanObjetivo.De(
            80, null, 15, 8, ValoresDeFabrica.KcalPorKgGrasa, gasto,
            ValoresDeFabrica.PctGastoMax, ValoresDeFabrica.PctPesoSemanal)!;

        Assert.False(plan.UsaComposicion);
        Assert.Equal(0, plan.GrasaAPerderKg);
        Assert.Equal(Redondeo.Kcal(gasto * ValoresDeFabrica.PctDeficitCorte), Redondeo.Kcal(plan.DeficitDiario));
        Assert.True(plan.DeficitDiario <= plan.TechoDiario + 0.5);

        var corte = DeficitObjetivo.De(plan, gasto, null, TipoObjetivo.Corte, null);
        var recomp = DeficitObjetivo.De(plan, gasto, null, TipoObjetivo.Recomposicion, null);
        var gana = DeficitObjetivo.De(plan, gasto, null, TipoObjetivo.GananciaMusculo, null);

        Assert.Equal(Redondeo.Kcal(plan.DeficitDiario), Redondeo.Kcal(corte.Diario));
        Assert.InRange(recomp.Diario, gasto * 0.09, gasto * 0.11);
        Assert.True(recomp.Diario < corte.Diario);
        Assert.True(gana.EsSuperavit);
        Assert.InRange(gana.Diario, gasto * 0.09, gasto * 0.11);
        Assert.Null(recomp.SemanasAEsteRitmo);

        var ritmo = RitmoObjetivo.De(plan.Plazo!, plan, null, recomp);
        Assert.False(ritmo.VaMasLento);
        Assert.DoesNotContain("Objetivo alcanzado", ritmo.Texto);
        Assert.Contains("por debajo del techo", ritmo.Texto);
    }

    [Fact]
    public void MifflinEsElBasalPorDefectoSinGrasaNiBascule()
    {
        Assert.Equal(MetodoBmr.Mifflin, Metabolismo.Elegir(null, null));
        Assert.Equal(MetodoBmr.KatchMcArdle, Metabolismo.Elegir(null, 18.8));
        Assert.Equal(MetodoBmr.Medido, Metabolismo.Elegir(1724, null));

        var bmr = Metabolismo.Calcular(
            MetodoBmr.Mifflin, null, 80, null, 175, 30, Sexo.Masculino);
        Assert.Equal(MetodoBmr.Mifflin, bmr.Aplicado);
        Assert.False(bmr.UsoRespaldo);
        Assert.Equal(Redondeo.Kcal(Metabolismo.MifflinStJeor(80, 175, 30, Sexo.Masculino)), Redondeo.Kcal(bmr.Kcal!.Value));
    }

    [Fact]
    public void ConPorcentajeDeGrasaElPlanSigueSaliendoDeLosKilosAPerder()
    {
        var plan = PlanObjetivo.De(
            74.1, 18.8, 15, 8, ValoresDeFabrica.KcalPorKgGrasa, 2069, 0.25, 0.008)!;
        var compuesto = PlanObjetivo.Calcular(
            74.1, 18.8, 15, 8, ValoresDeFabrica.KcalPorKgGrasa, 2069, 0.25, 0.008)!;

        Assert.True(plan.UsaComposicion);
        Assert.Equal(Redondeo.Kg(compuesto.GrasaAPerderKg), Redondeo.Kg(plan.GrasaAPerderKg));
        Assert.Equal(Redondeo.Kcal(compuesto.DeficitDiario), Redondeo.Kcal(plan.DeficitDiario));
    }

    [Fact]
    public void MacrosDejanLaProteinaFijaYRepartenElResto()
    {
        var macros = MacrosNecesarios.De(2069, 380, false, 180, 72.2, TipoObjetivo.Corte)!;

        Assert.Equal(1689, Redondeo.Kcal(macros.KcalAComer));
        Assert.Equal(180, macros.ProteinaG);
        Assert.Equal(60, macros.GrasaG);
        Assert.Equal(105, macros.CarbosG);
        Assert.False(macros.NoAlcanza);
        Assert.Contains("día sentado", macros.Texto);
    }

    [Fact]
    public void UnDiaConGymSumaCarbos()
    {
        var sentado = MacrosNecesarios.De(2069, 380, false, 180, 72.2, TipoObjetivo.Corte)!;
        var gym = MacrosNecesarios.De(2469, 380, false, 180, 72.2, TipoObjetivo.Corte, incluyeEntreno: true)!;

        Assert.True(gym.CarbosG > sentado.CarbosG);
        Assert.Equal(sentado.GrasaG, gym.GrasaG);
        Assert.Contains("gasto de este día", gym.Texto);
    }

    [Fact]
    public void UnRecorteFuerteDejaPocoMargenParaCarbos()
    {
        var macros = MacrosNecesarios.De(2069, 1178, false, 180, 74.1, TipoObjetivo.Corte)!;
        Assert.True(macros.AjustoGrasaAlPiso || macros.NoAlcanza);
        Assert.True(macros.CarbosG < macros.ProteinaG);
    }

    [Fact]
    public void EnGananciaLosCarbosSubenConElSuperavit()
    {
        var corte = MacrosNecesarios.De(2069, 380, false, 180, 74.1, TipoObjetivo.Corte)!;
        var gana = MacrosNecesarios.De(2069, 70, true, 150, 74.1, TipoObjetivo.GananciaMusculo)!;
        Assert.True(gana.KcalAComer > corte.KcalAComer);
        Assert.True(gana.CarbosG > corte.CarbosG);
    }

    [Fact]
    public void ConstantesFisiologicasSiguenLaDensidadDelTejido()
    {
        Assert.Equal(8178, ConstantesNutricion.KcalPorKgGrasaBase);
        Assert.Equal(1800, ConstantesNutricion.KcalPorKgMagroBase);
        Assert.Equal(ValoresDeFabrica.KcalPorKgGrasa, ConstantesNutricion.KcalPorKgGrasaBase);
        Assert.Equal(ValoresDeFabrica.KcalPorKgMagro, ConstantesNutricion.KcalPorKgMagroBase);

        var sinGrasa = ConstantesNutricion.Calcular(null);
        Assert.Equal(0.25, sinGrasa.PctGastoMax);
        Assert.Equal(0.0075, sinGrasa.PctPesoSemanal);
        Assert.Equal(0.50, sinGrasa.CobMinContraste);
        Assert.Equal(0.70, sinGrasa.CobMinCalibrado);

        var recorte = ConstantesNutricion.Calcular(GrasaPct);
        Assert.Equal(8178, recorte.KcalPorKgGrasa);
        Assert.Equal(1800, recorte.KcalPorKgMagro);
        Assert.InRange(recorte.PctGastoMax, 0.23, 0.25);
        Assert.Equal(0.0075, recorte.PctPesoSemanal);

        var magro = ConstantesNutricion.Calcular(10);
        var conGrasa = ConstantesNutricion.Calcular(30);
        Assert.True(magro.PctGastoMax < recorte.PctGastoMax);
        Assert.True(conGrasa.PctGastoMax > recorte.PctGastoMax);
        Assert.Equal(0.30, conGrasa.PctGastoMax);
        Assert.Equal(0.010, conGrasa.PctPesoSemanal);
        Assert.True(magro.PctPesoSemanal < recorte.PctPesoSemanal);
    }

    [Fact]
    public void TechosHelmsSubenConMasGrasaYElDeFabricaNoLosPisa()
    {
        Assert.Equal(0.10, ConstantesNutricion.PctRecomp(null));
        Assert.InRange(ConstantesNutricion.PctRecomp(10), 0.08, 0.10);
        Assert.Equal(0.15, ConstantesNutricion.PctRecomp(30));
        Assert.True(ConstantesNutricion.PctSuperavit(10) > ConstantesNutricion.PctSuperavit(25));

        var cfg = new Configuracion();
        var magro = PlanObjetivo.De(70, 10, 8, 12, ValoresDeFabrica.KcalPorKgGrasa, 2000, cfg)!;
        var alto = PlanObjetivo.De(70, 30, 20, 12, ValoresDeFabrica.KcalPorKgGrasa, 2000, cfg)!;
        Assert.True(magro.TechoDiario < alto.TechoDiario);
        Assert.Equal(
            Redondeo.Kcal(2000 * ConstantesNutricion.Calcular(10).PctGastoMax),
            Redondeo.Kcal(magro.TechoDiario));

        cfg.PctGastoMax = 0.20;
        var aMano = PlanObjetivo.De(70, 18.8, 15, 12, ValoresDeFabrica.KcalPorKgGrasa, 2000, cfg)!;
        Assert.Equal(0.20, aMano.PctGastoMaxAplicado);
        Assert.Equal(Redondeo.Kcal(2000 * 0.20), Redondeo.Kcal(aMano.TechoDiario));
        Assert.Contains("20 %", TextosObjetivo.TechoCorte(18.8, false, aMano.PctGastoMaxAplicado), StringComparison.Ordinal);
        Assert.Contains("Ajustes", TextosObjetivo.TechoCorte(18.8, false, aMano.PctGastoMaxAplicado), StringComparison.Ordinal);

        var recomp = DeficitObjetivo.De(aMano, 2000, 18.8, TipoObjetivo.Recomposicion, null);
        Assert.Equal(200, Redondeo.Kcal(recomp.TopeRecomp));
        Assert.Contains("10 %", TextosObjetivo.TechoRecomp(18.8, false, aMano.PctGastoMaxAplicado), StringComparison.Ordinal);
    }

    [Fact]
    public void CA35_ProgresionDeCargasPorEjercicio()
    {
        var pts = Progresion.De(
        [
            (new DateOnly(2026, 3, 24), null, 8),
            (new DateOnly(2026, 3, 24), 55, 8),
            (new DateOnly(2026, 4, 14), 60, 8),
            (new DateOnly(2026, 5, 12), 62.5, 7),
            (new DateOnly(2026, 6, 9), 65, 8),
            (new DateOnly(2026, 7, 7), 65, 8),
            (new DateOnly(2026, 8, 4), 67.5, 6),
            (new DateOnly(2026, 9, 1), 67.5, 7),
        ]);
        Assert.Equal(7, pts.Count);
        Assert.Equal(55, pts[0].Peso);
        Assert.Equal(67.5, pts[^1].Peso);

        var r = Progresion.Resumir(pts)!;
        Assert.Equal(55, r.DesdeKg);
        Assert.Equal(67.5, r.HastaKg);
        Assert.Equal(12.5, r.DeltaKg);
        Assert.Equal(7, r.Sesiones);
        Assert.Equal(55, r.MinKg);
        Assert.Equal(67.5, r.MaxKg);
        Assert.InRange(r.DeltaFraccion, 0.22, 0.23);

        var g = Progresion.Grafico(pts)!;
        Assert.Equal(7, g.Puntos.Count);
        Assert.StartsWith("M", g.Path);
        Assert.Contains("L", g.Path);
        Assert.True(g.Puntos[0].Y > g.Puntos[^1].Y);
        Assert.Null(Progresion.Grafico(pts.Take(1).ToList()));
    }
}
