using System;
using System.Collections.Generic;

namespace MunDev.Models;

public partial class Usuario
{
    public int UsuarioId { get; set; }

    required public string NombreUsuario { get; set; }

    required public string Email { get; set; }

    public string? ContrasenaHash { get; set; }

    public bool? Activo { get; set; }

    public virtual ICollection<ActividadUsuario> ActividadUsuarios { get; set; } = new List<ActividadUsuario>();

    public virtual ICollection<Comentario> Comentarios { get; set; } = new List<Comentario>();

    public virtual ICollection<Equipo> Equipos { get; set; } = new List<Equipo>();

    public virtual ICollection<Notificacion> Notificacions { get; set; } = new List<Notificacion>();

    public virtual ICollection<ProyectoUsuario> ProyectoUsuarios { get; set; } = new List<ProyectoUsuario>();

    public virtual ICollection<Proyecto> Proyectos { get; set; } = new List<Proyecto>();

    public virtual ICollection<Tarea> Tareas { get; set; } = new List<Tarea>();
}
