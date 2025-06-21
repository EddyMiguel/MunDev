using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MunDev.Data;
using MunDev.Models;

namespace MunDev.Controllers
{
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
            var munDevContext = _context.Tareas.Include(t => t.AsignadoAusuario).Include(t => t.Proyecto);
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
                .Include(t => t.AsignadoAusuario)
                .Include(t => t.Proyecto)
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
            ViewData["AsignadoAusuarioId"] = new SelectList(_context.Usuarios, "UsuarioId", "UsuarioId");
            ViewData["ProyectoId"] = new SelectList(_context.Proyectos, "ProyectoId", "ProyectoId");
            return View();
        }

        // POST: Tareas/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
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
            ViewData["AsignadoAusuarioId"] = new SelectList(_context.Usuarios, "UsuarioId", "UsuarioId", tarea.AsignadoAusuarioId);
            ViewData["ProyectoId"] = new SelectList(_context.Proyectos, "ProyectoId", "ProyectoId", tarea.ProyectoId);
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
            ViewData["AsignadoAusuarioId"] = new SelectList(_context.Usuarios, "UsuarioId", "UsuarioId", tarea.AsignadoAusuarioId);
            ViewData["ProyectoId"] = new SelectList(_context.Proyectos, "ProyectoId", "ProyectoId", tarea.ProyectoId);
            return View(tarea);
        }

        // POST: Tareas/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
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
            ViewData["AsignadoAusuarioId"] = new SelectList(_context.Usuarios, "UsuarioId", "UsuarioId", tarea.AsignadoAusuarioId);
            ViewData["ProyectoId"] = new SelectList(_context.Proyectos, "ProyectoId", "ProyectoId", tarea.ProyectoId);
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
