using System;
using System.Collections.Generic;

namespace MunDev.Models;

public partial class ActividadUsuario
{
    public int ActividadUsuarioId { get; set; }

    public int UsuarioId { get; set; }

    public string TipoActividad { get; set; } = null!;

    public string? DescripcionActividad { get; set; }

    public DateTime? FechaActividad { get; set; }

    public virtual Usuario Usuario { get; set; } = null!;
}
