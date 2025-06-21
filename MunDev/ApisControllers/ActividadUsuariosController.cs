using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MunDev.Data;
using MunDev.Models;

namespace MunDev.ApisControllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ActividadUsuariosController : ControllerBase
    {
        private readonly MunDevContext _context;

        public ActividadUsuariosController(MunDevContext context)
        {
            _context = context;
        }

        // GET: api/ActividadUsuarios
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ActividadUsuario>>> GetActividadUsuarios()
        {
            return await _context.ActividadUsuarios.ToListAsync();
        }

        // GET: api/ActividadUsuarios/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ActividadUsuario>> GetActividadUsuario(int id)
        {
            var actividadUsuario = await _context.ActividadUsuarios.FindAsync(id);

            if (actividadUsuario == null)
            {
                return NotFound();
            }

            return actividadUsuario;
        }

        // PUT: api/ActividadUsuarios/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutActividadUsuario(int id, ActividadUsuario actividadUsuario)
        {
            if (id != actividadUsuario.ActividadUsuarioId)
            {
                return BadRequest();
            }

            _context.Entry(actividadUsuario).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ActividadUsuarioExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/ActividadUsuarios
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<ActividadUsuario>> PostActividadUsuario(ActividadUsuario actividadUsuario)
        {
            _context.ActividadUsuarios.Add(actividadUsuario);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetActividadUsuario", new { id = actividadUsuario.ActividadUsuarioId }, actividadUsuario);
        }

        // DELETE: api/ActividadUsuarios/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteActividadUsuario(int id)
        {
            var actividadUsuario = await _context.ActividadUsuarios.FindAsync(id);
            if (actividadUsuario == null)
            {
                return NotFound();
            }

            _context.ActividadUsuarios.Remove(actividadUsuario);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ActividadUsuarioExists(int id)
        {
            return _context.ActividadUsuarios.Any(e => e.ActividadUsuarioId == id);
        }
    }
}
