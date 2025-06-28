using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MunDev.Data;
using MunDev.Models;
using System.Security.Claims; // AGREGADO: Para obtener el ID del usuario

namespace MunDev.Controllers
{
    [Authorize]
    public class EquiposController : Controller
    {
        private readonly MunDevContext _context;

        public EquiposController(MunDevContext context)
        {
            _context = context;
        }

        // AGREGADO: Método auxiliar para obtener el ID del usuario actual
        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return null; // Devuelve null si el usuario no está autenticado o su ID no es un entero válido
        }

        // AGREGADO: Método auxiliar para verificar si el usuario tiene acceso a un equipo (creador o miembro).
        private async Task<bool> IsUserAuthorizedForEquipo(Equipo equipo, int userId)
        {
            // 1. Es el creador del equipo?
            if (equipo.CreadoPorUsuarioId == userId)
            {
                return true;
            }

            // 2. Es miembro del equipo?
            var isMember = await _context.EquipoUsuarios
                                        .AnyAsync(eu => eu.EquipoId == equipo.EquipoId &&
                                                        eu.UsuarioId == userId);
            return isMember;
        }

        // GET: Equipos
        // AJUSTADO: Muestra solo los equipos del usuario logueado.
        public async Task<IActionResult> Index()
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                // Si el usuario no está logueado, redirige a login.
                return RedirectToAction("Login", "Account");
            }

            // Obtener los IDs de los equipos a los que el usuario es miembro
            var memberTeamIds = await _context.EquipoUsuarios
                                            .Where(eu => eu.UsuarioId == currentUserId.Value)
                                            .Select(eu => eu.EquipoId)
                                            .ToListAsync();

            // Filtrar equipos:
            // 1. Equipos creados por el usuario actual.
            // 2. Equipos donde el usuario actual es miembro.
            var equiposVisibles = await _context.Equipos
                                                .Include(e => e.CreadoPorUsuario) // Incluir CreadoPorUsuario
                                                .Where(e => e.CreadoPorUsuarioId == currentUserId.Value || // Es el creador
                                                            memberTeamIds.Contains(e.EquipoId)) // O es miembro
                                                .OrderBy(e => e.NombreEquipo) // Opcional: ordenar
                                                .ToListAsync();

            return View(equiposVisibles);
        }

        // GET: Equipos/Details/5
        // AJUSTADO: Incluye validación de autorización.
        public async Task<IActionResult> Details(int? id)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            if (id == null)
            {
                return NotFound();
            }

            var equipo = await _context.Equipos
                .Include(e => e.CreadoPorUsuario)
                .FirstOrDefaultAsync(m => m.EquipoId == id);

            if (equipo == null)
            {
                return NotFound("Equipo no encontrado.");
            }

            // VALIDACIÓN DE AUTORIZACIÓN: Solo el creador o un miembro del equipo puede ver los detalles.
            if (!await IsUserAuthorizedForEquipo(equipo, currentUserId.Value))
            {
                return Forbid(); // HTTP 403 Forbidden - El usuario no tiene permiso
            }

            return View(equipo);
        }

        // GET: Equipos/Create
        // AJUSTADO: Asigna automáticamente CreadoPorUsuarioId en POST y no lo pide en el formulario.
        public IActionResult Create()
        {
            // No necesitamos ViewData["CreadoPorUsuarioId"] aquí, porque se asignará en el POST.
            // Los usuarios solo pueden crear equipos bajo su propio nombre.
            return View();
        }

        // POST: Equipos/Create
        // AJUSTADO: Asigna CreadoPorUsuarioId y añade el creador como primer miembro del equipo.
        [HttpPost]
        [ValidateAntiForgeryToken]
        // ATENCIÓN: CreadoPorUsuarioId NO se incluye en el Bind, se asigna en el controlador por seguridad.
        public async Task<IActionResult> Create([Bind("EquipoId,NombreEquipo,Descripcion")] Equipo equipo)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            // Asigna el ID del usuario actual como creador del equipo (seguro y automático).
            equipo.CreadoPorUsuarioId = currentUserId.Value;

            if (ModelState.IsValid)
            {
                _context.Add(equipo);
                await _context.SaveChangesAsync(); // Guarda el equipo para obtener su EquipoId.

                // AGREGADO: Añadir al usuario creador como el primer miembro del equipo
                var equipoUsuario = new EquipoUsuario
                {
                    EquipoId = equipo.EquipoId,
                    UsuarioId = currentUserId.Value,
                    FechaUnion = DateTime.Now,
                    RolEnEquipo = "Administrador" // El creador es el administrador por defecto
                };
                _context.EquipoUsuarios.Add(equipoUsuario);
                await _context.SaveChangesAsync(); // Guarda la relación EquipoUsuario.

                TempData["SuccessMessage"] = "Equipo creado con éxito y te has añadido como administrador.";
                return RedirectToAction(nameof(Index));
            }

            // Si ModelState.IsValid falla, simplemente regresa la vista.
            // No hay SelectList de CreadoPorUsuarioId para recargar.
            return View(equipo);
        }

        // GET: Equipos/Edit/5
        // AJUSTADO: Incluye validación de autorización.
        public async Task<IActionResult> Edit(int? id)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            if (id == null)
            {
                return NotFound();
            }

            var equipo = await _context.Equipos.FindAsync(id);
            if (equipo == null)
            {
                return NotFound();
            }

            // VALIDACIÓN DE AUTORIZACIÓN: Solo el creador o un administrador del equipo puede editar.
            // NOTA: Aquí solo verificamos si es el creador. Si implementas roles de equipo más avanzados
            // deberías verificar si el usuario es un "Administrador" del equipo via EquipoUsuario.RolEnEquipo.
            if (!await IsUserAuthorizedForEquipo(equipo, currentUserId.Value))
            {
                return Forbid();
            }

            // No necesitamos ViewData["CreadoPorUsuarioId"] porque no se edita en el formulario.
            return View(equipo);
        }

        // POST: Equipos/Edit/5
        // AJUSTADO: Preserva CreadoPorUsuarioId y valida autorización.
        [HttpPost]
        [ValidateAntiForgeryToken]
        // ATENCIÓN: CreadoPorUsuarioId NO se incluye en el Bind para evitar manipulaciones.
        public async Task<IActionResult> Edit(int id, [Bind("EquipoId,NombreEquipo,Descripcion")] Equipo equipo)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            if (id != equipo.EquipoId)
            {
                return NotFound();
            }

            // Recupera el equipo existente (sin tracking) para verificar el creador original.
            var existingEquipo = await _context.Equipos.AsNoTracking().FirstOrDefaultAsync(e => e.EquipoId == id);
            if (existingEquipo == null)
            {
                return NotFound();
            }

            // VALIDACIÓN DE AUTORIZACIÓN: Asegúrate de que el usuario actual está autorizado a editar este equipo.
            if (!await IsUserAuthorizedForEquipo(existingEquipo, currentUserId.Value))
            {
                return Forbid();
            }

            // Asigna el CreadoPorUsuarioId del equipo existente al objeto que se va a actualizar
            // Esto evita que se sobrescriba con un valor nulo o incorrecto desde el Bind.
            equipo.CreadoPorUsuarioId = existingEquipo.CreadoPorUsuarioId;

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(equipo);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Equipo actualizado con éxito.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EquipoExists(equipo.EquipoId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            // Si ModelState.IsValid falla, simplemente regresa la vista.
            return View(equipo);
        }

        // GET: Equipos/Delete/5
        // AJUSTADO: Incluye validación de autorización.
        public async Task<IActionResult> Delete(int? id)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            if (id == null)
            {
                return NotFound();
            }

            var equipo = await _context.Equipos
                .Include(e => e.CreadoPorUsuario)
                .FirstOrDefaultAsync(m => m.EquipoId == id);
            if (equipo == null)
            {
                return NotFound();
            }

            // VALIDACIÓN DE AUTORIZACIÓN: Solo el creador o un administrador del equipo puede eliminar.
            if (!await IsUserAuthorizedForEquipo(equipo, currentUserId.Value))
            {
                return Forbid();
            }

            return View(equipo);
        }

        // POST: Equipos/Delete/5
        // AJUSTADO: Incluye validación de autorización.
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var equipo = await _context.Equipos.FindAsync(id);
            if (equipo == null)
            {
                return NotFound();
            }

            // VALIDACIÓN DE AUTORIZACIÓN: Asegúrate de que el usuario actual está autorizado a eliminar este equipo.
            if (!await IsUserAuthorizedForEquipo(equipo, currentUserId.Value))
            {
                return Forbid();
            }

            // Eliminar los EquipoUsuarios relacionados primero para evitar errores de clave externa
            var equipoUsuarios = await _context.EquipoUsuarios.Where(eu => eu.EquipoId == id).ToListAsync();
            _context.EquipoUsuarios.RemoveRange(equipoUsuarios);
            await _context.SaveChangesAsync();

            _context.Equipos.Remove(equipo);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Equipo eliminado con éxito.";
            return RedirectToAction(nameof(Index));
        }

        private bool EquipoExists(int id)
        {
            return _context.Equipos.Any(e => e.EquipoId == id);
        }
    }
}
