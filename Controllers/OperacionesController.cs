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
    private readonly ILogger<OperacionesController> _logger;

    public OperacionesController(
        ApplicationDbContext context,
        IIncidenciaCacheService cacheService,
        ILogger<OperacionesController> logger)
    {
        _context = context;
        _cacheService = cacheService;
        _logger = logger;
    }

    // GET: /Operaciones/Incidencias
    public async Task<IActionResult> Incidencias(string? q)
    {
        // 1. Si existe búsqueda por texto, se consulta directamente sin usar caché de Redis
        if (!string.IsNullOrWhiteSpace(q))
        {
            _logger.LogInformation("[CONSULTA DIRECTA] Búsqueda con texto '{Termino}' ejecutada directamente sin caché.", q);
            ViewBag.Busqueda = q;
            ViewBag.FuenteDatos = "Directa de Base de Datos (Búsqueda sin caché)";

            var resultados = await _context.Incidencias
                .Where(i => i.Estado == "Abierta" && (i.Estacion.Contains(q) || i.Descripcion.Contains(q)))
                .OrderByDescending(i => i.FechaRegistro)
                .ToListAsync();

            return View(resultados);
        }

        // 2. Para listado general, consultar primero la caché de Redis por 60s
        var incidenciasEnCache = await _cacheService.GetCachedIncidenciasAsync();
        if (incidenciasEnCache != null)
        {
            _logger.LogInformation("[REDIS LECTURA: HIT] Listado general obtenido de Redis exitosamente.");
            ViewBag.FuenteDatos = "Redis (Caché HIT 60s)";
            return View(incidenciasEnCache);
        }

        // 3. Si no existe en Redis (Cache Miss), consultar la base de datos y guardar en Redis
        _logger.LogInformation("[REDIS LECTURA: MISS] Listado general leído desde SQLite y almacenado en Redis.");
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

        incidencia.Estado = "Cerrada";
        incidencia.FechaCierre = DateTime.UtcNow;
        incidencia.CerradoPor = User.Identity?.Name ?? "supervisor@bicicletas.com";

        await _context.SaveChangesAsync();
        _logger.LogInformation("Incidencia {Id} cerrada satisfactoriamente en base de datos.", id);

        // Invalidar inmediatamente la clave de Redis antes de volver a consultarlo
        await _cacheService.InvalidateCacheAsync();
        _logger.LogInformation("[REDIS INVALIDATION] Clave de listado general invalidada en Redis tras cierre.");

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
