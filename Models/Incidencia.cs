using System.ComponentModel.DataAnnotations;

namespace PlataformaIncidencias.Models;

public class Incidencia
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Estación")]
    public string Estacion { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    [Display(Name = "Descripción")]
    public string Descripcion { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Prioridad { get; set; } = "Media"; // Alta, Media, Baja

    [Required]
    [StringLength(20)]
    public string Estado { get; set; } = "Abierta"; // Abierta, Cerrada

    [Display(Name = "Fecha de Registro")]
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    [Display(Name = "Fecha de Cierre")]
    public DateTime? FechaCierre { get; set; }

    public string? CerradoPor { get; set; }
}
