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

namespace MunDev.Controllers
{
    [Authorize]
    public class TareasController : Controller
    {
        private readonly MunDevContext _context;

        public TareasController(MunDevContext context)
        {
            _context = context;
        }

        // GET: Tareas
        public async Task<IActionResult> Index()
        {
            // Incluir las propiedades de navegación para mostrar los nombres en la tabla
            var munDevContext = _context.Tareas
                .Include(t => t.AsignadoAusuario) // Para mostrar el nombre del usuario asignado
                .Include(t => t.Proyecto);         // Para mostrar el nombre del proyecto
            return View(await munDevContext.ToListAsync());
        }

        // GET: Tareas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tarea = await _context.Tareas
                .Include(t => t.AsignadoAusuario) // Incluir AsignadoAusuario
                .Include(t => t.Proyecto)         // Incluir Proyecto
                .FirstOrDefaultAsync(m => m.TareaId == id);
            if (tarea == null)
            {
                return NotFound();
            }

            return View(tarea);
        }

        // GET: Tareas/Create
        public IActionResult Create()
        {
            // CAMBIO AQUÍ: Usar "NombreUsuario" y "NombreProyecto" para el texto de los SelectList
            ViewData["AsignadoAusuarioId"] = new SelectList(_context.Usuarios, "UsuarioId", "NombreUsuario"); // Asumo NombreUsuario
            ViewData["ProyectoId"] = new SelectList(_context.Proyectos, "ProyectoId", "NombreProyecto");     // Asumo NombreProyecto

            // Opciones fijas para EstadoTarea y Prioridad
            ViewBag.EstadoTareaOptions = new SelectList(new List<string> { "Pendiente", "En Progreso", "Completada", "Bloqueada" });
            ViewBag.PrioridadOptions = new SelectList(new List<string> { "Baja", "Media", "Alta", "Crítica" });

            return View();
        }

        // POST: Tareas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TareaId,ProyectoId,Titulo,Descripcion,FechaVencimiento,EstadoTarea,Prioridad,AsignadoAusuarioId")] Tarea tarea)
        {
            if (ModelState.IsValid)
            {
                _context.Add(tarea);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            // CAMBIO AQUÍ: Recargar SelectList con nombres si la validación falla
            ViewData["AsignadoAusuarioId"] = new SelectList(_context.Usuarios, "UsuarioId", "NombreUsuario", tarea.AsignadoAusuarioId);
            ViewData["ProyectoId"] = new SelectList(_context.Proyectos, "ProyectoId", "NombreProyecto", tarea.ProyectoId);
            ViewBag.EstadoTareaOptions = new SelectList(new List<string> { "Pendiente", "En Progreso", "Completada", "Bloqueada" }, tarea.EstadoTarea);
            ViewBag.PrioridadOptions = new SelectList(new List<string> { "Baja", "Media", "Alta", "Crítica" }, tarea.Prioridad);
            return View(tarea);
        }

        // GET: Tareas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tarea = await _context.Tareas.FindAsync(id);
            if (tarea == null)
            {
                return NotFound();
            }
            // CAMBIO AQUÍ: Cargar SelectList con nombres
            ViewData["AsignadoAusuarioId"] = new SelectList(_context.Usuarios, "UsuarioId", "NombreUsuario", tarea.AsignadoAusuarioId);
            ViewData["ProyectoId"] = new SelectList(_context.Proyectos, "ProyectoId", "NombreProyecto", tarea.ProyectoId);
            ViewBag.EstadoTareaOptions = new SelectList(new List<string> { "Pendiente", "En Progreso", "Completada", "Bloqueada" }, tarea.EstadoTarea);
            ViewBag.PrioridadOptions = new SelectList(new List<string> { "Baja", "Media", "Alta", "Crítica" }, tarea.Prioridad);
            return View(tarea);
        }

        // POST: Tareas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("TareaId,ProyectoId,Titulo,Descripcion,FechaVencimiento,EstadoTarea,Prioridad,AsignadoAusuarioId")] Tarea tarea)
        {
            if (id != tarea.TareaId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(tarea);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TareaExists(tarea.TareaId))
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
            // CAMBIO AQUÍ: Recargar SelectList con nombres si la validación falla
            ViewData["AsignadoAusuarioId"] = new SelectList(_context.Usuarios, "UsuarioId", "NombreUsuario", tarea.AsignadoAusuarioId);
            ViewData["ProyectoId"] = new SelectList(_context.Proyectos, "ProyectoId", "NombreProyecto", tarea.ProyectoId);
            ViewBag.EstadoTareaOptions = new SelectList(new List<string> { "Pendiente", "En Progreso", "Completada", "Bloqueada" }, tarea.EstadoTarea);
            ViewBag.PrioridadOptions = new SelectList(new List<string> { "Baja", "Media", "Alta", "Crítica" }, tarea.Prioridad);
            return View(tarea);
        }

        // GET: Tareas/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tarea = await _context.Tareas
                .Include(t => t.AsignadoAusuario)
                .Include(t => t.Proyecto)
                .FirstOrDefaultAsync(m => m.TareaId == id);
            if (tarea == null)
            {
                return NotFound();
            }

            return View(tarea);
        }

        // POST: Tareas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var tarea = await _context.Tareas.FindAsync(id);
            if (tarea != null)
            {
                _context.Tareas.Remove(tarea);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TareaExists(int id)
        {
            return _context.Tareas.Any(e => e.TareaId == id);
        }
    }
}
