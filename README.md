# NodeScope IT/OT

> Plataforma web multi-componente para ingestar, analizar, validar y explorar datasets técnicos en entornos IT/OT industriales.

---

## 📌 Qué es NodeScope

NodeScope es una herramienta operativa orientada a equipos que trabajan con exportaciones de sistemas SCADA/HMI, configuraciones de canales OPC UA y registros maestros de variables de PLC.

El flujo central del sistema consiste en:

1. **Subir** un dataset técnico (Excel, CSV o JSON exportado desde el entorno industrial)
2. **Procesar** y normalizar automáticamente las variables mediante el motor Python
3. **Cruzar** los datos con las fuentes de verdad (Excel maestro de variables)
4. **Detectar incidencias**: duplicados de NodeId, tipos inconsistentes, variables huérfanas, namespaces mal configurados
5. **Visualizar** el resultado en un dashboard con métricas, tabla navegable y filtros
6. **Exportar** informes HTML interactivos y CSVs derivados con trazabilidad completa

---

## 🏭 Contexto IT/OT: Líneas de producción

NodeScope está diseñado para gestionar instalaciones industriales con múltiples líneas de producción, cada una con su propio PLC, configuración OPC UA y pantallas SCADA/HMI.

| Línea | Descripción |
|---|---|
| **Línea A** | Línea principal, referencia de estructura canónica |
| **Línea B** | Segunda línea de producción |
| **Línea C** | Tercera línea de producción |

Cada línea exporta tres tipos de archivos que NodeScope procesa y cruza:

- **JSON SCADA/HMI** — exportado desde el HMI: contiene las variables vinculadas a los sinópticos y pantallas de producción.
- **JSON del Canal** — configuración del canal OPC UA: define qué variables expone el PLC al servidor OPC UA.
- **Excel maestro de variables** — fuente de verdad absoluta: lista oficial de variables aprobadas con sus tipos, DBs y namespaces.

---

## 🔍 Capacidades de análisis

El motor de análisis (Python) realiza un **cruce exhaustivo** entre las tres fuentes:

- Detecta variables presentes en el SCADA pero ausentes en el canal OPC UA
- Detecta variables del canal que no aparecen en el Excel maestro
- Valida consistencia de tipos entre PLC, canal y SCADA
- Identifica NodeIds duplicados o mal formados
- Detecta variables huérfanas sin DB asignado
- Genera un informe de auditoría con severidad: `[CRITICO]`, `[ALTO]`, `[MEDIO]`
- Exporta CSV con los resultados filtrados y descargables

---

## 🏗️ Arquitectura

```
NodeScope IT/OT
├── src/                         # Backend .NET 9
│   ├── NodeScope.Api/           # API REST + JWT Auth + Swagger
│   ├── NodeScope.Application/   # Casos de uso y orquestación
│   ├── NodeScope.Domain/        # Entidades y lógica de negocio
│   ├── NodeScope.Infrastructure/ # PostgreSQL + EF Core + persistencia
│   ├── NodeScope.Worker/        # Worker de ingesta asincróna
│   ├── NodeScope.AppHost/       # .NET Aspire orquestador
│   └── NodeScope.ServiceDefaults/ # Configuración compartida y observabilidad
├── frontend/nodescope-web/      # SPA Angular
│   ├── Dashboard con métricas
│   ├── Tabla navegable con filtros
│   ├── Gestión de importaciones e historial
│   └── Visor de incidencias y comparativas
├── python/processor/            # Motor de análisis y reportes
│   ├── Normalización de columnas
│   ├── Cruce de fuentes (SCADA/Canal/Excel)
│   ├── Detección de incidencias
│   ├── Generación de JSON canónico
│   └── Generación de informe HTML interactivo + CSVs
└── compose.yaml                 # PostgreSQL local vía Docker
```

**Stack tecnológico:**

| Capa | Tecnología |
|---|---|
| Backend | .NET 9 · ASP.NET Core · EF Core · SignalR · JWT |
| Base de datos | PostgreSQL (Docker) |
| Observabilidad | OpenTelemetry · .NET Aspire |
| Frontend | Angular · TypeScript · SCSS · RxJS · Signals |
| Motor de análisis | Python 3.11+ · pandas · openpyxl · jinja2 |
| Gestor de paquetes | pnpm (frontend) |

---

## 🚀 Desarrollo local

### Requisitos

- .NET SDK 9
- Node.js 20+ y `pnpm`
- Python 3.11+ (validado en 3.12)
- Docker

### 1) Iniciar PostgreSQL

```bash
docker compose up -d
```

### 2) Iniciar API

```bash
dotnet run --project src/NodeScope.Api/NodeScope.Api.csproj
```

URL de la API: `http://localhost:5003` — Swagger en `/swagger`

### 3) Iniciar frontend

```bash
cd frontend/nodescope-web
pnpm install
pnpm start
```

URL del frontend: `http://localhost:4200`

### 4) Generar informe HTML desde CLI

```bash
python3 python/processor/generarInformeNodos.py input.xlsx
```

Con salida personalizada y límite de filas:

```bash
python3 python/processor/generarInformeNodos.py input.xlsx output.html --limite-filas 5000
```

---

## 🔐 Credenciales de desarrollo

| Campo | Valor |
|---|---|
| Correo | `dev@nodescope.local` |
| Contraseña | `ChangeMe123!` |

---

## 📌 Notas

- Los artefactos generados, cachés y salidas de compilación se excluyen con `.gitignore`.
- El motor Python se invoca desde el Worker .NET; no actúa como backend independiente.
- Mantener utilidades en su dominio funcional (scripts CLI de Python dentro de `python/processor/`).
- El gestor de paquetes del frontend es **obligatoriamente `pnpm`**; no usar npm ni yarn.
