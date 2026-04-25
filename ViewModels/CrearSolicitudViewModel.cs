using System.ComponentModel.DataAnnotations;

namespace PlataformaCreditos.ViewModels;

public class CrearSolicitudViewModel
{
    [Required(ErrorMessage = "Ingrese el monto solicitado.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El monto solicitado debe ser mayor a 0.")]
    [Display(Name = "Monto solicitado")]
    public decimal MontoSolicitado { get; set; }
}
