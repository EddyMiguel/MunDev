using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MunDev.Data;
using MunDev.Models;
using Octokit; 
using Microsoft.Extensions.Configuration; 
using Microsoft.AspNetCore.Authorization;

namespace MunDev.Controllers
{
    [Authorize]
    public class RepositoriosController : Controller
    {
        private readonly MunDevContext _context;
       

        // Constructor: Inyecta el contexto de la base de datos y la configuración (si usas token).
        public RepositoriosController(MunDevContext context /*, IConfiguration configuration */)
        {
            _context = context;

        }

        // GET: Repositorios (Muestra una lista de repositorios)
        public async Task<IActionResult> Index()
        {
            // Incluye la propiedad de navegación Proyecto para mostrar el nombre del proyecto.
            var munDevContext = _context.Repositorios.Include(r => r.Proyecto);
            return View(await munDevContext.ToListAsync());
        }

        // GET: Repositorios/Details/5 (Muestra los detalles de un repositorio específico)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Busca el repositorio incluyendo la información del proyecto asociado.
            var repositorio = await _context.Repositorios
                .Include(r => r.Proyecto)
                .FirstOrDefaultAsync(m => m.RepositorioId == id);
            if (repositorio == null)
            {
                return NotFound();
            }

            return View(repositorio);
        }

        public IActionResult Create()
        {
            ViewData["ProyectoId"] = new SelectList(_context.Proyectos, "ProyectoId", "NombreProyecto");
            return View();
        }

        // POST: Repositorios/Create (Procesa el envío del formulario de creación)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RepositorioId,ProyectoId,NombreRepositorio,RepositorioUrl,FechaCreacion")] Repositorio repositorio)
        {
            
            if (string.IsNullOrWhiteSpace(repositorio.RepositorioUrl))
            {
                ModelState.AddModelError("RepositorioUrl", "La URL del repositorio es obligatoria.");
            }
            else if (!Uri.IsWellFormedUriString(repositorio.RepositorioUrl, UriKind.Absolute))
            {
                ModelState.AddModelError("RepositorioUrl", "La URL del repositorio no tiene un formato válido.");
            }
            else if (!repositorio.RepositorioUrl.Contains("github.com", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("RepositorioUrl", "La URL debe ser de GitHub (ej. https://github.com/usuario/repo).");
            }

            // Si hay errores de validación inicial, no continuamos con la llamada a la API de GitHub.
            if (!ModelState.IsValid)
            {
                ViewData["ProyectoId"] = new SelectList(_context.Proyectos, "ProyectoId", "NombreProyecto", repositorio.ProyectoId);
                return View(repositorio);
            }

            // 2. Inicializar cliente de GitHub.
            var githubClient = new GitHubClient(new ProductHeaderValue("MunDevApp")/*, new Credentials(_githubToken)*/);
            Octokit.Repository? githubRepoInfo = null;

            try
            {
                var uri = new Uri(repositorio.RepositorioUrl);
                var segments = uri.AbsolutePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

                if (segments.Length >= 2)
                {
                    string owner = segments[0];
                    string repoName = segments[1];

                    githubRepoInfo = await githubClient.Repository.Get(owner, repoName);
                    string githubRepoName = githubRepoInfo.Name;

                    if (string.IsNullOrWhiteSpace(repositorio.NombreRepositorio))
                    {
                        repositorio.NombreRepositorio = githubRepoName;
                    }
                    else if (!repositorio.NombreRepositorio.Equals(githubRepoName, StringComparison.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError("NombreRepositorio", $"El nombre del repositorio en GitHub es '{githubRepoName}'. Por favor, corríjalo.");
                    }
                }
                else
                {
                    ModelState.AddModelError("RepositorioUrl", "URL de GitHub mal formada (debe ser https://github.com/usuario/repositorio).");
                }
            }
            catch (NotFoundException)
            {
                ModelState.AddModelError("RepositorioUrl", "El repositorio no fue encontrado en GitHub. Verifique la URL.");
            }
            catch (ApiException ex)
            {
                ModelState.AddModelError("RepositorioUrl", $"Error al contactar GitHub: {ex.Message}. Intente más tarde.");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("RepositorioUrl", "Ocurrió un error inesperado al procesar la URL del repositorio.");
            }


            if (ModelState.IsValid)
            {
                repositorio.FechaCreacion = DateTime.Now;
                _context.Add(repositorio);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["ProyectoId"] = new SelectList(_context.Proyectos, "ProyectoId", "NombreProyecto", repositorio.ProyectoId);
            return View(repositorio);
        }

        // GET: Repositorios/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var repositorio = await _context.Repositorios.FindAsync(id);
            if (repositorio == null)
            {
                return NotFound();
            }
            ViewData["ProyectoId"] = new SelectList(_context.Proyectos, "ProyectoId", "NombreProyecto", repositorio.ProyectoId);
            return View(repositorio);
        }

        // POST: Repositorios/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("RepositorioId,ProyectoId,NombreRepositorio,RepositorioUrl,FechaCreacion")] Repositorio repositorio)
        {
            if (id != repositorio.RepositorioId)
            {
                return NotFound();
            }


            if (string.IsNullOrWhiteSpace(repositorio.RepositorioUrl))
            {
                ModelState.AddModelError("RepositorioUrl", "La URL del repositorio es obligatoria.");
            }
            else if (!Uri.IsWellFormedUriString(repositorio.RepositorioUrl, UriKind.Absolute))
            {
                ModelState.AddModelError("RepositorioUrl", "La URL del repositorio no tiene un formato válido.");
            }
            else if (!repositorio.RepositorioUrl.Contains("github.com", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("RepositorioUrl", "La URL debe ser de GitHub (ej. https://github.com/usuario/repo).");
            }

            if (!ModelState.IsValid)
            {
                ViewData["ProyectoId"] = new SelectList(_context.Proyectos, "ProyectoId", "NombreProyecto", repositorio.ProyectoId);
                return View(repositorio);
            }

            var githubClient = new GitHubClient(new ProductHeaderValue("MunDevApp")/*, new Credentials(_githubToken)*/);
            Octokit.Repository? githubRepoInfo = null;

            try
            {
                var uri = new Uri(repositorio.RepositorioUrl);
                var segments = uri.AbsolutePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

                if (segments.Length >= 2)
                {
                    string owner = segments[0];
                    string repoName = segments[1];

                    githubRepoInfo = await githubClient.Repository.Get(owner, repoName);
                    string githubRepoName = githubRepoInfo.Name;

                    if (string.IsNullOrWhiteSpace(repositorio.NombreRepositorio))
                    {
                        repositorio.NombreRepositorio = githubRepoName;
                    }
                    else if (!repositorio.NombreRepositorio.Equals(githubRepoName, StringComparison.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError("NombreRepositorio", $"El nombre del repositorio en GitHub es '{githubRepoName}'. Por favor, corríjalo.");
                    }
                }
                else
                {
                    ModelState.AddModelError("RepositorioUrl", "URL de GitHub mal formada (debe ser https://github.com/usuario/repositorio).");
                }
            }
            catch (NotFoundException)
            {
                ModelState.AddModelError("RepositorioUrl", "El repositorio no fue encontrado en GitHub. Verifique la URL.");
            }
            catch (ApiException ex)
            {
                ModelState.AddModelError("RepositorioUrl", $"Error al contactar GitHub: {ex.Message}. Intente más tarde.");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("RepositorioUrl", "Ocurrió un error inesperado al procesar la URL del repositorio.");
            }


            if (ModelState.IsValid)
            {
                try
                {
                    var existingRepo = await _context.Repositorios.AsNoTracking().FirstOrDefaultAsync(r => r.RepositorioId == id);
                    if (existingRepo != null)
                    {
                        repositorio.FechaCreacion = existingRepo.FechaCreacion;
                    }
                    else
                    {
                        repositorio.FechaCreacion = DateTime.Now;
                    }

                    _context.Update(repositorio);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RepositorioExists(repositorio.RepositorioId))
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
            ViewData["ProyectoId"] = new SelectList(_context.Proyectos, "ProyectoId", "NombreProyecto", repositorio.ProyectoId);
            return View(repositorio);
        }

        // GET: Repositorios/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var repositorio = await _context.Repositorios
                .Include(r => r.Proyecto)
                .FirstOrDefaultAsync(m => m.RepositorioId == id);
            if (repositorio == null)
            {
                return NotFound();
            }

            return View(repositorio);
        }

        // POST: Repositorios/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var repositorio = await _context.Repositorios.FindAsync(id);
            if (repositorio != null)
            {
                _context.Repositorios.Remove(repositorio);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool RepositorioExists(int id)
        {
            return _context.Repositorios.Any(e => e.RepositorioId == id);
        }
    }
}
