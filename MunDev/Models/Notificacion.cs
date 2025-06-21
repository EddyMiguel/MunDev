using System;
using System.Collections.Generic;

namespace MunDev.Models;

public partial class Notificacion
{
    public int NotificacionId { get; set; }

    public int UsuarioId { get; set; }

    public string Mensaje { get; set; } = null!;

    public DateTime? FechaCreacion { get; set; }

    public bool? Leida { get; set; }

    public string? TipoNotificacion { get; set; }

    public virtual Usuario Usuario { get; set; } = null!;
}
