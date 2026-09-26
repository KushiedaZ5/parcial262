namespace PlataformaIncidencias.Services;

public interface IAlgoliaSearchService
{
    Task<List<int>> BuscarIncidenciaIdsAsync(string query);
}
