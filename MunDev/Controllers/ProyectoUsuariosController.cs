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
        public async Task<IActionResult> Create()
        {
            ViewData["ProyectoId"] = new SelectList(_context.Proyectos, "ProyectoId", "NombreProyecto");
            ViewData["UsuarioId"] = new SelectList(_context.Usuarios, "UsuarioId", "NombreUsuario");
            ViewData["Roles"] = new SelectList(await _context.Rols.ToListAsync(), "NombreRol", "NombreRol");
            return View();
        }

        // POST: ProyectoUsuarios/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProyectoUsuarioId,ProyectoId,UsuarioId,RolEnProyecto,FechaAsignacion")] ProyectoUsuario proyectoUsuario)
        {
            if (ModelState.IsValid)
            {
                proyectoUsuario.FechaAsignacion = DateTime.Now;
                _context.Add(proyectoUsuario);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ProyectoId"] = new SelectList(_context.Proyectos, "ProyectoId", "NombreProyecto", proyectoUsuario.ProyectoId);
            ViewData["UsuarioId"] = new SelectList(_context.Usuarios, "UsuarioId", "NombreUsuario", proyectoUsuario.UsuarioId);
            ViewData["Roles"] = new SelectList(await _context.Rols.ToListAsync(), "NombreRol", "NombreRol");
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
            ViewData["ProyectoId"] = new SelectList(_context.Proyectos, "ProyectoId", "NombreProyecto", proyectoUsuario.ProyectoId);
            ViewData["UsuarioId"] = new SelectList(_context.Usuarios, "UsuarioId", "NombreUsuario", proyectoUsuario.UsuarioId);
            return View(proyectoUsuario);
        }

        // POST: ProyectoUsuarios/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
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
            ViewData["ProyectoId"] = new SelectList(_context.Proyectos, "ProyectoId", "NombreProyecto", proyectoUsuario.ProyectoId);
            ViewData["UsuarioId"] = new SelectList(_context.Usuarios, "UsuarioId", "NombreUsuario", proyectoUsuario.UsuarioId);
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
