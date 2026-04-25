using PlataformaCreditos.Models;

namespace PlataformaCreditos.ViewModels;

public class SolicitudFiltroViewModel
{
    public EstadoSolicitud? Estado { get; set; }
    public decimal? MontoMin { get; set; }
    public decimal? MontoMax { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public List<SolicitudCredito> Solicitudes { get; set; } = new();
}
