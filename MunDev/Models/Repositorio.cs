using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace MunDev.Models;

public partial class Repositorio
{
    public int RepositorioId { get; set; }

    [Display(Name = "Proyecto Asociado")] // Etiqueta amigable para la UI
    [Required(ErrorMessage = "El Proyecto es obligatorio.")]
    public int ProyectoId { get; set; }

    [Display(Name = "Nombre del Repositorio")]
    [Required(ErrorMessage = "El nombre del repositorio es obligatorio.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 100 caracteres.")]
    public string NombreRepositorio { get; set; } = null!;

    [Display(Name = "URL del Repositorio")]
    [Required(ErrorMessage = "La URL del repositorio es obligatoria.")]
    [Url(ErrorMessage = "Ingrese una URL válida (ej. https://github.com/usuario/repo).")] // Validación de formato URL
    [StringLength(255, ErrorMessage = "La URL no puede exceder los 255 caracteres.")]
    public string RepositorioUrl { get; set; } = null!;

    [Display(Name = "Fecha de Creación")]
    [DataType(DataType.DateTime)] // Para indicar que es una fecha/hora
    public DateTime? FechaCreacion { get; set; }

    [ValidateNever]
    public virtual Proyecto Proyecto { get; set; } = null!;
}
