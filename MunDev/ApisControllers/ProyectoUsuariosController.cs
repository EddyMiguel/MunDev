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
    public class ProyectoUsuariosController : ControllerBase
    {
        private readonly MunDevContext _context;

        public ProyectoUsuariosController(MunDevContext context)
        {
            _context = context;
        }

        // GET: api/ProyectoUsuarios
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProyectoUsuario>>> GetProyectoUsuarios()
        {
            return await _context.ProyectoUsuarios.ToListAsync();
        }

        // GET: api/ProyectoUsuarios/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ProyectoUsuario>> GetProyectoUsuario(int id)
        {
            var proyectoUsuario = await _context.ProyectoUsuarios.FindAsync(id);

            if (proyectoUsuario == null)
            {
                return NotFound();
            }

            return proyectoUsuario;
        }

        // PUT: api/ProyectoUsuarios/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProyectoUsuario(int id, ProyectoUsuario proyectoUsuario)
        {
            if (id != proyectoUsuario.ProyectoUsuarioId)
            {
                return BadRequest();
            }

            _context.Entry(proyectoUsuario).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProyectoUsuarioExists(id))
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

        // POST: api/ProyectoUsuarios
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<ProyectoUsuario>> PostProyectoUsuario(ProyectoUsuario proyectoUsuario)
        {
            _context.ProyectoUsuarios.Add(proyectoUsuario);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetProyectoUsuario", new { id = proyectoUsuario.ProyectoUsuarioId }, proyectoUsuario);
        }

        // DELETE: api/ProyectoUsuarios/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProyectoUsuario(int id)
        {
            var proyectoUsuario = await _context.ProyectoUsuarios.FindAsync(id);
            if (proyectoUsuario == null)
            {
                return NotFound();
            }

            _context.ProyectoUsuarios.Remove(proyectoUsuario);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ProyectoUsuarioExists(int id)
        {
            return _context.ProyectoUsuarios.Any(e => e.ProyectoUsuarioId == id);
        }
    }
}
