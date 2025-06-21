using System;
using System.Collections.Generic;

namespace MunDev.Models;

public partial class Proyecto
{
    public int ProyectoId { get; set; }

    public string NombreProyecto { get; set; } = null!;

    public string? Descripcion { get; set; }

    public DateOnly? FechaInicio { get; set; }

    public DateOnly? FechaFinEstimada { get; set; }

    public DateTime? FechaCreacion { get; set; }

    public string? EstadoProyecto { get; set; }

    public int? CreadoPorUsuarioId { get; set; }

    public int? EquipoId { get; set; }

    public virtual Usuario? CreadoPorUsuario { get; set; }

    public virtual Equipo? Equipo { get; set; }

    public virtual ICollection<ProyectoUsuario> ProyectoUsuarios { get; set; } = new List<ProyectoUsuario>();

    public virtual ICollection<Repositorio> Repositorios { get; set; } = new List<Repositorio>();

    public virtual ICollection<Tarea> Tareas { get; set; } = new List<Tarea>();
}
