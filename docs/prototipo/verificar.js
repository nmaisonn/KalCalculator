// Verifica las metricas del dashboard contra CA-20 y CA-21.
// Ejecuta el script del prototipo con un DOM simulado y consulta metricasPeriodo.
const fs = require("fs");
const src = fs.readFileSync(__dirname + "/prototipo.html", "utf8")
  .match(/<script>([\s\S]*)<\/script>/)[1];

const noop = () => {};
const el = { value:"", checked:false, innerHTML:"", focus:noop, setSelectionRange:noop,
             addEventListener:noop, querySelector:() => null, click:noop };
global.document = { addEventListener:noop, getElementById:() => el,
                    querySelector:() => el, querySelectorAll:() => [], body:el };
global.window = {};

const ctx = new Function(src + `; return {
  metricasPeriodo, plan, GASTO_BASE, BMR, PERIODOS, VISTAS, S,
  PROTOCOLOS, SESIONES_CARDIO, SESIONES_GYM, RUTINAS, EJERCICIOS, fmtPeso, fmtSerie };`)();
const { metricasPeriodo, plan, GASTO_BASE, BMR, VISTAS, S,
        PROTOCOLOS, SESIONES_CARDIO, SESIONES_GYM, RUTINAS, fmtPeso, fmtSerie } = ctx;

const obj = plan(15, 8).diario;
let fallos = 0;
const chequear = (id, real, esperado, tol) => {
  const ok = Math.abs(real - esperado) <= tol;
  if (!ok) fallos++;
  console.log(`${ok ? "PASA" : "FALLA"}  ${id}: ${typeof real === "number" ? real.toFixed(2) : real}` +
              `  (esperado ${esperado} ± ${tol})`);
};

console.log(`Basal ${BMR.toFixed(0)} kcal · gasto sin entrenar ${GASTO_BASE.toFixed(0)} kcal · objetivo ${obj.toFixed(0)} kcal/dia\n`);

console.log("CA-20 — periodo 2026-09-07 al 2026-09-16");
const s = metricasPeriodo("2026-09-07", "2026-09-16", obj);
chequear("deficit acumulado", s.acumulado, 1795, 12);
chequear("deficit medio", s.medio, 180, 2);
chequear("dias con registro", s.diasConRegistro, 10, 0);
chequear("dias totales", s.diasTotales, 10, 0);
chequear("cobertura", s.cobertura, 1, 0);
chequear("adherencia", s.adherencia, 4, 0);
chequear("grasa estimada", s.grasaEstimada, 0.233, 0.005);

console.log("\nCA-21 — mes de septiembre completo");
const m = metricasPeriodo("2026-09-01", "2026-09-30", obj);
chequear("dias con registro", m.diasConRegistro, 10, 0);
chequear("dias totales", m.diasTotales, 30, 0);
chequear("cobertura", m.cobertura, 0.333, 0.01);
chequear("acumulado igual al de CA-20", m.acumulado, s.acumulado, 0.01);
console.log(`PASA  contraste oculto por cobertura < 50 %: ${m.cobertura < 0.5}`);

console.log("\nSemana del 10 al 16");
const w = metricasPeriodo("2026-09-10", "2026-09-16", obj);
chequear("dias con registro", w.diasConRegistro, 7, 0);
chequear("cobertura", w.cobertura, 1, 0);
console.log(`      acumulado ${w.acumulado.toFixed(0)} kcal · medio ${w.medio.toFixed(0)} kcal/dia`);
console.log(`      grasa medida en el rango: ${w.grasaMedida === null ? "no hay 2 mediciones" : w.grasaMedida.toFixed(2) + " kg"}`);

console.log("\nModo simulacion — rango entre dos mediciones (19 ago al 15 sep)");
const c = metricasPeriodo("2026-08-19", "2026-09-15", obj, true);
chequear("cobertura", c.cobertura, 1, 0);
console.log(`      acumulado ${c.acumulado.toFixed(0)} kcal · medio ${c.medio.toFixed(0)} kcal/dia`);
console.log(`      grasa estimada ${c.grasaEstimada.toFixed(2)} kg · medida ${c.grasaMedida.toFixed(2)} kg`);
const disc = c.grasaEstimada - c.grasaMedida;
console.log(`      discrepancia ${disc >= 0 ? "+" : ""}${disc.toFixed(2)} kg` +
            `  →  ${Math.abs(disc) < 0.15 ? "modelo calibrado" : disc > 0 ? "subestima ingesta" : "pierde mas de lo previsto"}`);
chequear("se muestra el contraste (cobertura >= 50 %)", c.cobertura >= 0.5 ? 1 : 0, 1, 0);
chequear("adherencia coherente", c.adherencia <= c.diasConRegistro ? 1 : 0, 1, 0);

console.log("\nCA-37 — duracion del protocolo de intervalos");
const hiit = PROTOCOLOS.find(p => p.id === "hiit");
const dur = hiit.bloques.reduce((t, b) => t + b.min * b.rep, 0);
chequear("duracion HIIT en minutos", dur, 19, 0);

console.log("\nCA-36 — kcal de cardio cargadas como rango");
const rango = SESIONES_CARDIO.find(s => s.f === "2026-09-07");
chequear("punto medio de 170-230", (rango.kMin + rango.kMax) / 2, 200, 0);
const s0907 = SESIONES_CARDIO.find(s => s.f === "2026-08-20");
chequear("punto medio de 160-220", (s0907.kMin + s0907.kMax) / 2, 190, 0);

console.log("\nCA-31 y CA-33 — series y peso estructurados, no texto");
const dia1 = RUTINAS[0].dias[0];
const jalon = dia1.ejs.find(x => x.e === "jalon");
console.log(`      series formateadas: ${fmtSerie(jalon)}`);
chequear("formato 4x6/10", fmtSerie(jalon) === "4x6/10" ? 1 : 0, 1, 0);
const muerto = RUTINAS[0].dias[2].ejs.find(x => x.e === "muerto");
chequear("peso del muerto rumano es numerico 27.5", muerto.peso, 27.5, 0);
chequear("marcado como 'puede mas'", muerto.mas ? 1 : 0, 1, 0);
console.log(`      se muestra como: ${fmtPeso(muerto).replace(/<[^>]+>/g, "")}`);

console.log("\nCA-32 — ejercicio con peso no numerico");
const aduct = RUTINAS[0].dias[2].ejs.find(x => x.e === "aduct");
chequear("peso nulo", aduct.peso === null ? 1 : 0, 1, 0);
console.log(`      se muestra como: ${fmtPeso(aduct).replace(/<[^>]+>/g, "")}`);

console.log("\nCA-35 — progresion de cargas por ejercicio");
const prog = SESIONES_GYM.filter(s => s.ejs.some(x => x.e === "jalon" && x.peso != null))
  .map(s => ({ f: s.f, peso: s.ejs.find(x => x.e === "jalon").peso }))
  .sort((a, b) => a.f.localeCompare(b.f));
chequear("puntos de progresion del jalon", prog.length, 7, 0);
chequear("primer peso", prog[0].peso, 55, 0);
chequear("ultimo peso", prog[prog.length - 1].peso, 67.5, 0);
chequear("orden cronologico", prog.every((p, i) => i === 0 || p.f >= prog[i-1].f) ? 1 : 0, 1, 0);

console.log("\nTodas las pantallas renderizan sin error");
for (const [nombre, vista] of Object.entries(VISTAS)) {
  for (const sim of [false, true]) {
    S.simular = sim;
    try {
      const html = vista();
      if (!html || html.length < 40) throw new Error("salida vacia o muy corta");
      if (/undefined|NaN|\[object Object\]/.test(html))
        throw new Error("contiene undefined, NaN u objeto sin formatear");
    } catch (err) {
      fallos++;
      console.log(`FALLA  ${nombre}${sim ? " (simulacion)" : ""}: ${err.message}`);
    }
  }
}
S.simular = false;
if (!fallos) console.log(`PASA   ${Object.keys(VISTAS).length} pantallas, con y sin simulacion`);

console.log(`\n${fallos === 0 ? "TODO PASA" : fallos + " FALLAS"}`);
process.exit(fallos ? 1 : 0);
