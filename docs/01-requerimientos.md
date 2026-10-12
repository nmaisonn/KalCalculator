# kcal-tracker — Documento de requerimientos

| | |
|---|---|
| Versión | 2.0 (borrador para revisión) |
| Fecha | 2026-09-16 |
| Estado | **Pendiente de aprobación** |
| Autor | Análisis de `Alimentación.xlsx` |
| Alcance de esta versión | v1 — sistema multiusuario y configurable que cubre alimentación, entrenamiento de gym, cardio, composición corporal y objetivos |
| Cambios sobre la 1.0 | Multiusuario y personalización total (RF-I, RN-12), módulo de ejercicios y rutinas (RF-J), registro de entrenamiento con progresión de cargas (RF-K), cardio con parámetros e intervalos (RF-L), plantillas opcionales en lugar de semilla fija (RF-G2) |

---

## 1. Contexto

El producto nace del análisis de una planilla Excel real (`Alimentación.xlsx`, 7 hojas, 21 semanas de bitácora) con la que una persona siguió su alimentación, entrenamiento y composición corporal desde marzo de 2026. La planilla cumplió su función y produjo un resultado medible, pero llegó a su límite estructural y está degradando los datos que acumula.

Esa planilla cumple dos papeles en este documento: define **qué información** hay que poder manejar (sección 5.3) y **qué errores** hay que evitar (sección 2). No define los datos del sistema: el producto es multiusuario y cada persona carga y configura lo suyo.

Resultados que la planilla documenta, entre el 2026-03-20 y el 2026-09-15, y que sirven como caso de prueba de los cálculos:

| Métrica | Inicio | Actual | Variación |
|---|---|---|---|
| Peso | 73.9 kg | 72.2 kg | −1.7 kg |
| Grasa corporal | 21.8 % (16.11 kg) | 17.2 % (12.42 kg) | **−3.69 kg** |
| Masa muscular | 39.4 % (29.12 kg) | 42.2 % (30.47 kg) | **+1.35 kg** |

Es recomposición corporal exitosa: se perdió más del doble de grasa que de peso, porque parte se compensó con músculo nuevo.

## 2. Problemas identificados en la solución actual

Cada problema se numera para poder trazarlo contra los requerimientos que lo resuelven.

| ID | Problema | Evidencia |
|---|---|---|
| P-01 | Excel convierte decimales en fechas y corrompe datos en silencio | 14 celdas: `SeguimientoKcal` D2/F2/F5/D9/F11/F13/D15/D16/D17/D19/F19, `Ejercicio` D14, `Cardio` G5/G6 |
| P-02 | Gasto energético fijo y sin calibrar | Celda `L7` = 2250 kcal hardcodeadas |
| P-03 | Doble conteo del ejercicio | Las 2250 kcal ya incluyen actividad, y las fórmulas les suman el gym encima |
| P-04 | Los días sin registro generan déficit inventado | Semana 2 reporta 9310 kcal de déficit acumulado; el real es 310 |
| P-05 | Fórmulas ad hoc por celda, frágiles ante inserciones | `=(C11/2)+(C13/2)+C16+C18+C20`, `=Round(L6/3,0)` |
| P-06 | Datos en matriz semana × día, no escalable | 21 bloques copiados a mano en `Bitacora` |
| P-07 | Totales que omiten filas | `Ingesta Total` usa `SUM(B26:B30)` y deja fuera Ejercicio y Cardio |
| P-08 | Ingesta y composición corporal desconectadas | `SeguimientoKcal` y `Estadísticas` no se cruzan |
| P-09 | Comidas como texto libre, no consultable | "2 tostadas con requeson, una manzana verde y un taza de leche" |
| P-10 | Catálogos duplicados y desconectados | `Opciones` (menú) vs `SeguimientoKcal` (macros) |
| P-11 | Editar el catálogo altera el histórico | Las fórmulas referencian filas; mover una fila cambia días pasados |
| P-12 | Inconsistencia en cálculos manuales | `Estadísticas` fila 12 usa 12.158 kg donde la fila 6 dice 13.158 |

Consecuencia agregada de P-02, P-03 y P-04: la planilla cree tener un déficit de **436 kcal/día**, mientras el déficit implícito en las mediciones reales es de **148 kcal/día**. Sobreestima por un factor de **3**.

> Análisis completo con la evidencia: `docs/anexos/analisis-excel.md` y el canvas de análisis.

## 3. Objetivos del producto

| ID | Objetivo | Métrica de éxito |
|---|---|---|
| OBJ-1 | Registrar la comida del día más rápido que en la planilla | Un día típico se carga en ≤ 5 interacciones |
| OBJ-2 | Calcular el gasto energético por capas, sin doble conteo | Resuelve P-02, P-03 |
| OBJ-3 | Decir cuánto déficit hace falta para un % de grasa objetivo | Resuelve P-08 |
| OBJ-4 | Preservar la integridad del histórico | Resuelve P-01, P-05, P-07, P-11 |
| OBJ-5 | Calibrar el gasto real contra las mediciones propias | Resuelve P-02 |
| OBJ-6 | No inventar datos donde no hay registro | Resuelve P-04 |
| OBJ-7 | Que cada persona pueda adaptar el sistema a su forma de comer y entrenar | Ningún catálogo, tipo, constante ni umbral está fijo en el código |
| OBJ-8 | Cubrir toda la información que hoy vive en la planilla, y agregar lo que la planilla no puede hacer | Las 7 hojas tienen destino en el modelo; ver la sección 5.4 |

## 4. Usuarios y contexto de uso

- **Multiusuario**: cada persona tiene su cuenta, sus catálogos, sus rutinas y sus datos. Ningún dato es compartido entre cuentas. No hay roles ni administración: todas las cuentas son equivalentes.
- **Nada cableado**: el sistema no trae valores propios de una persona en particular. Los catálogos, los tipos de comida, los tipos de actividad, las constantes fisiológicas y los umbrales son datos del usuario, no constantes del código (RF-I, RN-12).
- **Punto de partida elegible**: al crear la cuenta, el usuario decide si arranca con los catálogos vacíos o si importa una plantilla de la que después edita lo que quiera (RF-I8).
- **Despliegue**: por ahora se ejecuta local en `localhost`, pero el modelo de datos y la autenticación están diseñados para multiusuario desde el inicio, de modo que publicarlo en un servidor no obligue a rehacer nada (D-12).
- **Dispositivo**: navegador de escritorio. No se contempla uso móvil en v1.
- **Frecuencia**: varias veces al día para cargar comidas; en cada entrenamiento para el gym; semanal o mensual para mediciones.
- **Conectividad**: no requiere internet, salvo la búsqueda nutricional asistida, que es opcional.

> El análisis de `Alimentación.xlsx` se conserva porque define **qué** información hay que poder manejar y **qué problemas** hay que evitar. Pero sus valores concretos ya no son la semilla del sistema: son una de las plantillas disponibles.

## 5. Alcance

### 5.1 En alcance (v1)

| Módulo | Incluye |
|---|---|
| Cuentas | Registro, ingreso, cambio de contraseña y baja de cuenta. Aislamiento total de datos entre cuentas |
| Personalización | Tipos de comida, tipos de actividad, categorías de alimentos, grupos musculares, constantes fisiológicas, umbrales y fórmula de metabolismo basal: todo editable por el usuario |
| Catálogo de alimentos | Alta, edición y baja lógica de alimentos con macros; alimentos de restaurante solo con kcal |
| Búsqueda nutricional asistida | Buscar los macros de un alimento no habitual en fuentes externas y en un modelo de lenguaje, como sugerencia a confirmar |
| Combos | Plantillas de varios alimentos cargables de una vez, definidas por el usuario |
| Registro diario | Cargar y quitar comidas por tipo de comida; ver kcal, macros y déficit del día |
| Dashboard de período | Vista consolidada de una semana, un mes o un rango: déficit acumulado, adherencia, macros medios y contraste entre lo estimado y lo medido |
| Actividad | Catálogo editable de actividades con kcal definidas por el usuario; registro por fecha |
| Ejercicios y rutinas | Catálogo de ejercicios por grupo muscular; rutinas de varios días con series, rango de repeticiones y peso de referencia. Varias rutinas coexistentes |
| Registro de entrenamiento | Sesiones de gym con el peso y las repeticiones realmente hechos, y la progresión de cargas por ejercicio en el tiempo |
| Cardio | Sesiones continuas con tiempo, velocidad, inclinación y kcal, y protocolos por intervalos con sus bloques. Progresión de los parámetros en el tiempo |
| Mediciones | Alta, edición y baja de peso, % de grasa, % de músculo y grasa visceral por fecha |
| Perfil | Altura, fecha de nacimiento, sexo, factor de actividad, % de grasa objetivo y plazo |
| Objetivo | Cálculo del peso objetivo, grasa a perder, déficit diario necesario y validación del plazo |
| Calibración | Cálculo del gasto real a partir de la ingesta registrada y el cambio de composición |
| Plantillas | Conjuntos opcionales de alimentos, combos, actividades, ejercicios y rutinas que el usuario importa si quiere, y edita libremente después |
| Respaldo | Exportar e importar los datos de la cuenta a JSON |

### 5.2 Fuera de alcance (v1)

| Ítem | Motivo | Fase prevista |
|---|---|---|
| Despliegue público en un servidor con HTTPS | El modelo y la autenticación quedan preparados (D-12), pero publicarlo implica hosting, dominio, correo transaccional y política de privacidad | v2 |
| Recuperación de contraseña por correo | Requiere un servicio de correo saliente, que solo tiene sentido al desplegar | v2 |
| Progresión de cargas asistida y volumen por grupo muscular | El registro de sesiones queda diseñado para soportarlo; la lógica de sugerencia se agrega cuando haya historial real | v2 |
| Roles, administración y cuentas compartidas | Todas las cuentas son equivalentes e independientes | descartado |
| Uso en móvil, PWA, funcionamiento sin señal | Se definió uso solo desde escritorio | v2 |
| Importador genérico del `.xlsx` | La carga inicial se resuelve con plantillas y carga manual; un parser genérico es esfuerzo sin retorno | descartado |
| Migración de las 21 semanas de bitácora en texto libre | Decisión PD-04: el registro arranca desde cero. El histórico queda como documento de consulta en el anexo | descartado |
| Entrada por lenguaje natural o foto con LLM | Requiere que el registro estructurado funcione primero | v3 |
| Aplicación móvil nativa | Fuera de alcance | descartado |
| Integración con básculas o wearables | Sin hardware que lo justifique | descartado |

### 5.3 Cobertura de la planilla, hoja por hoja

Verificación de que toda la información de `Alimentación.xlsx` tiene destino en el sistema. La evidencia del recorrido está en `docs/anexos/inventario_excel.py`.

| Hoja | Qué contiene | Dónde va | Qué se gana |
|---|---|---|---|
| `SeguimientoKcal` (filas 1-21) | Catálogo de alimentos con porción, kcal y macros | Catálogo de alimentos (RF-A) | Validación de coherencia, búsqueda asistida, macros nunca corrompidos |
| `SeguimientoKcal` (col. Pedido) | Comida de afuera con solo kcal | Alimentos sin macros (RF-A2, RN-09) | Las kcal sin macros se informan aparte en vez de contarse como cero |
| `SeguimientoKcal` (col. Sesión) | Actividades con kcal estimadas | Catálogo de actividades (RF-C) | Editable, y las kcal de cada sesión ajustables |
| `SeguimientoKcal` (matriz semanal) | kcal por tipo de comida y día, ingesta, consumo y déficit | Registro diario y cálculo (RF-B, RN-02, RN-03) | Sin doble conteo, sin gasto fijo, sin déficit en días vacíos |
| `Bitacora` | 21 semanas de comidas en texto libre, más Ejercicio y Cardio como Sí/No | Registro diario estructurado (RF-B) y sesiones (RF-C, RF-K, RF-L) | Consultable y sumable. El histórico en texto no se migra (PD-04) |
| `Ejercicio` | Rutina de 4 días: grupo muscular, ejercicio, series por reps, peso, variante | Ejercicios y rutinas (RF-J) | Estructurado, versionable y con historial de cargas |
| `EjercicioIA` | Una segunda rutina, de 5 días | Varias rutinas coexistentes (RF-J4) | Se pueden mantener y alternar rutinas sin duplicar hojas |
| `Cardio` | Progresión semanal de tiempo, velocidad, inclinación y kcal, más un protocolo de intervalos | Sesiones de cardio con parámetros (RF-L) | La progresión se ve como serie temporal, no como bloques copiados |
| `Estadísticas` | Mediciones de peso y composición corporal | Mediciones (RF-D) | kg derivados al leer, sin la inconsistencia de P-12 |
| `Opciones` | Menú de opciones por categoría | Categorías de alimentos editables (RF-I3) y catálogo | Deja de estar desconectado de los macros (P-10) |

Lo que el sistema agrega sobre la planilla: gasto calibrado con datos reales (RN-06), dashboard de período con contraste contra las mediciones (RN-11), búsqueda nutricional asistida (RF-A7), progresión de cargas por ejercicio (RF-K5) y cuentas independientes por persona (RF-I).

### 5.4 Supuestos

- SUP-1: Las mediciones de composición corporal provienen de una báscula de bioimpedancia. Se asume error sistemático constante, lo que las hace válidas para medir tendencia aunque el valor absoluto tenga sesgo.
- SUP-2: 7700 kcal por kilo de tejido adiposo. Es un **valor por defecto editable**, no una constante del código (RF-I5).
- SUP-3: 1500 kcal como costo de sintetizar un kilo de tejido magro. También editable.
- SUP-4: El factor de actividad cotidiana lo define cada usuario. No se asume ningún estilo de vida por defecto.
- SUP-5: Las kcal por sesión de gym y cardio son estimaciones del usuario y se toman como dato de entrada, no se calculan. El sistema no intenta derivarlas de METs ni de frecuencia cardíaca.
- SUP-6: Cada usuario es dueño de sus datos y el único que los ve. No hay funciones sociales, comparación entre usuarios ni datos agregados.

## 6. Requerimientos funcionales

Prioridad según MoSCoW: **D** = debe, **S** = debería, **P** = podría.

### 6.0 Cuentas y personalización (RF-I)

Este módulo es lo que convierte al sistema en algo de cada persona, en lugar de una aplicación con los datos de alguien adentro.

| ID | Requerimiento | Pri | Resuelve |
|---|---|---|---|
| RF-I1 | El sistema debe permitir crear una cuenta con correo y contraseña, e ingresar con esas credenciales | D | OBJ-7 |
| RF-I2 | El sistema debe aislar por completo los datos de cada cuenta, según RN-12 | D | OBJ-7 |
| RF-I3 | El sistema debe permitir editar los **tipos de comida** de cada usuario: nombre, orden y si están activos. No hay tipos fijos en el código | D | OBJ-7 |
| RF-I4 | El sistema debe permitir editar los **tipos de actividad**, las **categorías de alimentos** y los **grupos musculares** del usuario | D | OBJ-7 |
| RF-I5 | El sistema debe permitir editar las constantes fisiológicas del cálculo: kcal por kilo de grasa y por kilo de tejido magro, con los valores de SUP-2 y SUP-3 como punto de partida | S | OBJ-7 |
| RF-I6 | El sistema debe permitir elegir cómo se calcula el metabolismo basal: Katch-McArdle, Mifflin-St Jeor o un valor medido que el usuario ingresa | D | P-02 |
| RF-I7 | El sistema debe permitir configurar el factor de actividad con un valor libre, además de ofrecer opciones descritas en lenguaje claro | D | P-03 |
| RF-I8 | Al crear la cuenta, el sistema debe ofrecer arrancar vacío o importar una plantilla de catálogos, y dejar claro que después todo es editable | D | OBJ-7 |
| RF-I9 | El sistema debe permitir editar los umbrales de los cálculos: cobertura mínima para mostrar el contraste, cobertura para usar el gasto calibrado y los dos límites del techo de déficit seguro | P | — |
| RF-I10 | El sistema debe permitir cambiar la contraseña | D | — |
| RF-I11 | El sistema debe permitir eliminar la cuenta y todos sus datos, con confirmación que exija escribir el correo | S | — |
| RF-I12 | El sistema debe permitir exportar e importar los datos de la cuenta, sin mezclarlos con los de otra | D | — |
| RF-I13 | Desactivar un tipo de comida, un tipo de actividad o una categoría no debe alterar los registros históricos que los usaron | D | P-11 |

### 6.1 Catálogo de alimentos (RF-A)

| ID | Requerimiento | Pri | Resuelve |
|---|---|---|---|
| RF-A1 | El sistema debe permitir dar de alta un alimento con nombre, porción de referencia, kcal, proteína, carbohidratos y grasa | D | P-10 |
| RF-A2 | El sistema debe aceptar alimentos sin macros conocidos, con solo kcal, marcados como comida de afuera | D | — |
| RF-A3 | El sistema debe validar que las kcal declaradas sean coherentes con los macros, con tolerancia del 10 %, y advertir sin bloquear | S | P-01 |
| RF-A4 | El sistema debe permitir editar y desactivar alimentos sin alterar los registros históricos que los usaron | D | P-11 |
| RF-A5 | El sistema debe permitir agrupar alimentos en combos reutilizables | D | — |
| RF-A6 | El sistema debería mostrar las kcal totales de cada combo calculadas a partir de sus items | S | — |
| RF-A7 | El sistema debe permitir buscar la información nutricional de un alimento no presente en el catálogo, consultando fuentes externas | D | P-09 |
| RF-A8 | El sistema debe presentar el resultado de la búsqueda como **sugerencia editable**, y nunca guardarlo sin confirmación explícita del usuario | D | — |
| RF-A9 | El sistema debe registrar el origen de cada alimento (manual, base externa o modelo de lenguaje) y mostrarlo en el catálogo | D | — |
| RF-A10 | El sistema debe validar toda sugerencia externa con la coherencia entre kcal y macros de RN-10 antes de ofrecerla | D | — |
| RF-A11 | El sistema debe seguir siendo utilizable sin conexión: la búsqueda asistida se degrada con un aviso, sin bloquear la carga manual | D | — |

### 6.2 Registro diario de comidas (RF-B)

| ID | Requerimiento | Pri | Resuelve |
|---|---|---|---|
| RF-B1 | El sistema debe permitir registrar comidas para una fecha y uno de los tipos de comida que el usuario definió en RF-I3 | D | P-06 |
| RF-B2 | El sistema debe permitir agregar un alimento del catálogo indicando cantidad de porciones | D | — |
| RF-B3 | El sistema debe permitir cargar un combo completo en una sola acción | D | OBJ-1 |
| RF-B4 | El sistema debe copiar los macros del alimento al momento de registrarlo, de modo que editar el catálogo después no altere el registro | D | P-11 |
| RF-B5 | El sistema debe permitir quitar un item registrado | D | — |
| RF-B6 | El sistema debe mostrar el total de kcal y de cada macro del día | D | P-07 |
| RF-B7 | El sistema debe informar por separado las kcal cuyos macros son desconocidos, en vez de contarlas como cero | D | — |
| RF-B8 | El sistema debe permitir navegar a cualquier fecha para consultar o editar | D | — |
| RF-B9 | El sistema debería permitir copiar el registro completo de otro día a la fecha actual | S | OBJ-1 |
| RF-B10 | El sistema podría sugerir los combos más usados en ese tipo de comida | P | OBJ-1 |

### 6.3 Actividad y calorías quemadas (RF-C)

| ID | Requerimiento | Pri | Resuelve |
|---|---|---|---|
| RF-C1 | El sistema debe mantener un catálogo de actividades con nombre, uno de los tipos definidos por el usuario en RF-I4 y kcal estimadas | D | — |
| RF-C1b | El sistema debe permitir al usuario dar de alta, editar y desactivar actividades, definiendo libremente el nombre y las kcal de cada una | D | — |
| RF-C1c | El sistema debe permitir ajustar las kcal de una sesión concreta al registrarla, sin alterar el valor del catálogo | D | — |
| RF-C1d | Editar las kcal de una actividad del catálogo no debe modificar las sesiones ya registradas | D | P-11 |
| RF-C2 | El sistema debe permitir registrar una o más actividades por fecha | D | — |
| RF-C3 | El sistema debe calcular el metabolismo basal a partir de la última medición, con el método que el usuario eligió en RF-I6, según RN-01 | D | P-02 |
| RF-C4 | El sistema debe mostrar el gasto del día desglosado en metabolismo basal, actividad cotidiana y actividad deportiva | D | P-03 |
| RF-C5 | El sistema no debe incluir el ejercicio dentro del factor de actividad cotidiana, para evitar contarlo dos veces | D | P-03 |
| RF-C6 | El sistema debe recalcular el metabolismo basal automáticamente al ingresar una medición nueva | D | P-02 |

### 6.3b Ejercicios y rutinas (RF-J)

Cubre las hojas `Ejercicio` y `EjercicioIA`. Separa tres cosas que en la planilla están mezcladas: el **ejercicio** como concepto, la **rutina** que lo prescribe, y la **sesión** en que se hizo.

| ID | Requerimiento | Pri | Resuelve |
|---|---|---|---|
| RF-J1 | El sistema debe mantener un catálogo de ejercicios con nombre y grupo muscular, editable por el usuario | D | OBJ-8 |
| RF-J2 | El sistema debe permitir crear rutinas con nombre y una descripción opcional | D | P-06 |
| RF-J3 | El sistema debe permitir que una rutina tenga varios días, cada uno con nombre propio y orden | D | P-06 |
| RF-J4 | El sistema debe permitir varias rutinas simultáneas, con una marcada como activa | D | OBJ-8 |
| RF-J5 | El sistema debe permitir agregar ejercicios a un día de la rutina, con series, rango de repeticiones, peso de referencia y una observación libre | D | P-09 |
| RF-J6 | El sistema debe almacenar series y repeticiones de forma estructurada, no como el texto `4x6/10` de la planilla, y mostrarlas en ese formato | D | P-09 |
| RF-J7 | El sistema debe aceptar que el peso de referencia esté vacío o sea no numérico, según RN-13 | D | P-01 |
| RF-J8 | El sistema debe permitir reordenar, quitar y duplicar ejercicios dentro de un día | S | — |
| RF-J9 | El sistema debe permitir duplicar una rutina completa, para probar variantes sin perder la original | S | OBJ-8 |
| RF-J10 | Editar una rutina no debe alterar las sesiones ya registradas con ella | D | P-11 |

### 6.3c Registro de entrenamiento (RF-K)

Es el "un poco más" que la planilla no puede hacer: la rutina dice qué levantar, la sesión dice qué se levantó.

| ID | Requerimiento | Pri | Resuelve |
|---|---|---|---|
| RF-K1 | El sistema debe permitir registrar una sesión de gym en una fecha, eligiendo un día de una rutina | D | OBJ-8 |
| RF-K2 | El sistema debe precargar la sesión con los ejercicios de ese día y sus pesos de referencia, para que el usuario solo corrija lo que cambió | D | OBJ-1 |
| RF-K3 | El sistema debe registrar, por ejercicio, las series completadas, las repeticiones y el peso usado | D | OBJ-8 |
| RF-K4 | El sistema debe permitir desglosar un ejercicio serie por serie cuando cada una tuvo peso o repeticiones distintas, sin obligar a hacerlo | S | OBJ-1 |
| RF-K5 | El sistema debe mostrar la progresión histórica del peso de cada ejercicio a lo largo del tiempo | D | OBJ-8 |
| RF-K6 | El sistema debe permitir agregar a la sesión un ejercicio que no está en la rutina de ese día | S | — |
| RF-K7 | Las kcal estimadas se definen en el día de la rutina y se proponen al abrir la sesión, donde el usuario puede ajustarlas para ese día concreto | D | RN-02, D-17 |
| RF-K8 | El sistema debe permitir editar y eliminar una sesión registrada | D | — |
| RF-K9 | El sistema debería ofrecer actualizar el peso de referencia de la rutina cuando una sesión usó un peso mayor de forma sostenida | P | — |
| RF-K10 | El sistema debería mostrar el volumen de la sesión (series × repeticiones × peso) por grupo muscular | P | — |

### 6.3d Cardio (RF-L)

Cubre la hoja `Cardio`, que la planilla resuelve copiando un bloque por semana.

| ID | Requerimiento | Pri | Resuelve |
|---|---|---|---|
| RF-L1 | El sistema debe permitir registrar una sesión de cardio continuo con fecha, minutos, velocidad, inclinación y kcal | D | OBJ-8 |
| RF-L2 | El sistema debe aceptar las kcal como valor puntual o como rango mínimo y máximo, según RN-14 | D | P-01 |
| RF-L3 | El sistema debe permitir definir protocolos por intervalos, con bloques ordenados de minutos, velocidad, inclinación y cantidad de repeticiones del bloque | D | OBJ-8 |
| RF-L4 | El sistema debe calcular la duración total de un protocolo por intervalos a partir de sus bloques | D | P-07 |
| RF-L5 | El sistema debe permitir registrar una sesión a partir de un protocolo, ajustando lo que haya cambiado | S | OBJ-1 |
| RF-L6 | El sistema debe mostrar la progresión de velocidad, inclinación y kcal a lo largo del tiempo | D | OBJ-8 |
| RF-L7 | Las kcal de la sesión de cardio deben sumarse al gasto del día según RN-02, sin duplicarse con el catálogo de actividades | D | P-03 |
| RF-L8 | El sistema debe permitir editar y eliminar una sesión de cardio | D | — |

### 6.4 Mediciones de composición corporal (RF-D)

| ID | Requerimiento | Pri | Resuelve |
|---|---|---|---|
| RF-D1 | El sistema debe permitir registrar peso, % de grasa, % de músculo y grasa visceral en una fecha | D | — |
| RF-D2 | El sistema debe admitir una sola medición por fecha | D | — |
| RF-D3 | El sistema debe derivar la masa grasa y la masa magra en kg al momento de leer, sin almacenarlas | D | P-12 |
| RF-D4 | El sistema debe aceptar mediciones con solo peso, sin composición | S | — |
| RF-D5 | El sistema debe mostrar la evolución de peso, masa grasa y masa magra en el tiempo | D | OBJ-3 |
| RF-D6 | El sistema debe permitir editar una medición existente, incluida su fecha | D | — |
| RF-D7 | El sistema debe permitir eliminar una medición, con confirmación previa | D | — |
| RF-D8 | Al editar o eliminar una medición, el sistema debe recalcular el metabolismo basal, el plan de objetivo y la calibración, e informar si el cambio afecta a la medición usada como referencia | D | P-02 |

### 6.5 Perfil y configuración (RF-E)

| ID | Requerimiento | Pri | Resuelve |
|---|---|---|---|
| RF-E1 | El sistema debe permitir configurar altura, fecha de nacimiento y sexo | D | — |
| RF-E2 | El sistema debe permitir elegir el factor de actividad cotidiana entre opciones descritas en lenguaje claro | D | P-03 |
| RF-E3 | El sistema debe permitir definir el % de grasa objetivo y un plazo expresado en semanas (PD-02) | D | OBJ-3 |
| RF-E4 | El sistema debe permitir definir un objetivo diario de proteína | S | — |

### 6.6 Objetivo y déficit (RF-F)

| ID | Requerimiento | Pri | Resuelve |
|---|---|---|---|
| RF-F1 | El sistema debe calcular el peso objetivo asumiendo masa magra constante, según RN-04 | D | OBJ-3 |
| RF-F2 | El sistema debe informar los kg de grasa a perder y las kcal totales equivalentes | D | OBJ-3 |
| RF-F3 | El sistema debe calcular el déficit diario necesario para un plazo dado | D | OBJ-3 |
| RF-F4 | El sistema debe advertir cuando el déficit necesario supere el techo seguro de RN-05 | D | — |
| RF-F5 | El sistema debe proyectar la masa grasa semana a semana con el déficit elegido | D | — |
| RF-F6 | El sistema debe calcular el gasto energético real calibrado según RN-06, e informar la cobertura de registro del período | D | OBJ-5 |
| RF-F7 | El sistema debe usar el gasto calibrado en lugar de la fórmula cuando la cobertura supere el 70 % | S | OBJ-5 |
| RF-F8 | El sistema debería mostrar la comparación entre el déficit registrado y el déficit implícito en las mediciones | S | P-08 |

### 6.7 Dashboard de período (RF-H)

Vista consolidada que responde "cómo me fue esta semana" o "cómo me fue este mes" sin tener que recorrer día por día.

| ID | Requerimiento | Pri | Resuelve |
|---|---|---|---|
| RF-H1 | El sistema debe permitir elegir el período: semana, mes, últimos 90 días o rango libre de fechas | D | P-06 |
| RF-H2 | El sistema debe permitir navegar al período anterior y siguiente | D | — |
| RF-H3 | El sistema debe mostrar el déficit acumulado y el déficit medio diario del período, calculados solo sobre días con registro | D | P-04 |
| RF-H4 | El sistema debe mostrar la cobertura de registro del período: días con datos sobre días totales | D | P-04 |
| RF-H5 | El sistema debe mostrar el déficit de cada día del período en un gráfico, distinguiendo días sin registro de días en superávit | D | — |
| RF-H6 | El sistema debe mostrar la ingesta media diaria y los macros medios diarios del período | D | — |
| RF-H7 | El sistema debe mostrar la adherencia: cuántos días alcanzaron el objetivo de déficit, sobre el total de días con registro | D | — |
| RF-H8 | El sistema debe mostrar el resumen de entrenamiento del período: sesiones por tipo y kcal totales de actividad | D | — |
| RF-H9 | El sistema debe mostrar cuántos días incluyeron comida de afuera y qué proporción de las kcal del período representan | S | — |
| RF-H10 | El sistema debe contrastar la **grasa perdida estimada** por el déficit registrado contra la **grasa perdida medida** por las mediciones del período, según RN-11 | D | OBJ-5, P-08 |
| RF-H11 | El sistema debe mostrar la evolución de peso y masa grasa del período si contiene al menos dos mediciones | D | — |
| RF-H12 | El sistema debería mostrar el progreso hacia el objetivo: grasa perdida hasta ahora y grasa restante | S | OBJ-3 |
| RF-H13 | El sistema debería permitir exportar el resumen del período a CSV | P | — |

### 6.8 Datos y respaldo (RF-G)

| ID | Requerimiento | Pri | Resuelve |
|---|---|---|---|
| RF-G1 | El sistema debe persistir en un archivo SQLite local, con un esquema que soporte varias cuentas | D | — |
| RF-G2 | El sistema debe ofrecer plantillas opcionales de alimentos, combos, actividades, ejercicios y rutinas, que el usuario importa a su cuenta si quiere | D | OBJ-7 |
| RF-G2b | Las plantillas se copian a la cuenta al importarlas, y desde ese momento son datos del usuario, editables y sin vínculo con el origen | D | RN-12 |
| RF-G2c | Una de las plantillas disponibles debe ser la derivada de `Alimentación.xlsx`, con los 14 valores corregidos, ofrecida como una opción más y no como carga obligatoria | S | P-01 |
| RF-G3 | El sistema debe permitir exportar los datos de la cuenta activa a JSON | D | — |
| RF-G4 | El sistema debe permitir importar un JSON previamente exportado, a la cuenta activa | S | — |
| RF-G5 | Un día sin registro debe distinguirse de un día con ingesta cero, y quedar excluido de promedios y acumulados | D | P-04 |

## 7. Requerimientos no funcionales

| ID | Categoría | Requerimiento |
|---|---|---|
| RNF-01 | Persistencia | Archivo SQLite único con esquema multiusuario, ubicado en carpeta de datos, respaldable copiándolo. El motor es reemplazable por Postgres sin cambiar el modelo (D-12) |
| RNF-02 | Integridad | Los registros históricos no cambian ante ediciones del catálogo (macros copiados al registrar) |
| RNF-03 | Integridad | Toda cantidad numérica se almacena con tipo numérico explícito; ninguna conversión implícita de tipo |
| RNF-04 | Trazabilidad | Todo valor calculado debe poder explicarse en pantalla indicando fórmula y entradas |
| RNF-05 | Precisión | kcal redondeadas a entero al mostrar; kg con 2 decimales; porcentajes con 1 decimal. Sin redondeo intermedio |
| RNF-06 | Fechas | Fecha local sin componente de hora. Clave de día en formato ISO `YYYY-MM-DD` |
| RNF-07 | Localización | Interfaz en español, separador decimal según cultura `es-UY` |
| RNF-08 | Autonomía | Toda función esencial opera sin conexión a internet. La **única** excepción es la búsqueda nutricional asistida (RF-A7), que es opcional y se degrada con un aviso claro cuando no hay red o no hay credenciales configuradas |
| RNF-09 | Autenticación | Ingreso con correo y contraseña. La contraseña se almacena con un algoritmo de derivación con sal, nunca en claro ni con un hash simple. Sesión por cookie firmada y con expiración |
| RNF-09b | Autorización | Ninguna pantalla ni endpoint devuelve datos sin verificar la cuenta. El aislamiento se aplica en el acceso a datos y no depende de que cada consulta lo recuerde (RN-12, D-13) |
| RNF-09c | Superficie de red | Mientras corra local, escucha solo en `localhost`. La contraseña se transmite por HTTPS en cuanto se despliegue fuera de la máquina |
| RNF-09d | Datos personales | El correo es el único dato de contacto que se guarda. No se registra dirección IP, ubicación ni telemetría |
| RNF-10 | Rendimiento | Cualquier pantalla responde en menos de 300 ms con 5 años de datos acumulados. La búsqueda asistida queda excluida: depende de un servicio externo y debe mostrar estado de carga y permitir cancelar |
| RNF-11 | Mantenibilidad | La lógica de cálculo vive en clases sin dependencia de la interfaz ni de la base, y es testeable de forma unitaria |
| RNF-12 | Migraciones | El esquema evoluciona por migraciones versionadas de EF Core, nunca por recreación de la base |
| RNF-13 | Credenciales | La clave de API no se almacena en el repositorio ni en `appsettings.json`. Se lee de variable de entorno o del gestor de secretos de .NET. La interfaz nunca la muestra completa |
| RNF-14 | Privacidad | A los servicios externos se envía únicamente el nombre del alimento consultado. Nunca el peso, las mediciones, el historial ni ningún dato personal |
| RNF-15 | Costo | Cada consulta al modelo de lenguaje se dispara solo por acción explícita del usuario, nunca de forma automática o en segundo plano. El resultado se guarda en el catálogo para no volver a consultarlo |
| RNF-16 | Trazabilidad de origen | Todo alimento incorporado desde una fuente externa queda marcado con su origen y la fecha de obtención |
| RNF-17 | Sin valores cableados | Ningún catálogo, tipo, categoría, constante fisiológica ni umbral de cálculo está fijo en el código. Todos son datos de la cuenta con un valor por defecto (OBJ-7) |
| RNF-18 | Rendimiento multiusuario | Toda tabla con datos de usuario tiene índice que empieza por `UsuarioId`, para que el volumen de otras cuentas no degrade las consultas |

## 8. Reglas de negocio y fórmulas

Todas las reglas son verificables de forma unitaria y son la fuente de verdad para la implementación.

### RN-01 — Metabolismo basal

El usuario elige el método en RF-I6. Son tres:

**Katch-McArdle** (por defecto cuando la última medición tiene % de grasa), que parte de la masa magra:

```
masaMagra = peso × (1 − grasa% / 100)
BMR = 370 + 21.6 × masaMagra
```

**Mifflin-St Jeor**, que solo necesita peso, altura, edad y sexo:

```
BMR = 10 × peso + 6.25 × altura_cm − 5 × edad + (sexo = M ? 5 : −161)
```

**Valor medido**: el usuario ingresa su metabolismo basal si lo obtuvo por calorimetría o de un estudio. Se usa tal cual, sin fórmula.

Si el método elegido es Katch-McArdle y la medición no trae % de grasa, se cae a Mifflin-St Jeor y se indica en pantalla que se usó el respaldo.

*Justificación*: partir de la masa magra es más fiel que partir del peso total cuando existe medición de composición, porque el tejido adiposo tiene una tasa metabólica muy inferior a la del tejido magro. Pero un valor medido de verdad le gana a cualquier fórmula, y no hay razón para impedir usarlo.

### RN-02 — Gasto energético por capas

```
gastoSinDeporte = BMR × factorActividad
gastoDelDia     = gastoSinDeporte
                + Σ kcal de actividades registradas ese día
                + Σ kcal de sesiones de gym ese día
                + Σ kcal de sesiones de cardio ese día
```

El `factorActividad` refleja **solo** actividad no deportiva. El sistema ofrece opciones descritas en lenguaje claro (1.2 sedentario, 1.35 movimiento moderado, 1.5 muy activo) pero acepta cualquier valor que el usuario cargue (RF-I7). El ejercicio nunca entra en el factor: se suma explícitamente. Esto es lo que corrige P-03.

Una misma actividad no puede contarse dos veces. La principal defensa es de diseño: las kcal del gym viven en el día de la rutina y las del cardio en su sesión (D-17), así que el catálogo de actividades queda reservado para lo que no es ninguna de las dos, y el usuario no tiene motivo para crear ahí una entrada que duplique su entrenamiento. Como red de seguridad, si aun así aparecen una sesión y una actividad de catálogo en la misma fecha, el sistema advierte del posible solapamiento antes de sumar ambas (RF-L7).

### RN-03 — Déficit del día

```
déficit = gastoDelDia − ingestaDelDia
```

Positivo significa déficit; negativo, superávit. Solo se calcula si el día tiene al menos un registro de comida. Un día sin registros no tiene déficit definido (P-04, RF-G5).

### RN-04 — Peso objetivo

Se asume masa magra constante, que es el escenario deseable y el único que no obliga a estimar la pérdida de músculo:

```
masaMagra      = pesoActual × (1 − grasaActual% / 100)
pesoObjetivo   = masaMagra / (1 − grasaObjetivo% / 100)
grasaObjetivoKg= pesoObjetivo − masaMagra
grasaAPerderKg = masaGrasaActual − grasaObjetivoKg
kcalTotales    = grasaAPerderKg × 7700
déficitDiario  = kcalTotales / díasDelPlazo
```

### RN-05 — Techo de déficit seguro

```
techo = min(gastoDelDia × pctGastoMax, peso × pctPesoSemanal × kcalPorKgGrasa / 7)
```

Con los valores por defecto `pctGastoMax = 0.25` y `pctPesoSemanal = 0.0075`: el menor entre un cuarto del gasto diario y el equivalente a 0.75 % del peso corporal por semana. Superarlo acelera la balanza a costa de masa magra. Si el déficit necesario excede el techo, el sistema advierte y sugiere extender el plazo (RF-F4).

Los tres parámetros son editables (RF-I5, RF-I9). Quien tenga indicación profesional distinta puede ajustarlos, y el sistema deja registrado qué valores se usaron.

### RN-06 — Gasto calibrado con datos reales

Reemplaza cualquier fórmula cuando hay historial suficiente. Entre dos mediciones con composición corporal:

```
energíaLiberada = ΔgrasaKg × kcalPorKgGrasa − ΔmúsculoKg × kcalPorKgMagro
gastoReal       = ingestaMediaDiaria + energíaLiberada / díasDelPeríodo
cobertura       = díasConRegistro / díasDelPeríodo
```

Las dos constantes salen de la configuración del usuario (RF-I5), con 7700 y 1500 por defecto.

Condiciones mínimas: dos mediciones con composición, período de al menos 14 días y al menos 7 días con registro de ingesta. La `cobertura` se muestra siempre junto al resultado: con cobertura baja el número es indicativo, no confiable.

### RN-07 — Días sin registro

Un día sin comidas registradas se representa como ausencia de dato. No participa en promedios, acumulados ni proyecciones, y no se le asigna gasto ni déficit. Corrige P-04.

### RN-08 — Inmutabilidad del histórico

Al registrar un item de comida se copian nombre, porción y macros vigentes del alimento. El registro guarda referencia al alimento solo con fines informativos. Editar o desactivar un alimento nunca modifica registros anteriores. Corrige P-11.

### RN-09 — Alimentos sin macros

Un alimento puede tener kcal y macros nulos (comida de restaurante). Sus kcal se suman al total del día, pero sus macros no se cuentan como cero: se acumulan en un renglón aparte de "kcal sin macros conocidos", que se informa siempre que sea mayor a cero. Corrige el sesgo de subestimar proteína en días de comida afuera.

### RN-10 — Validación de datos nutricionales sugeridos

Ninguna sugerencia externa entra al catálogo sin pasar este filtro. La energía declarada tiene que ser coherente con los macros:

```
kcalCalculadas = proteína × 4 + carbohidratos × 4 + grasa × 9
desvío         = |kcalCalculadas − kcalDeclaradas| / kcalDeclaradas
```

| Desvío | Tratamiento |
|---|---|
| ≤ 5 % | Sugerencia consistente. Se ofrece normalmente |
| 5 % a 15 % | Se ofrece con advertencia visible del desvío |
| > 15 % | Se marca como no confiable. Se muestra el detalle del cálculo y se pide revisión manual antes de aceptar |

Además: cualquier valor negativo, o una suma de macros que exceda el peso de la porción cuando la porción está expresada en gramos, invalida la sugerencia por completo.

*Justificación*: un modelo de lenguaje puede producir cifras verosímiles pero incorrectas. Esta validación es aritmética y no depende de confiar en la fuente. Es la misma verificación que permitió detectar y reconstruir las 14 celdas corrompidas del Excel.

### RN-11 — Métricas de período y contraste con lo medido

Sobre un período `[desde, hasta]`, considerando **solo** los días con registro (RN-07):

```
díasConRegistro   = cantidad de días con al menos una comida cargada
cobertura         = díasConRegistro / díasTotalesDelPeríodo
déficitAcumulado  = Σ déficit de los días con registro
déficitMedio      = déficitAcumulado / díasConRegistro
adherencia        = días con déficit ≥ objetivoDiario / díasConRegistro
```

Contraste entre modelo y realidad, que es lo que cierra el ciclo:

```
grasaPerdidaEstimada = déficitAcumulado / kcalPorKgGrasa
grasaPerdidaMedida   = masaGrasa(primera medición) − masaGrasa(última medición)
discrepancia         = grasaPerdidaEstimada − grasaPerdidaMedida
```

Requiere al menos dos mediciones dentro del período. Interpretación que el sistema debe explicitar:

| Situación | Lectura |
|---|---|
| Estimada > medida | Se está subestimando la ingesta, sobreestimando el gasto, o ambas |
| Estimada ≈ medida | El modelo está calibrado |
| Estimada < medida | Se está perdiendo más de lo previsto: revisar si parte es masa magra |

La discrepancia solo se muestra con cobertura ≥ 50 %; por debajo, la estimación se calcula sobre tan pocos días que compararla induce a error. El umbral es configurable (RF-I9), con 50 % por defecto.

### RN-12 — Aislamiento de datos entre cuentas

Toda entidad que contenga datos del usuario lleva `UsuarioId` obligatorio. El filtrado por usuario **no** queda a criterio de cada consulta: se aplica de forma global en el acceso a datos, de modo que una consulta que se olvide de filtrar devuelva vacío en lugar de datos ajenos (D-13).

Consecuencias que la implementación debe respetar:

| Regla | Detalle |
|---|---|
| Sin datos globales editables | No existe catálogo compartido que un usuario pueda modificar y otro vea. Las plantillas son de solo lectura y se copian al importarlas |
| Referencias siempre dentro de la cuenta | Un `ComidaItem` no puede apuntar a un `Alimento` de otro usuario. Se valida al guardar |
| Exportación acotada | La exportación de RF-I12 contiene solo la cuenta activa |
| Borrado en cascada | Eliminar la cuenta elimina todos sus datos, sin dejar registros huérfanos |
| Sin filtraciones por conteo | Ninguna pantalla muestra totales, promedios ni cantidades calculadas sobre datos de otras cuentas |

### RN-13 — Peso de ejercicio: numérico cuando se puede, texto cuando no

La planilla registra el peso de tres formas distintas: un número (`40`), un número con un más que indica que se puede más (`65+`, `32.5 +`), y una descripción (`Máximo máquina`). Las tres son información legítima y el modelo las conserva sin perder la capacidad de graficar.

```
Peso        decimal, opcional  → el número, cuando existe
PuedeMas    booleano           → equivale al "+" de la planilla
Observacion texto, opcional    → "Máximo máquina", "12.5 mancuerna", "Declinado 70"
```

Reglas:

- Si hay número, se usa para la progresión de RF-K5 y para el volumen de RF-K10.
- Si no hay número, la sesión se registra igual y queda fuera de los gráficos de progresión, sin bloquear nada.
- El campo numérico **nunca** acepta texto. Es lo que evita que se repita P-01: en la planilla, el peso `27.5` del peso muerto rumano quedó convertido en la fecha `2026-05-27`.
- Al mostrar, se reconstruye la notación original: `27.5+`, `40`, `Máximo máquina`.

### RN-14 — Kcal de cardio con incertidumbre

La planilla anota las calorías del cardio como rango (`160-220`, `200-280`), lo que es más honesto que un valor puntual pero no sirve para sumar. El sistema conserva ambas cosas:

```
KcalMin, KcalMax  opcionales → el rango tal como lo estima el usuario
Kcal              obligatorio → el valor que entra en el gasto del día
```

Si el usuario carga un rango y no un valor puntual, `Kcal` toma el punto medio y se indica en pantalla que es una estimación derivada del rango. El gasto del día (RN-02) siempre usa `Kcal`, para que el déficit sea un número y no un intervalo.

*Justificación*: mezclar rangos en las sumas obligaría a que todo el sistema propague intervalos, lo que complica cada cálculo aguas abajo para una precisión que las estimaciones de partida no tienen. Guardar el rango deja la puerta abierta a mostrar una banda de incertidumbre en el dashboard más adelante, sin condicionar el modelo ahora.

## 9. Criterios de aceptación

Casos concretos. Los que verifican cálculos usan un **caso de prueba de referencia**, tomado de los datos reales de la planilla para que las cifras sean verificables a mano: última medición 2026-09-15, 72.2 kg, 17.2 % de grasa, 42.2 % de músculo, factor de actividad 1.2, método Katch-McArdle, 7700 kcal/kg de grasa. No son datos precargados del sistema: son el escenario de las pruebas.

| ID | Dado | Cuando | Entonces |
|---|---|---|---|
| CA-01 | La última medición es 72.2 kg y 17.2 % de grasa | Se calcula el metabolismo basal | Resulta 1661 kcal (masa magra 59.78 kg) |
| CA-02 | Factor de actividad 1.2 | Se consulta el gasto de un día sin entrenar | Resulta 1994 kcal, no 2250 |
| CA-03 | Un día con sesión de gym de 330 kcal | Se consulta el gasto del día | Resulta 2324 kcal (1994 + 330), sin duplicar el ejercicio |
| CA-04 | Objetivo 15 % de grasa | Se calcula el plan | Peso objetivo 70.3 kg, grasa a perder 1.87 kg, 14 389 kcal totales |
| CA-05 | Objetivo 15 % en un plazo de 8 semanas | Se calcula el déficit necesario | Resulta 257 kcal/día y se marca como sostenible (techo 498) |
| CA-06 | Objetivo 15 % en un plazo de 4 semanas | Se calcula el déficit necesario | Resulta 514 kcal/día y **se advierte** que supera el techo de 498 |
| CA-07 | Se carga el combo "Desayuno completo" | Se consulta el total del día | Suma 393 kcal, igual que la planilla |
| CA-08 | Se carga el combo "Almuerzo/cena pollo" | Se consulta el total del día | Suma 565 kcal |
| CA-09 | Se registra un Rudy doble sin fritas (950 kcal, sin macros) | Se consultan los macros del día | Las 950 kcal figuran en el total y también en "sin macros conocidos"; la proteína no aumenta |
| CA-10 | El alimento "Huevo" se edita de 72 a 80 kcal | Se consulta un día anterior que lo tenía cargado | El total de ese día no cambia |
| CA-11 | Una fecha sin ninguna comida registrada | Se consulta el déficit de esa fecha | Se informa "sin registro"; no se reporta un déficit de 1994 kcal |
| CA-12 | Mediciones del 2026-03-20 y del 2026-09-15, con la ingesta del período cargada | Se calcula el gasto calibrado | Se informa el gasto junto a la cobertura de registro del período |
| CA-13 | La plantilla derivada del Excel, ya importada | Se consulta el macro de proteína del pan | Vale 4.3 g, no una fecha (P-01 corregido) |
| CA-14 | La plantilla derivada del Excel | Se consultan las kcal declaradas contra los macros de cada alimento | Todos cierran con menos del 10 % de diferencia |
| CA-15 | Una sugerencia externa con 20 g de proteína, 0 de carbos, 10 g de grasa y 400 kcal declaradas | Se valida con RN-10 | 170 contra 400 es 57 % de desvío: se marca como no confiable y se exige revisión manual |
| CA-16 | Una sugerencia con 20 g de proteína, 30 g de carbos, 10 g de grasa y 290 kcal declaradas | Se valida con RN-10 | 290 contra 290 cierra exacto: se ofrece sin advertencia |
| CA-17 | No hay conexión a internet | Se abre el buscador de alimentos | La carga manual funciona con normalidad y la búsqueda asistida aparece deshabilitada con el motivo |
| CA-18 | Una actividad del catálogo de 330 kcal se edita a 400 | Se consulta una sesión registrada antes del cambio | Sigue valiendo 330 kcal |
| CA-19 | La medición del 2026-09-15 se elimina | Se consulta el metabolismo basal | Se recalcula sobre la medición del 2026-08-19 y el sistema informa que la referencia cambió |
| CA-20 | Período 2026-09-07 al 2026-09-16, con los 10 días registrados y gasto según RN-02 | Se consulta el dashboard | Déficit acumulado 1 790 kcal, medio 179 kcal/día, cobertura 10 de 10, adherencia 4 de 10 contra un objetivo de 257 kcal |
| CA-21 | El mismo período dentro del mes de septiembre completo | Se consulta el dashboard mensual | Cobertura 10 de 30 (33 %) y la discrepancia de RN-11 **no** se muestra, por estar debajo del 50 % |
| CA-22 | Un período con dos mediciones y cobertura del 100 % | Se consulta el contraste de RN-11 | Se muestran la grasa estimada, la medida y la discrepancia con su interpretación |
| CA-23 | Dos cuentas, cada una con sus alimentos | La cuenta A lista su catálogo | Ve solo los suyos. Ningún alimento, medición, rutina ni sesión de B aparece, ni siquiera en totales |
| CA-24 | Una consulta de la implementación que omite filtrar por usuario | Se ejecuta | Devuelve vacío, no datos de otra cuenta (RN-12, D-13) |
| CA-25 | La cuenta A intenta registrar una comida referenciando un alimento de la cuenta B por su identificador | Se guarda | Se rechaza con error de validación |
| CA-26 | Una cuenta nueva | Se completa el registro | Los catálogos están vacíos y se ofrece importar plantillas. Ningún alimento, actividad o rutina aparece sin haberlo elegido |
| CA-27 | El usuario renombra el tipo de comida "Merienda" a "Meriendita" y desactiva "Media mañana" | Se consulta un día anterior que tenía comidas en ambos | Los registros siguen visibles y sumando; el tipo desactivado no se ofrece para cargas nuevas |
| CA-28 | El usuario define 8000 kcal por kilo de grasa en lugar de 7700 | Se recalcula el plan de objetivo | Las kcal totales y el déficit diario usan 8000, y la pantalla indica el valor aplicado |
| CA-29 | El método de metabolismo basal es "valor medido" con 1720 kcal | Se consulta el gasto de un día sin entrenar con factor 1.2 | Resulta 2064 kcal, sin aplicar Katch-McArdle |
| CA-30 | El método es Katch-McArdle y la última medición no trae % de grasa | Se calcula el metabolismo basal | Se usa Mifflin-St Jeor y la pantalla informa que se aplicó el respaldo |
| CA-31 | La rutina tiene "Press banca, 4 series, 6 a 10 reps, 20 kg" | Se consulta la prescripción | Se muestra como `4x6/10` y se almacena en campos separados, no como texto |
| CA-32 | Un ejercicio con peso de referencia "Máximo máquina", sin número | Se registra una sesión con él | La sesión se guarda; el ejercicio queda fuera del gráfico de progresión sin bloquear el registro (RN-13) |
| CA-33 | El peso muerto rumano tiene 27.5 kg y el usuario marca que puede más | Se consulta la rutina | Se muestra `27.5+`, y el campo numérico contiene 27.5, nunca una fecha (P-01) |
| CA-34 | La rutina activa prescribe 40 kg en press inclinado y la sesión se registró con 45 | Se edita la rutina a 42.5 kg | La sesión registrada sigue diciendo 45 kg (RF-J10) |
| CA-35 | Tres sesiones de press banca con 20, 22.5 y 25 kg en fechas distintas | Se consulta la progresión del ejercicio | Se muestran los tres puntos en orden cronológico |
| CA-36 | Una sesión de cardio de 30 minutos a 5 km/h con 7 % de inclinación y kcal entre 160 y 220 | Se consulta el gasto del día | Se suman 190 kcal, el punto medio, y se indica que proviene de un rango (RN-14) |
| CA-37 | Un protocolo de intervalos con 5 min de entrada, 6 repeticiones de 30 s rápido más 1 min lento, y 5 min de salida | Se consulta su duración | Resulta 19 minutos |
| CA-38 | Un día con una sesión de gym de 330 kcal y además una actividad de catálogo de 330 kcal creada a mano | Se consulta el gasto del día | El sistema advierte del posible doble conteo antes de sumar ambas (RN-02) |
| CA-39 | Un día de rutina con `KcalEstimadas = 330` | Se abre una sesión de gym desde ese día y se guarda sin tocar el campo | La sesión queda con 330 kcal. Si el usuario lo cambia a 280, la sesión guarda 280 y el día de rutina sigue en 330 (RF-K7, D-17) |

## 10. Trazabilidad problema → requerimiento

| Problema | Se resuelve con |
|---|---|
| P-01 | RF-A3, RF-G2, RNF-03, CA-13, CA-14 |
| P-02 | RF-C3, RF-C6, RF-F6, RN-01, RN-06 |
| P-03 | RF-C4, RF-C5, RN-02, CA-03 |
| P-04 | RF-G5, RN-07, CA-11 |
| P-05 | RN-08, RNF-11 |
| P-06 | RF-B1, modelo relacional |
| P-07 | RF-B6 |
| P-08 | RF-F1 a RF-F8 |
| P-09 | RF-B2, RF-B3, RF-J5, RF-J6 (registro estructurado, también de la rutina) |
| P-10 | RF-A1, RF-A5, RF-I3, RF-I4 |
| P-11 | RF-A4, RF-B4, RF-C1d, RF-I13, RF-J10, RN-08, CA-10, CA-27, CA-34 |
| P-12 | RF-D3 |

## 11. Riesgos

| ID | Riesgo | Impacto | Mitigación |
|---|---|---|---|
| R-01 | La báscula de bioimpedancia tiene error absoluto alto | El % de grasa objetivo puede no corresponder al real | Se usa para tendencia, no para valor absoluto (SUP-1). Medir siempre en las mismas condiciones |
| R-02 | El usuario abandona el registro diario | Sin datos, la calibración de RN-06 no funciona | Priorizar velocidad de carga (OBJ-1): combos de un clic y copiar día anterior |
| R-03 | Las kcal por sesión de gym son estimaciones gruesas | El déficit diario tiene error | RN-06 corrige el sesgo agregado con el tiempo, sin depender de esas estimaciones |
| R-04 | Subestimación sistemática de porciones | El déficit real es menor al calculado | RN-06 lo detecta y lo cuantifica al comparar con las mediciones |
| R-05 | Pérdida del archivo SQLite | Pérdida total del histórico | RF-G3 exportación a JSON; el archivo es copiable y versionable |
| R-06 | El modelo de lenguaje devuelve valores nutricionales verosímiles pero falsos | Datos corruptos en el catálogo, que es justamente el problema que vino a resolver el producto | RN-10 valida la aritmética; RF-A8 exige confirmación; RF-A9 marca el origen para poder auditar después |
| R-07 | Costo acumulado de las consultas al modelo | Gasto imprevisto | RNF-15: solo por acción explícita, y el resultado se guarda para no repetir la consulta. El catálogo converge: los alimentos habituales se cargan una vez |
| R-08 | El servicio externo cambia su API o deja de estar disponible | La búsqueda asistida deja de funcionar | Es una función opcional detrás de una interfaz propia (D-09). Su caída no afecta ninguna función esencial (RNF-08) |
| R-09 | Fuga de datos personales hacia un tercero | Privacidad | RNF-14: se envía solo el nombre del alimento. Ni peso, ni mediciones, ni historial |
| R-10 | Confiar en la grasa perdida estimada con cobertura baja | Conclusiones equivocadas sobre el progreso | RN-11 oculta la discrepancia por debajo del 50 % de cobertura y muestra siempre la cobertura junto a la cifra |
| R-11 | Una consulta que olvide filtrar por usuario expone datos de otra cuenta | Grave: es el peor defecto posible en un sistema con datos de salud | El filtrado es global en el acceso a datos, no por consulta (RN-12, D-13). CA-24 lo verifica de forma explícita |
| R-12 | Contraseñas débiles o mal almacenadas | Acceso indebido a datos personales | RNF-09: derivación con sal, nunca hash simple. Longitud mínima al registrar |
| R-13 | Tanta configuración abruma a quien recién empieza | Abandono antes de cargar el primer día | Todo trae valor por defecto sensato y la configuración avanzada está separada de la básica. Las plantillas evitan arrancar con la pantalla vacía (RF-I8) |
| R-14 | Registrar cada sesión de gym resulta tedioso y se abandona | Se pierde la progresión de cargas, que es la razón del módulo | RF-K2 precarga la sesión desde la rutina: en el caso habitual solo hay que confirmar. RF-K4 hace opcional el detalle serie por serie |
| R-15 | Doble conteo entre una sesión de gym o cardio y la actividad del catálogo del mismo día | Vuelve a aparecer P-03, el problema que el producto vino a corregir | D-17 quita el motivo para duplicar: las kcal del gym viven en la rutina y el catálogo queda para lo que no es gym ni cardio. RN-02 advierte del solapamiento si aun así ocurre; CA-38 lo verifica |
| R-16 | Migrar de SQLite a Postgres al desplegar | Retrabajo si el modelo asumió un solo usuario | D-12: el modelo es multiusuario desde el día uno y el acceso a datos no usa nada específico de SQLite |

## 12. Decisiones técnicas

Registradas en detalle en `docs/03-modelo-y-decisiones.md`. Resumen de lo acordado:

| Decisión | Elección | Alternativas descartadas |
|---|---|---|
| Plataforma | .NET 10, ASP.NET Core Razor Pages | Blazor Server, Blazor WebAssembly, React + TypeScript, Python |
| Persistencia | SQLite local vía EF Core, con esquema multiusuario | IndexedDB en el navegador, Postgres desde el inicio |
| Interactividad | Refrescos parciales con htmx sobre handlers de página (D-16) | `fetch` escrito a mano, recarga completa por POST, framework de front |
| Despliegue | Ejecución local en `localhost`, preparado para servidor (D-12) | Publicar en la nube en v1 |
| Autenticación | Cookie de sesión más el hasher de Identity, sin el resto del andamiaje (D-12) | Identity completo con roles y tokens, proveedores externos, magic links |
| Aislamiento | Filtro global por usuario en el acceso a datos (D-13) | Filtrar en cada consulta |
| Punto de partida | Plantillas opcionales que se copian a la cuenta (D-14) | Semilla obligatoria con datos de una persona |

## 13. Pendientes de definición

### 13.1 Resueltos

| ID | Pendiente | Decisión | Impacto |
|---|---|---|---|
| PD-02 | ¿El objetivo se expresa como plazo o como fecha límite? | **Plazo en semanas** | `Perfil.PlazoSemanas`, selector de P3 |
| PD-04 | ¿Se migran las 21 semanas de bitácora? | **No. El registro arranca desde cero** | Fuera de alcance 5.2; el histórico queda como consulta en el anexo |
| PD-10 | ¿Un usuario o varios? | **Multiusuario en el modelo desde el día uno, corriendo local por ahora** | D-12, D-13, RN-12, RF-I. Multiusuario sale de "fuera de alcance" |
| PD-01 | Altura, fecha de nacimiento y sexo del perfil | **Ya no es un pendiente del diseño**: son datos que cada usuario carga en Ajustes. Ninguno es obligatorio para empezar a usar el sistema | RF-E1. El sistema indica qué falta cuando un cálculo lo necesita, en lugar de asumir un valor |
| PD-05 | Confirmar el % de grasa objetivo | **Ya no es un pendiente del diseño**: lo define cada usuario en Ajustes, y el sistema no propone ninguno por defecto | RF-E3 |
| PD-11 | ¿Hasta dónde llega el módulo de gym? | **Rutinas editables más registro de sesiones con la carga real y su progresión** | RF-J, RF-K. Sale de "fuera de alcance v2" |
| PD-12 | ¿Cómo se registra el cardio? | **Con sus parámetros: tiempo, velocidad, inclinación, kcal e intervalos** | RF-L, RN-14 |
| PD-06 | Para los refrescos parciales, ¿`fetch` a mano o htmx? | **htmx** | D-16. Los handlers devuelven fragmentos de HTML y el markup declara dónde se insertan |
| PD-14 | ¿Se marca esfuerzo percibido por serie? | **No en la versión 1**, para no agregar fricción al registro (R-14). Queda como columna opcional a futuro | RF-K3 |
| PD-15 | ¿Las kcal de una sesión de gym se estiman por rutina o por sesión? | **Se definen en el día de la rutina y se pueden ajustar en cada sesión** | D-17, RF-K7, RN-02. Elimina la causa del doble conteo en lugar de advertirla |

### 13.2 Abiertos

Ninguno bloquea el desarrollo: todos tienen un valor por defecto razonable o son locales a una pantalla que todavía no se construyó.

| ID | Pendiente | Necesario para |
|---|---|---|
| PD-03 | ¿Objetivo diario de proteína a mostrar como referencia? | RF-E4 |
| PD-07 | ¿Qué proveedor para la búsqueda asistida? Ver D-09 | RF-A7, etapa 15 |
| PD-08 | ¿La adherencia se mide contra el déficit del plan, o contra un valor fijo aparte? | RF-H7 |
| PD-09 | ¿El dashboard debería ser la pantalla inicial, en lugar de Hoy? | RF-H1 |
| PD-13 | ¿Qué plantillas se ofrecen además de la derivada del Excel? | RF-G2, etapa 5 |
