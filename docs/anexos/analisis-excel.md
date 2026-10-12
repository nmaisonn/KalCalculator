# Anexo — Análisis de `Alimentación.xlsx`

Evidencia que respalda los problemas P-01 a P-12 del documento de requerimientos. Archivo leído con `openpyxl` el 2026-09-16.

## 1. Inventario de hojas

| Hoja | Dimensión | Contenido | Uso en v1 |
|---|---|---|---|
| `Ejercicio` | 19 × 10 | Rutina de 4 días con pesos actuales | Solo la columna de kcal por sesión |
| `EjercicioIA` | 35 × 5 | Rutina de 5 días, duplica la anterior | No se usa |
| `SeguimientoKcal` | 48 × 14 | Catálogo de macros, pedidos de restaurante, kcal por sesión y 2 semanas de conteo | Catálogo, combos y actividades |
| `Bitacora` | 205 × 10 | 21 semanas de comidas en texto libre | No se migra (PD-04) |
| `Cardio` | 15 × 8 | Progresión de cinta por semana | Actividad de cardio |
| `Estadísticas` | 14 × 7 | 6 mediciones de composición corporal | Mediciones históricas |
| `Opciones` | 10 × 5 | Menú de alimentos permitidos por categoría | Categorías del catálogo |

## 2. P-01 — Celdas corrompidas por conversión a fecha

Excel interpretó cada decimal escrito con punto como una fecha del año en curso. El patrón es constante: **día.mes de la fecha almacenada equivale al decimal original**.

| Hoja | Celda | Campo | Valor almacenado | Valor real |
|---|---|---|---|---|
| SeguimientoKcal | D2 | Pan Los Sorchantes — proteína | 2026-03-04 | 4.3 g |
| SeguimientoKcal | F2 | Pan Los Sorchantes — grasa | 2026-01-01 | 1.1 g |
| SeguimientoKcal | F5 | Queso magro — grasa | 2026-09-02 | 2.9 g |
| SeguimientoKcal | D9 | Yogur Viva Light — proteína | 2026-09-09 | 9.9 g |
| SeguimientoKcal | F11 | Pechuga s/piel — grasa | 2026-05-06 | 6.5 g |
| SeguimientoKcal | F13 | Cuadril magro — grasa | 2026-05-17 | 17.5 g |
| SeguimientoKcal | D15 | Arroz 50 g — proteína | 2026-08-03 | 3.8 g |
| SeguimientoKcal | D16 | Arroz 60 g — proteína | 2026-05-04 | 4.5 g |
| SeguimientoKcal | D17 | Arroz 70 g — proteína | 2026-03-05 | 5.3 g |
| SeguimientoKcal | D19 | Huevo — proteína | 2026-03-06 | 6.3 g |
| SeguimientoKcal | F19 | Huevo — grasa | 2026-08-04 | 4.8 g |
| Ejercicio | D14 | Muerto rumano — peso | 2026-05-27 | 27.5 kg |
| Cardio | G5 | HIIT 30 s — velocidad | 2026-05-06 | 6.5 km/h |
| Cardio | G6 | HIIT 1 min — velocidad | 2026-08-04 | 4.8 km/h |

### Verificación cruzada

Los valores reales se confirman reconstruyendo las kcal desde los macros, con la equivalencia de 4 kcal/g para proteína y carbohidratos y 9 kcal/g para grasa:

| Alimento | Cálculo | Resultado | kcal declaradas | Δ |
|---|---|---|---|---|
| Pan (2 rebanadas) | 4.3×4 + 25×4 + 1.1×9 | 127.1 | 127 | 0.1 % |
| Queso magro | 8×4 + 0 + 2.9×9 | 58.1 | 58 | 0.2 % |
| Yogur Viva Light | 9.9×4 + 15×4 + 0 | 99.6 | 100 | 0.4 % |
| Pechuga 250 g | 57×4 + 0 + 6.5×9 | 286.5 | 285 | 0.5 % |
| Cuadril 250 g | 55×4 + 0 + 17.5×9 | 377.5 | 375 | 0.7 % |
| Huevo | 6.3×4 + 0.4×4 + 4.8×9 | 70.0 | 72 | 2.8 % |
| Arroz 50 g | 3.8×4 + 39×4 + 0.3×9 | 173.9 | 179 | 2.8 % |

Los siete cierran con menos del 3 % de diferencia, lo que confirma la interpretación. Este cálculo es el origen del requerimiento RF-A3 y del criterio CA-14.

## 3. P-02, P-03, P-04 — El modelo energético

La planilla calcula, en `SeguimientoKcal`:

```
Consumo Total del día = $L7 + Ejercicio + Cardio
```

donde `L7` = 2250 kcal, etiquetada "Por existir un dia".

Tres defectos encadenados:

1. **2250 es un valor fijo** que nunca se recalibró contra los resultados observados (P-02).
2. **2250 ya incluye actividad**, y las fórmulas le suman el gym por encima (P-03). Un factor de actividad que contemple ejercicio, más el ejercicio explícito, cuenta lo mismo dos veces.
3. **Los días sin ingesta cargada igual reciben 2250 kcal de consumo** (P-04). En la semana 2, los días 17 a 20 de septiembre están vacíos pero aportan 2250 kcal de déficit cada uno: el total de la semana informa 9310 kcal cuando lo real hasta la fecha es 310.

### Déficit registrado

Días con datos en `SeguimientoKcal`, filas 35 y 48:

| Fecha | Déficit según la planilla |
|---|---|
| 2026-09-07 | 721 |
| 2026-09-08 | 461 |
| 2026-09-09 | 291 |
| 2026-09-10 | 1 294 |
| 2026-09-11 | 249 |
| 2026-09-12 | 220 |
| 2026-09-13 | 809 |
| 2026-09-14 | 176 |
| 2026-09-15 | −1 205 |
| 2026-09-16 | 1 339 |
| **Promedio** | **436 kcal/día** |

### Déficit implícito en las mediciones

Entre la primera y la última medición con composición corporal:

```
Período          2026-03-20 → 2026-09-15 = 179 días
Grasa perdida    16.1102 − 12.4184 = 3.6918 kg  →  ×7700 = 28 427 kcal liberadas
Músculo ganado   30.4684 − 29.1166 = 1.3518 kg  →  ×1500 = 2 028 kcal invertidas
Energía neta     28 427 − 2 028 = 26 399 kcal
Déficit real     26 399 / 179 = 148 kcal/día
```

**La planilla sobreestima el déficit por un factor de 3** (436 contra 148). Es la justificación de OBJ-5 y de la regla RN-06.

Salvedad importante: de las 21 semanas de bitácora, solo las 5 primeras están completas, así que la ingesta del período no está registrada en su totalidad. El déficit real podría estar peor medido de lo que sugiere esta comparación. Esto motiva que RN-06 exija informar siempre la cobertura de registro.

### El modelo corregido sí cierra

Si a los mismos 10 días se les aplica el gasto por capas de RN-02 (1994 kcal en día sin entrenar, más la actividad registrada de cada día) en lugar de las 2250 fijas más ejercicio, los déficits cambian bastante:

| Fecha | Ingesta | Actividad | Gasto RN-02 | Déficit corregido | Déficit de la planilla |
|---|---|---|---|---|---|
| 2026-09-07 | 1 984 | 455 | 2 449 | 465 | 721 |
| 2026-09-08 | 1 789 | 0 | 1 994 | 205 | 461 |
| 2026-09-09 | 2 211 | 252 | 2 246 | 35 | 291 |
| 2026-09-10 | 1 506 | 550 | 2 544 | 1 038 | 1 294 |
| 2026-09-11 | 2 001 | 0 | 1 994 | −7 | 249 |
| 2026-09-12 | 2 030 | 0 | 1 994 | −36 | 220 |
| 2026-09-13 | 1 441 | 0 | 1 994 | 553 | 809 |
| 2026-09-14 | 2 074 | 0 | 1 994 | −80 | 176 |
| 2026-09-15 | 3 785 | 330 | 2 324 | −1 461 | −1 205 |
| 2026-09-16 | 1 511 | 600 | 2 594 | 1 083 | 1 339 |
| **Total** | | | | **1 790** | 4 355 |
| **Media** | | | | **179 kcal/día** | 436 kcal/día |

La columna de gasto muestra 1 994 redondeado; el cálculo usa 1 993.66, de ahí que el total sea 1 790 y no 1 795.

Contra el déficit real de **148 kcal/día** que implican las mediciones:

| Modelo | Déficit medio | Error contra lo medido |
|---|---|---|
| Planilla (2250 fijas + ejercicio) | 436 kcal/día | **+195 %** |
| RN-02 (basal × factor + actividad) | 179 kcal/día | **+21 %** |

El modelo por capas explica los resultados observados con un error de 22 %, atribuible a la subestimación de porciones (R-04) y a la precisión de la báscula (R-01). El de la planilla se equivoca por casi el triple.

Esto es evidencia directa de que la corrección de P-03 (el doble conteo del ejercicio) es la que más pesa, y valida los criterios CA-02, CA-03 y CA-20.

## 4. Mediciones históricas (hoja `Estadísticas`)

| Fecha | Peso | Grasa % | Grasa kg | Músculo % | Músculo kg | Visceral |
|---|---|---|---|---|---|---|
| 2022-11-04 | 71.8 | 15.8 | 11.34 | 43.3 | 31.09 | 6 |
| 2026-03-20 | 73.9 | 21.8 | 16.11 | 39.4 | 29.12 | 7 |
| 2026-04-22 | 74.0 | 20.6 | 15.24 | 40.2 | 29.75 | 7 |
| 2026-08-16 | 74.1 | 18.8 | 13.93 | 41.2 | 30.53 | 7 |
| 2026-08-19 | 73.1 | 18.0 | 13.16 | 41.7 | 30.48 | 7 |
| 2026-09-15 | 72.2 | 17.2 | 12.42 | 42.2 | 30.47 | 7 |

Las columnas en kg son fórmulas (`=B2*C2/100`), no datos ingresados: de ahí RF-D3.

### P-12 — Inconsistencia en el scratchpad

Las filas 12 a 14 calculan cuánta grasa falta para llegar al 15 %:

| Celda | Contenido | Observación |
|---|---|---|
| B12 | 73.1 | peso de agosto, no el actual |
| D12 | 12.158 | la fila 6 dice **13.158** para el mismo peso |
| D14 | `=D6-D13` → 2.193 | resultado que arrastra el error |

Con la medición del 2026-09-15 y masa magra constante, el valor correcto es **1.87 kg**, no 2.19. La diferencia de un dígito en D12 desplaza el objetivo casi un kilo entero.

## 5. Datos que forman la plantilla derivada del Excel

Estos valores no se cargan solos: forman una de las plantillas que el usuario puede importar si quiere (RF-G2c, D-14). Una cuenta nueva arranca vacía.

### Catálogo de alimentos, con macros corregidos

| Alimento | Porción | kcal | P | C | G |
|---|---|---|---|---|---|
| Pan Los Sorchantes | 2 rebanadas (50 g) | 127 | 4.3 | 25 | 1.1 |
| Requeson light | 18 g | 46 | 4 | 0.8 | 3 |
| Leche descremada | 200 ml | 68 | 7 | 10 | 0 |
| Queso magro | 29 g | 58 | 8 | 0 | 2.9 |
| Manzana verde | 1 (180 g) | 94 | 0.5 | 25 | 0.3 |
| Mandarina | 1 (90 g) | 48 | 0.7 | 12 | 0.3 |
| Yogur Viva Light | 1 vaso (200 g) | 100 | 9.9 | 15 | 0 |
| Pechuga s/piel cruda | 250 g | 285 | 57 | 0 | 6.5 |
| Cuadril magro crudo | 250 g | 375 | 55 | 0 | 17.5 |
| Hígado de vaca | 250 g | 340 | 51 | 0 | 9 |
| Arroz Saman crudo | 50 g | 179 | 3.8 | 39 | 0.3 |
| Arroz Saman crudo | 60 g | 215 | 4.5 | 47 | 0.4 |
| Arroz Saman crudo | 70 g | 251 | 5.3 | 55 | 0.4 |
| Zanahoria hervida | 1 (70 g) | 25 | 0.6 | 6 | 0.2 |
| Huevo | 1 | 72 | 6.3 | 0.4 | 4.8 |
| Aceite | 1 cucharadita (5 ml) | 40 | 0 | 0 | 4 |

### Comida de afuera, solo kcal (RN-09)

| Ítem | kcal |
|---|---|
| Muzza Requete + 3 fainás | 2 250 |
| Muzza Requete + 1 fainá | 1 600 |
| Mila napo del club + fritas | 1 450 |
| Rudy doble con fritas | 1 400 |
| Doble cuarto con fritas | 1 120 |
| Mila napo del club sin fritas | 1 000 |
| Rudy doble sin fritas | 950 |
| Doble cuarto sin fritas | 780 |
| Tira + medio chorizo | 740 |
| Mila al horno de carne | 700 |
| Mila al horno de pollo | 600 |
| 2 empanadas | 490 |
| Coca 250 ml | 105 |
| Muzza + jamón | 100 |
| Milka Oreo, 2 cuadraditos | 92 |

### Combos (columna A de `SeguimientoKcal`, filas 7, 10, 12 y 14)

| Combo | Composición | kcal |
|---|---|---|
| Desayuno completo | Pan + Requeson + Leche + Queso magro + Manzana | 393 |
| Merienda completa | Pan + Requeson + Leche + Yogur + Mandarina | 389 |
| Almuerzo/cena pollo | Pechuga + Arroz 60 g + Zanahoria + Aceite | 565 |
| Almuerzo/cena cuadril | Cuadril + Arroz 60 g + Zanahoria + Aceite | 655 |

Los cuatro totales se verificaron sumando sus componentes. Son los criterios CA-07 y CA-08.

### Actividades (columnas K y L)

| Actividad | Tipo | kcal |
|---|---|---|
| Día 1 — Espalda + bíceps | gym | 330 |
| Día 2 — Pecho + tríceps | gym | 310 |
| Día 3 — Pierna | gym | 390 |
| Día 4 — Hombro + inclinada + brazos | gym | 280 |
| Cinta 30 min, 5 km/h al 10 % | cardio | 290 |
| Fútbol 5 | deporte | 550 |

## 6. Otros defectos estructurales

| Problema | Evidencia concreta |
|---|---|
| P-05 | Fórmulas distintas por día: `=$C7`, `=C14 + I16`, `=(C11/2)+(C13/2)+C16+C18+C20`, `=Round(L6/3,0)` |
| P-06 | `Bitacora` repite un bloque de 9 filas por semana, 21 veces. `SeguimientoKcal`, uno de 12 filas |
| P-07 | `Ingesta Total` = `SUM(B26:B30)` deja fuera las filas 31 (Ejercicio) y 32 (Cardio) |
| P-09 | Las 21 semanas están en texto libre: "2 tostadas con requeson, una manzana verde y un taza de leche" |
| P-10 | `Opciones` lista comidas permitidas; `SeguimientoKcal` lista macros. Son dos catálogos sin relación |
| — | Fechas duplicadas: semanas 6 y 7 comparten 27 abr a 3 may; semanas 20 y 21 comparten 27 jul a 2 ago |
| — | Las dos hojas de rutina se contradicen: Press banca 20 contra 27.5+; Jalón 65+ contra 70 |
