# kcal-tracker — Especificación de pantallas

| | |
|---|---|
| Versión | 2.0 (borrador para revisión) |
| Estado | **Pendiente de aprobación** |
| Referencia | `01-requerimientos.md`, `03-modelo-y-decisiones.md` |
| Prototipo navegable | `prototipo/prototipo.html` |
| Cambios sobre la 1.0 | Pantallas nuevas P10 a P16 (ejercicios, rutinas, sesión de gym, cardio, ingreso, registro y bienvenida), P6 Ajustes reorganizada en cinco pestañas con toda la personalización, y navegación agrupada en tres bloques |

Los wireframes son esquemáticos: definen qué información aparece, con qué jerarquía y qué acciones existen. No definen tipografía, paleta ni espaciado.

---

## 1. Mapa de navegación

```
   ┌──────────────────┐   sin sesión    ┌──────────────────────────────┐
   │ Ingreso   (P14)  ├────────────────►│ Registro de cuenta    (P15)  │
   └────────┬─────────┘                 └───────────┬──────────────────┘
            │ con sesión                            │ primera vez
            │                                       ▼
            │                          ┌──────────────────────────────┐
            │                          │ Bienvenida: ¿arrancás vacío  │
            │◄─────────────────────────┤ o importás plantillas? (P16) │
            │                          └──────────────────────────────┘
            ▼
   ┌─────────────────┐
   │  Barra lateral  │
   └────────┬────────┘
            │
   ── REGISTRO DIARIO ──────────────────────────────────────────────────
   ├──► Dashboard (P8) ──── clic en un día ────► Hoy en esa fecha
   ├──► Hoy (P1) ────────── navegar por fecha ─► Hoy en otra fecha
   ├──► Objetivo (P3)
   │
   ── ENTRENAMIENTO ────────────────────────────────────────────────────
   ├──► Actividad (P2)
   ├──► Rutinas (P11) ───── editar un día ─────► Ejercicios (P10)
   │         │
   │         └── entrenar hoy ──► Sesión de gym (P12) ──► Progresión
   ├──► Cardio (P13) ────── protocolos e historial
   │
   ── SEGUIMIENTO Y AJUSTES ────────────────────────────────────────────
   ├──► Mediciones (P4)
   ├──► Alimentos (P5) ──┬─ editar combo ──────► Combo (P7)
   │                     └─ buscar alimento ──► Búsqueda asistida (P9)
   │                                                   │ confirmar
   │                                                   ▼
   │                                            Alta de alimento
   └──► Ajustes (P6) ─── pestañas: Perfil · Objetivo · Tipos y categorías
                                   · Cálculo avanzado · Cuenta
```

Pantalla inicial: **P1 — Hoy**, en la fecha actual, una vez iniciada la sesión.

El menú se agrupa en tres bloques porque con trece pantallas una lista plana deja de ser navegable. Los nombres de los bloques son fijos; el contenido de cada pantalla depende de lo que el usuario haya configurado.

> **Nota sobre la pantalla inicial (PD-09)**: el dashboard es el candidato natural a ser la home, pero el uso dominante es cargar comida varias veces al día, y el objetivo OBJ-1 fija un techo de 5 interacciones para un día típico. Anteponer el dashboard agregaría un clic a cada carga. Por eso la inicial sigue siendo Hoy y el dashboard queda primero en el menú, a un clic. Es reversible con una línea de configuración.

### 1.1 Inventario de pantallas

| ID | Pantalla | Rol |
|---|---|---|
| P1 | Hoy | Registro diario de comidas y actividad. Pantalla inicial |
| P2 | Actividad | Catálogo de actividades y gasto por capas |
| P3 | Objetivo | Plan para alcanzar un % de grasa y gasto calibrado |
| P4 | Mediciones | Composición corporal en el tiempo |
| P5 | Alimentos | Catálogo de alimentos y combos |
| P6 | Ajustes | Perfil, objetivo, tipos y categorías, cálculo avanzado, cuenta |
| P7 | Combo | Formulario de combo |
| P8 | Dashboard | Vista consolidada de un período |
| P9 | Búsqueda asistida | Consulta nutricional a fuentes externas |
| P10 | Ejercicios | Catálogo de ejercicios por grupo muscular |
| P11 | Rutinas | Rutinas, sus días y lo que prescriben |
| P12 | Sesión de gym | Registro de lo que se levantó y progresión por ejercicio |
| P13 | Cardio | Sesiones, protocolos de intervalos y progresión |
| P14 | Ingreso | Correo y contraseña |
| P15 | Registro de cuenta | Alta de usuario |
| P16 | Bienvenida | Elección del punto de partida |

Origen de cada grupo: P1, P2 y P3 son lo que se pidió al principio; P8 y P9 se agregaron con el dashboard y la búsqueda nutricional; P10 a P13 cubren las hojas `Ejercicio`, `EjercicioIA` y `Cardio` de la planilla; P14 a P16 aparecen con las cuentas.

## 2. Estructura común

Todas las pantallas comparten la barra lateral izquierda y un encabezado con el título de la sección. La barra lateral muestra, de forma permanente, un resumen de contexto:

```
┌──────────────────────┐
│ kcal-tracker         │
├──────────────────────┤
│ ▸ Hoy                │
│   Actividad          │
│   Objetivo           │
│   Mediciones         │
│   Alimentos          │
│   Ajustes            │
├──────────────────────┤
│ 72.2 kg · 17.2 %     │  ← última medición
│ Objetivo: 15 %       │
│ Faltan 1.87 kg       │
└──────────────────────┘
```

Regla transversal (RNF-04): cada número calculado tiene un ícono de información que despliega la fórmula y los valores de entrada usados. Ninguna cifra aparece sin poder explicarse.

---

## 3. P1 — Hoy

**Propósito**: registrar la comida del día y ver el balance energético. Es la pantalla de uso diario y la que debe optimizarse para velocidad (OBJ-1).

**Requerimientos que implementa**: RF-B1 a RF-B10, RF-C2, RN-02, RN-03, RN-07.

### 3.1 Wireframe

```
┌───────────────────────────────────────────────────────────────────────────┐
│  ‹  miércoles 16 de septiembre de 2026  ›              [ Hoy ] [Copiar día]│
├───────────────────────────────────────────────────────────────────────────┤
│                                                                           │
│   DÉFICIT DE HOY              Ingesta        Gasto        Proteína         │
│   ┌────────────┐              ┌───────┐    ┌───────┐    ┌───────┐         │
│   │  +468 kcal │              │ 1 856 │    │ 2 324 │    │ 142 g │         │
│   └────────────┘              └───────┘    └───────┘    └───────┘         │
│   Objetivo 257 · alcanzado                                                │
│                                                                           │
│   ├────────proteína────────┬──────carbos──────┬───grasa───┤               │
│   1 856 kcal ingeridas                    Tope del día: 2 067 kcal        │
│                                                                           │
├─────────────────────────────────────┬─────────────────────────────────────┤
│  CARGAR                             │  COMIDO HOY                         │
│                                     │                                     │
│  Combos                             │  Desayuno                 393 kcal  │
│  [Desayuno 393] [Merienda 389]      │   Pan Los Sorchantes  127     ✕     │
│  [Pollo 565]    [Cuadril 655]       │   Requeson light       46     ✕     │
│                                     │   Leche descremada     68     ✕     │
│  Buscar alimento                    │   Queso magro          58     ✕     │
│  ┌─────────────────────────────┐    │   Manzana verde        94     ✕     │
│  │ 🔍                          │    │                                     │
│  └─────────────────────────────┘    │  Almuerzo                 565 kcal  │
│                                     │   Pechuga s/piel      285     ✕     │
│  Frecuentes                         │   Arroz Saman 60 g    215     ✕     │
│  (pan) (huevo) (pollo) (arroz)      │   Zanahoria            25     ✕     │
│  (leche) (manzana) (yogur)          │   Aceite               40     ✕     │
│                                     │                                     │
│  De afuera                          │  Cena                   sin cargar  │
│  (Rudy 950) (Mila 1000) (coca 105)  │                                     │
│                                     │                                     │
├─────────────────────────────────────┴─────────────────────────────────────┤
│  ACTIVIDAD DE HOY                                                         │
│  [✓ Día 1 Espalda+bíceps 330]  [ Día 2 310 ]  [ Cinta 290 ]  [ Fútbol 550]│
│  Gasto: basal 1 661 + cotidiano 333 + deporte 330 = 2 324 kcal            │
└───────────────────────────────────────────────────────────────────────────┘
```

### 3.2 Elementos

| Elemento | Comportamiento |
|---|---|
| Navegador de fecha | Flechas a día anterior y siguiente. Botón "Hoy" vuelve a la fecha actual. No permite fechas futuras |
| Déficit del día | Cifra principal de la pantalla. Verde si alcanza el objetivo diario, ámbar si no llega, rojo si hay superávit |
| Tarjetas de resumen | Ingesta, gasto y proteína del día. La de proteína se marca según el objetivo de RF-E4 |
| Barra de macros | Proporción de kcal aportada por proteína, carbohidratos y grasa. Un cuarto segmento gris aparece solo si hay kcal sin macros (RF-B7, RN-09) |
| Botones de combo | Un clic agrega todos los items del combo al tipo de comida que corresponda (RF-B3) |
| Buscador | Filtra el catálogo por nombre. Enter agrega el primer resultado |
| Frecuentes | Los alimentos más usados por el usuario, como atajos de un clic |
| De afuera | Alimentos marcados como restaurante, separados visualmente porque no aportan macros |
| Lista "Comido hoy" | Agrupada por tipo de comida, con subtotal por grupo. Cada item se puede quitar |
| Selector de actividad | Botones de alternancia. Marcar uno recalcula el gasto en el momento (RF-C2) |
| Desglose del gasto | Línea de texto que explica las tres capas, para hacer visible que el ejercicio no se cuenta dos veces (RF-C4, RF-C5) |
| Copiar día | Abre un selector de fecha y copia todos los items de ese día a la fecha actual (RF-B9) |

### 3.3 Estados

| Estado | Presentación |
|---|---|
| Sin registro | Las tarjetas de ingesta y déficit muestran "sin registro" en lugar de cero. Mensaje: *"Este día no tiene comidas cargadas. No se le asigna déficit."* (RN-07, CA-11) |
| Sin mediciones | El gasto no se puede calcular. Aviso con enlace directo a P4: *"Cargá una medición para poder calcular tu gasto."* |
| Sin perfil configurado | Aviso con enlace a P6, indicando que faltan altura, fecha de nacimiento y sexo |
| Día con comida de afuera | Aparece el segmento gris en la barra de macros y la nota de kcal sin macros conocidos |
| Superávit | El déficit se muestra en negativo y en rojo, con el texto "superávit" |

---

## 4. P2 — Actividad

**Propósito**: mostrar cómo se compone el gasto energético y administrar el catálogo de actividades. Es la pantalla que hace transparente la corrección de P-02 y P-03.

**Requerimientos**: RF-C1 a RF-C6, RN-01, RN-02.

### 4.1 Wireframe

```
┌───────────────────────────────────────────────────────────────────────────┐
│  Actividad y gasto energético                                             │
├───────────────────────────────────────────────────────────────────────────┤
│                                                                           │
│   Basal          Día sin entrenar     Día de gym        Excel decía       │
│   ┌───────┐      ┌───────┐            ┌───────┐         ┌───────┐         │
│   │ 1 661 │      │ 1 994 │            │ 2 324 │         │ 2 250 │         │
│   └───────┘      └───────┘            └───────┘         └───────┘         │
│                                                                           │
│  ┌─────────────────────────────────────────────────────────────────────┐  │
│  │ Cómo se calcula                                    Katch-McArdle    │  │
│  │                                                                     │  │
│  │  Con 72.2 kg y 17.2 % de grasa, tu masa magra es 59.78 kg.          │  │
│  │  BMR = 370 + 21.6 × 59.78 = 1 661 kcal                              │  │
│  │  Gasto sin deporte = 1 661 × 1.2 = 1 994 kcal                       │  │
│  │                                                                     │  │
│  │  El factor 1.2 cubre solo actividad cotidiana. El entrenamiento se  │  │
│  │  suma aparte, así que no se cuenta dos veces.                       │  │
│  └─────────────────────────────────────────────────────────────────────┘  │
│                                                                           │
│  CATÁLOGO DE ACTIVIDADES                                   [+ Nueva]      │
│  ┌─────────────────────────────────────────────────────────────────────┐  │
│  │ Actividad                        Tipo      kcal   Gasto del día  ⋯  │  │
│  ├─────────────────────────────────────────────────────────────────────┤  │
│  │ Día 1 — Espalda + bíceps         gym        330        2 324     ✎  │  │
│  │ Día 2 — Pecho + tríceps          gym        310        2 304     ✎  │  │
│  │ Día 3 — Pierna                   gym        390        2 384     ✎  │  │
│  │ Día 4 — Hombro + brazos          gym        280        2 274     ✎  │  │
│  │ Cinta 30 min, 5 km/h al 10 %     cardio     290        2 284     ✎  │  │
│  │ Fútbol 5                         deporte    550        2 544     ✎  │  │
│  └─────────────────────────────────────────────────────────────────────┘  │
│                                                                           │
│  ÚLTIMAS SESIONES REGISTRADAS                                             │
│  16 sep · Día 1 Espalda + bíceps · 330 kcal                               │
│  15 sep · Cinta 30 min · 290 kcal                                         │
│  14 sep · sin actividad                                                   │
└───────────────────────────────────────────────────────────────────────────┘
```

### 4.2 Elementos y estados

| Elemento | Comportamiento |
|---|---|
| Tarjetas de gasto | Valores calculados en vivo desde la última medición y el perfil |
| Tarjeta "Excel decía" | Comparación con las 2250 kcal de la planilla. Es informativa y se puede ocultar de forma permanente |
| Bloque explicativo | Muestra fórmula, entradas y resultado (RNF-04). Indica cuál de las dos fórmulas de RN-01 se está usando y por qué |
| Tabla de actividades | Alta, edición y baja lógica. El usuario define libremente nombre, tipo y kcal de cada actividad (RF-C1b). La columna "Gasto del día" es informativa: el gasto sin deporte más esa actividad |
| Formulario de actividad | Nombre, tipo (gym, cardio, deporte), kcal y minutos opcionales. Al editar las kcal de una actividad ya usada, avisa que las sesiones ya registradas no cambian (RF-C1d, CA-18) |
| Ajuste por sesión | En P1, cada actividad marcada permite sobrescribir sus kcal solo para ese día, sin tocar el catálogo (RF-C1c). Útil cuando una sesión fue más corta o más intensa de lo habitual |
| Últimas sesiones | Los últimos 10 días con actividad registrada, editables y eliminables. Los días sin actividad se listan como tal, sin inventar valores |

Formulario de alta y edición de actividad:

```
┌──────────────────────────────────────────────┐
│  Editar actividad                            │
├──────────────────────────────────────────────┤
│  Nombre    [ Día 1 — Espalda + bíceps      ] │
│  Tipo      [ gym ▾ ]                         │
│  kcal      [ 330 ]                           │
│  Minutos   [ 60  ]  (opcional)               │
│                                              │
│  ⚠ Esta actividad se usó en 4 sesiones.      │
│    Cambiar las kcal no las modifica.          │
│                                              │
│         [ Cancelar ]  [ Guardar ]  [Eliminar]│
└──────────────────────────────────────────────┘
```

| Estado | Presentación |
|---|---|
| Sin mediciones | No se pueden mostrar las tarjetas. Aviso con enlace a P4 |
| Sin perfil | Se muestra el basal por Katch-McArdle si hay % de grasa; si no, aviso pidiendo completar P6 |
| Catálogo vacío | Ocurre en una cuenta nueva que no importó plantillas (CA-26). Se muestra un mensaje con acceso al alta manual y a la importación de plantillas |

---

## 5. P3 — Objetivo

**Propósito**: responder cuánto déficit hace falta para llegar al % de grasa deseado, en cuánto tiempo, y si el plazo es sensato. Es la razón de ser del producto (OBJ-3).

**Requerimientos**: RF-F1 a RF-F8, RN-04, RN-05, RN-06.

### 5.1 Wireframe

```
┌───────────────────────────────────────────────────────────────────────────┐
│  Objetivo                                                                 │
├───────────────────────────────────────────────────────────────────────────┤
│                                                                           │
│   Grasa objetivo  [ 15 % ▾ ]      Plazo  [ 8 semanas ▾ ]                  │
│                                                                           │
│   Peso al llegar   Grasa por perder   kcal totales   Déficit diario       │
│   ┌───────┐        ┌───────┐          ┌────────┐     ┌───────┐            │
│   │70.3 kg│        │1.87 kg│          │ 14 389 │     │  257  │            │
│   └───────┘        └───────┘          └────────┘     └───────┘            │
│                                                                           │
│  ┌─────────────────────────────────────────────────────────────────────┐  │
│  │ ✓  El plazo es sostenible                                           │  │
│  │    257 kcal/día está por debajo del techo de 498 kcal (el menor      │  │
│  │    entre un cuarto de tu gasto y 0.75 % del peso por semana).        │  │
│  └─────────────────────────────────────────────────────────────────────┘  │
│                                                                           │
│  MASA GRASA PROYECTADA                                                    │
│  kg                                                                       │
│  12.5┤●                                                                   │
│      │  ●                                                                 │
│  12.0┤     ●                                                              │
│      │        ●                                                           │
│  11.5┤           ●                                                        │
│      │              ●                                                     │
│  11.0┤                 ●                                                  │
│      │                    ●                                               │
│  10.5┤━━━━━━━━━━━━━━━━━━━━━━●  objetivo 15 %                              │
│      └┬───┬───┬───┬───┬───┬───┬───┬──                                     │
│      hoy  1   2   3   4   5   6   7   8   semanas                         │
│                                                                           │
│  TU GASTO REAL, CALIBRADO                                                 │
│  ┌─────────────────────────────────────────────────────────────────────┐  │
│  │  Período 20 mar → 15 sep 2026 · 179 días · cobertura de registro 6 %│  │
│  │                                                                     │  │
│  │  Grasa perdida        3.69 kg   →  28 427 kcal liberadas            │  │
│  │  Músculo ganado       1.35 kg   →  −2 028 kcal invertidas           │  │
│  │  Déficit real                      148 kcal/día                     │  │
│  │                                                                     │  │
│  │  ⚠ Cobertura baja: solo 10 de 179 días tienen ingesta registrada.   │  │
│  │    El número es indicativo. Con 4 semanas de registro continuo se   │  │
│  │    vuelve confiable y reemplaza a la fórmula.                       │  │
│  └─────────────────────────────────────────────────────────────────────┘  │
│                                                                           │
│  COMPARACIÓN CON LA PLANILLA                                              │
│  Gasto en día sin entrenar     2 250 fijas    →   1 994 desde masa magra  │
│  Grasa para llegar al 15 %     2.19 kg        →   1.87 kg                 │
│  Días sin datos                2 250 de déficit → sin registro            │
│  Déficit diario                436 kcal       →   148 kcal                │
└───────────────────────────────────────────────────────────────────────────┘
```

### 5.2 Elementos y estados

| Elemento | Comportamiento |
|---|---|
| Selectores | Grasa objetivo y plazo. Cambiarlos recalcula todo en el momento, sin guardar hasta confirmar |
| Tarjetas del plan | Resultado de RN-04. La de déficit diario cambia de color según RN-05 |
| Panel de validación | Verde si el déficit está bajo el techo; rojo con sugerencia de extender el plazo si lo supera (RF-F4, CA-06) |
| Gráfico de proyección | Masa grasa semana a semana, con línea de referencia en el objetivo (RF-F5) |
| Panel de calibración | Resultado de RN-06 con su desglose. **Siempre** acompañado de la cobertura de registro |
| Comparación con la planilla | Bloque informativo, ocultable de forma permanente |

| Estado | Presentación |
|---|---|
| Objetivo ya alcanzado | Mensaje de logro. Los selectores permiten fijar un objetivo nuevo |
| Una sola medición | El plan se calcula, pero el panel de calibración se reemplaza por: *"Hacen falta al menos dos mediciones separadas por 14 días."* |
| Cobertura menor al 70 % | El resultado calibrado se muestra con advertencia y no reemplaza a la fórmula (RF-F7) |
| Cobertura mayor o igual al 70 % | El gasto calibrado pasa a ser el valor usado en P1 y P2, indicándolo explícitamente |
| Sin medición de % de grasa | No se puede calcular el plan. Aviso: el objetivo se define sobre composición, no sobre peso |

---

## 6. P4 — Mediciones

**Propósito**: registrar y ver la evolución de la composición corporal. Alimenta RN-01, RN-04 y RN-06.

**Requerimientos**: RF-D1 a RF-D5.

```
┌───────────────────────────────────────────────────────────────────────────┐
│  Mediciones                                              [+ Nueva]        │
├───────────────────────────────────────────────────────────────────────────┤
│  EVOLUCIÓN                                                                │
│  kg                                        ── masa grasa   ── masa magra  │
│  31┤          ●─────●─────●──────●──────●   (magra)                       │
│    │                                                                      │
│  16┤●                                                                     │
│    │   ●───●                                                              │
│  12┤            ●────●─────●   (grasa)                                    │
│    └┬──────┬──────┬──────┬──────┬──────┬─                                 │
│    nov'22 mar    abr    ago 16 ago 19 sep 15                              │
│                                                                           │
│  ┌─────────────────────────────────────────────────────────────────────┐  │
│  │ Fecha        Peso   Grasa %  Grasa kg  Músc %  Músc kg  Visc.   ⋯  │  │
│  ├─────────────────────────────────────────────────────────────────────┤  │
│  │ 2026-09-15   72.2    17.2     12.42     42.2    30.47     7     ✎  │  │
│  │ 2026-08-19   73.1    18.0     13.16     41.7    30.48     7     ✎  │  │
│  │ 2026-08-16   74.1    18.8     13.93     41.2    30.53     7     ✎  │  │
│  │ 2026-04-22   74.0    20.6     15.24     40.2    29.75     7     ✎  │  │
│  │ 2026-03-20   73.9    21.8     16.11     39.4    29.12     7     ✎  │  │
│  │ 2022-11-04   71.8    15.8     11.34     43.3    31.09     6     ✎  │  │
│  └─────────────────────────────────────────────────────────────────────┘  │
│  Las columnas en kg son derivadas, no se ingresan.                        │
└───────────────────────────────────────────────────────────────────────────┘
```

| Elemento | Comportamiento |
|---|---|
| Gráfico | Masa grasa y masa magra en kg. Eje temporal real, con el salto de 2022 a 2026 visible a escala |
| Tabla | Orden descendente por fecha. Las columnas en kg son calculadas al leer y no editables (RF-D3) |
| Formulario de alta y edición | Fecha, peso, % de grasa, % de músculo, visceral. Solo el peso es obligatorio (RF-D4, RF-D6). La fecha también es editable |
| Eliminar | Con confirmación previa que indica qué se va a borrar (RF-D7) |
| Aviso de recálculo | Si la medición editada o eliminada es la última, se advierte que cambia el metabolismo basal, el plan de objetivo y la calibración, y se indica cuál pasa a ser la referencia (RF-D8, CA-19) |
| Validación | Una medición por fecha (RF-D2). Al repetir fecha, ofrece editar la existente |

Formulario de edición:

```
┌────────────────────────────────────────────────────────┐
│  Editar medición                                       │
├────────────────────────────────────────────────────────┤
│  Fecha          [ 2026-09-15 ]                         │
│  Peso           [ 72.2 ] kg                            │
│  Grasa          [ 17.2 ] %      →  12.42 kg (derivado) │
│  Músculo        [ 42.2 ] %      →  30.47 kg (derivado) │
│  Grasa visceral [ 7 ]                                  │
│                                                        │
│  ⚠ Es tu última medición: cambiarla recalcula el gasto  │
│    basal (hoy 1 661 kcal) y el plan de objetivo.        │
│                                                        │
│              [ Cancelar ]  [ Guardar ]  [ Eliminar ]   │
└────────────────────────────────────────────────────────┘
```

Confirmación de borrado:

```
┌────────────────────────────────────────────────────────┐
│  ¿Eliminar la medición del 2026-09-15?                 │
│                                                        │
│  72.2 kg · 17.2 % grasa · 42.2 % músculo               │
│                                                        │
│  Es la más reciente. La referencia pasará a ser la del  │
│  2026-08-19 (73.1 kg, 18.0 %), y el gasto basal subirá  │
│  de 1 661 a 1 665 kcal.                                 │
│                                                        │
│                    [ Cancelar ]  [ Eliminar ]          │
└────────────────────────────────────────────────────────┘
```

---

## 7. P5 — Alimentos y P7 — Combo

**Propósito**: administrar el catálogo. Es una pantalla de mantenimiento, de uso poco frecuente.

**Requerimientos**: RF-A1 a RF-A6.

```
┌───────────────────────────────────────────────────────────────────────────┐
│  Alimentos                             [ Todos | Con macros | De afuera ] │
│                                                          [+ Nuevo]        │
├───────────────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────────────────────┐  │
│  │ Alimento                Porción         kcal   P     C     G    ⋯  │  │
│  ├─────────────────────────────────────────────────────────────────────┤  │
│  │ Pan Los Sorchantes      2 reb. (50 g)   127   4.3  25.0  1.1   ✎  │  │
│  │ Requeson light          18 g             46   4.0   0.8  3.0   ✎  │  │
│  │ Pechuga s/piel cruda    250 g           285  57.0   0.0  6.5   ✎  │  │
│  │ Rudy doble sin fritas   1               950    —     —    —    ✎  │  │
│  │                                              ↑ de afuera, sin macros│  │
│  └─────────────────────────────────────────────────────────────────────┘  │
│                                                                           │
│  COMBOS                                                  [+ Nuevo]        │
│  Desayuno completo    Pan + Requeson + Leche + Queso + Manzana    393  ✎  │
│  Merienda completa    Pan + Requeson + Leche + Yogur + Mandarina  389  ✎  │
│  Almuerzo/cena pollo  Pechuga + Arroz 60 g + Zanahoria + Aceite   565  ✎  │
│  Almuerzo/cena cuadril Cuadril + Arroz 60 g + Zanahoria + Aceite  655  ✎  │
└───────────────────────────────────────────────────────────────────────────┘
```

| Elemento | Comportamiento |
|---|---|
| Filtros | Todos, con macros, de afuera |
| Formulario de alta y edición | Al guardar, valida coherencia entre kcal y macros con tolerancia del 10 % y advierte sin bloquear (RF-A3) |
| Baja | Es baja lógica. El alimento desaparece de los buscadores pero los registros históricos lo conservan (RF-A4, RN-08) |
| Aviso de edición | Al editar un alimento ya usado, se informa: *"Los días ya registrados no cambian."* (CA-10) |
| Columna Origen | Indica de dónde salió cada alimento: plantilla, manual, Open Food Facts, USDA o estimación de modelo (RF-A9) |
| P7 — Combo | Nombre, tipo de comida sugerido y lista de alimentos con cantidades. Las kcal se calculan (RF-A6) |

---

## 7b. P9 — Búsqueda nutricional asistida

**Propósito**: cargar un alimento que no está en el catálogo sin tener que buscar sus macros a mano.

**Requerimientos**: RF-A7 a RF-A11, RN-10. Decisión de arquitectura: D-09.

```
┌───────────────────────────────────────────────────────────────────────────┐
│  Buscar información nutricional                                    [ ✕ ]  │
├───────────────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────┐                      │
│  │ tarta de jamón y queso, una porción             │  [ Buscar ]          │
│  └─────────────────────────────────────────────────┘                      │
│                                                                           │
│  Consultando… Open Food Facts ✓  ·  USDA ✓  ·  modelo de lenguaje ⋯       │
│                                                                           │
│  RESULTADOS                                                               │
│  ┌─────────────────────────────────────────────────────────────────────┐  │
│  │ ● Tarta de jamón y queso                    Open Food Facts         │  │
│  │   1 porción (150 g)   ·  385 kcal   P 16.2   C 28.4   G 22.1        │  │
│  │   ✓ Coherente: los macros dan 384 kcal, 0.3 % de desvío             │  │
│  │                                              [ Usar este ]           │  │
│  ├─────────────────────────────────────────────────────────────────────┤  │
│  │ ○ Tarta de jamón y queso, casera        estimación del modelo        │  │
│  │   1 porción (180 g)   ·  460 kcal   P 19     C 32     G 26          │  │
│  │   ⚠ Desvío 8 %: los macros dan 438 kcal y declara 460               │  │
│  │   Es una estimación, no una etiqueta. Revisá antes de aceptar.       │  │
│  │                                              [ Usar este ]           │  │
│  └─────────────────────────────────────────────────────────────────────┘  │
│                                                                           │
│  Al elegir uno se abre el alta de alimento con los campos ya cargados,     │
│  para que los edites antes de guardar. Nada se guarda automáticamente.     │
└───────────────────────────────────────────────────────────────────────────┘
```

### 7b.1 Comportamiento

| Aspecto | Definición |
|---|---|
| Disparo | Solo por acción explícita del usuario. Nunca automático ni mientras escribe (RNF-15) |
| Cascada | Consulta en el orden de D-09 y muestra de qué fuente viene cada resultado. Las fuentes no disponibles se listan con su motivo |
| Validación | Cada resultado trae el desvío de RN-10 calculado y visible. Un desvío mayor al 15 % se marca como no confiable y exige revisión manual |
| Confirmación | Elegir un resultado abre el formulario de alta con los campos precargados y editables. El guardado es un segundo paso deliberado (RF-A8) |
| Trazabilidad | El alimento guardado retiene su origen y la fecha de obtención (RNF-16) |
| Privacidad | Se envía solo el texto que escribió el usuario. Ningún dato de peso, mediciones ni historial (RNF-14) |
| Cancelación | La búsqueda se puede cancelar mientras está en curso |

### 7b.2 Estados

| Estado | Presentación |
|---|---|
| Sin conexión | El botón queda deshabilitado con el motivo. La carga manual sigue disponible con normalidad (CA-17) |
| Sin clave configurada | Las fuentes que la requieren se listan como no disponibles, con un enlace a P6 para configurarla |
| Sin resultados | Se ofrece el alta manual con el texto buscado ya puesto como nombre |
| Todos los resultados con desvío alto | Se muestran igual, marcados como no confiables, con el detalle del cálculo de RN-10 a la vista |
| Error del servicio | Mensaje concreto de qué fuente falló, y los resultados de las que sí respondieron |

---

## 8. P6 — Ajustes

**Propósito**: es la pantalla que hace que el sistema sea de cada persona. Todo lo que en la versión 1.0 era una constante del código vive acá.

**Requerimientos**: RF-E1 a RF-E4, RF-I3 a RF-I12, RF-G3, RF-G4.

Cinco pestañas, para no poner treinta campos en una sola vista:

```
┌───────────────────────────────────────────────────────────────────────────┐
│  Ajustes    [ Perfil ] [ Objetivo ] [ Tipos y categorías ] [ Cálculo ]     │
│             [ Cuenta ]                                                    │
├───────────────────────────────────────────────────────────────────────────┤
│  PERFIL                                                                   │
│  Nombre              [ Nicolás              ]  (opcional)                 │
│  Altura              [ 178 ] cm                                           │
│  Fecha nacimiento    [ 1990-01-01 ]                                       │
│  Sexo                ( • ) Masculino   (   ) Femenino   (   ) Prefiero no │
│                                                          decirlo         │
│  Actividad fuera del gym                                                  │
│   (   ) Sedentario, trabajo de escritorio            × 1.2                │
│   ( • ) Algo de movimiento diario                    × 1.35               │
│   (   ) Trabajo de pie o muy activo                  × 1.5                │
│   (   ) Otro valor:  [ 1.42 ]                                             │
│  El entrenamiento no entra acá: se registra aparte en cada día.           │
└───────────────────────────────────────────────────────────────────────────┘
```

```
┌───────────────────────────────────────────────────────────────────────────┐
│  Ajustes › Tipos y categorías                                             │
│  Nada de esto viene fijo. Renombrá, reordená o desactivá lo que no uses.   │
├───────────────────────────────────────────────────────────────────────────┤
│  TIPOS DE COMIDA                                    [ + Agregar ]         │
│  ⣿ 1  Desayuno              activo    ✎                                   │
│  ⣿ 2  Media mañana          activo    ✎                                   │
│  ⣿ 3  Almuerzo              activo    ✎                                   │
│  ⣿ 4  Merienda              activo    ✎                                   │
│  ⣿ 5  Cena                  activo    ✎                                   │
│  ⣿ 6  Extra                 activo    ✎                                   │
│     (arrastrá para reordenar)                                             │
│                                                                           │
│  TIPOS DE ACTIVIDAD          CATEGORÍAS DE ALIMENTO      GRUPOS MUSCULARES│
│  ⣿ Gym            ✎          ⣿ Comida            ✎       ⣿ Pecho        ✎ │
│  ⣿ Cardio         ✎          ⣿ Acompañamiento    ✎       ⣿ Espalda      ✎ │
│  ⣿ Deporte        ✎          ⣿ Vegetal           ✎       ⣿ Pierna       ✎ │
│  [ + Agregar ]               ⣿ Fruta             ✎       ⣿ Hombro       ✎ │
│                              ⣿ Extra             ✎       ⣿ Bíceps       ✎ │
│                              ⣿ Restaurante       ✎       ⣿ Tríceps      ✎ │
│                              [ + Agregar ]               [ + Agregar ]    │
└───────────────────────────────────────────────────────────────────────────┘
```

```
┌───────────────────────────────────────────────────────────────────────────┐
│  Ajustes › Cálculo                                                        │
│  Estos valores afectan todos los cálculos. Los de fábrica son los más      │
│  aceptados; cambialos solo si tenés un motivo.                            │
├───────────────────────────────────────────────────────────────────────────┤
│  METABOLISMO BASAL                                                        │
│   ( • ) Katch-McArdle — usa tu masa magra medida        1 661 kcal        │
│   (   ) Mifflin-St Jeor — usa peso, altura, edad y sexo 1 690 kcal        │
│   (   ) Valor medido, lo ingreso yo:   [      ] kcal                      │
│  Ahora mismo se usa Katch-McArdle sobre la medición del 2026-09-15.       │
│                                                                           │
│  CONSTANTES FISIOLÓGICAS                            de fábrica            │
│  kcal por kilo de grasa corporal   [ 7700 ]         7700                  │
│  kcal por kilo de tejido magro     [ 1500 ]         1500                  │
│                                                                           │
│  LÍMITES DE SEGURIDAD DEL DÉFICIT                                         │
│  Máximo % del gasto diario         [ 25   ] %       25 %                  │
│  Máximo % del peso por semana      [ 0.75 ] %       0.75 %                │
│  Con tus datos, el techo queda en 498 kcal/día.                           │
│                                                                           │
│  UMBRALES DE CONFIABILIDAD                                                │
│  Cobertura mínima para comparar    [ 50 ] %         50 %                  │
│    con la báscula en el dashboard                                         │
│  Cobertura mínima para usar el     [ 70 ] %         70 %                  │
│    gasto calibrado en lugar de la fórmula                                 │
│                                                                           │
│  [ Restaurar valores de fábrica ]                    [ Guardar cambios ]   │
└───────────────────────────────────────────────────────────────────────────┘
```

```
┌───────────────────────────────────────────────────────────────────────────┐
│  Ajustes › Cuenta                                                         │
├───────────────────────────────────────────────────────────────────────────┤
│  Correo         nicolas@ejemplo.com                                       │
│  Cuenta creada  2026-09-16                                                │
│                                                                           │
│  CONTRASEÑA                                                               │
│  Actual  [            ]   Nueva  [            ]   Repetir [            ]  │
│  [ Cambiar contraseña ]                                                   │
│                                                                           │
│  TUS DATOS                                                                │
│  1 240 comidas · 86 sesiones de gym · 34 de cardio · 6 mediciones         │
│  [ Exportar todo a JSON ]   [ Importar desde JSON ]                       │
│  [ Importar plantillas ]  ← lo mismo que se ofreció al crear la cuenta    │
│                                                                           │
│  ELIMINAR LA CUENTA                                                       │
│  Se borra todo y no hay vuelta atrás. Exportá antes si querés guardarlo.   │
│  Escribí tu correo para confirmar:  [                        ]            │
│  [ Eliminar mi cuenta y todos mis datos ]                                 │
└───────────────────────────────────────────────────────────────────────────┘
```

### 8.1 Elementos

| Elemento | Comportamiento |
|---|---|
| Factor de actividad | Opciones en lenguaje claro con el multiplicador visible, más un valor libre (RF-I7). La nota aclara la corrección de P-03 |
| Tipos y categorías | Alta, renombrado, reordenado por arrastre y baja lógica. Desactivar no toca el histórico: los registros viejos siguen visibles y sumando (RF-I13, CA-27) |
| Método de metabolismo basal | Muestra el resultado de cada método con los datos reales del usuario, para que la elección sea informada. Si falta el % de grasa, indica que Katch-McArdle caería al respaldo (CA-30) |
| Constantes y límites | Cada campo muestra el valor de fábrica al lado, y el efecto del cambio se ve de inmediato ("el techo queda en 498") |
| Restaurar de fábrica | Vuelve la pestaña Cálculo a los valores por defecto, sin tocar datos |
| Eliminar cuenta | Exige escribir el correo, no solo confirmar. Borra en cascada (RF-I11, RN-12) |
| Importar | Pide confirmación explícita porque reemplaza el contenido actual de la cuenta |

### 8.2 Estados

| Estado | Presentación |
|---|---|
| Un tipo de comida en uso se quiere eliminar | Se ofrece desactivarlo en lugar de borrarlo, indicando en cuántos registros aparece |
| Método "valor medido" sin valor cargado | No se puede guardar: el campo es obligatorio en ese caso |
| Constante fisiológica fuera de rango razonable | Se acepta pero se advierte, con el valor de fábrica como referencia. La decisión es del usuario |
| Perfil incompleto y método Mifflin elegido | Se indica qué dato falta (altura, fecha de nacimiento o sexo) y que hasta entonces se usa Katch-McArdle |

---

## 8b. P8 — Dashboard

**Propósito**: responder "cómo me fue esta semana" o "este mes" en una sola vista, y contrastar lo que dice el registro contra lo que dice la báscula.

**Requerimientos**: RF-H1 a RF-H13, RN-11.

### 8b.1 Wireframe

```
┌───────────────────────────────────────────────────────────────────────────┐
│  Dashboard        [ Semana | Mes | 90 días | Rango ]      ‹  sep 2026  ›   │
├───────────────────────────────────────────────────────────────────────────┤
│                                                                           │
│  Déficit acumulado   Déficit medio    Cobertura      Adherencia           │
│  ┌────────┐          ┌───────┐        ┌─────────┐    ┌────────┐          │
│  │ 1 790  │          │  179  │        │ 10 / 30 │    │ 4 / 10 │          │
│  │  kcal  │          │kcal/día│       │  33 %   │    │  40 %  │          │
│  └────────┘          └───────┘        └─────────┘    └────────┘          │
│                                                                           │
│  DÉFICIT POR DÍA                                                          │
│  kcal                                                                     │
│  1200┤        █                                    █                      │
│   800┤                              █                                     │
│   400┤  █                                                                 │
│     0┼──█──░──░──▀──▀──█──▀──░──░──░──░──░──░──░──░──░──░──░──░──░──░──   │
│  -400┤           ▀        ▀                                               │
│ -1200┤                                █                                   │
│      └┬───┬───┬───┬───┬───┬───┬───┬───┬───┬────────────────────────       │
│       7   8   9  10  11  12  13  14  15  16  17 … 30                      │
│       █ déficit    ▀ superávit    ░ sin registro                          │
│                                                                           │
├───────────────────────────────────┬───────────────────────────────────────┤
│  PROMEDIOS DIARIOS                │  ENTRENAMIENTO                        │
│  Ingesta          1 953 kcal      │  Gym            4 sesiones · 1 310    │
│  Proteína           118 g         │  Cardio         3 sesiones ·   677    │
│  Carbos             172 g         │  Deporte        1 sesión  ·   550     │
│  Grasa               61 g         │  Total activ.   2 537 kcal            │
│  Sin macros          9 % de kcal  │  Días sin nada  3 de 10               │
│                                   │                                       │
│  Comida de afuera  2 días · 18 %  │                                       │
├───────────────────────────────────┴───────────────────────────────────────┤
│  ¿COINCIDE EL REGISTRO CON LA BÁSCULA?                                    │
│  ┌─────────────────────────────────────────────────────────────────────┐  │
│  │  Grasa perdida estimada por el déficit registrado      0.23 kg      │  │
│  │  Grasa perdida medida entre las mediciones del período    —          │  │
│  │                                                                     │  │
│  │  ⚠ Cobertura 33 %: por debajo del 50 % no se muestra la             │  │
│  │    comparación, porque la estimación se basa en muy pocos días.     │  │
│  └─────────────────────────────────────────────────────────────────────┘  │
│                                                                           │
│  PROGRESO HACIA EL 15 %                                                   │
│  ├████████████████████████████░░░░░░░░░░░░░┤  3.69 de 5.56 kg  ·  66 %    │
│  Faltan 1.87 kg de grasa                                                  │
└───────────────────────────────────────────────────────────────────────────┘
```

### 8b.2 Elementos

| Elemento | Comportamiento |
|---|---|
| Selector de período | Semana, mes, 90 días o rango libre. Las flechas navegan al período anterior y siguiente (RF-H1, RF-H2) |
| Tarjetas de cabecera | Déficit acumulado y medio, calculados solo sobre días con registro (RF-H3). Cobertura y adherencia se muestran como fracción y porcentaje, para que no se confundan con un promedio sobre el período completo |
| Gráfico de déficit diario | Tres estados visualmente distintos: déficit, superávit y sin registro. Es la representación directa de RN-07: un día vacío no es un día de cero |
| Clic en una barra | Navega a P1 en esa fecha |
| Promedios diarios | Ingesta y macros medios. Incluye qué porcentaje de las kcal del período vino de alimentos sin macros conocidos (RN-09) |
| Comida de afuera | Días con al menos un alimento marcado como de afuera, y su peso en kcal sobre el total (RF-H9) |
| Entrenamiento | Sesiones y kcal por tipo, más los días sin ninguna actividad (RF-H8) |
| Contraste con la báscula | El bloque de RN-11. Es la sección más importante de la pantalla: es donde el modelo se valida o se refuta |
| Progreso hacia el objetivo | Barra desde el punto de partida histórico hasta el objetivo, con lo recorrido y lo que falta (RF-H12) |

### 8b.3 Estados

| Estado | Presentación |
|---|---|
| Período sin ningún registro | Las tarjetas muestran "sin datos". El gráfico se dibuja con todas las barras en gris y un mensaje explicando que no hay nada cargado en el período |
| Cobertura menor al 50 % | El contraste con la báscula muestra solo la estimación, con el aviso de por qué no se compara (RN-11, CA-21) |
| Menos de dos mediciones en el período | El contraste indica que hacen falta dos mediciones dentro del rango elegido |
| Sin objetivo configurado | La adherencia y la barra de progreso se reemplazan por un enlace a P6 |
| Discrepancia mayor al 50 % | Se destaca con su interpretación: probable subestimación de porciones o sobreestimación del gasto |

---

## 8c. P10 — Ejercicios y P11 — Rutinas

**Propósito**: reemplazar las hojas `Ejercicio` y `EjercicioIA`, que hoy son cuatro bloques copiados a mano por rutina.

**Requerimientos**: RF-J1 a RF-J10, RN-13.

### 8c.1 P10 — Catálogo de ejercicios

```
┌───────────────────────────────────────────────────────────────────────────┐
│  Ejercicios                                          [ + Nuevo ejercicio ]│
│  Agrupados por el grupo muscular que definiste en Ajustes                  │
├───────────────────────────────────────────────────────────────────────────┤
│  Grupo muscular       Ejercicio                    Usado en    Sesiones   │
│  ─────────────────────────────────────────────────────────────────────────│
│  Espalda              Jalón                        2 rutinas       8   ✎  │
│                       Remo con barra T             2 rutinas       6   ✎  │
│                       Remo bajo / máquina          1 rutina        4   ✎  │
│  Pecho                Press banca                  1 rutina        5   ✎  │
│                       Press inclinado              2 rutinas       7   ✎  │
│                       Aperturas                    2 rutinas       7   ✎  │
│  Pierna               Sentadilla                   1 rutina        3   ✎  │
│                       Peso muerto rumano           2 rutinas       3   ✎  │
│  ...                                                                      │
└───────────────────────────────────────────────────────────────────────────┘
```

Las columnas "Usado en" y "Sesiones" existen para poder decidir con información antes de desactivar un ejercicio.

### 8c.2 P11 — Rutina

```
┌───────────────────────────────────────────────────────────────────────────┐
│  Rutinas       [ Mi rutina ● activa ] [ Rutina de 5 días ] [ + Nueva ]    │
├───────────────────────────────────────────────────────────────────────────┤
│  Mi rutina                          [ Duplicar ] [ Editar ] [ Eliminar ]  │
│  4 días · 26 ejercicios · activa desde el 2026-03-20                      │
│                                                                           │
│  ┌── Día 1 — Espalda + bíceps ────────────────────── [ Entrenar hoy ] ──┐ │
│  │  Gasto estimado: [ 330 ] kcal                                        │ │
│  │  Grupo      Ejercicio              Series  Reps    Peso              │ │
│  │  Espalda    Jalón                    4     6/10    65 kg +           │ │
│  │  Espalda    Remo con barra T         3     8/10    45 kg +           │ │
│  │  Espalda    Remo bajo / máquina      3    10/12    50 kg   (35 kg)   │ │
│  │  Hombro     Deltoides polea          3    12/15    30 kg             │ │
│  │  Bíceps     Curl barra               3     8/10    10 kg   (15 manc.)│ │
│  │  Bíceps     Curl martillo            2    10/12    10 kg             │ │
│  │  Bíceps     Curl inclinado           2    10/12    25 kg   (12.5)    │ │
│  │                                            [ + Agregar ejercicio ]   │ │
│  └──────────────────────────────────────────────────────────────────────┘ │
│                                                                           │
│  ┌── Día 2 — Pecho + tríceps ───────────────────────  [ Entrenar hoy ]──┐ │
│  │  Pecho      Press banca              4     6/10    27.5 kg +         │ │
│  │  Pecho      Press inclinado          3     8/10    45 kg   (Decl. 70)│ │
│  │  ...                                                                 │ │
│  └──────────────────────────────────────────────────────────────────────┘ │
│                                            [ + Agregar día ]              │
└───────────────────────────────────────────────────────────────────────────┘
```

### 8c.3 Elementos

| Elemento | Comportamiento |
|---|---|
| Selector de rutina | Varias rutinas coexisten; una está marcada como activa. Es lo que en la planilla son dos hojas separadas (RF-J4) |
| Series y reps | Se editan como tres números y se muestran como `4` y `6/10`. Nunca se guardan como el texto `4x6/10` (RF-J6, CA-31) |
| Peso | Campo numérico más un `+` opcional y una observación entre paréntesis. Acepta que se escriba `65+` y lo separa (RN-13, D-15) |
| Peso sin número | "Máximo máquina" se guarda como observación con el peso vacío. La fila queda fuera de los gráficos de progresión sin bloquear nada (CA-32) |
| Gasto estimado del día | Las kcal que se proponen al entrenar ese día. Se define una vez acá en lugar de cargarse en cada sesión, y por eso el catálogo de actividades no necesita entradas de gym (RF-K7, D-17). Puede quedar vacío |
| `Entrenar hoy` | Crea una sesión en P12 precargada con los ejercicios, pesos y kcal estimadas de ese día (RF-K2) |
| `Duplicar` | Copia la rutina completa para probar variantes sin perder la original (RF-J9) |
| Editar la rutina | Avisa que las sesiones ya registradas no cambian (RF-J10, CA-34) |

---

## 8d. P12 — Sesión de gym

**Propósito**: registrar lo que realmente se levantó, que es la información que la planilla no puede conservar porque sobrescribe la celda.

**Requerimientos**: RF-K1 a RF-K10.

```
┌───────────────────────────────────────────────────────────────────────────┐
│  Sesión de gym            ‹  2026-09-16  ›     Día 1 — Espalda + bíceps   │
├───────────────────────────────────────────────────────────────────────────┤
│  Precargado desde tu rutina. Cambiá solo lo que hiciste distinto.          │
│                                                                           │
│  Ejercicio            Rutina dice    Series  Reps   Peso        Últ. vez  │
│  ────────────────────────────────────────────────────────────────────────  │
│  Jalón                4x6/10 · 65    [4]     [8]    [67.5] ▲    65   ✓    │
│  Remo con barra T     3x8/10 · 45    [3]     [8]    [45  ]  =   45   ✓    │
│  Remo bajo/máquina    3x10/12 · 50   [3]     [10]   [50  ]  =   50   ✓    │
│  Deltoides polea      3x12/15 · 30   [3]     [12]   [30  ]  =   30   ✓    │
│  Curl barra           3x8/10 · 10    [3]     [8]    [10  ]  =   10   ✓    │
│  Curl martillo        2x10/12 · 10   [2]     [10]   [10  ]  =   10   ✓    │
│  Curl inclinado       2x10/12 · 25   [2]     [10]   [25  ]  =   25   ✓    │
│                                                                           │
│  [ + Agregar un ejercicio que no está en la rutina ]                      │
│                                                                           │
│  ▸ Detallar serie por serie   (opcional, para cuando no fueron iguales)    │
│                                                                           │
│  kcal de la sesión  [ 330 ]   ← del catálogo "Día 1", editable            │
│  Nota               [                                                  ]   │
│                                                                           │
│  ⚠ Ya registraste la actividad "Día 1 — Espalda + bíceps" (330 kcal) hoy.  │
│    Si guardás esta sesión con kcal, se contarían dos veces. [ Ver día ]    │
│                                                                           │
│                                        [ Cancelar ]  [ Guardar sesión ]   │
├───────────────────────────────────────────────────────────────────────────┤
│  PROGRESIÓN — Jalón                                                       │
│  kg                                                                       │
│  70┤                                              ●                       │
│  65┤                          ●───────●───────●                           │
│  60┤          ●───────●                                                   │
│  55┤  ●                                                                   │
│    └──┬───────┬───────┬───────┬───────┬───────┬───────┬──────             │
│      mar     abr     may     jun     jul     ago     sep                  │
│  De 55 a 67.5 kg en 6 meses. +22.7 %                                      │
└───────────────────────────────────────────────────────────────────────────┘
```

### 8d.1 Elementos

| Elemento | Comportamiento |
|---|---|
| Precarga | Los campos vienen con lo que prescribe la rutina y lo que se usó la última vez. En el caso habitual solo hay que confirmar (RF-K2, mitiga R-14) |
| Columna `Últ. vez` | El peso de la sesión anterior de ese ejercicio, con un tilde si se igualó o superó. Da contexto sin salir de la pantalla |
| Indicador ▲ = ▼ | Compara con la sesión anterior. Es la retroalimentación que hace que valga la pena registrar |
| Detalle por serie | Colapsado por defecto. Al abrirlo, permite peso y reps distintos por serie (RF-K4). No es obligatorio |
| kcal de la sesión | Se propone desde las kcal estimadas del día de rutina y se puede ajustar para este día (RF-K7, D-17) |
| Advertencia de doble conteo | Si el mismo día ya hay una actividad del catálogo que representa este entrenamiento, se avisa antes de guardar (RN-02, CA-38). Con D-17 el catálogo ya no debería tener entradas de gym, así que es una red de seguridad y no el caso habitual |
| Progresión | Gráfico del ejercicio seleccionado en la tabla. Solo aparece para ejercicios con peso numérico (RF-K5, CA-35) |

### 8d.2 Estados

| Estado | Presentación |
|---|---|
| Sin rutinas creadas | Se ofrece crear una rutina en P11, o registrar una sesión libre eligiendo ejercicios del catálogo |
| Sesión libre, sin día de rutina | La columna "Rutina dice" no aparece; se agregan ejercicios a mano |
| Primera sesión de un ejercicio | "Últ. vez" queda vacío y no hay gráfico de progresión todavía |
| Ejercicio con peso no numérico | Los campos de peso aceptan la observación; el gráfico indica que no hay serie numérica (CA-32) |
| Sesión ya registrada en esa fecha | Se abre en modo edición, con los valores guardados (RF-K8) |

---

## 8e. P13 — Cardio

**Propósito**: reemplazar la hoja `Cardio`, que hoy es un bloque copiado por semana y donde la progresión solo se ve leyendo hacia abajo.

**Requerimientos**: RF-L1 a RF-L8, RN-14.

```
┌───────────────────────────────────────────────────────────────────────────┐
│  Cardio                        [ + Sesión ]  [ + Protocolo de intervalos ]│
├───────────────────────────────────────────────────────────────────────────┤
│  PROGRESIÓN                                                               │
│  km/h                                                            % incl.  │
│  5.6┤                                    ●  velocidad              10     │
│  5.4┤                        ●───────●                              9     │
│  5.2┤            ●                                                  8     │
│  5.0┤  ●───────                    ▲───────▲  inclinación           7     │
│     └──┬───────┬───────┬───────┬───────┬────                              │
│       sem 1   sem 2   sem 3   sem 4   sem 5                               │
│                                                                           │
│  SESIONES                                                                 │
│  Fecha        Tipo        Min   Vel     Incl    kcal          Protocolo   │
│  ───────────────────────────────────────────────────────────────────────  │
│  2026-09-16   continuo    30    5.5     9 %     240           —      ✎ 🗑 │
│  2026-09-09   continuo    30    5.2     8 %     210 (180-240) —      ✎ 🗑 │
│  2026-09-07   continuo    30    5.0     7 %     190 (160-220) —      ✎ 🗑 │
│  2026-09-04   intervalos  19    var.    var.    260           HIIT   ✎ 🗑 │
│                                                                           │
│  PROTOCOLOS DE INTERVALOS                                                 │
│  ┌── HIIT jueves ─────────────────────── 19 min ── [ Registrar ] [ ✎ ] ──┐│
│  │  #  Bloque              Min    Vel    Incl    Repeticiones            ││
│  │  1  Entrada             5.0    5.0    5 %     ×1                      ││
│  │  2  Sprint              0.5    9.0    5 %     ×6   ┐ se alternan      ││
│  │  3  Recuperación        1.0    4.8    5 %     ×6   ┘                  ││
│  │  4  Salida              5.0    5.0    5 %     ×1                      ││
│  │     Total: 5 + 6×(0.5+1.0) + 5 = 19 min                               ││
│  └───────────────────────────────────────────────────────────────────────┘│
└───────────────────────────────────────────────────────────────────────────┘
```

### 8e.1 Elementos

| Elemento | Comportamiento |
|---|---|
| Progresión | Velocidad e inclinación en el tiempo, con doble eje. Es lo que la planilla obliga a reconstruir leyendo bloque por bloque (RF-L6) |
| kcal con rango | Cuando el usuario cargó un rango, se muestra el valor usado y el rango entre paréntesis. El gasto del día usa el punto medio (RN-14, CA-36) |
| Protocolos | Bloques ordenados con repeticiones. La duración total se calcula y se muestra desglosada, para que se pueda verificar (RF-L4, CA-37) |
| `Registrar` sobre un protocolo | Crea una sesión precargada con los parámetros del protocolo, ajustables (RF-L5) |
| Tipo de sesión | Continuo o por intervalos. En intervalos, velocidad e inclinación figuran como "var." porque cambian por bloque |

### 8e.2 Estados

| Estado | Presentación |
|---|---|
| Menos de dos sesiones | El gráfico de progresión se reemplaza por un mensaje explicando que hacen falta dos puntos |
| Sesión sin velocidad ni inclinación | Se acepta: son campos opcionales. La sesión suma kcal igual y queda fuera del gráfico de parámetros |
| kcal cargadas solo como rango | Se muestra el punto medio con una marca que lo aclara, y el rango completo al lado (RN-14) |
| Protocolo sin bloques | La duración es 0 y no se puede registrar hasta agregar al menos uno |

---

## 8f. P14, P15 y P16 — Ingreso, registro y bienvenida

**Requerimientos**: RF-I1, RF-I8, RNF-09.

```
┌─────────────────────────────────┐   ┌───────────────────────────────────┐
│  Ingresar                       │   │  Crear cuenta                     │
│                                 │   │                                   │
│  Correo    [                 ]  │   │  Correo      [                 ]  │
│  Contraseña[                 ]  │   │  Contraseña  [                 ]  │
│                                 │   │  Repetir     [                 ]  │
│           [ Ingresar ]          │   │  Nombre      [       ] (opcional) │
│                                 │   │                                   │
│  ¿No tenés cuenta? Crear una    │   │          [ Crear cuenta ]         │
└─────────────────────────────────┘   └───────────────────────────────────┘
```

Bienvenida, una sola vez, tras crear la cuenta:

```
┌───────────────────────────────────────────────────────────────────────────┐
│  Listo. ¿Por dónde querés empezar?                                        │
│                                                                           │
│  Todo lo que elijas acá es editable después. Nada queda fijo.             │
│                                                                           │
│  ○ Arrancar vacío                                                         │
│    Los catálogos quedan en blanco y vas cargando lo tuyo a medida         │
│    que lo necesitás.                                                      │
│                                                                           │
│  ● Importar una plantilla y editarla                                      │
│    ┌───────────────────────────────────────────────────────────────────┐  │
│    │ ☑ Tipos de comida        desayuno, media mañana, almuerzo, …      │  │
│    │ ☑ Categorías y grupos    vegetales, frutas · pecho, espalda, …    │  │
│    │ ☐ Alimentos              21 alimentos con macros verificados      │  │
│    │ ☐ Combos                 4 combinaciones habituales               │  │
│    │ ☐ Actividades            gym, cardio y deporte con kcal           │  │
│    │ ☐ Ejercicios y rutinas   26 ejercicios y una rutina de 4 días     │  │
│    └───────────────────────────────────────────────────────────────────┘  │
│                                                                           │
│                                       [ Omitir ]  [ Importar y empezar ]  │
└───────────────────────────────────────────────────────────────────────────┘
```

| Aspecto | Definición |
|---|---|
| Selección granular | Se elige qué importar, no todo o nada. Los tipos y categorías vienen marcados porque sin ellos no se puede registrar nada |
| Copia, no vínculo | Lo importado queda como datos propios, editables, sin relación con la plantilla (D-14, RF-G2b) |
| Reversible | La importación se puede repetir después desde Ajustes, y lo importado se puede borrar |
| Sin datos ajenos | Una cuenta nueva que omite todo queda completamente vacía (CA-26) |

---

## 9. Recorridos principales

### 9.0 Primer uso de una cuenta nueva

1. **P15** → correo, contraseña → Crear cuenta.
2. **P16** → elegir qué importar. Tipos de comida y categorías vienen marcados; el resto se decide.
3. → Importar y empezar → cae en **P1**, ya con lo mínimo para registrar.
4. Opcional: **P6 › Perfil** para altura, fecha de nacimiento y factor de actividad.
5. Opcional: **P4** para la primera medición, que es lo que habilita el metabolismo basal por masa magra.

Sin el paso 4 y 5 el sistema funciona igual: registra comidas y suma kcal. Lo que falta es el cálculo del gasto, y la interfaz lo indica en lugar de inventar un valor.

### 9.1 Cargar un día típico (OBJ-1, objetivo ≤ 5 interacciones)

1. Abrir la aplicación → cae en **P1** en la fecha de hoy.
2. Clic en un combo de desayuno.
3. Clic en un combo de almuerzo.
4. Clic en un combo de merienda.
5. Clic en la actividad del día.

El déficit del día queda calculado. Cinco clics, ninguna escritura de texto. Los combos y las actividades son los que el usuario definió: el recorrido no depende de cuáles sean.

### 9.1b Registrar un entrenamiento de gym

1. **P11** → sobre el día de la rutina que toca, clic en `Entrenar hoy`.
2. **P12** se abre precargada con los ejercicios, sus series prescritas y el peso de la última vez.
3. Corregir solo lo que cambió. En el caso habitual, uno o dos campos.
4. `Guardar sesión`.

Queda registrada la carga real de cada ejercicio, y la progresión se actualiza. Si el mismo día ya había una actividad del catálogo que representa ese entrenamiento, el paso 4 avisa del doble conteo antes de guardar (CA-38).

### 9.2 Registrar una medición y ver el impacto

1. **P4** → "Nueva" → fecha, peso, % de grasa, % de músculo → Guardar.
2. El metabolismo basal se recalcula (RF-C6) y **P2** y **P3** reflejan el cambio sin acción adicional.
3. **P3** actualiza el plan: nueva grasa a perder y nuevo déficit necesario.

### 9.3 Replantear el objetivo

1. **P3** → cambiar grasa objetivo a 14 % → el déficit necesario sube.
2. Si supera el techo, aparece la advertencia de RN-05.
3. Extender el plazo hasta que la validación pase a verde.
4. Confirmar para persistir el objetivo en el perfil.

## 10. Definiciones pendientes de la interfaz

| ID | Pendiente |
|---|---|
| PDI-01 | ¿La lista "Comido hoy" se agrupa por tipo de comida, o alcanza una lista plana? El wireframe asume agrupada |
| PDI-02 | ¿Hace falta registrar la hora de cada comida, o solo el tipo? El wireframe asume solo el tipo |
| PDI-03 | ¿Los bloques de comparación con la planilla son útiles a largo plazo, o solo durante la transición? |
| PDI-04 | En el dashboard, ¿hace falta un gráfico de macros a lo largo del período, o alcanza con los promedios? |
| PDI-05 | ¿El período por defecto del dashboard es la semana o el mes? Los wireframes muestran mes |
| PDI-06 | ¿La sesión de gym se registra desde P11 con `Entrenar hoy`, o también desde P1 como una actividad más? Duplicar el acceso puede confundir |
| PDI-07 | En P12, ¿el indicador contra la sesión anterior compara el peso, el volumen o ambos? El wireframe compara peso |
| PDI-08 | ¿Conviene que P10 y P11 sean dos pantallas, o una sola con el catálogo de ejercicios como panel lateral? |
| PDI-09 | ¿El menú agrupado en tres bloques es suficiente con 13 pantallas, o hace falta colapsar los grupos? |
