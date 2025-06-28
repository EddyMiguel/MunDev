
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; 
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation; 

namespace MunDev.Models;

public class Perfil
{
    [Key]
    [ForeignKey("Usuario")]
    public int UsuarioId { get; set; } 

    [Display(Name = "Biografía")]
    [StringLength(1000, ErrorMessage = "La biografía no puede exceder los 1000 caracteres.")]
    [DataType(DataType.MultilineText)] 
    public string? Bio { get; set; }

    [Display(Name = "URL del Avatar")]
    [Url(ErrorMessage = "Ingrese una URL válida para el avatar.")]
    [StringLength(500, ErrorMessage = "La URL no puede exceder los 500 caracteres.")]
    public string? AvatarUrl { get; set; }

    [Display(Name = "Fecha de Nacimiento")]
    [DataType(DataType.Date)]
    public DateTime? FechaNacimiento { get; set; }

    [Display(Name = "Sitio Web")]
    [Url(ErrorMessage = "Ingrese una URL válida para el sitio web.")]
    [StringLength(255, ErrorMessage = "La URL no puede exceder los 255 caracteres.")]
    public string? SitioWeb { get; set; }

    [ValidateNever]
    public virtual Usuario Usuario { get; set; } = null!;
}
