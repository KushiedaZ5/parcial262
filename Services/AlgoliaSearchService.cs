using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using PlataformaIncidencias.Data;

namespace PlataformaIncidencias.Services;

public class AlgoliaSearchService : IAlgoliaSearchService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AlgoliaSearchService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public AlgoliaSearchService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<AlgoliaSearchService> logger,
        IServiceProvider serviceProvider)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task<List<int>> BuscarIncidenciaIdsAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new List<int>();
        }

        var appId = _configuration["Algolia:ApplicationId"]
            ?? _configuration["Algolia__ApplicationId"]
            ?? Environment.GetEnvironmentVariable("Algolia__ApplicationId");

        var apiKey = _configuration["Algolia:ApiKey"]
            ?? _configuration["Algolia__ApiKey"]
            ?? Environment.GetEnvironmentVariable("Algolia__ApiKey");

        var indexName = _configuration["Algolia:IndexName"]
            ?? _configuration["Algolia__IndexName"]
            ?? Environment.GetEnvironmentVariable("Algolia__IndexName")
            ?? "incidencias";

        // Si están configuradas las credenciales de Algolia, consultamos la API oficial de Algolia en servidor
        if (!string.IsNullOrWhiteSpace(appId) && !string.IsNullOrWhiteSpace(apiKey))
        {
            try
            {
                var requestUrl = $"https://{appId}-dsn.algolia.net/1/indexes/{indexName}/query";
                using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                request.Headers.Add("X-Algolia-Application-Id", appId);
                request.Headers.Add("X-Algolia-API-Key", apiKey);

                var payload = new { query = query };
                request.Content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseJson);
                    var matchedIds = new List<int>();

                    if (doc.RootElement.TryGetProperty("hits", out var hitsElement) && hitsElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var hit in hitsElement.EnumerateArray())
                        {
                            if (hit.TryGetProperty("objectID", out var objIdProp))
                            {
                                if (int.TryParse(objIdProp.GetString(), out var id))
                                {
                                    matchedIds.Add(id);
                                }
                            }
                            else if (hit.TryGetProperty("id", out var idProp))
                            {
                                if (idProp.TryGetInt32(out var id))
                                {
                                    matchedIds.Add(id);
                                }
                            }
                        }
                    }

                    _logger.LogInformation("[Algolia Search] Búsqueda en índice '{Index}' con texto '{Query}' retornó {Count} resultados.", indexName, query, matchedIds.Count);
                    return matchedIds;
                }
                else
                {
                    _logger.LogWarning("[Algolia Search] Error de API HTTP {StatusCode}. Realizando fallback resiliente en base de datos local.", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Algolia Search] Excepción al consultar Algolia. Realizando fallback en base de datos local.");
            }
        }

        // Fallback local: Búsqueda sobre la base de datos por Estación o Descripción
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var localIds = await context.Incidencias
            .Where(i => i.Estacion.ToLower().Contains(query.ToLower()) || i.Descripcion.ToLower().Contains(query.ToLower()))
            .Select(i => i.Id)
            .ToListAsync();

        _logger.LogInformation("[Algolia Fallback Local] Búsqueda local por texto '{Query}' retornó {Count} incidencias coincidentes.", query, localIds.Count);
        return localIds;
    }
}
