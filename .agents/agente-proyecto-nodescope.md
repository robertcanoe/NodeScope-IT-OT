# NodeScope IT/OT — Especificación completa del proyecto

## Visión del producto

NodeScope IT/OT es una plataforma web para **analizar, validar, visualizar y explotar datos técnicos** procedentes de Excel, CSV, exportaciones OPC UA y fuentes de telemetría relacionadas con entornos IT y OT. El objetivo es que un usuario suba un dataset técnico y obtenga una aplicación útil desde el minuto uno: panel de métricas, tabla navegable, filtros, validaciones, trazabilidad de importaciones, detección de errores y generación de informes HTML interactivos. La idea encaja con un stack profesional donde el backend principal vive en C# y el procesamiento especializado de datos se resuelve con Python, mientras el frontend se encarga de ofrecer una experiencia clara, rápida y mantenible.[cite:11][cite:12][cite:13][web:65][web:66][web:69]

El problema real que resuelve es muy concreto: en equipos de soporte, automatización, SCADA, integración y software industrial es frecuente recibir archivos técnicos desestructurados con listas de variables, nodos, tags, señales, inventarios o lecturas, pero sin una capa de visualización ni validación operativa. Angular se ha estudiado incluso en contextos HMI/SCADA near real-time, y OPC UA suele terminar expuesto mediante dashboards de supervisión y exploración, por lo que un producto orientado a inspección y validación tiene sentido técnico y práctico.[web:31][web:41][web:47][web:65]

## Propuesta exacta

NodeScope IT/OT será una **plataforma web multiusuario** donde cada usuario podrá crear proyectos de análisis, subir ficheros técnicos, procesarlos, guardar ejecuciones y abrir resultados renderizados en un dashboard web. Cada carga generará tres salidas utilizables: una representación estructurada persistida en base de datos, una capa de métricas para el panel del frontend y un informe HTML interactivo exportable para compartir resultados con otras personas del equipo.[web:64][web:67][web:69][web:72]

La plataforma no será un simple visor. Incluirá reglas de validación orientadas a datos técnicos: detección de NodeId duplicados, namespaces dominantes, tipos de dato más frecuentes, inconsistencias en naming, celdas vacías, errores de formato, agrupaciones por origen y estadísticas comparativas entre importaciones. Ese enfoque la convierte en una herramienta útil para soporte, QA técnico, validación de ingeniería, formación y revisión previa a integraciones de campo.[web:65][web:68][web:71]

## Qué hace el sistema

### Flujo funcional principal

1. El usuario inicia sesión y crea un proyecto de trabajo.
2. Sube un archivo Excel, CSV o JSON técnico.
3. El backend en ASP.NET Core guarda el fichero, registra la importación y lanza el proceso de análisis.
4. Un servicio Python procesa el fichero, normaliza columnas, calcula métricas y genera un JSON estructurado más un informe HTML renderizado.
5. El backend persiste los resultados principales en PostgreSQL y guarda los artefactos generados.
6. El frontend Angular consulta la API y muestra dashboard, tabla, incidencias, comparativas e historial.
7. El usuario puede abrir el informe HTML generado, descargar CSV filtrado y revisar errores detectados en la ejecución.[web:59][web:64][web:66][web:73]

### Casos de uso incluidos

- Analizar una exportación OPC UA y ver distribución por tipos de dato, namespaces y nodos repetidos.[web:65][web:68][web:71]
- Subir un Excel de señales y detectar inconsistencias de naming o columnas incompletas.[web:60][web:69][web:72]
- Comparar dos importaciones del mismo proyecto para ver qué cambió entre versiones del dataset técnico.[web:64][web:67]
- Compartir un informe HTML visual con soporte, QA o ingeniería sin necesidad de abrir Python ni Excel.[web:64][web:67]
- Centralizar histórico de importaciones para auditoría técnica interna.[web:66][web:69]

## Stack elegido

## Backend principal

El backend principal será **ASP.NET Core Web API con .NET 8/9 y C#** porque es el núcleo del producto, la capa de autenticación, el punto de orquestación de procesos, la API consumida por el frontend y la base del modelo de dominio. Además, .NET Aspire está enfocado a aplicaciones distribuidas y observables, lo que lo hace muy adecuado cuando hay API, workers y servicios auxiliares dentro de una misma solución.[web:22][web:27][web:51]

### Tecnologías backend

- ASP.NET Core Web API para endpoints REST.[web:42]
- Entity Framework Core para acceso a datos relacionales.[cite:12]
- PostgreSQL como base de datos principal por flexibilidad, buen soporte JSON y buen encaje con analítica técnica.
- FluentValidation para validación de requests.
- MediatR opcional para organizar casos de uso por comandos y consultas.
- Serilog para logging estructurado.
- OpenTelemetry para trazas, métricas y correlación entre frontend, API y procesos.[web:49][web:50][web:51]
- .NET Aspire para orquestación local del ecosistema distribuido, service discovery y observabilidad unificada.[web:22][web:27]
- SignalR para estados de procesamiento en tiempo real, por ejemplo “subiendo”, “procesando”, “completado” o “error”.[web:36]
- JWT Bearer para autenticación.
- Swagger/OpenAPI para documentación técnica interna.

### Arquitectura backend

La arquitectura será **modular monolith bien separada**, no microservicios desde el principio. Esa decisión es directa y práctica: un solo despliegue principal, separación por capas y módulos claros, pero con un diseño listo para escalar a servicios independientes si un día fuese necesario. .NET Aspire y OpenTelemetry aportan valor incluso sin necesidad de ir a una arquitectura de microservicios completa.[web:22][web:27][web:49]

Capas:

- `Api`: controladores, auth, DTOs, middlewares.
- `Application`: casos de uso, validaciones, contratos, servicios de aplicación.
- `Domain`: entidades, value objects, reglas de negocio.
- `Infrastructure`: EF Core, repositorios, almacenamiento de archivos, integración con Python, observabilidad.
- `Worker`: procesamiento asíncrono de importaciones pesadas.

### Módulos backend

- `Auth`: login, refresh token, permisos.
- `Projects`: proyectos de análisis.
- `Imports`: subida de ficheros, registro de importaciones, estados.
- `Datasets`: estructura normalizada del archivo procesado.
- `Validation`: reglas e incidencias detectadas.
- `Reports`: informes HTML y exportaciones.
- `Telemetry`: métricas internas, estados, tiempos de proceso.
- `Audit`: trazabilidad de acciones sobre proyectos e importaciones.

## Frontend

El frontend será **Angular** porque encaja mejor para una aplicación seria, escalable y con UI de tipo panel operativo. También se ha utilizado y estudiado en escenarios cercanos a HMI/SCADA web, y permite estructurar una SPA sólida con dominios bien separados.[web:31][web:35][web:37][web:39][web:42]

### Tecnologías frontend

- Angular 20/21 con TypeScript, usando standalone components y routing moderno.[cite:12]
- Angular Material o una UI custom ligera basada en CDK, según el acabado visual deseado.
- RxJS para flujos reactivos.
- Signals de Angular para estado local cuando aporte simplicidad.
- NgRx solo si finalmente el estado global crece mucho; para este proyecto puede mantenerse sin él al inicio operativo.
- Chart.js o ECharts para métricas visuales en dashboards; el patrón de Python + JS para alimentar gráficos está ampliamente usado.[web:59][web:61][web:73]
- Angular HttpClient para integración con la API.
- Angular Router con lazy loading por dominios.
- Guards para control de acceso.
- OpenTelemetry browser tracing para correlacionar acciones del usuario con backend si se desea observabilidad extremo a extremo.[web:49]

### Módulos frontend

- `auth`: login, logout, sesión.
- `dashboard`: resumen global de proyectos e importaciones.
- `projects`: listado, detalle y configuración.
- `imports`: subida de archivos, histórico y estados.
- `analysis`: KPIs, gráficos, hallazgos, incidencias.
- `dataset-viewer`: tabla principal con filtros y búsqueda avanzada.
- `reports`: visor del HTML generado y descargas.
- `compare`: comparación entre importaciones del mismo proyecto.
- `settings`: perfil, preferencias y parámetros de validación.

## Python dentro del stack

Python no será el backend principal. Python será el **motor especializado de análisis y renderizado documental**. Ese papel es perfecto porque pandas, openpyxl y la generación de HTML automatizado son ideales para transformar ficheros técnicos en reportes visuales e interactivos.[web:59][web:64][web:67][web:69]

### Qué hará Python exactamente

- Leer Excel, CSV y JSON técnicos.
- Normalizar cabeceras y columnas.
- Detectar columnas equivalentes como `Nombre de variable`, `NodeId`, `Tipo de dato`, `Namespace`, `Ruta`, `Descripción`.
- Calcular métricas agregadas.
- Detectar duplicados, vacíos, formatos sospechosos y agrupaciones útiles.
- Generar un JSON canónico para que C# lo consuma.
- Generar un HTML interactivo listo para abrirse en el navegador.
- Generar CSVs auxiliares derivados, por ejemplo incidencias o nodos duplicados.

### Librerías Python

- `pandas` para transformación de datos.[web:61][web:69]
- `openpyxl` para lectura de Excel.
- `jinja2` para plantillas HTML cuando convenga estructurar mejor los informes.[web:64]
- `json` y `pathlib` para serialización y manejo de archivos.
- `python-dateutil` si se requiere mejor tratamiento de fechas.

### Cómo se integrará con C#

La integración será directa por **ejecución de proceso controlado** desde el backend o worker de .NET. El backend guardará el archivo subido, invocará el script Python con los parámetros necesarios, y Python devolverá artefactos en disco y un JSON con metadatos de la ejecución. Después, C# leerá esos resultados, persistirá la parte necesaria en base de datos y expondrá todo al frontend mediante endpoints claros.[web:61][web:64][web:73]

Contrato de entrada hacia Python:

```json
{
  "importId": "guid",
  "projectId": "guid",
  "inputPath": "/data/uploads/file.xlsx",
  "outputDir": "/data/results/import-guid",
  "profile": "opcua-default"
}
```

Contrato de salida desde Python:

```json
{
  "success": true,
  "totalRows": 1280,
  "totalColumns": 6,
  "reportHtmlPath": "/data/results/import-guid/report.html",
  "normalizedJsonPath": "/data/results/import-guid/dataset.json",
  "issuesCsvPath": "/data/results/import-guid/issues.csv",
  "metrics": {
    "duplicates": 12,
    "nullNodeIds": 0,
    "dominantType": "String",
    "dominantNamespace": "ns=4"
  }
}
```

## Base de datos

La base principal será **PostgreSQL**. La razón es sencilla: relaciones claras para usuarios, proyectos e importaciones, y además soporte cómodo para almacenar JSONB con resúmenes técnicos o snapshots normalizados cuando interese guardar estructuras flexibles.

### Entidades principales

#### User

- `Id`
- `Email`
- `PasswordHash`
- `DisplayName`
- `Role`
- `CreatedAt`
- `LastLoginAt`

#### Project

- `Id`
- `OwnerUserId`
- `Name`
- `Description`
- `SourceType` (`OPCUA`, `ExcelSignals`, `GenericCsv`, `Logs`)
- `CreatedAt`
- `UpdatedAt`

#### ImportJob

- `Id`
- `ProjectId`
- `OriginalFileName`
- `StoredFilePath`
- `Status` (`Pending`, `Processing`, `Completed`, `Failed`)
- `StartedAt`
- `CompletedAt`
- `ProcessorVersion`
- `RowCount`
- `IssueCount`
- `ReportHtmlPath`
- `NormalizedJsonPath`
- `SummaryJson`

#### DatasetColumn

- `Id`
- `ImportJobId`
- `Name`
- `NormalizedName`
- `DataTypeDetected`
- `DistinctCount`
- `NullCount`

#### DatasetRecord

Esta tabla no guardará siempre el dataset entero como columnas físicas si el archivo es variable. Para datasets heterogéneos se almacenará una forma híbrida:

- `Id`
- `ImportJobId`
- `RecordIndex`
- `PayloadJson`

#### ValidationIssue

- `Id`
- `ImportJobId`
- `Severity` (`Info`, `Warning`, `Error`)
- `Code`
- `Message`
- `ColumnName`
- `RowIndex`
- `RawValue`
- `CreatedAt`

#### GeneratedArtifact

- `Id`
- `ImportJobId`
- `Type` (`ReportHtml`, `IssuesCsv`, `NormalizedJson`, `FilteredCsv`)
- `Path`
- `MimeType`
- `SizeBytes`
- `CreatedAt`

#### AuditLog

- `Id`
- `UserId`
- `ProjectId`
- `Action`
- `TargetType`
- `TargetId`
- `MetadataJson`
- `CreatedAt`

## Almacenamiento de archivos

Los ficheros se almacenarán fuera de la base de datos, en disco o almacenamiento compatible con objeto según entorno. La aplicación trabajará con una abstracción `IFileStorage` para que el sistema funcione igual en local y en despliegue. Lo importante es que haya tres carpetas lógicas:

- `uploads/` para originales.
- `processing/` para trabajo temporal.
- `artifacts/` para reportes, JSONs y CSVs derivados.

## API exacta

### Auth

- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `POST /api/auth/logout`
- `GET /api/auth/me`

### Projects

- `GET /api/projects`
- `POST /api/projects`
- `GET /api/projects/{projectId}`
- `PUT /api/projects/{projectId}`
- `DELETE /api/projects/{projectId}`

### Imports

- `GET /api/projects/{projectId}/imports`
- `POST /api/projects/{projectId}/imports`
- `GET /api/imports/{importId}`
- `GET /api/imports/{importId}/status`
- `POST /api/imports/{importId}/reprocess`
- `GET /api/imports/{importId}/download/original`

### Analysis

- `GET /api/imports/{importId}/summary`
- `GET /api/imports/{importId}/columns`
- `GET /api/imports/{importId}/records`
- `GET /api/imports/{importId}/issues`
- `GET /api/imports/{importId}/charts`
- `GET /api/imports/{importId}/namespaces`
- `GET /api/imports/{importId}/types`

### Reports

- `GET /api/imports/{importId}/report`
- `GET /api/imports/{importId}/artifacts`
- `GET /api/artifacts/{artifactId}/download`

### Compare

- `GET /api/projects/{projectId}/compare?leftImportId=...&rightImportId=...`

## Validaciones incluidas

El producto incluirá validaciones directas y útiles desde el primer día, no como extras secundarios. Estas reglas se ejecutarán sobre el dataset ya normalizado:

- NodeId duplicado.
- Nombre de variable duplicado.
- Tipo de dato vacío o no reconocido.
- Namespace no detectable dentro del NodeId.
- Filas con campos críticos vacíos.
- Detección de columnas esperadas ausentes.
- Inconsistencia de formato entre filas equivalentes.
- Exceso de longitud en nombres técnicos.
- Diferencias entre importaciones consecutivas del mismo proyecto.

Cada incidencia tendrá severidad, fila afectada, columna implicada y mensaje legible para usuario técnico.

## Frontend exacto por pantallas

### Login

Pantalla simple con email y contraseña, más acceso al último proyecto usado si la sesión ya existe.

### Dashboard principal

Mostrará:

- Total de proyectos.
- Total de importaciones.
- Últimas ejecuciones.
- Errores recientes.
- Distribución por tipo de fuente.
- Accesos rápidos a proyectos recientes.

### Detalle de proyecto

Mostrará:

- Datos básicos del proyecto.
- Tabla de importaciones históricas.
- Estado de la última ejecución.
- Botón de nueva subida.
- Acceso a comparación entre importaciones.

### Pantalla de importación

Permitirá:

- Arrastrar o seleccionar archivo.
- Ver validación inicial de extensión.
- Seguir el estado en vivo con SignalR.
- Mostrar resultado final de forma inmediata al terminar el análisis.[web:36]

### Vista de análisis

Tendrá:

- KPIs superiores.
- Gráfico de tipos de dato.
- Gráfico de namespaces.
- Resumen de incidencias.
- Ranking de columnas con más problemas.
- Tarjetas con hallazgos destacados.

### Dataset viewer

Tendrá:

- Tabla paginada.
- Búsqueda por texto.
- Filtro por tipo.
- Filtro por namespace.
- Ordenación por columnas.
- Exportación de la vista actual.

### Reports

Permitirá:

- Abrir el HTML renderizado generado por Python.
- Descargar CSVs derivados.
- Revisar metadatos de generación.

### Compare

Permitirá:

- Elegir dos importaciones del mismo proyecto.
- Ver diferencias en filas, tipos, namespaces y duplicados.
- Resaltar altas, bajas y cambios.

## Comunicación en tiempo real

SignalR se usará para la experiencia de procesamiento. El frontend abrirá un canal durante la subida y recibirá eventos de estado. Esto encaja bien con procesos que pueden tardar varios segundos mientras Python lee, transforma y genera artefactos.[web:36]

Eventos:

- `import-uploaded`
- `import-started`
- `import-progress`
- `import-completed`
- `import-failed`

## Observabilidad

La observabilidad será parte del stack operativo desde el primer momento. .NET Aspire y OpenTelemetry sirven precisamente para correlacionar el comportamiento de la app distribuida y visualizar trazas, métricas y eventos entre frontend, API y servicios asociados.[web:27][web:49][web:50][web:51]

### Qué se instrumentará

- Requests HTTP.
- Tiempo total de análisis por importación.
- Tiempo de ejecución del script Python.
- Número de filas procesadas.
- Número de incidencias detectadas.
- Fallos por tipo de fuente.
- Traza completa desde acción del usuario hasta resultado del backend.[web:49]

## Seguridad

La seguridad será simple y realista:

- JWT para autenticación.
- Contraseñas con hash fuerte.
- Autorización por propietario de proyecto o rol administrador.
- Validación estricta de tipos de archivo.
- Limitación de tamaño de subida.
- Ejecución controlada del proceso Python sin exponer comandos arbitrarios.
- Sanitización de nombres de archivo.
- Logs de auditoría para acciones críticas.

## Estructura de carpetas

### Solución .NET

```text
NodeScope.sln
src/
  NodeScope.AppHost/
  NodeScope.ServiceDefaults/
  NodeScope.Api/
  NodeScope.Application/
  NodeScope.Domain/
  NodeScope.Infrastructure/
  NodeScope.Worker/
  NodeScope.Contracts/
frontend/
  nodescope-web/
python/
  processor/
    main.py
    processors/
    templates/
    services/
    models/
    outputs/
storage/
  uploads/
  artifacts/
  temp/
```

### Frontend Angular

```text
frontend/nodescope-web/src/app/
  core/
    auth/
    http/
    layout/
    guards/
    interceptors/
  shared/
    components/
    pipes/
    utils/
    models/
  features/
    dashboard/
    projects/
    imports/
    analysis/
    dataset-viewer/
    reports/
    compare/
    settings/
```

### Python

```text
python/processor/
  main.py
  processors/
    excel_processor.py
    csv_processor.py
    opcua_profile.py
  services/
    metrics_service.py
    validation_service.py
    report_service.py
  templates/
    report.html.j2
  models/
    contracts.py
```

## Contratos entre capas

### DTO de subida

```json
{
  "projectId": "guid",
  "fileName": "Nodos_DBPruebasFormacion.xlsx",
  "contentType": "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
}
```

### Response de resumen

```json
{
  "importId": "guid",
  "status": "Completed",
  "rowCount": 842,
  "issueCount": 17,
  "dominantType": "String",
  "dominantNamespace": "ns=2",
  "generatedAt": "2026-04-28T13:40:00Z"
}
```

### Response de chart data

```json
{
  "types": [
    { "label": "String", "value": 430 },
    { "label": "Bool", "value": 190 },
    { "label": "Int", "value": 140 }
  ],
  "namespaces": [
    { "label": "ns=2", "value": 510 },
    { "label": "ns=4", "value": 210 }
  ]
}
```

## Diseño de interfaz

La interfaz será de tipo **webapp técnica**, no landing decorativa. El estilo debe ser sobrio, panelizado y orientado a productividad. Para este tipo de producto conviene una jerarquía de dashboard clara y una SPA funcional, no un diseño teatral. El sistema de diseño debe priorizar legibilidad, densidad de información moderada, tablas claras y gráficos limpios.[web:31][web:37][web:49]

### Criterios visuales

- Tema oscuro y claro.
- Layout con sidebar y header.
- KPIs arriba, gráficos en segunda fila, tabla abajo.
- Tipografía sans limpia y profesional.
- Colores reservados a estados, métricas y alertas.
- Tabla con filtros rápidos, búsqueda y exportación.
- Componentes reutilizables: cards, chips, badges, charts, table toolbar, panel de incidencias.

## Perfil técnico del producto

Este proyecto te posiciona como alguien que sabe trabajar con:

- Backend enterprise en C# y ASP.NET Core.[cite:11][cite:12]
- Frontend serio y escalable con Angular.[cite:12][cite:13]
- Procesamiento de datos con Python y pandas.[web:61][web:69]
- Integración entre tecnologías heterogéneas por contratos claros.[web:64][web:73]
- Observabilidad moderna con OpenTelemetry y Aspire.[web:27][web:49][web:50][web:51]
- Contexto IT/OT, datos técnicos y casos cercanos a SCADA/OPC UA.[cite:11][web:31][web:65]

## Decisiones cerradas del stack

| Capa | Tecnología elegida | Uso exacto |
|---|---|---|
| Frontend | Angular 20/21 + TypeScript | SPA principal, dashboard, tablas, filtros, auth, comparación de importaciones [cite:12][cite:13][web:42] |
| UI | Angular Material/CDK + CSS custom | Componentes base, tablas, overlays, formularios |
| Backend API | ASP.NET Core Web API | Auth, proyectos, importaciones, reportes, comparación [web:42] |
| Lógica de aplicación | C# + EF Core + validaciones | Casos de uso, persistencia, reglas y consultas [cite:12] |
| Procesos asíncronos | Worker Service en .NET | Lanzar y monitorizar análisis pesados [web:27] |
| Motor de análisis | Python + pandas + openpyxl + Jinja2 | Normalización, métricas, validaciones y HTML renderizado [web:59][web:64][web:69] |
| Base de datos | PostgreSQL | Usuarios, proyectos, importaciones, incidencias, artefactos |
| Tiempo real | SignalR | Estado de procesamiento y feedback al usuario [web:36] |
| Observabilidad | OpenTelemetry + .NET Aspire | Trazas, métricas, correlación y panel técnico [web:27][web:49][web:50][web:51] |
| Documentación API | Swagger/OpenAPI | Contrato técnico interno |
| Almacenamiento | File storage local o compatible S3 | Originales, temporales y artefactos |

## Núcleo diferencial

Lo diferencial de NodeScope IT/OT no es “subir un Excel”. Lo diferencial es unir en un solo producto cuatro cosas que normalmente están separadas: ingestión técnica, validación estructurada, visualización profesional y generación de informes renderizados listos para compartir. Esa combinación tiene sentido real en entornos IT y OT porque reduce trabajo manual y convierte archivos técnicos opacos en información accionable.[web:64][web:65][web:66][web:69]

## Resumen operativo del producto

NodeScope IT/OT será una plataforma web hecha con **Angular + ASP.NET Core + PostgreSQL + Python**, donde C# actúa como cerebro principal del sistema, Angular como capa de operación visual y Python como motor analítico y generador de informes HTML interactivos. La aplicación recibirá archivos técnicos, los procesará, detectará incidencias, persistirá resultados, renderizará dashboards e informes y ofrecerá una experiencia clara y profesional orientada a datos técnicos de entornos IT/OT.[cite:11][cite:12][cite:13][web:59][web:65][web:69]
