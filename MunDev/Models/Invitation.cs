using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace MunDev.Models;

public class Invitation
{
    public int InvitationId { get; set; }

    [Required(ErrorMessage = "El correo electrónico del invitado es obligatorio.")]
    [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido.")]
    [StringLength(255, ErrorMessage = "El correo electrónico no puede exceder los 255 caracteres.")]
    public string InvitedEmail { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public string InvitationToken { get; set; } = null!;

    [Required]
    public int InvitedByUserId { get; set; }

    [Required(ErrorMessage = "El equipo al que se invita es obligatorio.")]
    public int EquipoId { get; set; }

    [Required]
    public DateTime DateSent { get; set; }

    [Required]
    public DateTime ExpirationDate { get; set; }

    public bool IsAccepted { get; set; } = false;

    public int? AcceptedByUserId { get; set; }

    [ValidateNever]
    public virtual Usuario InvitedByUser { get; set; } = null!;

    [ValidateNever]
    public virtual Usuario? AcceptedByUser { get; set; }

    [ValidateNever]
    public virtual Equipo Equipo { get; set; } = null!;
}