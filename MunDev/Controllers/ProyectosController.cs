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
using System.Security.Claims; // AGREGADO: Necesario para obtener el ID del usuario

namespace MunDev.Controllers
{
    [Authorize] // Requiere que el usuario esté autenticado para acceder a este controlador
    public class ProyectosController : Controller
    {
        private readonly MunDevContext _context;

        public ProyectosController(MunDevContext context)
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

        // AGREGADO: Método auxiliar para verificar si el usuario tiene acceso a un proyecto.
        // Un usuario tiene acceso si es el creador del proyecto O es miembro del equipo al que pertenece el proyecto.
        private async Task<bool> IsUserAuthorizedForProyecto(Proyecto proyecto, int userId)
        {
            // 1. Es el creador del proyecto?
            if (proyecto.CreadoPorUsuarioId == userId)
            {
                return true;
            }

            // 2. Pertenece el proyecto a un equipo Y el usuario es miembro de ese equipo?
            if (proyecto.EquipoId.HasValue)
            {
                var isMemberOfTeam = await _context.EquipoUsuarios
                                                  .AnyAsync(eu => eu.EquipoId == proyecto.EquipoId.Value &&
                                                                  eu.UsuarioId == userId);
                return isMemberOfTeam;
            }

            // Si no es el creador y no pertenece a un equipo asociado, o el proyecto no tiene equipo.
            return false;
        }


        // GET: Proyectos
        // AJUSTADO: Muestra proyectos personales del usuario actual y proyectos de equipos a los que pertenece.
        public async Task<IActionResult> Index()
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                // Si el usuario no está logueado, redirige a login. Esto se solapa con [Authorize] pero es una buena práctica.
                return RedirectToAction("Login", "Account");
            }

            // Obtener los IDs de todos los equipos a los que pertenece el usuario actual.
            var userTeamIds = await _context.EquipoUsuarios
                                            .Where(eu => eu.UsuarioId == currentUserId.Value)
                                            .Select(eu => eu.EquipoId)
                                            .ToListAsync();

            // Construir la consulta de proyectos:
            // 1. Proyectos creados por el usuario actual (proyectos personales).
            // 2. Proyectos que tienen un EquipoId asignado Y cuyo EquipoId está en la lista de equipos del usuario.
            var proyectosVisibles = await _context.Proyectos
                                                .Include(p => p.CreadoPorUsuario) // Incluye el usuario creador para mostrar su nombre
                                                .Include(p => p.Equipo) // Incluye el equipo asociado
                                                .Where(p => p.CreadoPorUsuarioId == currentUserId.Value || // Es el creador
                                                            (p.EquipoId.HasValue && userTeamIds.Contains(p.EquipoId.Value))) // O es miembro del equipo del proyecto
                                                .OrderBy(p => p.NombreProyecto) // Opcional: ordenar
                                                .ToListAsync();

            return View(proyectosVisibles);
        }

        // GET: Proyectos/Details/5
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

            var proyecto = await _context.Proyectos
                .Include(p => p.CreadoPorUsuario)
                .Include(p => p.Equipo) // Necesario para la validación de equipo en IsUserAuthorizedForProyecto
                .FirstOrDefaultAsync(m => m.ProyectoId == id);

            if (proyecto == null)
            {
                return NotFound("Proyecto no encontrado.");
            }

            // VALIDACIÓN DE AUTORIZACIÓN: Solo el creador o un miembro del equipo puede ver los detalles.
            if (!await IsUserAuthorizedForProyecto(proyecto, currentUserId.Value))
            {
                return Forbid(); // HTTP 403 Forbidden - El usuario no tiene permiso
            }

            return View(proyecto);
        }

        // GET: Proyectos/Create
        // AJUSTADO: Carga equipos solo para el usuario actual y asigna automáticamente CreadoPorUsuarioId en POST.
        public async Task<IActionResult> Create()
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            // Obtener solo los equipos a los que el usuario actual pertenece para el dropdown de selección.
            var userTeams = await _context.EquipoUsuarios
                                          .Where(eu => eu.UsuarioId == currentUserId.Value)
                                          .Select(eu => eu.Equipo)
                                          .ToListAsync();

            // Crea un SelectList para los equipos del usuario. Si no hay equipos, estará vacío.
            ViewData["EquipoId"] = new SelectList(userTeams, "EquipoId", "NombreEquipo");

            // No necesitamos ViewData["CreadoPorUsuarioId"] porque el CreadoPorUsuarioId se asignará automáticamente en el método POST.
            return View();
        }

        // POST: Proyectos/Create
        // AJUSTADO: Asigna CreadoPorUsuarioId, FechaCreacion, y valida EquipoId.
        [HttpPost]
        [ValidateAntiForgeryToken]
        // ATENCIÓN: CreadoPorUsuarioId NO se incluye en el Bind, se asigna en el controlador por seguridad.
        public async Task<IActionResult> Create([Bind("ProyectoId,NombreProyecto,Descripcion,FechaInicio,FechaFinEstimada,EstadoProyecto,EquipoId")] Proyecto proyecto)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            // Asigna el ID del usuario actual como creador del proyecto (seguro y automático).
            proyecto.CreadoPorUsuarioId = currentUserId.Value;

            // Asigna la fecha de creación si no se envió o si es el valor por defecto.
            if (proyecto.FechaCreacion == default(DateTime) || proyecto.FechaCreacion == null)
            {
                proyecto.FechaCreacion = DateTime.Now;
            }

            // Validar que el usuario pertenezca al EquipoId seleccionado (si se seleccionó uno).
            if (proyecto.EquipoId.HasValue)
            {
                var isMemberOfSelectedTeam = await _context.EquipoUsuarios
                                                           .AnyAsync(eu => eu.EquipoId == proyecto.EquipoId.Value &&
                                                                           eu.UsuarioId == currentUserId.Value);
                if (!isMemberOfSelectedTeam)
                {
                    ModelState.AddModelError("EquipoId", "No puedes asociar el proyecto a un equipo al que no perteneces.");
                }
            }

            if (ModelState.IsValid)
            {
                _context.Add(proyecto);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Proyecto creado con éxito.";
                return RedirectToAction(nameof(Index));
            }

            // Si hay errores de validación, recargar el SelectList de equipos para la vista.
            var userTeams = await _context.EquipoUsuarios
                                          .Where(eu => eu.UsuarioId == currentUserId.Value)
                                          .Select(eu => eu.Equipo)
                                          .ToListAsync();
            ViewData["EquipoId"] = new SelectList(userTeams, "EquipoId", "NombreEquipo", proyecto.EquipoId);
            // Ya no necesitamos ViewData["CreadoPorUsuarioId"] aquí.
            return View(proyecto);
        }

        // GET: Proyectos/Edit/5
        // AJUSTADO: Incluye validación de autorización y carga equipos del usuario.
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

            var proyecto = await _context.Proyectos
                                        .Include(p => p.Equipo) // Necesario para precargar el valor seleccionado
                                        .FirstOrDefaultAsync(m => m.ProyectoId == id);
            if (proyecto == null)
            {
                return NotFound();
            }

            // VALIDACIÓN DE AUTORIZACIÓN: Solo el creador o un miembro del equipo puede editar.
            if (!await IsUserAuthorizedForProyecto(proyecto, currentUserId.Value))
            {
                return Forbid();
            }

            // Cargar la lista de equipos del usuario actual para el dropdown
            var userTeams = await _context.EquipoUsuarios
                                          .Where(eu => eu.UsuarioId == currentUserId.Value)
                                          .Select(eu => eu.Equipo)
                                          .ToListAsync();
            ViewData["EquipoId"] = new SelectList(userTeams, "EquipoId", "NombreEquipo", proyecto.EquipoId);

            // No necesitamos ViewData["CreadoPorUsuarioId"] aquí.
            return View(proyecto);
        }

        // POST: Proyectos/Edit/5
        // AJUSTADO: Preserva CreadoPorUsuarioId y FechaCreacion, valida EquipoId y autorización.
        [HttpPost]
        [ValidateAntiForgeryToken]
        // ATENCIÓN: CreadoPorUsuarioId NO se incluye en el Bind para evitar manipulaciones.
        public async Task<IActionResult> Edit(int id, [Bind("ProyectoId,NombreProyecto,Descripcion,FechaInicio,FechaFinEstimada,EstadoProyecto,EquipoId")] Proyecto proyecto)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            if (id != proyecto.ProyectoId)
            {
                return NotFound();
            }

            // Recupera el proyecto existente (sin tracking) para verificar el creador original y el equipo original.
            // Esto es crucial para la seguridad y para preservar los datos que no se editan en el formulario.
            var existingProyecto = await _context.Proyectos.AsNoTracking()
                                                         .Include(p => p.Equipo) // Incluir Equipo para la validación de autorización
                                                         .FirstOrDefaultAsync(p => p.ProyectoId == id);
            if (existingProyecto == null)
            {
                return NotFound();
            }

            // VALIDACIÓN DE AUTORIZACIÓN: Asegúrate de que el usuario actual está autorizado a editar este proyecto.
            if (!await IsUserAuthorizedForProyecto(existingProyecto, currentUserId.Value))
            {
                return Forbid();
            }

            // Asigna el CreadoPorUsuarioId y FechaCreacion del proyecto existente al objeto que se va a actualizar
            // Esto evita que se sobrescriban con valores nulos o incorrectos desde el Bind.
            proyecto.CreadoPorUsuarioId = existingProyecto.CreadoPorUsuarioId;
            proyecto.FechaCreacion = existingProyecto.FechaCreacion; // Preserva la fecha original

            // Validar que el usuario pertenezca al EquipoId seleccionado (si se seleccionó uno).
            if (proyecto.EquipoId.HasValue)
            {
                var isMemberOfSelectedTeam = await _context.EquipoUsuarios
                                                           .AnyAsync(eu => eu.EquipoId == proyecto.EquipoId.Value &&
                                                                           eu.UsuarioId == currentUserId.Value);
                if (!isMemberOfSelectedTeam)
                {
                    ModelState.AddModelError("EquipoId", "No puedes asociar el proyecto a un equipo al que no perteneces.");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(proyecto);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Proyecto actualizado con éxito.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Manejo de concurrencia: si el proyecto fue modificado o eliminado por otro usuario.
                    if (!ProyectoExists(proyecto.ProyectoId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw; // Otro error de concurrencia, re-lanza
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            // Si ModelState.IsValid falla, recargar el SelectList de equipos.
            var userTeams = await _context.EquipoUsuarios
                                          .Where(eu => eu.UsuarioId == currentUserId.Value)
                                          .Select(eu => eu.Equipo)
                                          .ToListAsync();
            ViewData["EquipoId"] = new SelectList(userTeams, "EquipoId", "NombreEquipo", proyecto.EquipoId);
            return View(proyecto);
        }

        // GET: Proyectos/Delete/5
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

            var proyecto = await _context.Proyectos
                .Include(p => p.CreadoPorUsuario)
                .Include(p => p.Equipo) // Necesario para la validación de equipo
                .FirstOrDefaultAsync(m => m.ProyectoId == id);

            if (proyecto == null)
            {
                return NotFound();
            }

            // VALIDACIÓN DE AUTORIZACIÓN: Solo el creador o un miembro del equipo puede eliminar.
            if (!await IsUserAuthorizedForProyecto(proyecto, currentUserId.Value))
            {
                return Forbid();
            }

            return View(proyecto);
        }

        // POST: Proyectos/Delete/5
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

            // Recupera el proyecto incluyendo su Equipo para la validación de autorización
            var proyecto = await _context.Proyectos
                                        .Include(p => p.Equipo)
                                        .FirstOrDefaultAsync(p => p.ProyectoId == id);

            if (proyecto == null)
            {
                return NotFound();
            }

            // VALIDACIÓN DE AUTORIZACIÓN: Asegúrate de que el usuario actual está autorizado a eliminar este proyecto.
            if (!await IsUserAuthorizedForProyecto(proyecto, currentUserId.Value))
            {
                return Forbid();
            }

            _context.Proyectos.Remove(proyecto);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Proyecto eliminado con éxito.";
            return RedirectToAction(nameof(Index));
        }

        // Este método se mantiene simple, solo verifica la existencia del proyecto por ID.
        private bool ProyectoExists(int id)
        {
            return _context.Proyectos.Any(e => e.ProyectoId == id);
        }
    }
}
