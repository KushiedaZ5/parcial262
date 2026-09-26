using System.Text;
using System.Text.Json;

namespace PlataformaIncidencias.Services;

public class PieHostService : IPieHostService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PieHostService> _logger;

    public PieHostService(HttpClient httpClient, IConfiguration configuration, ILogger<PieHostService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task PublicarIncidenciaActualizadaAsync(int id, string estado)
    {
        var clusterId = _configuration["PieHost:ClusterId"]
            ?? _configuration["PieHost__ClusterId"]
            ?? Environment.GetEnvironmentVariable("PieHost__ClusterId")
            ?? "free.piehost.com";

        var channelId = _configuration["PieHost:ChannelId"]
            ?? _configuration["PieHost__ChannelId"]
            ?? Environment.GetEnvironmentVariable("PieHost__ChannelId")
            ?? "incidencias";

        var apiKey = _configuration["PieHost:ApiKey"]
            ?? _configuration["PieHost__ApiKey"]
            ?? Environment.GetEnvironmentVariable("PieHost__ApiKey");

        var secretKey = _configuration["PieHost:SecretKey"]
            ?? _configuration["PieHost__SecretKey"]
            ?? Environment.GetEnvironmentVariable("PieHost__SecretKey");

        var eventPayload = new
        {
            evento = "IncidenciaActualizada",
            id = id,
            estado = estado,
            timestamp = DateTime.UtcNow
        };

        var messageJson = JsonSerializer.Serialize(eventPayload);

        // Si están configuradas las credenciales de PieHost, publicamos a la API REST de PieHost
        if (!string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(secretKey))
        {
            try
            {
                var url = $"https://{clusterId}/api/v3/publish";
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("x-pie-key", apiKey);
                request.Headers.Add("x-pie-secret", secretKey);

                var bodyObj = new
                {
                    key = apiKey,
                    secret = secretKey,
                    channelId = channelId,
                    message = messageJson
                };

                request.Content = new StringContent(JsonSerializer.Serialize(bodyObj), Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("[PIEHOST PUBLICACIÓN] Evento 'IncidenciaActualizada' para #{Id} emitido a PieHost canal '{Canal}'.", id, channelId);
                    return;
                }
                else
                {
                    _logger.LogWarning("[PIEHOST PUBLICACIÓN] Error de respuesta de PieHost: {StatusCode}.", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[PIEHOST ERROR] Excepción al publicar evento WebSocket hacia PieHost.");
            }
        }
        else
        {
            _logger.LogInformation("[PIEHOST LOCAL] Evento 'IncidenciaActualizada' registrado en servidor para #{Id} (Estado: {Estado}). Mensaje: {Message}", id, estado, messageJson);
        }
    }
}
