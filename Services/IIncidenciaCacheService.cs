using PlataformaIncidencias.Models;

namespace PlataformaIncidencias.Services;

public interface IIncidenciaCacheService
{
    Task<List<Incidencia>?> GetCachedIncidenciasAsync();
    Task SetCachedIncidenciasAsync(List<Incidencia> incidencias, TimeSpan? expiry = null);
    Task InvalidateCacheAsync();
}
