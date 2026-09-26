using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaIncidencias.Data;
using PlataformaIncidencias.Models;
using PlataformaIncidencias.Services;

namespace PlataformaIncidencias.Controllers;

public class OperacionesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IIncidenciaCacheService _cacheService;
    private readonly IAlgoliaSearchService _algoliaSearchService;
    private readonly IPieHostService _pieHostService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OperacionesController> _logger;

    public OperacionesController(
        ApplicationDbContext context,
        IIncidenciaCacheService cacheService,
        IAlgoliaSearchService algoliaSearchService,
        IPieHostService pieHostService,
        IConfiguration configuration,
        ILogger<OperacionesController> logger)
    {
        _context = context;
        _cacheService = cacheService;
        _algoliaSearchService = algoliaSearchService;
        _pieHostService = pieHostService;
        _configuration = configuration;
        _logger = logger;
    }

    // GET: /Operaciones/Incidencias
    public async Task<IActionResult> Incidencias(string? q)
    {
        ViewBag.PieHostCluster = _configuration["PieHost:ClusterId"] ?? _configuration["PieHost__ClusterId"] ?? "free.piehost.com";
        ViewBag.PieHostChannel = _configuration["PieHost:ChannelId"] ?? _configuration["PieHost__ChannelId"] ?? "incidencias";
        ViewBag.PieHostApiKey = _configuration["PieHost:ApiKey"] ?? _configuration["PieHost__ApiKey"] ?? "";

        // 1. Si existe búsqueda por texto, se consulta Algolia directamente en servidor (sin usar caché de Redis)
        if (!string.IsNullOrWhiteSpace(q))
        {
            _logger.LogInformation("[ALGOLIA SEARCH] Búsqueda con texto '{Termino}' ejecutada en servidor (sin caché Redis).", q);
            var algoliaIds = await _algoliaSearchService.BuscarIncidenciaIdsAsync(q);

            // Filtro estricto: Solo incidencias abiertas existentes en base de datos
            var resultados = await _context.Incidencias
                .Where(i => i.Estado == "Abierta" && algoliaIds.Contains(i.Id))
                .OrderByDescending(i => i.FechaRegistro)
                .ToListAsync();

            ViewBag.Busqueda = q;
            ViewBag.FuenteDatos = "Algolia Search (Directa sin caché Redis)";
            return View(resultados);
        }

        _logger.LogInformation("Consulta general de incidencias abiertas sin filtro de texto.");

        // 2. Para listado general sin búsqueda, consultar primero la caché de Redis (TTL 60s)
        var incidenciasEnCache = await _cacheService.GetCachedIncidenciasAsync();
        if (incidenciasEnCache != null)
        {
            _logger.LogInformation("[REDIS LECTURA: HIT] Listado general obtenido de Redis exitosamente.");
            ViewBag.FuenteDatos = "Redis (Caché HIT 60s)";
            return View(incidenciasEnCache);
        }

        // 3. Cache Miss: Consultar la base de datos SQLite y almacenar en Redis por 60s
        _logger.LogInformation("[REDIS LECTURA: MISS] Listado general leído desde SQLite y guardado en Redis.");
        var listaDesdeDb = await _context.Incidencias
            .Where(i => i.Estado == "Abierta")
            .OrderByDescending(i => i.FechaRegistro)
            .ToListAsync();

        await _cacheService.SetCachedIncidenciasAsync(listaDesdeDb, TimeSpan.FromSeconds(60));
        ViewBag.FuenteDatos = "SQLite Base de Datos (Almacenado en Redis 60s)";

        return View(listaDesdeDb);
    }

    // POST: /Operaciones/Cerrar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cerrar(int id)
    {
        var incidencia = await _context.Incidencias.FindAsync(id);
        if (incidencia == null)
        {
            return NotFound();
        }

        // SECUENCIA REQUERIDA POR EL EXAMEN:
        // 1. Cierre y persistencia en base de datos
        incidencia.Estado = "Cerrada";
        incidencia.FechaCierre = DateTime.UtcNow;
        incidencia.CerradoPor = User.Identity?.Name ?? "supervisor@bicicletas.com";

        await _context.SaveChangesAsync();
        _logger.LogInformation("[SECUENCIA 1/3] Incidencia {Id} cerrada y persistida en base de datos SQLite.", id);

        // 2. Invalidación de Redis antes de cualquier nueva consulta
        await _cacheService.InvalidateCacheAsync();
        _logger.LogInformation("[SECUENCIA 2/3] Clave de listado general invalidada en Redis.");

        // 3. Publicación por PieHost del evento IncidenciaActualizada
        await _pieHostService.PublicarIncidenciaActualizadaAsync(id, "Cerrada");
        _logger.LogInformation("[SECUENCIA 3/3] Evento IncidenciaActualizada emitido por PieHost con Id={Id} y Estado=Cerrada.", id);

        if (Request.Headers.Accept.ToString().Contains("application/json"))
        {
            return Json(new { success = true, id = id, estado = "Cerrada" });
        }

        return RedirectToAction(nameof(Incidencias));
    }

    // GET: /Operaciones/ObtenerIncidenciasJson
    [HttpGet]
    public async Task<IActionResult> ObtenerIncidenciasJson()
    {
        var lista = await _context.Incidencias
            .Where(i => i.Estado == "Abierta")
            .OrderByDescending(i => i.FechaRegistro)
            .Select(i => new
            {
                i.Id,
                i.Estacion,
                i.Descripcion,
                i.Prioridad,
                i.Estado,
                FechaRegistro = i.FechaRegistro.ToString("yyyy-MM-dd HH:mm")
            })
            .ToListAsync();

        return Json(lista);
    }
}
