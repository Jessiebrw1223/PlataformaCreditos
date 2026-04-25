# Plataforma de Créditos — Gestión de Solicitudes y Evaluación

Examen parcial resuelto con ASP.NET Core MVC (.NET 8), Identity, EF Core, SQLite, Razor Views, Session y Cache con Redis.

## Preguntas resueltas

### Pregunta 1 — Bootstrap + Modelo de datos
Rama sugerida: `feature/bootstrap-dominio`

Implementado:

- Proyecto MVC con Identity.
- EF Core con SQLite.
- Modelos:
  - `Cliente`
  - `SolicitudCredito`
  - `EstadoSolicitud`
- Restricciones:
  - `IngresosMensuales > 0`
  - `MontoSolicitado > 0`
  - Índice único filtrado para permitir solo una solicitud `Pendiente` por cliente.
- Seeder inicial:
  - Usuario analista.
  - Dos clientes.
  - Dos solicitudes iniciales: una pendiente y una aprobada.

### Pregunta 2 — Catálogo de solicitudes y filtros
Rama sugerida: `feature/catalogo-solicitudes`

Implementado:

- Vista `Mis solicitudes`.
- Filtros:
  - Estado.
  - Monto mínimo.
  - Monto máximo.
  - Fecha inicio.
  - Fecha fin.
- Vista detalle.
- Validaciones server-side:
  - No acepta montos negativos.
  - No acepta fecha inicio mayor a fecha fin.

### Pregunta 3 — Registro y validaciones de solicitud
Rama sugerida: `feature/solicitudes`

Implementado:

- Formulario para registrar solicitud.
- Crea la solicitud en estado `Pendiente`.
- Validaciones server-side:
  - Usuario autenticado.
  - Cliente activo.
  - No más de una solicitud pendiente por cliente.
  - Monto no mayor a 10 veces los ingresos mensuales.
- Feedback con `TempData` y `ModelState`.

### Pregunta 4 — Sesiones y Redis
Rama sugerida: `feature/sesion-redis`

Implementado:

- Session configurada.
- Si existe `Redis__ConnectionString`, la sesión/cache usa Redis.
- Si no existe Redis localmente, usa memoria para desarrollo.
- Guarda la última solicitud visitada.
- Muestra en layout: `Ver última solicitud S/ {Monto}`.
- Cachea por 60 segundos el listado de solicitudes del usuario.
- Invalida cache principal cuando:
  - Se registra una nueva solicitud.
  - El analista aprueba o rechaza una solicitud.

### Pregunta 5 — Panel de Analista
Rama sugerida: `feature/panel-analista`

Implementado:

- Rol `Analista`.
- Panel `/Analista`.
- `[Authorize(Roles = "Analista")]`.
- Lista solicitudes pendientes.
- Aprobar solicitud.
- Rechazar solicitud con motivo obligatorio.
- Validaciones:
  - No aprobar si el monto excede 5 veces los ingresos.
  - No procesar solicitudes ya aprobadas o rechazadas.
  - Motivo obligatorio en rechazo.
  - Acceso denegado a usuarios sin rol.

### Pregunta 6 — Despliegue en Render
Rama sugerida: `deploy/render`

Implementado:

- `Dockerfile`.
- `render.yaml`.
- Variables preparadas para Render.

## Usuarios de prueba

### Analista

```txt
Email: analista@demo.com
Password: Analista123*
```

### Cliente

```txt
Email: cliente@demo.com
Password: Cliente123*
```

### Segundo cliente

```txt
Email: cliente2@demo.com
Password: Cliente123*
```

## Ejecutar localmente

```bash
dotnet restore
dotnet ef database update
dotnet run
```

Luego abrir:

```txt
https://localhost:5001
```

o la URL que indique la consola.

## Migraciones

Si deseas recrear la base de datos:

```bash
dotnet ef database drop
dotnet ef database update
dotnet run
```

## Variables de entorno locales

```txt
ConnectionStrings__DefaultConnection=Data Source=creditos.db
Redis__ConnectionString=
```

Para Redis Labs / Redis Cloud:

```txt
Redis__ConnectionString=redis-xxxxx.cxxx.region.cloud.redislabs.com:port,password=TU_PASSWORD,ssl=True,abortConnect=False
```

## Variables mínimas para Render

```txt
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:${PORT}
ConnectionStrings__DefaultConnection=Data Source=/app/creditos.db
Redis__ConnectionString=redis-xxxxx.cxxx.region.cloud.redislabs.com:port,password=TU_PASSWORD,ssl=True,abortConnect=False
```

## Git — flujo por ramas y PR

No trabajar directo en `main`.

```bash
git checkout -b feature/bootstrap-dominio
git add .
git commit -m "Pregunta 1: bootstrap dominio identity sqlite"
git push -u origin feature/bootstrap-dominio
```

Crear PR hacia `main`.

Luego:

```bash
git checkout main
git pull origin main
git checkout -b feature/catalogo-solicitudes
```

Ramas requeridas:

```txt
feature/bootstrap-dominio
feature/catalogo-solicitudes
feature/solicitudes
feature/sesion-redis
feature/panel-analista
deploy/render
```

## Notas técnicas

- El proyecto fue ajustado a `.NET 8`, porque el examen exige ASP.NET Core MVC con .NET 8.
- Identity usa `AddDefaultIdentity` con roles.
- El seed crea automáticamente los usuarios, clientes, rol y solicitudes iniciales.
- La app ejecuta `Database.Migrate()` al iniciar para facilitar Render.
- Para producción real, se recomienda usar PostgreSQL en vez de SQLite persistido dentro del contenedor.
