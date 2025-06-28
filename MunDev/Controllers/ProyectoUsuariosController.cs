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
    public class ProyectoUsuariosController : Controller
    {
        private readonly MunDevContext _context;

        public ProyectoUsuariosController(MunDevContext context)
        {
            _context = context;
        }

        // GET: ProyectoUsuarios
        public async Task<IActionResult> Index()
        {
            // Incluir las propiedades de navegación Proyecto y Usuario para mostrar sus nombres
            var munDevContext = _context.ProyectoUsuarios
                .Include(p => p.Proyecto)
                .Include(p => p.Usuario);
            return View(await munDevContext.ToListAsync());
        }

        // GET: ProyectoUsuarios/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var proyectoUsuario = await _context.ProyectoUsuarios
                .Include(p => p.Proyecto)
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(m => m.ProyectoUsuarioId == id);
            if (proyectoUsuario == null)
            {
                return NotFound();
            }

            return View(proyectoUsuario);
        }

        // GET: ProyectoUsuarios/Create
        public IActionResult Create()
        {
            // CAMBIO AQUÍ: Usar "NombreProyecto" y "NombreUsuario" para el texto de los SelectList
            ViewData["ProyectoId"] = new SelectList(_context.Proyectos, "ProyectoId", "NombreProyecto");
            ViewData["UsuarioId"] = new SelectList(_context.Usuarios, "UsuarioId", "NombreUsuario"); // O "Email" si usas el email como nombre visible
            // Ejemplo de roles fijos. Puedes obtenerlos de una tabla de Roles si tienes una.
            ViewBag.Roles = new SelectList(new List<string> { "Desarrollador", "Diseñador", "QA", "Scrum Master", "Líder de Proyecto" });
            return View();
        }

        // POST: ProyectoUsuarios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Bind para las propiedades reales que vienen del formulario (IDs y RolEnProyecto)
        public async Task<IActionResult> Create([Bind("ProyectoUsuarioId,ProyectoId,UsuarioId,RolEnProyecto,FechaAsignacion")] ProyectoUsuario proyectoUsuario)
        {
            // Asigna la fecha de asignación si no se proporciona desde el formulario.
            if (proyectoUsuario.FechaAsignacion == default(DateTime))
            {
                proyectoUsuario.FechaAsignacion = DateTime.Now;
            }

            if (ModelState.IsValid)
            {
                _context.Add(proyectoUsuario);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            // CAMBIO AQUÍ: Recargar SelectList con nombres si la validación falla
            ViewData["ProyectoId"] = new SelectList(_context.Proyectos, "ProyectoId", "NombreProyecto", proyectoUsuario.ProyectoId);
            ViewData["UsuarioId"] = new SelectList(_context.Usuarios, "UsuarioId", "NombreUsuario", proyectoUsuario.UsuarioId);
            ViewBag.Roles = new SelectList(new List<string> { "Desarrollador", "Diseñador", "QA", "Scrum Master", "Líder de Proyecto" }, proyectoUsuario.RolEnProyecto);
            return View(proyectoUsuario);
        }

        // GET: ProyectoUsuarios/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var proyectoUsuario = await _context.ProyectoUsuarios.FindAsync(id);
            if (proyectoUsuario == null)
            {
                return NotFound();
            }
            // CAMBIO AQUÍ: Cargar SelectList con nombres
            ViewData["ProyectoId"] = new SelectList(_context.Proyectos, "ProyectoId", "NombreProyecto", proyectoUsuario.ProyectoId);
            ViewData["UsuarioId"] = new SelectList(_context.Usuarios, "UsuarioId", "NombreUsuario", proyectoUsuario.UsuarioId);
            ViewBag.Roles = new SelectList(new List<string> { "Desarrollador", "Diseñador", "QA", "Scrum Master", "Líder de Proyecto" }, proyectoUsuario.RolEnProyecto);
            return View(proyectoUsuario);
        }

        // POST: ProyectoUsuarios/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProyectoUsuarioId,ProyectoId,UsuarioId,RolEnProyecto,FechaAsignacion")] ProyectoUsuario proyectoUsuario)
        {
            if (id != proyectoUsuario.ProyectoUsuarioId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Al editar, la FechaAsignacion no se envía desde el formulario.
                    // Para evitar sobrescribirla, la recuperamos del original.
                    var existingPu = await _context.ProyectoUsuarios.AsNoTracking().FirstOrDefaultAsync(pu => pu.ProyectoUsuarioId == id);
                    if (existingPu != null)
                    {
                        proyectoUsuario.FechaAsignacion = existingPu.FechaAsignacion;
                    }
                    else
                    {
                        proyectoUsuario.FechaAsignacion = DateTime.Now; // Si no se encuentra, asigna la actual.
                    }

                    _context.Update(proyectoUsuario);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProyectoUsuarioExists(proyectoUsuario.ProyectoUsuarioId))
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
            ViewData["ProyectoId"] = new SelectList(_context.Proyectos, "ProyectoId", "NombreProyecto", proyectoUsuario.ProyectoId);
            ViewData["UsuarioId"] = new SelectList(_context.Usuarios, "UsuarioId", "NombreUsuario", proyectoUsuario.UsuarioId);
            ViewBag.Roles = new SelectList(new List<string> { "Desarrollador", "Diseñador", "QA", "Scrum Master", "Líder de Proyecto" }, proyectoUsuario.RolEnProyecto);
            return View(proyectoUsuario);
        }

        // GET: ProyectoUsuarios/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var proyectoUsuario = await _context.ProyectoUsuarios
                .Include(p => p.Proyecto)
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(m => m.ProyectoUsuarioId == id);
            if (proyectoUsuario == null)
            {
                return NotFound();
            }

            return View(proyectoUsuario);
        }

        // POST: ProyectoUsuarios/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var proyectoUsuario = await _context.ProyectoUsuarios.FindAsync(id);
            if (proyectoUsuario != null)
            {
                _context.ProyectoUsuarios.Remove(proyectoUsuario);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProyectoUsuarioExists(int id)
        {
            return _context.ProyectoUsuarios.Any(e => e.ProyectoUsuarioId == id);
        }
    }
}
