// Controllers/PerfilesController.cs
// Este controlador permite a los usuarios gestionar su propio perfil.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MunDev.Data;
using MunDev.Models;
using System.Security.Claims; // Necesario para obtener el ID del usuario
using Microsoft.AspNetCore.Authorization; // Para requerir autenticación

namespace MunDev.Controllers
{
    // Asegura que solo los usuarios autenticados puedan acceder a este controlador
    [Authorize]
    public class PerfilesController : Controller
    {
        private readonly MunDevContext _context;

        public PerfilesController(MunDevContext context)
        {
            _context = context;
        }

        // Método auxiliar para obtener el ID del usuario actual
        private int? GetCurrentUserId()
        {
            // Busca el ClaimTypes.NameIdentifier que contiene el ID de usuario
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return null; // El usuario no está autenticado o su ID no es válido.
        }

        // GET: Perfiles/MyProfile
        // Muestra el perfil del usuario actual o redirige a Crear si no tiene uno.
        public async Task<IActionResult> MyProfile()
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                // Esto no debería ocurrir si el controlador está [Authorize], pero es una buena salvaguarda.
                return RedirectToAction("Login", "Account");
            }

            // Busca el perfil asociado al ID del usuario actual.
            var perfil = await _context.Perfiles
                .Include(p => p.Usuario) // Incluye la información del usuario si necesitas mostrarla
                .FirstOrDefaultAsync(p => p.UsuarioId == currentUserId.Value);

            if (perfil == null)
            {
                // Si el usuario no tiene un perfil, redirige a la acción de crear uno nuevo.
                TempData["InfoMessage"] = "Parece que no tienes un perfil creado. ¡Por favor, crea uno ahora!";
                return RedirectToAction(nameof(Create));
            }

            return View("Details", perfil); // Reutiliza la vista Details para mostrar "Mi Perfil"
        }

        // GET: Perfiles/Create
        // Muestra el formulario para crear un nuevo perfil para el usuario actual.
        public async Task<IActionResult> Create()
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            // Evitar que un usuario cree múltiples perfiles
            var existingProfile = await _context.Perfiles.AnyAsync(p => p.UsuarioId == currentUserId.Value);
            if (existingProfile)
            {
                TempData["ErrorMessage"] = "Ya tienes un perfil creado. Puedes editarlo en su lugar.";
                return RedirectToAction(nameof(MyProfile)); // Redirige a editar su perfil existente
            }

            var perfil = new Perfil { UsuarioId = currentUserId.Value };
            // Puedes precargar algunos datos del usuario si quieres, ej:
            // var currentUser = await _context.Usuarios.FindAsync(currentUserId.Value);
            // perfil.Bio = $"Hola, soy {currentUser.NombreUsuario}.";

            return View(perfil);
        }

        // POST: Perfiles/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Bio,AvatarUrl,FechaNacimiento,SitioWeb")] Perfil perfil)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            // Asegurar que el Perfil se asocie al UsuarioId del usuario logueado, no del formulario.
            perfil.UsuarioId = currentUserId.Value;

            // Evitar duplicados si el usuario refresca la página o envía varias veces.
            var existingProfile = await _context.Perfiles.AnyAsync(p => p.UsuarioId == currentUserId.Value);
            if (existingProfile)
            {
                ModelState.AddModelError("", "Ya tienes un perfil creado. Por favor, edita tu perfil existente.");
                return View(perfil); // Vuelve a la vista con el error
            }

            if (ModelState.IsValid)
            {
                _context.Add(perfil);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "¡Tu perfil ha sido creado con éxito!";
                return RedirectToAction(nameof(MyProfile));
            }
            return View(perfil);
        }

        // GET: Perfiles/Edit
        // Muestra el formulario para editar el perfil del usuario actual.
        public async Task<IActionResult> Edit()
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var perfil = await _context.Perfiles.FindAsync(currentUserId.Value);
            if (perfil == null)
            {
                TempData["InfoMessage"] = "No tienes un perfil creado para editar. Por favor, crea uno primero.";
                return RedirectToAction(nameof(Create));
            }

            return View(perfil);
        }

        // POST: Perfiles/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([Bind("UsuarioId,Bio,AvatarUrl,FechaNacimiento,SitioWeb")] Perfil perfil)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue || perfil.UsuarioId != currentUserId.Value)
            {
                // Asegura que el ID del perfil que se intenta editar coincide con el ID del usuario logueado.
                return Forbid(); // Intento de editar un perfil que no le pertenece.
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(perfil);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "¡Tu perfil ha sido actualizado con éxito!";
                    return RedirectToAction(nameof(MyProfile));
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Si el perfil no existe para el usuario actual, es un error.
                    if (!await _context.Perfiles.AnyAsync(e => e.UsuarioId == perfil.UsuarioId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw; // Otro error de concurrencia
                    }
                }
            }
            return View(perfil);
        }

        // GET: Perfiles/Details (Esta será reutilizada por MyProfile)
        // Puedes mantenerla para ver el perfil de cualquier usuario si pasas su ID,
        // pero MyProfile ya filtra por el usuario actual.
        [AllowAnonymous] // Si quieres que los perfiles puedan ser públicos (solo vista)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var perfil = await _context.Perfiles
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(m => m.UsuarioId == id);

            if (perfil == null)
            {
                return NotFound("Perfil no encontrado.");
            }

            return View(perfil);
        }
    }
}
