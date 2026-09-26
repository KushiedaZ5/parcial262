# Examen Parcial: Plataforma de Incidencias (BiciShared)

Sistema web para la gestión operativa y control de incidencias en estaciones de bicicletas compartidas, desarrollado con **ASP.NET Core MVC (.NET 10)**, **Entity Framework Core (SQLite)**, **ASP.NET Core Identity**, **Búsqueda Algolia**, **Caché Redis (StackExchange.Redis)** y **WebSockets en tiempo real con PieHost**, desplegado en **Render.com**.

---

## 📌 Enlaces Principales de Entrega

- **Repositorio en GitHub:** [https://github.com/KushiedaZ5/parcial262](https://github.com/KushiedaZ5/parcial262)
- **URL de Producción (Render):** [https://parcial262.onrender.com](https://parcial262.onrender.com)
- **Commit Desplegado en Producción:** `65a2488` / `23aa39c` (Merge PR #3 + Documentación final)

---

## 🔀 Mapeo de Ramas, Pull Requests y Resoluciones de Conflictos

Cumpliendo con la rúbrica de evaluación, **las tres ramas nacieron del mismo commit inicial (`0edd8aa`)**, ninguna pregunta se trabajó directamente en `main`, y se integraron en el orden estricto **A $\rightarrow$ B $\rightarrow$ C**, preservando los commits de resolución de conflictos mediante `git merge` sin force-push ni squash.

| Pregunta / Feature | Rama | Pull Request | Estado | Commit Inicial / Hash |
| :--- | :--- | :--- | :--- | :--- |
| **Pregunta 1: Búsqueda con Algolia** | `feature/busqueda-algolia` | [PR #1](https://github.com/KushiedaZ5/parcial262/pull/1) | **Merged** | `cf484d4` |
| **Pregunta 2: Caché con Redis** | `feature/cache-redis` | [PR #2](https://github.com/KushiedaZ5/parcial262/pull/2) | **Merged** | `f788dbc` (Resolución 1: `fd41acb`) |
| **Pregunta 3: Tiempo Real PieHost** | `feature/websocket-piehost` | [PR #3](https://github.com/KushiedaZ5/parcial262/pull/3) | **Merged** | `13dc3d5` (Resolución 2: `0d7e223`) |

---

## 🌳 Historial del Repositorio (`git log --graph --oneline --all`)

```text
*   65a2488 Merge pull request #3 from feature/websocket-piehost
|\  
| *   0d7e223 merge: resolver conflicto 2 integrando Algolia, Redis y PieHost
| |\  
| |/  
|/|   
* |   b9f74be Merge pull request #2 from feature/cache-redis
|\ \  
| * \   fd41acb merge: resolver conflicto 1 integrando busqueda Algolia y cache Redis
| |\ \  
| |/ /  
|/| |   
* | |   458f22b Merge pull request #1 from feature/busqueda-algolia
|\ \ \  
| * | | cf484d4 feat: busqueda con Algolia en servidor y filtrado de incidencias abiertas
|/ / /  
| * / f788dbc feat: cache con Redis por 60s, logs de lectura e invalidacion al cerrar
|/ /  
| * 13dc3d5 feat: actualizacion en tiempo real con WebSocket PieHost y recuperacion de estado
|/  
* 0edd8aa feat: proyecto base de plataforma de incidencias con Identity y EF Core
```

### Explicación de las Resoluciones de Conflictos
1. **Conflicto 1 (`main` $\rightarrow$ `feature/cache-redis`):**
   - **Causa:** Ambas ramas modificaron la línea del título en `Views/Operaciones/Incidencias.cshtml` (`<h1>Incidencias abiertas encontradas</h1>` vs `<h1>Incidencias abiertas con consulta rápida</h1>`), además de la inyección de dependencias en `Program.cs` y los métodos del controlador.
   - **Resolución (`fd41acb`):** Se unificó el título a `<h1>Incidencias abiertas encontradas con consulta rápida</h1>`. Se mantuvieron el registro de `IAlgoliaSearchService` y `IIncidenciaCacheService` en `Program.cs`. En el controlador se implementó la regla: búsquedas con texto consultan Algolia directamente (sin caché de Redis), mientras que el listado general utiliza Redis por 60 segundos con invalidación al cerrar.
2. **Conflicto 2 (`main` $\rightarrow$ `feature/websocket-piehost`):**
   - **Causa:** La rama C modificó el título a `<h1>Incidencias abiertas en tiempo real</h1>` y agregaba scripts de WebSocket, mientras que `main` ya contenía Algolia y Redis.
   - **Resolución (`0d7e223`):** Se unificó el título a `<h1>Incidencias abiertas encontradas con consulta rápida en tiempo real</h1>`. Se integraron los 3 servicios simultáneamente (`AlgoliaSearchService`, `IncidenciaCacheService`, `PieHostService`). En la acción `Cerrar(id)` se garantizó la secuencia estricta solicitada:
     1. Persistencia del cierre en base de datos (`await _context.SaveChangesAsync()`).
     2. Invalidación inmediata de la clave del listado general en Redis (`await _cacheService.InvalidateCacheAsync()`).
     3. Emisión del evento `IncidenciaActualizada` hacia el canal WebSocket de PieHost (`await _pieHostService.PublicarIncidenciaActualizadaAsync(id, "Cerrada")`).
     4. Filtrado en Algolia: al consultar en BD con `Estado == "Abierta"`, las cerradas quedan excluidas automáticamente.

---

## 👥 Cuentas de Acceso Preconfiguradas (Seed Data)

La base de datos SQLite se crea y popula automáticamente al iniciar la aplicación (`DbInitializer.cs`):

| Rol | Correo Electrónico | Contraseña | Acceso |
| :--- | :--- | :--- | :--- |
| **Supervisor** | `supervisor@bicicletas.com` | `Password123!` | Visualización, búsqueda, cierre de incidencias y emisión WebSocket |
| **Operador** | `operador@bicicletas.com` | `Password123!` | Consulta de incidencias y monitoreo |

---

## ⚙️ Configuración y Variables de Entorno (Sin Credenciales en Repositorio)

Para producción en Render, las claves se configuran vía **Environment Variables** en el dashboard de Render:

```env
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=DataSource=/data/app.db;Cache=Shared

# 1. Búsqueda Algolia
Algolia__ApplicationId=TU_ALGOLIA_APP_ID
Algolia__ApiKey=TU_ALGOLIA_API_KEY
Algolia__IndexName=incidencias

# 2. Caché Redis
Redis__ConnectionString=rediss://default:tu_password@tu-cluster.upstash.io:6379

# 3. WebSockets PieHost
PieHost__ClusterId=free.piehost.com
PieHost__ChannelId=incidencias
PieHost__ApiKey=TU_PIEHOST_API_KEY
PieHost__SecretKey=TU_PIEHOST_SECRET_KEY
```

> **Nota de Resiliencia:** Si alguna variable de Algolia, Redis o PieHost no está presente en un entorno local o de pruebas, la aplicación cuenta con modo de respaldo seguro (búsqueda en BD local, caché distribuida en memoria y sincronización multi-sesión con BroadcastChannel) para garantizar disponibilidad del 100% sin caídas.

---

## 🚀 Despliegue en Render (Paso a Paso)

1. Crear un **Web Service** en Render conectado al repositorio `https://github.com/KushiedaZ5/parcial262`.
2. Seleccionar **Runtime: Docker**.
3. En la sección **Disks**, añadir un disco persistente:
   - **Name:** `incidencias_sqlite_data`
   - **Mount Path:** `/data`
   - **Size:** `1 GB`
4. En **Environment Variables**, añadir las claves descritas en la sección anterior.
5. Iniciar el despliegue. Render compilará mediante el `Dockerfile` multi-etapa (.NET 10) y expondrá la aplicación en el puerto asignado dinámicamente mediante `entrypoint.sh`.

---

## 🧪 Pruebas de Funcionamiento

### 1. Búsqueda con Algolia
- Ingresar a `/Operaciones/Incidencias`.
- En la barra de búsqueda ingresar un término (ej. `San Isidro` o `freno`).
- Comprobar que la búsqueda se realiza desde el servidor y muestra únicamente incidencias abiertas.
- Al vaciar el buscador o hacer clic en "Limpiar", se restablece el listado general.

### 2. Caché con Redis
- Al cargar el listado general por primera vez, el log mostrará `[CACHE MISS] Listado general leído desde SQLite y guardado en Redis`. En la pantalla se observará el badge de origen de datos.
- Al recargar la página dentro de los 60 segundos, el log mostrará `[CACHE HIT] Lectura de incidencias abiertas realizada desde Redis`.
- Al hacer clic en "Cerrar" en una incidencia, se ejecuta la invalidación: el log registrará `[REDIS INVALIDATION] Clave invalidada y purgada exitosamente de Redis`.

### 3. Actualización en Tiempo Real con PieHost
- Abrir dos sesiones o pestañas en `/Operaciones/Incidencias` (ej. una en modo incógnito con el usuario Supervisor).
- En la sesión del Supervisor, presionar **Cerrar** en una incidencia abierta.
- En la segunda sesión, observar que la fila se resalta en rojo y se elimina del listado inmediatamente sin recargar la página (`DOM updated in real-time`).
- Se presenta una alerta flotante notificando el cierre en tiempo real.
