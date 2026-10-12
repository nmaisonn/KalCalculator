# kcal-tracker

App web para seguimiento de alimentación, entrenamiento y composición corporal. ASP.NET Core Razor Pages (.NET 10), EF Core y SQLite local. Cada cuenta ve solo lo suyo. La UI está en español rioplatense (tratá de *vos*).

Este README es la fuente de verdad para clonar, instalar lo mínimo y dejar el servidor corriendo. Si sos una IA, leé primero la sección **Para una IA**.

Hay un archivo listo para pegarle a otra IA que todavía no tiene el repo: [`PARA-LA-IA.txt`](PARA-LA-IA.txt).

## Clonar

```bash
git clone https://github.com/nmaisonn/KalCalculator.git kcal-tracker
cd kcal-tracker
```

Si el repo es **privado**, invitá a la otra persona como colaborador en GitHub (Settings → Collaborators) antes de que clone. Después, Git + SDK .NET 10 y los comandos de **Cómo levantar**.

## Para una IA que abre este workspace

Hacé esto, en este orden, sin inventar Docker, Node, PostgreSQL ni Azure:

1. El workspace tiene que ser la **raíz del repo** (el directorio que contiene `KcalTracker.slnx` y `global.json`). No abras solo `src/KcalTracker`.
2. Comprobá `dotnet --version`. Tiene que empezar con `10.`. Si no hay SDK 10, instalalo desde [dotnet.microsoft.com/download/dotnet/10.0](https://dotnet.microsoft.com/download/dotnet/10.0) y reintentá. `global.json` pide `10.0.300` con `rollForward: latestFeature` (sirve cualquier 10.0.x).
3. Desde la raíz:

```bash
dotnet restore
dotnet test
dotnet run --project src/KcalTracker --launch-profile http
```

4. Abrí **http://localhost:5236**. Si pide login, andá a **Crear una cuenta** (`/Registro`): correo + contraseña de al menos 8 caracteres. No uses cuentas de prueba que puedan estar en la base. No borres `data/kcal.db`.
5. La primera vez de una cuenta nueva aparece **Bienvenida** para importar plantillas (alimentos, gym, cardio). Es opcional; la app igual corre.
6. Parar el servidor: Ctrl+C. Si `dotnet test` o el build fallan porque `KcalTracker.exe` está locked, cortá el `dotnet run` y reintentá.
7. No hace falta Node, Python, Docker, base de datos aparte ni claves de API para levantar. No crees `.env` ni pongas secretos en `appsettings.json`.
8. No hagas commit ni push a menos que te lo pidan. No cambies `git config`.
9. Al tocar UI, verificá en el navegador el flujo (no alcanza un screenshot). La UI usa *vos*.
10. Los tests (`dotnet test`) son xUnit con SQLite en memoria; no necesitan red.

Windows, macOS y Linux sirven. El perfil `http` no pide certificado.

## Prerrequisitos

| Qué | Versión | Para qué |
|---|---|---|
| [Git](https://git-scm.com/) | cualquiera reciente | clonar |
| [.NET SDK](https://dotnet.microsoft.com/download/dotnet/10.0) | **10.0.x** | restaurar, testear y correr |

Comprobar el SDK:

```bash
dotnet --version
```

En Windows, solo si más adelante usás HTTPS:

```bash
dotnet dev-certs https --trust
```

## Cómo levantar (persona o IA)

Desde la raíz del repo:

```bash
dotnet restore
dotnet test
dotnet run --project src/KcalTracker --launch-profile http
```

Abrir **http://localhost:5236**.

### Cuenta

- **Alguien nuevo (p. ej. otro equipo o un familiar):** en la pantalla Entrar, **Crear una cuenta**. Queda aislada: no ve las comidas ni las pesadas de otras cuentas aunque compartan el mismo `kcal.db`.
- **Base vacía en Development:** si no hay ningún usuario, al arrancar se crea `demo@localhost` / `password`. Esa semilla **no corre** si la base ya tiene cuentas.
- Este repo versiona `data/kcal.db`. Puede traer cuentas de quien lo subió. No las uses; registrá la tuya.

Después del registro, `/Bienvenida` ofrece plantillas (catálogo derivado de la planilla original). Importarlas o saltearlas.

Parar: Ctrl+C. Volver a correr: el mismo `dotnet run`.

### Perfil HTTPS (opcional)

```bash
dotnet run --project src/KcalTracker --launch-profile https
```

Abre `https://localhost:7083` (también escucha HTTP en 5236).

## Dónde queda la base

SQLite en **`data/kcal.db`**, junto al repo, para que clone y commit se lleven el historial. Ese archivo **sí va a git**. Los `-wal`/`-shm` no.

La app encuentra la raíz buscando `KcalTracker.slnx` hacia arriba desde el content root. Si corrés un publish sin el `.slnx` cerca, cae a `%LocalAppData%\kcal-tracker\kcal.db` (o el equivalente en Linux/macOS).

Para apuntar a otro archivo, secretos de usuario o `appsettings.*.local.json` (ignorado por git):

```json
{
  "ConnectionStrings": {
    "Kcal": "Data Source=C:/ruta/kcal.db"
  }
}
```

## Claves opcionales (búsqueda nutricional)

Sin estas claves la app corre igual. Faltan solo sugerencias USDA y estimación de platos caseros por modelo.

| Variable o secreto | Qué desbloquea | Dónde sacarla |
|---|---|---|
| `USDA_API_KEY` o `Nutricion:UsdaApiKey` | búsqueda USDA FoodData Central | gratis en [fdc.nal.usda.gov](https://fdc.nal.usda.gov/) |
| `OPENAI_API_KEY` o `Nutricion:OpenAiApiKey` | estimación de platos caseros | clave de OpenAI o Azure OpenAI |

Preferí secretos de usuario (no se suben al repo):

```bash
cd src/KcalTracker
dotnet user-secrets set "Nutricion:UsdaApiKey" "PEGAR_CLAVE"
dotnet user-secrets set "Nutricion:OpenAiApiKey" "PEGAR_CLAVE"
```

También sirven variables de entorno `USDA_API_KEY` y `OPENAI_API_KEY`. No las pongas en `appsettings.json`.

Para Azure OpenAI, además podés setear `Nutricion:OpenAiBaseUrl` y `Nutricion:OpenAiModel`.

## Tests

```bash
dotnet test
```

Si el build falla porque `KcalTracker.exe` está en uso, cortá el `dotnet run` y reintentá.

## Herramientas de EF Core (solo para migraciones nuevas)

El arranque ya aplica migraciones. Esto es solo si vas a cambiar el modelo:

```bash
dotnet tool restore
dotnet ef migrations add Nombre --project src/KcalTracker --output-dir Datos/Migraciones
```

El manifiesto está en `.config/dotnet-tools.json`.

## Estructura

```
KcalTracker.slnx          solución (no hay .sln)
global.json               SDK .NET 10
PARA-LA-IA.txt            prompt para clonar en otra máquina
data/kcal.db              SQLite (sí va al repo)
src/KcalTracker/          app Razor Pages
tests/KcalTracker.Tests/  xUnit
docs/                     requerimientos, pantallas, modelo, anexo del Excel
docs/prototipo/           mockup HTML (no hace falta para correr la app)
```

Pantallas con la app en marcha: Hoy (`/`), Dashboard, Objetivo, Mediciones, Alimentos, Combos, Rutinas, Ejercicios, Cardio, Actividades, Ajustes. Login: `/Ingreso`. Alta: `/Registro`.

## Qué cubre el producto (útil si vas a tocar código)

Alimentación (catálogo, combos, registro diario), gym (ejercicios, rutinas, sesión), cardio, actividades, composición corporal, metabolismo/gasto/objetivo y un dashboard de período. Aislamiento por `UsuarioId` con filtro global de EF Core.

Reglas que no hay que “corregir” de casualidad:

- GET cotidiano = GER × PAL (sedentario). Gym, cardio y deporte se suman **brutos** al gasto del día; el cupo “A comer” sigue siendo GET sedentario − objetivo.
- Un día sin comidas no es ingesta 0: no entra en medias ni en el déficit del dashboard.
- Grasa/músculo en kg no se tipean: se derivan de peso × %.
- En Dashboard, **Exportar para analizar** arma un Excel del período (comida, entreno, mediciones) pensado para pasárselo a un modelo. La hoja `Para_IA` del xlsx explica las reglas.

## Documentación de producto

| Documento | Contenido |
|---|---|
| [01 — Requerimientos](docs/01-requerimientos.md) | Objetivos, RF/RNF, reglas de negocio |
| [02 — Pantallas](docs/02-pantallas.md) | Navegación y wireframes |
| [03 — Modelo y decisiones](docs/03-modelo-y-decisiones.md) | Datos, arquitectura, decisiones |
| [Anexo — Análisis del Excel](docs/anexos/analisis-excel.md) | Evidencia de la planilla original |
| [`docs/prototipo/prototipo.html`](docs/prototipo/prototipo.html) | Mockup navegable (doble clic) |

El prototipo no es la app: no persiste. Para verificar su lógica (hace falta Node): `node docs/prototipo/verificar.js`.

## Si algo no arranca

| Síntoma | Qué hacer |
|---|---|
| `dotnet --version` no es 10.x | Instalar el SDK 10 desde el enlace de prerrequisitos |
| Puerto 5236 en uso | Cortá `KcalTracker.exe` / el `dotnet run` anterior, o cambiá `applicationUrl` en `src/KcalTracker/Properties/launchSettings.json` |
| `KcalTracker.exe` locked al testear | El servidor sigue vivo; Ctrl+C y reintentá |
| HTTPS no confiable en el navegador | Usá `--launch-profile http` |
| Quiere una base vacía | No borres el `kcal.db` del repo salvo que te lo pidan. Registrá una cuenta nueva |
| Semilla demo no aparece | Normal si `data/kcal.db` ya tiene usuarios |

`data/kcal.db` sí se versiona. No commitees `.env` ni secretos.
