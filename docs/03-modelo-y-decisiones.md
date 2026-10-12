# kcal-tracker — Modelo de datos y decisiones técnicas

| | |
|---|---|
| Versión | 2.0 (borrador para revisión) |
| Estado | **Pendiente de aprobación** |
| Referencia | `01-requerimientos.md`, `02-pantallas.md` |
| Cambios sobre la 1.0 | Modelo multiusuario con aislamiento por filtro global (D-12, D-13), plantillas en lugar de semilla (D-14), tablas de ejercicios, rutinas, sesiones de gym y cardio, y catálogos de tipos configurables (D-15) |

---

## 1. Modelo de datos

### 1.1 Diagrama

Cinco áreas. **Toda** tabla marcada con `※` lleva `UsuarioId` obligatorio y cuelga de `Usuario` (RN-12).

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  CUENTA Y CONFIGURACIÓN                                                     │
│                                                                             │
│   ┌──────────────┐        ┌────────────────┐      ┌──────────────────┐      │
│   │   Usuario    │        │  Perfil     ※  │      │ Configuracion ※  │      │
│   ├──────────────┤ 1..1   ├────────────────┤      ├──────────────────┤      │
│   │ Id           ├───────►│ AlturaCm     ? │      │ KcalPorKgGrasa   │      │
│   │ Email UNIQUE │        │ FechaNac     ? │      │ KcalPorKgMagro   │      │
│   │ HashClave    │        │ Sexo         ? │      │ MetodoBmr        │      │
│   │ Nombre     ? │        │ FactorActiv    │      │ BmrMedido      ? │      │
│   │ CreadoEl     │        │ GrasaObjPct  ? │      │ PctGastoMax      │      │
│   └──────┬───────┘        │ PlazoSemanas   │      │ PctPesoSemanal   │      │
│          │                │ ProteinaObjG ? │      │ CobMinContraste  │      │
│          │                └────────────────┘      │ CobMinCalibrado  │      │
│          │                                        └──────────────────┘      │
│          │   catálogos de tipos, todos editables (RF-I3, RF-I4)             │
│          ├──►┌──────────────┐  ┌───────────────┐  ┌──────────────────┐      │
│          │   │ TipoComida ※ │  │TipoActividad ※│  │ CategoriaAlim. ※ │      │
│          │   ├──────────────┤  ├───────────────┤  ├──────────────────┤      │
│          │   │ Nombre       │  │ Nombre        │  │ Nombre           │      │
│          │   │ Orden        │  │ Orden         │  │ Orden            │      │
│          │   │ Activo       │  │ Activo        │  │ Activo           │      │
│          │   └──────────────┘  └───────────────┘  └──────────────────┘      │
│          │   ┌───────────────────┐                                          │
│          │   │ GrupoMuscular  ※  │  Nombre, Orden, Activo                   │
│          │   └───────────────────┘                                          │
└──────────┼──────────────────────────────────────────────────────────────────┘
           │
┌──────────┼──────────────────────────────────────────────────────────────────┐
│  ALIMENTACIÓN                                                               │
│          │                                                                  │
│   ┌──────▼───────┐      ┌──────────────┐         ┌──────────────┐           │
│   │  Alimento ※  │      │   Combo   ※  │  1..n   │ ComboItem    │           │
│   ├──────────────┤◄─────┤ Nombre       ├────────►│ AlimentoId   │           │
│   │ Nombre       │      │ TipoComidaId?│         │ Cantidad     │           │
│   │ Porcion      │      │ Activo       │         └──────────────┘           │
│   │ Kcal         │      └──────────────┘                                    │
│   │ Proteina  ?  │                                                          │
│   │ Carbos    ?  │      ┌──────────────┐  1..n   ┌──────────────────┐       │
│   │ Grasa     ?  │      │  Comida   ※  ├────────►│  ComidaItem      │       │
│   │ CategoriaId ?│      ├──────────────┤         ├──────────────────┤       │
│   │ EsAfuera     │      │ Fecha        │         │ AlimentoId     ? │       │
│   │ Activo       │◄─────┤ TipoComidaId │         │ Cantidad         │       │
│   │ Origen       │  ref │ Nota       ? │         │ ─── copias ───   │       │
│   │ ObtenidoEl ? │  info │ UNIQUE       │        │ Nombre, Porcion  │       │
│   └──────────────┘      │ (Usu,Fecha,  │        │ Kcal, P, C, G    │        │
│                         │  TipoComida) │        │ EsAfuera         │        │
│                         └──────────────┘        └──────────────────┘        │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│  GYM: la rutina prescribe, la sesión registra lo que pasó                    │
│                                                                             │
│   ┌───────────────┐         ┌────────────────┐        ┌──────────────────┐   │
│   │ Ejercicio  ※  │         │   Rutina    ※  │  1..n  │  RutinaDia       │   │
│   ├───────────────┤         ├────────────────┤───────►├──────────────────┤   │
│   │ Nombre        │         │ Nombre         │        │ Nombre           │   │
│   │ GrupoMuscId ? │         │ Descripcion  ? │        │ Orden            │   │
│   │               │         │                │        │ KcalEstimadas ?  │   │
│   │ Activo        │         │ EsActiva       │        └────────┬─────────┘   │
│   └───────┬───────┘         └────────────────┘                 │ 1..n        │
│           │                                                    ▼             │
│           │  ref               ┌───────────────────────────────────────┐     │
│           ├───────────────────►│  RutinaEjercicio                      │     │
│           │                    ├───────────────────────────────────────┤     │
│           │                    │ EjercicioId · Orden                   │     │
│           │                    │ Series · RepsMin · RepsMax            │     │
│           │                    │ PesoRef ? · PuedeMas · Observacion ?  │     │
│           │                    └───────────────────────────────────────┘     │
│           │                                                                  │
│           │  ref      ┌────────────────┐  1..n   ┌──────────────────────────┐│
│           └──────────►│ SesionGym   ※  ├────────►│  SesionEjercicio         ││
│                       ├────────────────┤         ├──────────────────────────┤│
│                       │ Fecha          │         │ EjercicioId ?            ││
│                       │ RutinaDiaId  ? │         │ Orden                    ││
│                       │ Nombre    ◄────┼─ copia  │ ─── copias ───           ││
│                       │ Kcal         ? │         │ Nombre · GrupoMuscular   ││
│                       │ Nota         ? │         │ Series · Reps            ││
│                       └────────────────┘         │ Peso ? · PuedeMas        ││
│                                                  │ Observacion ?            ││
│                                                  └───────────┬──────────────┘│
│                                                    0..n (opcional, RF-K4)    │
│                                                              ▼               │
│                                                  ┌──────────────────────────┐│
│                                                  │  SerieRealizada          ││
│                                                  │  Numero · Reps · Peso ?  ││
│                                                  └──────────────────────────┘│
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│  CARDIO Y OTRAS ACTIVIDADES                                                 │
│                                                                             │
│  ┌───────────────┐       ┌──────────────────┐      ┌──────────────────────┐ │
│  │ Actividad  ※  │       │ ProtocoloCardio※ │ 1..n │  BloqueCardio        │ │
│  ├───────────────┤       ├──────────────────┤─────►├──────────────────────┤ │
│  │ Nombre        │       │ Nombre           │      │ Orden · Minutos      │ │
│  │ TipoActivId   │       │ Descripcion    ? │      │ Velocidad ?          │ │
│  │ Kcal          │       │ Activo           │      │ Inclinacion ?        │ │
│  │ Minutos     ? │       └──────────────────┘      │ Repeticiones         │ │
│  │ Activo        │                │ ref            └──────────────────────┘ │
│  └───────┬───────┘                │                                         │
│          │ ref                    ▼                                         │
│          ▼               ┌──────────────────────────┐                       │
│  ┌────────────────┐      │   SesionCardio        ※  │                       │
│  │ ActividadLog ※ │      ├──────────────────────────┤                       │
│  ├────────────────┤      │ Fecha · ProtocoloId  ?   │                       │
│  │ Fecha          │      │ Minutos · Velocidad ?    │                       │
│  │ ActividadId  ? │      │ Inclinacion ?            │                       │
│  │ Nombre  ◄──────┼copia │ Kcal · KcalMin ? KcalMax?│                       │
│  │ Tipo    ◄──────┼copia │ Nota ?                   │                       │
│  │ Kcal    ◄──────┼copia └──────────────────────────┘                       │
│  │ Minutos      ? │                                                         │
│  └────────────────┘                                                         │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│  COMPOSICIÓN CORPORAL                                                       │
│   ┌───────────────────────────────────────────────────────────┐             │
│   │ Medicion  ※    Fecha · PesoKg · GrasaPct ? · MusculoPct ? │             │
│   │                Visceral ?      UNIQUE (UsuarioId, Fecha)  │             │
│   └───────────────────────────────────────────────────────────┘             │
└─────────────────────────────────────────────────────────────────────────────┘

  ? = admite nulo     ※ = lleva UsuarioId, aislada por RN-12
```

Las tres capas del gym son la clave del diseño y conviene no confundirlas:

| Capa | Qué representa | Ejemplo |
|---|---|---|
| `Ejercicio` | El movimiento como concepto, independiente de cualquier rutina | "Press banca", pecho |
| `RutinaEjercicio` | Lo que la rutina **prescribe** | 4 series de 6 a 10 reps con 20 kg de referencia |
| `SesionEjercicio` | Lo que **realmente se hizo** ese día | 4 series de 8 con 22.5 kg |

La planilla mezcla las tres en una sola celda, que es la razón por la que no puede mostrar progresión: al sobrescribir el peso pierde lo anterior.

### 1.2 Tablas

#### Convención común a todas las tablas de datos de usuario

Para no repetirlo en cada tabla:

| Columna | Tipo | Nulo | Notas |
|---|---|---|---|
| Id | INTEGER PK | no | autoincremental |
| UsuarioId | INTEGER FK | no | a `Usuario`. Borrado en cascada. Presente en todas las tablas marcadas `※` |

Todo índice de una tabla `※` empieza por `UsuarioId` (RNF-18). Las tablas hijas (`ComidaItem`, `ComboItem`, `RutinaDia`, `RutinaEjercicio`, `SesionEjercicio`, `SerieRealizada`, `BloqueCardio`) heredan el usuario de su padre y no repiten la columna, porque su ciclo de vida está atado por cascada.

#### `Usuario`

| Columna | Tipo | Nulo | Notas |
|---|---|---|---|
| Id | INTEGER PK | no | |
| Email | TEXT | no | único, normalizado a minúsculas |
| HashClave | TEXT | no | derivación con sal (RNF-09). Nunca la contraseña |
| Nombre | TEXT | sí | para saludar en la interfaz |
| CreadoEl | TEXT | no | |

#### `Configuracion` ※

Una fila por usuario. Es lo que hace que ningún número del cálculo esté en el código (RNF-17).

| Columna | Tipo | Nulo | Por defecto | Notas |
|---|---|---|---|---|
| KcalPorKgGrasa | REAL | no | 7700 | SUP-2, RF-I5 |
| KcalPorKgMagro | REAL | no | 1500 | SUP-3 |
| MetodoBmr | INTEGER | no | katchMcArdle | enum: katchMcArdle, mifflin, medido (RF-I6) |
| BmrMedido | REAL | sí | — | obligatorio si `MetodoBmr = medido` |
| PctGastoMax | REAL | no | 0.25 | techo de RN-05 |
| PctPesoSemanal | REAL | no | 0.0075 | techo de RN-05 |
| CobMinContraste | REAL | no | 0.50 | umbral de RN-11 |
| CobMinCalibrado | REAL | no | 0.70 | umbral de RF-F7 |

#### `TipoComida` ※, `TipoActividad` ※, `CategoriaAlimento` ※, `GrupoMuscular` ※

Los cuatro comparten forma. Reemplazan a los enums fijos de la versión 1.0.

| Columna | Tipo | Nulo | Notas |
|---|---|---|---|
| Nombre | TEXT | no | libre |
| Orden | INTEGER | no | define el orden de presentación |
| Activo | INTEGER | no | baja lógica. Desactivar no afecta el histórico (RF-I13) |

Índice único: `(UsuarioId, Nombre)`.

#### `Alimento` ※

| Columna | Tipo | Nulo | Notas |
|---|---|---|---|
| Nombre | TEXT | no | |
| Porcion | TEXT | no | descripción de la porción de referencia, ej. "2 rebanadas (50 g)" |
| Kcal | REAL | no | por porción |
| Proteina | REAL | **sí** | gramos por porción. Nulo = desconocido, no cero (RN-09) |
| Carbos | REAL | **sí** | idem |
| Grasa | REAL | **sí** | idem |
| CategoriaAlimentoId | INTEGER FK | sí | a `CategoriaAlimento` del mismo usuario |
| EsAfuera | INTEGER | no | 0 o 1. Comida de restaurante o delivery |
| Activo | INTEGER | no | baja lógica (RF-A4) |
| Origen | INTEGER | no | enum: plantilla, manual, OpenFoodFacts, USDA, modeloLenguaje (RF-A9, RNF-16) |
| ObtenidoEl | TEXT | sí | fecha de obtención cuando el origen es externo |

Índice: `(UsuarioId, Nombre)`.

#### `Comida` ※

| Columna | Tipo | Nulo | Notas |
|---|---|---|---|
| Fecha | TEXT | no | `YYYY-MM-DD`, sin hora (RNF-06) |
| TipoComidaId | INTEGER FK | no | a `TipoComida` del mismo usuario |
| Nota | TEXT | sí | texto libre opcional |

Restricción única: `(UsuarioId, Fecha, TipoComidaId)`. Índice: `(UsuarioId, Fecha)`.

La ausencia de fila para una fecha es lo que representa "día sin registro" (RN-07). No existe fila con totales en cero.

#### `ComidaItem`

| Columna | Tipo | Nulo | Notas |
|---|---|---|---|
| Id | INTEGER PK | no | |
| ComidaId | INTEGER FK | no | cascada al borrar la comida |
| AlimentoId | INTEGER FK | **sí** | referencia informativa. Nulo si el alimento fue borrado físicamente |
| Cantidad | REAL | no | número de porciones |
| Nombre | TEXT | no | **copia** al momento de registrar |
| Porcion | TEXT | no | **copia** |
| Kcal | REAL | no | **copia**, por porción |
| Proteina | REAL | sí | **copia** |
| Carbos | REAL | sí | **copia** |
| Grasa | REAL | sí | **copia** |
| EsAfuera | INTEGER | no | **copia** |

Las columnas marcadas como copia implementan RN-08: son la razón por la que editar el catálogo no altera el histórico (CA-10). Es desnormalización deliberada; la justificación está en la decisión D-05.

#### `Combo` ※ y `ComboItem`

`Combo`: Nombre, TipoComidaId sugerido (nulo permitido), Activo.
`ComboItem`: ComboId (FK, cascada), AlimentoId (FK), Cantidad.

A diferencia de `ComidaItem`, aquí **no** se copian macros: un combo es una plantilla, y debe reflejar el catálogo vigente en el momento en que se usa.

#### `Actividad` ※

| Columna | Tipo | Nulo | Notas |
|---|---|---|---|
| Nombre | TEXT | no | |
| TipoActividadId | INTEGER FK | no | a `TipoActividad` del mismo usuario |
| Kcal | REAL | no | estimación del usuario (SUP-5) |
| Minutos | REAL | sí | |
| Activo | INTEGER | no | baja lógica |

#### `ActividadLog` ※

| Columna | Tipo | Nulo | Notas |
|---|---|---|---|
| Fecha | TEXT | no | `YYYY-MM-DD` |
| ActividadId | INTEGER FK | sí | referencia informativa |
| Nombre | TEXT | no | copia |
| Tipo | TEXT | no | copia del nombre del tipo |
| Kcal | REAL | no | copia, ajustable por sesión (RF-C1c) |
| Minutos | REAL | sí | copia |

Índice: `(UsuarioId, Fecha)`.

Las kcal se copian igual que en `ComidaItem`: editar el catálogo de actividades no altera las sesiones ya registradas (RF-C1d, CA-18). Y como es una copia, se puede ajustar para una sesión puntual sin tocar el catálogo.

#### `Ejercicio` ※

| Columna | Tipo | Nulo | Notas |
|---|---|---|---|
| Nombre | TEXT | no | "Press banca", "Jalón" |
| GrupoMuscularId | INTEGER FK | sí | a `GrupoMuscular` del mismo usuario |
| Activo | INTEGER | no | baja lógica |

Índice único: `(UsuarioId, Nombre)`.

#### `Rutina` ※ y `RutinaDia`

`Rutina`: Nombre, Descripcion (nulo), EsActiva. Varias rutinas pueden coexistir; a lo sumo una tiene `EsActiva = 1` (RF-J4).

`RutinaDia`: RutinaId (FK, cascada), Nombre, Orden, KcalEstimadas (nulo). Es el "Día 1", "Día 2" de la planilla, pero con nombre libre: "Espalda y bíceps" vale igual.

`KcalEstimadas` es la decisión D-17: el gasto del entrenamiento se define una vez acá y se propone al abrir cada sesión, en lugar de vivir en el catálogo de actividades. Es nulo si el usuario prefiere no estimarlo.

#### `RutinaEjercicio`

Lo que la rutina **prescribe**.

| Columna | Tipo | Nulo | Notas |
|---|---|---|---|
| RutinaDiaId | INTEGER FK | no | cascada |
| EjercicioId | INTEGER FK | no | |
| Orden | INTEGER | no | |
| Series | INTEGER | no | el `4` de `4x6/10` |
| RepsMin | INTEGER | no | el `6` |
| RepsMax | INTEGER | sí | el `10`. Nulo cuando es un número fijo de reps |
| PesoRef | REAL | **sí** | kg. Nulo cuando no aplica un número (RN-13) |
| PuedeMas | INTEGER | no | el `+` de la planilla (RN-13) |
| Observacion | TEXT | sí | "Máximo máquina", "Declinado 70", "12.5 mancuerna" |

`Series`, `RepsMin` y `RepsMax` separados son lo que permite calcular volumen (RF-K10) y lo que la planilla no puede hacer con el texto `4x6/10` (RF-J6, CA-31).

#### `SesionGym` ※ y `SesionEjercicio`

Lo que **se hizo**.

`SesionGym`: Fecha, RutinaDiaId (FK, nulo si fue una sesión libre), Nombre (copia del día de rutina), Kcal (nulo, se propone desde `RutinaDia.KcalEstimadas` y queda editable para ese día), Nota. Índice `(UsuarioId, Fecha)`.

| `SesionEjercicio` | Tipo | Nulo | Notas |
|---|---|---|---|
| SesionGymId | INTEGER FK | no | cascada |
| EjercicioId | INTEGER FK | sí | referencia informativa |
| Orden | INTEGER | no | |
| Nombre | TEXT | no | **copia** |
| GrupoMuscular | TEXT | sí | **copia** |
| Series | INTEGER | no | series completadas |
| Reps | INTEGER | sí | repeticiones representativas |
| Peso | REAL | sí | kg usados (RN-13) |
| PuedeMas | INTEGER | no | |
| Observacion | TEXT | sí | |

Las copias cumplen RN-08: editar la rutina no cambia lo registrado (RF-J10, CA-34).

#### `SerieRealizada`

Desglose **opcional** de RF-K4, para cuando las series no fueron iguales entre sí.

| Columna | Tipo | Nulo | Notas |
|---|---|---|---|
| SesionEjercicioId | INTEGER FK | no | cascada |
| Numero | INTEGER | no | 1, 2, 3… |
| Reps | INTEGER | no | |
| Peso | REAL | sí | |

Si existen filas, `SesionEjercicio.Series`, `Reps` y `Peso` pasan a ser el resumen derivado de ellas: series es el conteo, y reps y peso son los de la serie más pesada. Si no existen, valen los del propio `SesionEjercicio`. Esto permite el caso rápido sin renunciar al detalle.

#### `ProtocoloCardio` ※ y `BloqueCardio`

`ProtocoloCardio`: Nombre, Descripcion, Activo. Es la "Opción HIIT jueves" de la planilla.

| `BloqueCardio` | Tipo | Nulo | Notas |
|---|---|---|---|
| ProtocoloId | INTEGER FK | no | cascada |
| Orden | INTEGER | no | |
| Minutos | REAL | no | duración del bloque |
| Velocidad | REAL | sí | km/h |
| Inclinacion | REAL | sí | % |
| Repeticiones | INTEGER | no | cuántas veces se repite el bloque. 1 por defecto |

Duración total = `Σ Minutos × Repeticiones` (RF-L4, CA-37).

#### `SesionCardio` ※

| Columna | Tipo | Nulo | Notas |
|---|---|---|---|
| Fecha | TEXT | no | |
| ProtocoloId | INTEGER FK | sí | nulo si fue cardio continuo suelto |
| Minutos | REAL | no | |
| Velocidad | REAL | sí | km/h |
| Inclinacion | REAL | sí | % |
| Kcal | REAL | no | el valor que entra al gasto del día (RN-14) |
| KcalMin | REAL | sí | extremo inferior del rango, si el usuario lo estimó así |
| KcalMax | REAL | sí | extremo superior |
| Nota | TEXT | sí | |

Índice: `(UsuarioId, Fecha)`. Cuando se cargan `KcalMin` y `KcalMax` sin `Kcal`, esta última toma el punto medio (RN-14, CA-36).

#### `Medicion` ※

| Columna | Tipo | Nulo | Notas |
|---|---|---|---|
| Fecha | TEXT | no | único junto a `UsuarioId` (RF-D2) |
| PesoKg | REAL | no | |
| GrasaPct | REAL | sí | RF-D4 permite medición de solo peso |
| MusculoPct | REAL | sí | |
| Visceral | REAL | sí | |

No se almacenan `GrasaKg` ni `MagraKg`: se derivan al leer (RF-D3). Es lo que evita la inconsistencia P-12.

#### `Perfil` ※

Una fila por usuario: AlturaCm, FechaNacimiento, Sexo, FactorActividad, GrasaObjetivoPct, PlazoSemanas, ProteinaObjetivoG. Todos nulos salvo `FactorActividad` y `PlazoSemanas`, para que se pueda empezar a usar el sistema sin completar el perfil entero.

### 1.3 Consultas derivadas (no son tablas)

| Vista | Definición |
|---|---|
| `ResumenDia(fecha)` | Ingesta, macros, kcal sin macros, gasto y déficit. Devuelve "sin registro" si no hay `Comida` para esa fecha |
| `MedicionActual` | La `Medicion` de fecha máxima del usuario |
| `GastoCalibrado` | Resultado de RN-06 con su cobertura |
| `ProgresionEjercicio(ejercicioId)` | Serie temporal de `SesionEjercicio.Peso` con su fecha, para los que tienen peso numérico (RF-K5, CA-35) |
| `ProgresionCardio` | Serie temporal de velocidad, inclinación y kcal de `SesionCardio` (RF-L6) |
| `KcalActividadDelDia(fecha)` | Suma de `ActividadLog`, `SesionGym.Kcal` y `SesionCardio.Kcal` de la fecha. Detecta solapamiento para la advertencia de RN-02 |

## 2. Arquitectura

```
src/KcalTracker/
├── Program.cs                     arranque, DI, migraciones
├── Dominio/                       entidades y enums, sin dependencias
│   ├── Usuario.cs
│   ├── Configuracion.cs           constantes y umbrales por usuario
│   ├── TipoComida.cs              los cuatro catálogos de tipos
│   ├── TipoActividad.cs
│   ├── CategoriaAlimento.cs
│   ├── GrupoMuscular.cs
│   ├── Alimento.cs
│   ├── Comida.cs
│   ├── ComidaItem.cs
│   ├── Combo.cs
│   ├── Actividad.cs
│   ├── ActividadLog.cs
│   ├── Ejercicio.cs
│   ├── Rutina.cs                  + RutinaDia, RutinaEjercicio
│   ├── SesionGym.cs               + SesionEjercicio, SerieRealizada
│   ├── ProtocoloCardio.cs         + BloqueCardio
│   ├── SesionCardio.cs
│   ├── Medicion.cs
│   └── Perfil.cs
├── Calculo/                       lógica pura, testeable sin base ni UI
│   ├── Metabolismo.cs             RN-01, RN-02
│   ├── BalanceDiario.cs           RN-03
│   ├── PlanObjetivo.cs            RN-04, RN-05
│   ├── Calibracion.cs             RN-06
│   ├── Progresion.cs              RF-K5, RF-K10, RF-L6
│   └── DuracionProtocolo.cs       RF-L4
├── Datos/
│   ├── KcalDbContext.cs           filtro global por usuario (D-13)
│   ├── IUsuarioActual.cs          quién es el usuario de la petición
│   ├── Plantillas/                conjuntos opcionales, de solo lectura (D-14)
│   │   ├── PlantillaBase.cs
│   │   └── PlantillaExcel.cs      la derivada de Alimentación.xlsx
│   └── Migraciones/
├── Nutricion/                     búsqueda asistida (D-09)
│   ├── IProveedorNutricional.cs
│   ├── ProveedorEnCascada.cs
│   ├── OpenFoodFactsProveedor.cs
│   ├── UsdaProveedor.cs
│   ├── ModeloLenguajeProveedor.cs
│   └── ValidadorSugerencia.cs     RN-10, sin dependencias externas
├── Servicios/                     orquestan Datos + Calculo para la UI
│   ├── ServicioDia.cs
│   ├── ServicioPeriodo.cs         RN-11, métricas del dashboard
│   ├── ServicioObjetivo.cs
│   └── ServicioRespaldo.cs
├── Pages/                         P1 a P8, cada una .cshtml + .cshtml.cs
│   ├── Dashboard.cshtml
│   ├── Hoy.cshtml
│   ├── Actividad.cshtml
│   ├── Objetivo.cshtml
│   ├── Mediciones.cshtml
│   ├── Alimentos.cshtml
│   ├── Ajustes.cshtml
│   └── Shared/                    layout y parciales reutilizables
│       ├── _Layout.cshtml
│       ├── _ResumenDia.cshtml     parcial que devuelven los handlers AJAX
│       └── _PlanObjetivo.cshtml
└── wwwroot/
    ├── app.css
    └── lib/htmx.min.js            refrescos parciales (D-16)

tests/KcalTracker.Tests/           casos CA-01 a CA-38
```

`Calculo/` no referencia EF Core ni Blazor: recibe valores y devuelve valores (RNF-11). Es lo que permite verificar los criterios de aceptación como pruebas unitarias.

## 3. Decisiones técnicas

### D-01 — Plataforma: .NET 10 con ASP.NET Core Razor Pages

**Elegido**: Razor Pages sobre .NET 10, un solo proyecto web.

**Alternativas**:
- *Blazor Server*: daría interactividad inmediata sin escribir JavaScript, pero introduce una conexión SignalR permanente y un modelo de estado en el servidor que es maquinaria de más para este caso.
- *Blazor WebAssembly*: correría en el navegador y necesitaría una API HTTP intermedia para llegar a SQLite. Una capa entera de serialización sin beneficio.
- *React + TypeScript*, *Python*: descartados por preferencia de stack.

**Motivo**: el usuario eligió Razor Pages. Es el modelo más estándar y predecible de ASP.NET Core, sin dependencias de runtime más allá del framework, y con acceso directo a EF Core desde el page model.

**Consecuencia sobre la interactividad**: las pantallas P1 y P3 recalculan valores al vuelo (agregar un alimento, mover el plazo del objetivo). Con Razor Pages puro cada cambio sería un POST con recarga completa, lo que en P1 resulta molesto.

**Mitigación**: los recálculos se resuelven con *handlers* que devuelven el fragmento de HTML a reemplazar, invocados con htmx. Resuelto en D-16.

### D-02 — Persistencia: SQLite vía EF Core

**Elegido**: archivo SQLite en `%LOCALAPPDATA%\kcal-tracker\kcal.db`.

**Motivo**: acordado con el usuario. Un archivo único, respaldable copiándolo, consultable con cualquier cliente SQL, sin servidor que administrar. Cubre R-05.

**Consecuencia**: el esquema evoluciona por migraciones de EF Core (RNF-12). SQLite tiene soporte limitado de `ALTER TABLE`, que EF Core resuelve recreando tablas; hay que revisar cada migración generada antes de aplicarla.

**Restricción que impone D-12**: el acceso a datos no usa nada específico de SQLite. Sin SQL crudo con funciones propias del motor, sin tipos que no existan en Postgres. Cambiar de proveedor debe ser cambiar la cadena de conexión y regenerar migraciones.

### D-03 — Fechas como TEXT en formato ISO

**Elegido**: `DateOnly` en el dominio, almacenado como TEXT `YYYY-MM-DD`.

**Motivo**: es el tipo correcto semánticamente (un día no tiene hora) y ordena lexicográficamente igual que cronológicamente. Evita por completo la clase de error de P-01: nunca hay ambigüedad entre número y fecha.

**Alternativa descartada**: almacenar como número de serie o como `DATETIME` con hora en cero, que invita a errores de zona horaria.

### D-04 — Macros nulos en lugar de cero

**Elegido**: `Proteina`, `Carbos` y `Grasa` admiten nulo.

**Motivo**: "no sé cuánta proteína tiene este Rudy" y "este alimento tiene cero proteína" son afirmaciones distintas. Colapsarlas subestimaría la proteína en los días de comida afuera. Implementa RN-09.

**Consecuencia**: toda suma de macros debe decidir explícitamente qué hace con los nulos, y la interfaz debe mostrar el renglón de kcal sin macros conocidos.

### D-05 — Copiar macros al registrar (desnormalización deliberada)

**Elegido**: `ComidaItem` y `ActividadLog` guardan copia de los valores, no solo la referencia.

**Motivo**: es la corrección directa de P-11, el defecto más insidioso de la planilla. Hoy, mover una fila en la tabla de alimentos cambia en silencio el valor de días pasados. Un registro histórico debe ser un hecho inmutable.

**Contra**: rompe la tercera forma normal y duplica datos. Aceptado a conciencia: la integridad temporal vale más que la normalización en un sistema cuyo propósito es medir cambios a lo largo del tiempo.

**Alternativa descartada**: versionar los alimentos con vigencias y resolver por fecha. Es más elegante y bastante más complejo, sin beneficio para un usuario único.

### D-06 — La lógica de cálculo no conoce la base

**Elegido**: `Calculo/` recibe primitivos y registros, y devuelve resultados. Sin `DbContext`, sin inyección, sin async.

**Motivo**: las reglas de negocio son el corazón del producto y donde un error pasa inadvertido. Aisladas, los criterios de aceptación se escriben como pruebas unitarias directas.

### D-07 — Escuchar solo en localhost, con autenticación desde el inicio

> **Revisada en la versión 2.0.** La 1.0 no tenía login, porque asumía un solo usuario en su propia máquina. Con la decisión PD-10 eso cambió.

**Elegido**: la aplicación se ata a `127.0.0.1` **y** exige ingresar con correo y contraseña (D-12, D-13).

**Motivo**: atarse a `localhost` sigue siendo correcto mientras no haya despliegue, porque evita exponer el servicio sin HTTPS. Pero la autenticación no se posterga: es lo que permite que el aislamiento por usuario (RN-12) tenga un sujeto desde la primera línea de código, en lugar de agregarse encima de un sistema que ya asumió que hay uno solo.

**Consecuencia**: hay una pantalla de ingreso incluso corriendo local, lo que es una molestia menor. A cambio, acceder desde otro dispositivo de la red o publicarlo en un servidor deja de requerir rediseño: solo HTTPS y recuperación de contraseña por correo.

### D-08 — Sin importador genérico del Excel

**Elegido**: no hay parser de `.xlsx`. Los datos de partida se ofrecen como plantillas en código C# (D-14), y todo lo demás se carga desde la interfaz.

**Motivo**: un parser genérico con manejo de las hojas en matriz sería trabajo considerable y desechable, para un archivo que se importaría una sola vez. Las plantillas quedan además como documentación ejecutable de los datos de partida.

**Consecuencia**: las 21 semanas de bitácora en texto libre no se cargan. Resuelto en PD-04: el registro diario arranca desde cero y el histórico de comidas queda solo como documento de consulta en el anexo. Las mediciones de composición corporal sí están disponibles en la plantilla derivada del Excel, porque son las que habilitan la calibración de RN-06 desde el primer día.

### D-09 — Búsqueda nutricional: fuentes en cascada, no un LLM solo

**El problema**: hace falta cubrir dos casos muy distintos. Un producto envasado ("Yogur Viva Light") tiene una etiqueta con valores exactos y verificables. Un plato preparado ("milanesa napolitana del club con fritas") no tiene etiqueta y solo puede estimarse.

**Elegido**: una cascada de fuentes, cada una para lo que sabe hacer.

| Orden | Fuente | Cubre | Costo | Confiabilidad |
|---|---|---|---|---|
| 1 | **Open Food Facts** | Productos envasados, incluidas marcas de Uruguay. API abierta, sin clave | Gratis | Alta: son etiquetas transcritas |
| 2 | **USDA FoodData Central** | Alimentos genéricos y crudos: cortes de carne, cereales, verduras | Gratis, requiere clave gratuita | Muy alta: es referencia de laboratorio |
| 3 | **Modelo de lenguaje** | Platos preparados y comida de restaurante, donde no existe etiqueta | Por consulta | Baja: estima. Requiere RN-10 y confirmación |

La interfaz consulta en ese orden y se detiene en el primer resultado con desvío aceptable según RN-10. Si solo responde el modelo de lenguaje, la sugerencia se presenta marcada como estimación.

**Motivo**: usar un LLM para buscar el dato de un producto que tiene etiqueta pública es la peor opción disponible: paga por adivinar algo que otra fuente sabe con certeza. Reservarlo para lo que ninguna base cubre aprovecha su fortaleza real, que es interpretar descripciones en lenguaje natural.

**Diseño**: una interfaz `IProveedorNutricional` con una implementación por fuente y un `ProveedorEnCascada` que las ordena. Agregar, quitar o reordenar fuentes no toca el resto del sistema, y cubre R-08.

```csharp
public interface IProveedorNutricional
{
    string Nombre { get; }
    bool Disponible { get; }
    Task<IReadOnlyList<SugerenciaNutricional>> BuscarAsync(string texto, CancellationToken ct);
}

public sealed record SugerenciaNutricional(
    string Nombre, string Porcion,
    double Kcal, double? Proteina, double? Carbos, double? Grasa,
    OrigenDato Origen, double DesvioMacros, bool EsEstimacion);
```

**Consecuencia**: `Alimento` gana dos columnas, `Origen` y `ObtenidoEl`, para cumplir RNF-16 y poder auditar más adelante de dónde salió cada cifra.

**Pendiente PD-07**: elegir el proveedor de modelo de lenguaje. Para este uso la diferencia entre proveedores es menor, porque la respuesta se valida con RN-10 y la confirma el usuario. El criterio que pesa es cuál tenés a mano con clave ya configurada.

### D-10 — La clave de API fuera del repositorio

**Elegido**: se lee de variable de entorno o del gestor de secretos de .NET (`dotnet user-secrets`). Nunca en `appsettings.json`, que sí se versiona.

**Motivo**: RNF-13. Un secreto en el repositorio es un secreto filtrado, incluso en un repositorio privado.

**Consecuencia**: si no hay clave configurada, el proveedor correspondiente reporta `Disponible = false` y la interfaz explica qué falta, en lugar de fallar con un error de autenticación (CA-17).

### D-12 — Multiusuario en el modelo desde el día uno, despliegue local por ahora

**Elegido**: el esquema, la autenticación y el acceso a datos son multiusuario desde la primera migración. La aplicación se ejecuta en `localhost` con SQLite.

**Alternativas**:
- *Un solo usuario ahora, multiusuario después*: agregar `UsuarioId` a quince tablas con datos ya cargados es una migración riesgosa, y obliga a revisar cada consulta escrita hasta ese momento. El costo de hacerlo al inicio es una columna y un filtro; el de hacerlo después es una auditoría completa.
- *Desplegar en un servidor ya en v1*: requiere hosting, dominio, certificado, correo transaccional para recuperar contraseñas y una política de privacidad. Nada de eso agrega valor mientras el sistema todavía se está definiendo.

**Motivo**: es la decisión que mantiene abiertas las dos puertas. Cuesta poco ahora y evita el retrabajo de R-16.

**Consecuencia**: hay pantalla de ingreso incluso corriendo local. Es una molestia menor y a cambio el camino a publicarlo queda despejado: cambiar el proveedor de EF Core a Postgres, poner HTTPS y agregar recuperación de contraseña por correo.

La autenticación usa cookie de sesión y el hasher de ASP.NET Core Identity (PBKDF2 con sal), no Identity completo. Roles, claims store, tokens y doble factor quedan fuera: el esquema documentado es la tabla `Usuario` de cinco columnas, y eso es todo lo que hace falta mientras no haya recuperación de contraseña por correo.

### D-13 — Aislamiento por filtro global, no por consulta

**Elegido**: un filtro de consulta global de EF Core sobre cada entidad con `UsuarioId`, alimentado por el usuario de la petición.

```csharp
// KcalDbContext.OnModelCreating — el filtro se aplica a toda entidad EntidadDeUsuario
modelBuilder.Entity<T>().HasQueryFilter(e => e.UsuarioId == UsuarioIdActual);
```

**Alternativa descartada**: filtrar en cada consulta con `.Where(x => x.UsuarioId == actual)`. Funciona hasta que alguien escribe una consulta y se olvida, y ese olvido no falla: devuelve datos de otra persona. Es un defecto silencioso en datos de salud, el peor escenario posible (R-11).

**Motivo**: convierte el aislamiento en una propiedad del modelo en lugar de una disciplina del programador. Una consulta que omita el filtro devuelve vacío, no datos ajenos. CA-24 verifica precisamente eso.

**Consecuencias que hay que tener presentes**:

| Aspecto | Detalle |
|---|---|
| Escritura | El filtro aplica a lecturas, no a inserciones. `UsuarioId` se asigna en un interceptor de `SaveChanges`, no a mano en cada alta |
| Referencias cruzadas | Asignar un `AlimentoId` de otra cuenta no lo impide el filtro. Se valida al guardar (CA-25) |
| Migraciones y plantillas | Corren fuera del contexto de una petición y necesitan `IgnoreQueryFilters()` de forma explícita y consciente |
| Pruebas | `IUsuarioActual` se sustituye en las pruebas, lo que permite verificar el aislamiento con dos usuarios en la misma base |

### D-14 — Plantillas que se copian, en lugar de semilla obligatoria

**Elegido**: las plantillas son conjuntos de datos de solo lectura definidos en código. Al importar una, sus filas se **copian** a la cuenta del usuario, con `Origen = plantilla`. Desde ese instante son datos suyos, sin vínculo con el origen.

**Alternativas**:
- *Semilla que se inserta al crear la base*: es lo que tenía la versión 1.0, y metía los alimentos de una persona concreta en la cuenta de cualquiera.
- *Catálogo global compartido que todos consultan*: ahorra duplicación, pero rompe RN-12 y crea la pregunta de quién puede editarlo. Un usuario corrigiendo las kcal del pan se las cambiaría a todos.

**Motivo**: responde a que el sistema sea de cada persona. Copiar duplica filas, y eso está bien: son decenas de registros por cuenta y la independencia vale mucho más que el espacio.

**Consecuencia**: la plantilla derivada de `Alimentación.xlsx` pasa de ser el punto de partida obligatorio a ser una opción entre otras (RF-G2c). Sus 14 valores corregidos siguen documentados en el anexo, porque son la evidencia de P-01.

### D-15 — Series y repeticiones estructuradas, peso con tres campos

**Elegido**: `Series`, `RepsMin` y `RepsMax` como enteros separados; y para el peso, la tripleta `Peso` numérico opcional, `PuedeMas` booleano y `Observacion` de texto (RN-13).

**Alternativa descartada**: guardar `"4x6/10"` y `"27.5+"` como texto, que es lo que hace la planilla. Es cómodo de cargar y no sirve para nada más: no se puede graficar la progresión, ni calcular volumen, ni comparar dos sesiones.

**Motivo**: el texto `4x6/10` es un dato estructurado disfrazado de cadena. Descomponerlo cuesta tres columnas y habilita RF-K5, RF-K10 y CA-31. Y el campo de peso numérico que no acepta texto es la barrera que evita repetir P-01, donde `27.5` terminó guardado como la fecha `2026-05-27`.

**Consecuencia**: hay que formatear al mostrar (`4x6/10`, `27.5+`, `Máximo máquina`) y parsear con tolerancia al cargar, aceptando que el usuario escriba `27.5+` en el campo y separándolo en número y bandera.

### D-11 — El dashboard calcula en el momento, sin tabla de resúmenes

**Elegido**: las métricas de período (RN-11) se calculan al vuelo con una consulta por período. No hay tabla de agregados precalculados.

**Motivo**: el volumen es diminuto (365 días por año, decenas de items por día) y SQLite resuelve estas agregaciones en milisegundos. Una tabla de resúmenes introduciría el problema clásico de invalidación: si se edita una comida vieja, hay que recordar recalcular. Ese es exactamente el tipo de defecto que tenía la planilla.

**Consecuencia**: RNF-10 se cumple sin caché. Si algún día el volumen creciera, una vista materializada sería un cambio local a `ServicioPeriodo`.

### D-16 — Refrescos parciales con htmx

**Elegido**: htmx, servido como archivo local en `wwwroot/lib/`. Los handlers de Razor Pages devuelven vistas parciales, y el markup declara con atributos qué se pide y dónde se inserta.

**Alternativas**:
- *`fetch` escrito a mano*: cero dependencias, pero cada pantalla con interacción repite el mismo bloque de código para llamar, esperar, reemplazar el nodo y manejar el error. Con más de diez pantallas interactivas, es donde se acumulan las inconsistencias.
- *Recarga completa por POST*: lo más simple, descartado porque en P1 se agregan varios alimentos seguidos y perder el foco y la posición en cada uno hace incómodo el uso diario, que es justamente el que tiene que ser sin fricción (RNF-01).

**Motivo**: htmx encaja con Razor Pages porque el servidor sigue devolviendo HTML, que es lo que Razor ya sabe hacer. La lógica de cálculo se queda entera en C# y no aparece una segunda implementación en JavaScript, que es el riesgo real de las dos alternativas anteriores. Son 14 kB y no requiere paso de build.

**Consecuencia**: cada fragmento que se refresca necesita su vista parcial (`_ResumenDia.cshtml`, `_PlanObjetivo.cshtml` y similares), lo que es una disciplina de organización más que un costo. Se sirve desde `wwwroot` y no desde un CDN, para que la aplicación funcione sin conexión (RNF-06).

### D-17 — Las kcal del entrenamiento viven en la rutina, no en el catálogo de actividades

**Elegido**: `RutinaDia.KcalEstimadas` guarda el gasto estimado del entrenamiento. Al abrir una sesión se propone ese valor y queda editable para ese día concreto (RF-K7). El catálogo de actividades queda reservado para lo que no es gym ni cardio: un partido de fútbol, una caminata larga, andar en bicicleta.

**Alternativas**:
- *Cargar las kcal en cada sesión, sin valor propuesto*: más preciso si la duración varía mucho, pero es un dato más para completar cada vez, y el costo de registrar es lo que hace que la gente abandone (R-14).
- *Tomarlas del catálogo de actividades, como en la versión 1.0*: obliga a mantener una entrada "Día 1 — Espalda y bíceps" en el catálogo **y** el día de rutina con el mismo nombre. Es la duplicación que genera el doble conteo de P-03.

**Motivo**: la segunda alternativa es la que había, y su problema no es de comodidad sino estructural: si el mismo entrenamiento existe como actividad de catálogo y como día de rutina, registrar ambos es un error esperable y no una distracción del usuario. Moviendo el dato a la rutina, el catálogo deja de tener entradas que representen entrenamientos, y la causa desaparece. La advertencia de RN-02 queda como red de seguridad, no como defensa principal.

**Consecuencia**: `Actividad` y `ActividadLog` siguen existiendo con su propósito acotado. CA-38 sigue siendo válido, pero verifica un caso que ahora hay que provocar a propósito.

## 4. Plan de implementación

Cada etapa deja el sistema compilando y con sus pruebas en verde.

| # | Etapa | Entrega verificable |
|---|---|---|
| 1 | `Usuario`, cookie de sesión, filtro global de D-13 y migración inicial | Se crea una cuenta y se ingresa. **Pruebas CA-23 a CA-25 antes de seguir** |
| 2 | Resto del dominio y el esquema completo | La base se crea con todas las tablas, todas aisladas por usuario |
| 3 | `Configuracion` y los cuatro catálogos de tipos, con P6 Ajustes | Nada del cálculo queda en el código: pruebas CA-27 a CA-30 |
| 4 | `Calculo/` con pruebas de CA-01 a CA-06 | Las reglas RN-01 a RN-05 verificadas sin UI, leyendo la configuración |
| 5 | Plantillas y pantalla de bienvenida, pruebas CA-13, CA-14, CA-26 | Una cuenta nueva arranca vacía y puede importar lo que elija |
| 6 | P4 Mediciones | Alta, edición y borrado con recálculo (CA-19) |
| 7 | P5 Alimentos y P7 Combo, con prueba CA-10 | Catálogo administrable sin afectar el histórico |
| 8 | P1 Hoy, con pruebas CA-07 a CA-09 y CA-11 | Registro diario funcionando de punta a punta |
| 9 | P2 Actividad con catálogo editable, prueba CA-18 | Gasto por capas visible, y kcal por actividad definidas por el usuario |
| 10 | P10 Ejercicios y P11 Rutinas, pruebas CA-31 a CA-33 | La rutina de la planilla se carga estructurada |
| 11 | P12 Sesión de gym y progresión, pruebas CA-34, CA-35 | Queda el historial de cargas que la planilla no puede tener |
| 12 | P13 Cardio con protocolos, pruebas CA-36 a CA-38 | Velocidad, inclinación e intervalos registrados y con progresión |
| 13 | P3 Objetivo, con `Calibracion` y prueba CA-12 | El producto responde la pregunta central |
| 14 | `ServicioPeriodo` y P8 Dashboard, pruebas CA-20 a CA-22 | Vista consolidada de semana y mes, con el contraste entre estimado y medido |
| 15 | Búsqueda asistida: `ValidadorSugerencia` con CA-15 y CA-16, luego Open Food Facts, USDA y modelo de lenguaje | Alimentos nuevos se cargan sin tipear macros a mano |
| 16 | Exportar e importar JSON por cuenta, y baja de cuenta | Respaldo cubierto y RF-I11 |

Dos etapas están ubicadas a propósito:

- La **1** va primero y no avanza sin sus pruebas. El aislamiento entre cuentas es la única propiedad del sistema cuyo incumplimiento no se nota hasta que ya causó daño (R-11). Construir quince pantallas sobre un aislamiento no verificado es la forma más cara de descubrir el problema.
- La **15** implementa el validador de RN-10 antes de conectar cualquier fuente externa. Es la defensa contra R-06 y no debe quedar para después.

## 5. Verificación

| Tipo | Alcance |
|---|---|
| Pruebas unitarias | Los criterios de cálculo sobre `Calculo/`, sin base de datos: CA-01 a CA-09, CA-15, CA-16, CA-20 a CA-22, CA-28 a CA-31, CA-36, CA-37 |
| Pruebas de integración | Aislamiento entre cuentas con dos usuarios sobre SQLite en memoria (CA-23 a CA-25), plantillas, inmutabilidad del histórico (CA-10, CA-18, CA-27, CA-34) y consultas derivadas |
| Verificación manual | Los recorridos de la sección 9 de `02-pantallas.md` |

La prueba de aislamiento merece una nota: no alcanza con verificar que la cuenta A no vea los datos de B a través de la interfaz. CA-24 exige una prueba que consulte el `DbContext` **sin** filtrar por usuario y compruebe que igual devuelve vacío. Es la única forma de verificar que la garantía está en el modelo y no en la disciplina de quien escribió la consulta.
