# NodeScope IT/OT

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

NodeScope es una plataforma multi-componente para ingestar, validar y explorar datasets tecnicos en entornos IT/OT.

## Estructura del repositorio

- `src/`: backend en .NET y capas de dominio/aplicacion.
- `src/NodeScope.Worker/`: worker .NET que orquesta el procesamiento de ingestas.
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

### 2) Configurar JWT (solo desarrollo)

Define la clave JWT con una longitud minima de 32 caracteres (no la commits en appsettings):

```bash
export Jwt__SigningKey="dev-signing-key-please-change-32chars+"
```

### 3) Iniciar API

```bash
dotnet run --project src/NodeScope.Api/NodeScope.Api.csproj
```

URL por defecto de la API: `http://localhost:5003`

### 4) Iniciar worker

```bash
dotnet run --project src/NodeScope.Worker/NodeScope.Worker.csproj
```

### 5) Iniciar frontend

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

## Proyecto

- Licencia: MIT (`LICENSE`)
- Cambios: `CHANGELOG.md`
- Contribuir: `CONTRIBUTING.md`
- Seguridad: `SECURITY.md`
- Conducta: `CODE_OF_CONDUCT.md`

## Docker (local)

API:

```bash
docker build -t nodescope-api:latest .
docker run --rm -p 8080:8080 \
  -e ConnectionStrings__Database="Host=host.docker.internal;Port=5432;Database=nodescope;Username=nodescope;Password=changeme" \
  -e Jwt__Issuer="https://localhost/nodescope" \
  -e Jwt__Audience="nodescope-spa-clients" \
  -e Jwt__SigningKey="dev-signing-key-please-change-32chars+" \
  nodescope-api:latest
```

Worker:

```bash
docker build -t nodescope-worker:latest -f Dockerfile.worker .
docker run --rm \
  -e ConnectionStrings__Database="Host=host.docker.internal;Port=5432;Database=nodescope;Username=nodescope;Password=changeme" \
  nodescope-worker:latest
```

Frontend:

```bash
docker build -t nodescope-web:latest -f frontend/nodescope-web/Dockerfile .
docker run --rm -p 8080:8080 nodescope-web:latest
```

Compose (API + worker + web + Postgres):

```bash
docker compose -f compose.prod.yaml up --build
```

`compose.prod.yaml` monta el volumen `nodescope_storage` en `/data` en **api** y **worker** (misma ruta que `Processing__StorageRoot`) para que las subidas y los artefactos (informe HTML, JSON, CSV) estén en un disco compartido. La imagen del worker incluye Python y el procesador bajo `/opt/nodescope-processor`. Los imports completados **antes** de usar ese volumen pueden seguir apuntando a rutas antiguas: usa **Re-run analysis** o vuelve a subir el fichero.
