using System.ComponentModel.DataAnnotations;

namespace PlataformaCreditos.ViewModels;

public class RechazarSolicitudViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El motivo de rechazo es obligatorio.")]
    [StringLength(300, MinimumLength = 5, ErrorMessage = "El motivo debe tener entre 5 y 300 caracteres.")]
    public string MotivoRechazo { get; set; } = string.Empty;
}
