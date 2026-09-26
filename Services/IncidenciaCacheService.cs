using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Distributed;
using PlataformaIncidencias.Models;

namespace PlataformaIncidencias.Services;

public class IncidenciaCacheService : IIncidenciaCacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<IncidenciaCacheService> _logger;
    private const string CacheKey = "incidencias_abiertas_list";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        PropertyNameCaseInsensitive = true
    };

    public IncidenciaCacheService(IDistributedCache cache, ILogger<IncidenciaCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<Incidencia>?> GetCachedIncidenciasAsync()
    {
        try
        {
            var cachedBytes = await _cache.GetAsync(CacheKey);
            if (cachedBytes != null && cachedBytes.Length > 0)
            {
                var list = JsonSerializer.Deserialize<List<Incidencia>>(cachedBytes, JsonOptions);
                _logger.LogInformation("[CACHE HIT] Lectura de incidencias abiertas realizada desde Redis (Clave: '{Key}', Elementos: {Count}).", CacheKey, list?.Count ?? 0);
                return list;
            }

            _logger.LogInformation("[CACHE MISS] Clave '{Key}' no encontrada en Redis. Se requerirá lectura desde la base de datos.", CacheKey);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[CACHE ERROR] Error al conectar o consultar Redis. Se continuará con la base de datos.");
            return null;
        }
    }

    public async Task SetCachedIncidenciasAsync(List<Incidencia> incidencias, TimeSpan? expiry = null)
    {
        try
        {
            var ttl = expiry ?? TimeSpan.FromSeconds(60);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            };

            var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(incidencias, JsonOptions);
            await _cache.SetAsync(CacheKey, jsonBytes, options);

            _logger.LogInformation("[CACHE SET] Listado de incidencias abiertas guardado en Redis por {Segundos} segundos (Clave: '{Key}', Elementos: {Count}).",
                ttl.TotalSeconds, CacheKey, incidencias.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[CACHE ERROR] Error al guardar listado en Redis.");
        }
    }

    public async Task InvalidateCacheAsync()
    {
        try
        {
            await _cache.RemoveAsync(CacheKey);
            _logger.LogInformation("[CACHE INVALIDATION] Clave '{Key}' invalidada y purgada exitosamente de Redis.", CacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[CACHE ERROR] Error al invalidar la clave '{Key}' en Redis.", CacheKey);
        }
    }
}
