using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MunDev.Data;
using MunDev.Models;
using System.Net.Mail;     // For SmtpClient, MailMessage
using System.Net;          // For NetworkCredential
using System.Security.Claims; // For Claims (getting UserId)
using Microsoft.AspNetCore.Authorization; // For the [Authorize] attribute
using Microsoft.Extensions.Configuration; // For IConfiguration (reading appsettings.json)
using System.Threading.Tasks; // For async/await

namespace MunDev.Controllers
{
    [Authorize] // Ensures that only authenticated users can access this controller
    public class InvitationsController : Controller
    {
        private readonly MunDevContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<InvitationsController> _logger;

        public InvitationsController(MunDevContext context, IConfiguration configuration, ILogger<InvitationsController> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return null;
        }

        // GET: Invitations/SendInvitation
        public async Task<IActionResult> SendInvitation(int? teamId)
        {
            // The [Authorize] attribute handles this, but an explicit check can add clarity
            if (!User.Identity?.IsAuthenticated ?? true) // CORRECTED: Use ?. and ?? for User.Identity
            {
                TempData["ErrorMessage"] = "Debe iniciar sesión para enviar invitaciones.";
                return RedirectToAction("Login", "Account");
            }

            var currentUserId = 0;
            // CORRECTED: Ensure User.Identity is not null before accessing IsAuthenticated
            if (User.Identity != null && User.Identity.IsAuthenticated && int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out currentUserId))
            {
                var userTeams = await _context.EquipoUsuarios
                                              .Where(eu => eu.UsuarioId == currentUserId)
                                              .Select(eu => eu.Equipo)
                                              .ToListAsync();
                ViewData["EquipoId"] = new SelectList(userTeams, "EquipoId", "NombreEquipo", teamId);
            }
            else
            {
                ViewData["EquipoId"] = new SelectList(new List<Equipo>(), "EquipoId", "NombreEquipo");
                TempData["ErrorMessage"] = "No se pudieron cargar tus equipos. Por favor, inicia sesión nuevamente.";
            }

            var invitation = new Invitation();
            if (teamId.HasValue)
            {
                invitation.EquipoId = teamId.Value;
            }

            return View(invitation);
        }

        // POST: Invitations/SendInvitation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendInvitation([Bind("InvitedEmail,EquipoId")] Invitation invitation)
        {
            Console.WriteLine("DEBUG: Iniciando SendInvitation POST.");

            int invitedByUserId = 0;
            if (User.Identity?.IsAuthenticated ?? false) // CORRECTED: Use ?. and ?? for User.Identity
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out invitedByUserId))
                {
                    invitation.InvitedByUserId = invitedByUserId;
                    Console.WriteLine($"DEBUG: Usuario remitente ID: {invitedByUserId}");
                }
                else
                {
                    ModelState.AddModelError("", "No se pudo identificar al usuario que envía la invitación. Por favor, inicie sesión de nuevo.");
                    Console.WriteLine("DEBUG: Error: No se pudo identificar al remitente.");
                }
            }
            else
            {
                ModelState.AddModelError("", "Debe iniciar sesión para enviar invitaciones.");
                Console.WriteLine("DEBUG: Error: Usuario no autenticado.");
            }

            if (!ModelState.IsValid)
            {
                Console.WriteLine("DEBUG: ModelState no es válido al inicio. Errores:");
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        Console.WriteLine($" - {error.ErrorMessage}");
                    }
                }
                if (invitedByUserId == 0)
                {
                    ViewData["EquipoId"] = new SelectList(new List<Equipo>(), "EquipoId", "NombreEquipo", invitation.EquipoId);
                }
                else
                {
                    var userTeamsOnError = await _context.EquipoUsuarios
                                                        .Where(eu => eu.UsuarioId == invitedByUserId)
                                                        .Select(eu => eu.Equipo)
                                                        .ToListAsync();
                    ViewData["EquipoId"] = new SelectList(userTeamsOnError, "EquipoId", "NombreEquipo", invitation.EquipoId);
                }
                return View(invitation);
            }

            var equipo = await _context.Equipos.FindAsync(invitation.EquipoId);
            if (equipo == null)
            {
                ModelState.AddModelError("EquipoId", "El equipo seleccionado no es válido.");
                Console.WriteLine("DEBUG: Error: Equipo no válido.");
                if (invitedByUserId == 0)
                {
                    ViewData["EquipoId"] = new SelectList(new List<Equipo>(), "EquipoId", "NombreEquipo", invitation.EquipoId);
                }
                else
                {
                    var userTeamsOnError = await _context.EquipoUsuarios
                                                        .Where(eu => eu.UsuarioId == invitedByUserId)
                                                        .Select(eu => eu.Equipo)
                                                        .ToListAsync();
                    ViewData["EquipoId"] = new SelectList(userTeamsOnError, "EquipoId", "NombreEquipo", invitation.EquipoId);
                }
                return View(invitation);
            }

            var existingUserByEmail = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == invitation.InvitedEmail);
            if (existingUserByEmail != null)
            {
                var existingMember = await _context.EquipoUsuarios
                                                   .AnyAsync(eu => eu.EquipoId == invitation.EquipoId && eu.UsuarioId == existingUserByEmail.UsuarioId);
                if (existingMember)
                {
                    ModelState.AddModelError("InvitedEmail", "Este usuario ya es miembro de este equipo.");
                    Console.WriteLine("DEBUG: Error: Usuario ya es miembro del equipo.");
                }
            }

            var inviterIsTeamMember = await _context.EquipoUsuarios
                                                    .AnyAsync(eu => eu.EquipoId == invitation.EquipoId && eu.UsuarioId == invitedByUserId);
            if (!inviterIsTeamMember)
            {
                ModelState.AddModelError("EquipoId", "Solo los miembros del equipo pueden enviar invitaciones a este equipo.");
                Console.WriteLine("DEBUG: Error: Remitente no es miembro del equipo.");
            }

            if (!ModelState.IsValid)
            {
                Console.WriteLine("DEBUG: ModelState no es válido después de validaciones personalizadas. Errores:");
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        Console.WriteLine($" - {error.ErrorMessage}");
                    }
                }
                if (invitedByUserId == 0)
                {
                    ViewData["EquipoId"] = new SelectList(new List<Equipo>(), "EquipoId", "NombreEquipo", invitation.EquipoId);
                }
                else
                {
                    var userTeamsOnError = await _context.EquipoUsuarios
                                                        .Where(eu => eu.UsuarioId == invitedByUserId)
                                                        .Select(eu => eu.Equipo)
                                                        .ToListAsync();
                    ViewData["EquipoId"] = new SelectList(userTeamsOnError, "EquipoId", "NombreEquipo", invitation.EquipoId);
                }
                return View(invitation);
            }

            invitation.InvitationToken = Guid.NewGuid().ToString();
            invitation.DateSent = DateTime.Now;
            invitation.ExpirationDate = DateTime.Now.AddDays(7);
            invitation.IsAccepted = false;
            invitation.AcceptedByUserId = null;

            _context.Add(invitation);
            await _context.SaveChangesAsync();
            Console.WriteLine("DEBUG: Invitación guardada en DB.");

            var callbackUrl = Url.Action(
                "AcceptInvitation",
                "Invitations",
                new { token = invitation.InvitationToken },
                protocol: HttpContext.Request.Scheme);
            Console.WriteLine($"DEBUG: Callback URL generada: {callbackUrl}");

            try
            {
                Console.WriteLine("DEBUG: Intentando enviar correo electrónico...");
                var smtpHost = _configuration["EmailSettings:SmtpHost"];
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                var enableSsl = bool.Parse(_configuration["EmailSettings:EnableSsl"] ?? "true");
                var senderEmail = _configuration["EmailSettings:SenderEmail"];
                var senderName = _configuration["EmailSettings:SenderName"];
                var username = _configuration["EmailSettings:Username"];
                var password = _configuration["EmailSettings:Password"];

                using (var client = new SmtpClient(smtpHost!, smtpPort))
                {
                    client.EnableSsl = enableSsl;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(username, password);
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(senderEmail!, senderName),
                        Subject = $"¡Has sido invitado a unirte al equipo '{equipo.NombreEquipo}' en MunDev!",
                        Body = $"Hola,<br/><br/>Has sido invitado a unirte al equipo <b>{equipo.NombreEquipo}</b> en MunDev por {User.Identity?.Name ?? "un usuario"}. Haz clic en el siguiente enlace para aceptar tu invitación:<br/><br/><a href='{callbackUrl}'>{callbackUrl}</a><br/><br/>La invitación expirará en 7 días.<br/><br/>Saludos,<br/>El equipo de MunDev",
                        IsBodyHtml = true,
                    };
                    mailMessage.To.Add(invitation.InvitedEmail);

                    await client.SendMailAsync(mailMessage);
                    Console.WriteLine("DEBUG: Correo electrónico enviado con éxito.");
                }

                TempData["SuccessMessage"] = "Invitación enviada con éxito por correo electrónico.";
                Console.WriteLine("DEBUG: TempData[SuccessMessage] establecido.");

                if (existingUserByEmail != null)
                {
                    Console.WriteLine($"DEBUG: Intentando enviar notificación WebSocket a usuario existente ID: {existingUserByEmail.UsuarioId}");
                    await WebSocketNotificationsController.SendNotificationToUser(existingUserByEmail.UsuarioId,
                        $"¡Has recibido una invitación para unirte al equipo '{equipo.NombreEquipo}'! Revisa tu correo o haz clic <a href='{callbackUrl}'>aquí</a> para aceptar.");
                    Console.WriteLine("DEBUG: Notificación WebSocket para invitado procesada.");
                }
                else
                {
                    Console.WriteLine("DEBUG: Invitado no es un usuario existente, no se envía notificación WebSocket directa.");
                }

                Console.WriteLine("DEBUG: Redirigiendo a SendInvitation.");
                return RedirectToAction(nameof(SendInvitation));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: ¡ERROR! Se atrapó una excepción: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"DEBUG: Inner Exception: {ex.InnerException.Message}");
                }
                ModelState.AddModelError("", $"Error al enviar la invitación por correo electrónico: {ex.Message}. Por favor, intente de nuevo. Verifique la configuración de correo en appsettings.json.");

                if (invitedByUserId == 0)
                {
                    ViewData["EquipoId"] = new SelectList(new List<Equipo>(), "EquipoId", "NombreEquipo", invitation.EquipoId);
                }
                else
                {
                    var userTeamsOnError = await _context.EquipoUsuarios
                                                        .Where(eu => eu.UsuarioId == invitedByUserId)
                                                        .Select(eu => eu.Equipo)
                                                        .ToListAsync();
                    ViewData["EquipoId"] = new SelectList(userTeamsOnError, "EquipoId", "NombreEquipo", invitation.EquipoId);
                }
                return View(invitation);
            }
        }


        // GET: Invitations/AcceptInvitation?token={invitationToken}
        public async Task<IActionResult> AcceptInvitation(string? token)
        {
            Console.WriteLine($"DEBUG: Iniciando AcceptInvitation GET con token: {token}");
            if (string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "Token de invitación no válido o faltante.";
                Console.WriteLine("DEBUG: Error: Token faltante.");
                return RedirectToAction("Index", "Home");
            }

            var invitation = await _context.Invitations
                .Include(i => i.Equipo)
                .FirstOrDefaultAsync(i => i.InvitationToken == token && !i.IsAccepted && i.ExpirationDate > DateTime.Now);

            if (invitation == null)
            {
                TempData["ErrorMessage"] = "Invitación no encontrada, ya usada o ha expirado.";
                Console.WriteLine("DEBUG: Error: Invitación no encontrada/válida.");
                return RedirectToAction("Index", "Home");
            }

            if (User.Identity?.IsAuthenticated ?? false) // CORRECTED: Use ?. and ?? for User.Identity
            {
                var currentUserEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                Console.WriteLine($"DEBUG: Usuario autenticado: {currentUserEmail}");
                if (currentUserEmail == invitation.InvitedEmail)
                {
                    TempData["InfoMessage"] = $"Ya has iniciado sesión como {currentUserEmail}. Puedes aceptar la invitación directamente.";
                    Console.WriteLine("DEBUG: Info: Usuario autenticado coincide con el invitado.");
                }
                else
                {
                    TempData["WarningMessage"] = $"Has iniciado sesión como {currentUserEmail}. Para aceptar esta invitación ({invitation.InvitedEmail}), por favor cierra sesión o usa la cuenta correcta.";
                    Console.WriteLine("DEBUG: Advertencia: Usuario autenticado NO coincide con el invitado.");
                }
            }


            ViewBag.InvitedEmail = invitation.InvitedEmail;
            ViewBag.InvitationToken = invitation.InvitationToken;
            ViewBag.EquipoNombre = invitation.Equipo?.NombreEquipo; // Ensure Equipo is not null
            ViewBag.IsAuthenticated = User.Identity?.IsAuthenticated ?? false;

            Console.WriteLine("DEBUG: Mostrando vista de aceptación de invitación.");
            return View(invitation);
        }

        // POST: Invitations/AcceptInvitation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptInvitation(string invitationToken, string password)
        {
            Console.WriteLine($"DEBUG: Iniciando AcceptInvitation POST para token: {invitationToken}");
            var invitation = await _context.Invitations
                .Include(i => i.Equipo)
                .Include(i => i.InvitedByUser)
                .FirstOrDefaultAsync(i => i.InvitationToken == invitationToken && !i.IsAccepted && i.ExpirationDate > DateTime.Now);

            if (invitation == null)
            {
                TempData["ErrorMessage"] = "Invitación no válida, ya usada o ha expirado.";
                Console.WriteLine("DEBUG: Error: Invitación no válida/usada/expirada.");
                return RedirectToAction("Index", "Home");
            }

            Usuario userToAssociate;
            var existingUser = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == invitation.InvitedEmail);

            if (existingUser != null)
            {
                userToAssociate = existingUser;
                var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                // CORRECTED: Check for null on currentUserIdClaim before accessing .Value
                if (currentUserIdClaim == null || !int.TryParse(currentUserIdClaim.Value, out int currentUserId) || currentUserId != existingUser.UsuarioId)
                {
                    TempData["ErrorMessage"] = "El correo electrónico ya está registrado. Por favor, inicie sesión con su cuenta existente para aceptar la invitación.";
                    Console.WriteLine("DEBUG: Error: Usuario existente pero no logueado/coincidente.");
                    return RedirectToAction("Login", "Account", new { returnUrl = HttpContext.Request.Path + HttpContext.Request.QueryString });
                }
                Console.WriteLine($"DEBUG: Usuario existente {userToAssociate.NombreUsuario} identificado.");
            }
            else
            {
                var suggestedUserName = invitation.InvitedEmail.Split('@')[0];
                var finalUserName = suggestedUserName;
                int suffix = 1;
                while (await _context.Usuarios.AnyAsync(u => u.NombreUsuario == finalUserName))
                {
                    finalUserName = $"{suggestedUserName}{suffix}";
                    suffix++;
                }

                userToAssociate = new Usuario
                {
                    Email = invitation.InvitedEmail,
                    NombreUsuario = finalUserName,
                    ContrasenaHash = BCrypt.Net.BCrypt.HashPassword(password),
                    Activo = true
                };
                _context.Add(userToAssociate);
                await _context.SaveChangesAsync();
                Console.WriteLine($"DEBUG: Nuevo usuario {userToAssociate.NombreUsuario} creado (ID: {userToAssociate.UsuarioId}).");

                await WebSocketNotificationsController.SendNotificationToUser(userToAssociate.UsuarioId,
                    $"¡Bienvenido a MunDev, {userToAssociate.NombreUsuario}! Has aceptado la invitación al equipo '{invitation.Equipo?.NombreEquipo}'.");
                Console.WriteLine("DEBUG: Notificación WebSocket enviada a nuevo usuario.");
            }

            var alreadyMember = await _context.EquipoUsuarios
                                             .AnyAsync(eu => eu.EquipoId == invitation.EquipoId && eu.UsuarioId == userToAssociate.UsuarioId);
            if (alreadyMember)
            {
                TempData["ErrorMessage"] = $"Ya eres miembro del equipo '{invitation.Equipo?.NombreEquipo}'. La invitación ha sido marcada como aceptada.";
                invitation.IsAccepted = true;
                invitation.AcceptedByUserId = userToAssociate.UsuarioId;
                _context.Update(invitation);
                await _context.SaveChangesAsync();
                Console.WriteLine("DEBUG: Error: Usuario ya es miembro, marcando invitación como aceptada.");
                return RedirectToAction("Index", "Home");
            }

            var equipoUsuario = new EquipoUsuario
            {
                EquipoId = invitation.EquipoId,
                UsuarioId = userToAssociate.UsuarioId,
                FechaUnion = DateTime.Now,
                RolEnEquipo = "Miembro"
            };
            _context.EquipoUsuarios.Add(equipoUsuario);

            invitation.IsAccepted = true;
            invitation.AcceptedByUserId = userToAssociate.UsuarioId;
            _context.Update(invitation);

            await _context.SaveChangesAsync();
            _logger.LogInformation("DEBUG: Usuario asociado al equipo e invitación marcada como aceptada.");

            if (invitation.InvitedByUserId > 0)
            {
                _logger.LogInformation($"DEBUG: Intentando enviar notificación WebSocket a remitente ID: {invitation.InvitedByUserId}");
                await WebSocketNotificationsController.SendNotificationToUser(invitation.InvitedByUserId,
                    $"{userToAssociate.NombreUsuario} ({userToAssociate.Email}) ha aceptado tu invitación al equipo '{invitation.Equipo?.NombreEquipo}'.");
                _logger.LogInformation("DEBUG: Notificación WebSocket para remitente procesada.");
            }

            TempData["SuccessMessage"] = $"¡Invitación aceptada! Ahora eres miembro del equipo '{invitation.Equipo?.NombreEquipo}'.";
            _logger.LogInformation("DEBUG: Invitación aceptada, redirigiendo a Home/Index.");
            return RedirectToAction("Index", "Home");
        }
    }
}

