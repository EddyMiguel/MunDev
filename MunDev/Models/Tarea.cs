using System;
using System.Collections.Generic;

namespace MunDev.Models;

public partial class Tarea
{
    public int TareaId { get; set; }

    public int ProyectoId { get; set; }

    public string Titulo { get; set; } = null!;

    public string? Descripcion { get; set; }

    public DateOnly? FechaVencimiento { get; set; }

    public string? EstadoTarea { get; set; }

    public string? Prioridad { get; set; }

    public int? AsignadoAusuarioId { get; set; }

    public virtual Usuario? AsignadoAusuario { get; set; }

    public virtual ICollection<Comentario> Comentarios { get; set; } = new List<Comentario>();

    public virtual Proyecto Proyecto { get; set; } = null!;
}
