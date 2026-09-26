namespace PlataformaIncidencias.Services;

public interface IPieHostService
{
    Task PublicarIncidenciaActualizadaAsync(int id, string estado);
}
