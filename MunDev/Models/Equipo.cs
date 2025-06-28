using System;
using System.Collections.Generic;

namespace MunDev.Models;

public partial class Equipo
{
    public int EquipoId { get; set; }

    public string NombreEquipo { get; set; } = null!;

    public string? Descripcion { get; set; }

    public int? CreadoPorUsuarioId { get; set; }

    public virtual Usuario? CreadoPorUsuario { get; set; }

    public virtual ICollection<Proyecto> Proyectos { get; set; } = new List<Proyecto>();
    public virtual ICollection<EquipoUsuario> EquipoUsuarios { get; set; } = new List<EquipoUsuario>();
}
