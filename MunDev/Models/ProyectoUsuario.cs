using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace MunDev.Models;

public partial class ProyectoUsuario
{
    public int ProyectoUsuarioId { get; set; }

    public int ProyectoId { get; set; }

    public int UsuarioId { get; set; }

    public string? RolEnProyecto { get; set; }

    public DateTime? FechaAsignacion { get; set; }

    [ValidateNever]
    public virtual Proyecto Proyecto { get; set; } = null!;

    [ValidateNever]
    public virtual Usuario Usuario { get; set; } = null!;
}
