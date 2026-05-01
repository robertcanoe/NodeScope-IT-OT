# NodeScope IT/OT

NodeScope es una plataforma multi-componente para ingestar, validar y explorar datasets tecnicos en entornos IT/OT.

## Estructura del repositorio

- `src/`: backend en .NET y capas de dominio/aplicacion.
- `frontend/nodescope-web/`: aplicacion web Angular.
- `python/processor/`: procesamiento de ingesta y generacion de informes en Python.
- `storage/`: almacenamiento local de subidas y artefactos generados (ignorado en git).
- `compose.yaml`: servicio local de PostgreSQL.

## Requisitos

- .NET SDK 9
- Node.js 20+ y `pnpm`
- Python 3.11+ (3.12 validado)
- Docker (para PostgreSQL local)

## Desarrollo local

### 1) Iniciar PostgreSQL

```bash
docker compose up -d
```

### 2) Iniciar API

```bash
dotnet run --project src/NodeScope.Api/NodeScope.Api.csproj
```

URL por defecto de la API: `http://localhost:5003`

### 3) Iniciar frontend

```bash
cd frontend/nodescope-web
pnpm install
pnpm start
```

URL por defecto del frontend: `http://localhost:4200`

## Procesador Python e informes

El script del worker de ingesta se configura en:

- `src/NodeScope.Api/appsettings.json` -> `Processing.ProcessorScriptPath`

Script CLI para generar informe HTML interactivo:

```bash
python3 python/processor/generarInformeNodos.py data-prueba/Nodos_DBPruebasFormacion.xlsx
```

Salida personalizada y limite de filas:

```bash
python3 python/processor/generarInformeNodos.py input.xlsx output.html --limite-filas 5000
```

## Usuario de desarrollo por defecto

- Correo: `dev@nodescope.local`
- Contrasena: `ChangeMe123!`

## Notas

- Los artefactos generados, caches y salidas de compilacion se excluyen con `.gitignore`.
- Mantener utilidades en su dominio funcional (por ejemplo, scripts CLI de Python dentro de `python/processor/`).
