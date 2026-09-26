using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaIncidencias.Data;
using PlataformaIncidencias.Models;
using PlataformaIncidencias.Services;

namespace PlataformaIncidencias.Controllers;

public class OperacionesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAlgoliaSearchService _algoliaSearchService;
    private readonly ILogger<OperacionesController> _logger;

    public OperacionesController(
        ApplicationDbContext context,
        IAlgoliaSearchService algoliaSearchService,
        ILogger<OperacionesController> logger)
    {
        _context = context;
        _algoliaSearchService = algoliaSearchService;
        _logger = logger;
    }

    // GET: /Operaciones/Incidencias
    public async Task<IActionResult> Incidencias(string? q)
    {
        IQueryable<Incidencia> query = _context.Incidencias.Where(i => i.Estado == "Abierta");

        if (!string.IsNullOrWhiteSpace(q))
        {
            _logger.LogInformation("Ejecutando búsqueda con Algolia en servidor para término '{Termino}'...", q);
            var algoliaIds = await _algoliaSearchService.BuscarIncidenciaIdsAsync(q);
            query = query.Where(i => algoliaIds.Contains(i.Id));
            ViewBag.Busqueda = q;
        }
        else
        {
            _logger.LogInformation("Consulta general de incidencias abiertas sin filtro de búsqueda.");
        }

        var lista = await query.OrderByDescending(i => i.FechaRegistro).ToListAsync();
        return View(lista);
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
        _logger.LogInformation("Incidencia {Id} cerrada satisfactoriamente en la base de datos.", id);

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
