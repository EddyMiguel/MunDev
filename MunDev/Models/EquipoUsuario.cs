using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace MunDev.Models;

public class EquipoUsuario
{
    public int EquipoUsuarioId { get; set; }

    [Required]
    public int EquipoId { get; set; }

    [Required]
    public int UsuarioId { get; set; }

    [StringLength(50)]
    [Display(Name = "Rol en el Equipo")]
    public string? RolEnEquipo { get; set; }

    [Display(Name = "Fecha de Unión")]
    public DateTime? FechaUnion { get; set; }

    [ValidateNever]
    public virtual Equipo Equipo { get; set; } = null!;

    [ValidateNever]
    public virtual Usuario Usuario { get; set; } = null!;
}
