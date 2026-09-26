using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaIncidencias.Data;
using PlataformaIncidencias.Models;
using PlataformaIncidencias.Services;

namespace PlataformaIncidencias.Controllers;

public class OperacionesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IPieHostService _pieHostService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OperacionesController> _logger;

    public OperacionesController(
        ApplicationDbContext context,
        IPieHostService pieHostService,
        IConfiguration configuration,
        ILogger<OperacionesController> logger)
    {
        _context = context;
        _pieHostService = pieHostService;
        _configuration = configuration;
        _logger = logger;
    }

    // GET: /Operaciones/Incidencias
    public async Task<IActionResult> Incidencias(string? q)
    {
        _logger.LogInformation("Consultando listado de incidencias abiertas desde la base de datos.");
        
        var query = _context.Incidencias.Where(i => i.Estado == "Abierta");

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(i => i.Estacion.Contains(q) || i.Descripcion.Contains(q));
            ViewBag.Busqueda = q;
        }

        ViewBag.PieHostCluster = _configuration["PieHost:ClusterId"] ?? _configuration["PieHost__ClusterId"] ?? "free.piehost.com";
        ViewBag.PieHostChannel = _configuration["PieHost:ChannelId"] ?? _configuration["PieHost__ChannelId"] ?? "incidencias";
        ViewBag.PieHostApiKey = _configuration["PieHost:ApiKey"] ?? _configuration["PieHost__ApiKey"] ?? "";

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

        // 1. Guardar primero el estado en la base de datos
        incidencia.Estado = "Cerrada";
        incidencia.FechaCierre = DateTime.UtcNow;
        incidencia.CerradoPor = User.Identity?.Name ?? "supervisor@bicicletas.com";

        await _context.SaveChangesAsync();
        _logger.LogInformation("Incidencia {Id} cerrada y persistida en base de datos.", id);

        // 2. Publicar desde el servidor el evento IncidenciaActualizada con Id y Estado en PieHost
        await _pieHostService.PublicarIncidenciaActualizadaAsync(id, "Cerrada");
        _logger.LogInformation("[PIEHOST PUBLICACIÓN] Evento IncidenciaActualizada emitido: Id={Id}, Estado=Cerrada", id);

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
