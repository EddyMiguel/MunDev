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
    public class NotificationsController : Controller
    {
        private readonly MunDevContext _context;

        public NotificationsController(MunDevContext context)
        {
            _context = context;
        }

        // GET: Notifications
        public async Task<IActionResult> Index()
        {
            var munDevContext = _context.Notificacions.Include(n => n.Usuario);
            return View(await munDevContext.ToListAsync());
        }

        // GET: Notifications/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var notificacion = await _context.Notificacions
                .Include(n => n.Usuario)
                .FirstOrDefaultAsync(m => m.NotificacionId == id);
            if (notificacion == null)
            {
                return NotFound();
            }

            return View(notificacion);
        }

        // GET: Notifications/Create
        public IActionResult Create()
        {
            ViewData["UsuarioId"] = new SelectList(_context.Usuarios, "UsuarioId", "NombreUsuario");
            ViewBag.TipoNotificacionOptions = new SelectList(new List<string> { "Información", "Advertencia", "Error", "Éxito" });
            // Por defecto, una notificación nueva se asume como no leída
            ViewBag.LeidaOptions = new SelectList(new List<SelectListItem>
            {
                new SelectListItem { Value = "true", Text = "Sí" },
                new SelectListItem { Value = "false", Text = "No", Selected = true }
            }, "Value", "Text");

            return View();
        }

        // POST: Notifications/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("NotificacionId,UsuarioId,Mensaje,FechaCreacion,Leida,TipoNotificacion")] Notificacion notificacion)
        {
            if (notificacion.FechaCreacion == default(DateTime))
            {
                notificacion.FechaCreacion = DateTime.Now;
            }

            if (ModelState.IsValid)
            {
                _context.Add(notificacion);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["UsuarioId"] = new SelectList(_context.Usuarios, "UsuarioId", "NombreUsuario", notificacion.UsuarioId);
            ViewBag.TipoNotificacionOptions = new SelectList(new List<string> { "Información", "Advertencia", "Error", "Éxito" }, notificacion.TipoNotificacion);
            ViewBag.LeidaOptions = new SelectList(new List<SelectListItem>
            {
                new SelectListItem { Value = "true", Text = "Sí" },
                new SelectListItem { Value = "false", Text = "No" }
            }, "Value", "Text", notificacion.Leida.ToString().ToLower());
            return View(notificacion);
        }

        // GET: Notifications/Edit/5
        // ESTOS MÉTODOS FUERON ELIMINADOS PARA EVITAR LA EDICIÓN.
        // public async Task<IActionResult> Edit(int? id) { ... }
        // POST: Notifications/Edit/5
        // [HttpPost]
        // [ValidateAntiForgeryToken]
        // public async Task<IActionResult> Edit(int id, [Bind(...)] Notificacion notificacion) { ... }


        // GET: Notifications/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var notificacion = await _context.Notificacions
                .Include(n => n.Usuario)
                .FirstOrDefaultAsync(m => m.NotificacionId == id);
            if (notificacion == null)
            {
                return NotFound();
            }

            return View(notificacion);
        }

        // POST: Notifications/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var notificacion = await _context.Notificacions.FindAsync(id);
            if (notificacion != null)
            {
                _context.Notificacions.Remove(notificacion);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool NotificacionExists(int id)
        {
            return _context.Notificacions.Any(e => e.NotificacionId == id);
        }
    }
}
