using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MunDev.Data;
using MunDev.Models;
using Octokit; // Importante: Agrega este using para usar la librería de GitHub
using Microsoft.Extensions.Configuration; // Necesario si usas IConfiguration para tokens

namespace MunDev.Controllers
{
    public class RepositoriosController : Controller
    {
        private readonly MunDevContext _context;
        // Opcional: Propiedad para almacenar el token de GitHub si se usa autenticación.
        // private readonly string? _githubToken;

        // Constructor: Inyecta el contexto de la base de datos y la configuración (si usas token).
        public RepositoriosController(MunDevContext context /*, IConfiguration configuration */)
        {
            _context = context;
            // Si necesitas un Personal Access Token (PAT) de GitHub para mayor límite de solicitudes
            // o para acceder a repositorios privados, puedes cargarlo desde appsettings.json.
            // Asegúrate de inyectar IConfiguration en el constructor:
            // _githubToken = configuration["GitHub:PersonalAccessToken"];
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

        // GET: Repositorios/Create (Muestra el formulario para crear un nuevo repositorio)
        public IActionResult Create()
        {
            // Carga los proyectos para el SelectList, mostrando el NombreProyecto.
            // Esto permite al usuario seleccionar un proyecto de una lista.
            ViewData["ProyectoId"] = new SelectList(_context.Proyectos, "ProyectoId", "NombreProyecto");
            return View();
        }

        // POST: Repositorios/Create (Procesa el envío del formulario de creación)
        [HttpPost]
        [ValidateAntiForgeryToken]
        // [Bind] especifica qué propiedades del modelo deben ser vinculadas desde el formulario
        // Asegúrate de que RepositorioId no esté aquí si es auto-incremental y no lo envías desde el formulario.
        public async Task<IActionResult> Create([Bind("RepositorioId,ProyectoId,NombreRepositorio,RepositorioUrl,FechaCreacion")] Repositorio repositorio)
        {
            // === INICIO DE LA LÓGICA DE INTEGRACIÓN Y VALIDACIÓN CON GITHUB ===

            // 1. Validaciones iniciales de la URL (obligatorio, formato URL, es de GitHub).
            // Estas validaciones se hacen antes de llamar a la API externa para optimizar.
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
                ModelState.AddModelError("RepositorioUrl", "La URL debe ser de GitHub (ej. [https://github.com/usuario/repo](https://github.com/usuario/repo)).");
            }

            // Si hay errores de validación inicial, no continuamos con la llamada a la API de GitHub.
            // Recargamos el SelectList y devolvemos la vista.
            if (!ModelState.IsValid)
            {
                ViewData["ProyectoId"] = new SelectList(_context.Proyectos, "ProyectoId", "NombreProyecto", repositorio.ProyectoId);
                return View(repositorio);
            }

            // 2. Inicializar cliente de GitHub.
            // Puedes usar this._githubToken si lo has configurado para autenticación.
            var githubClient = new GitHubClient(new ProductHeaderValue("MunDevApp")/*, new Credentials(_githubToken)*/);
            Octokit.Repository? githubRepoInfo = null;

            try
            {
                // Extraer el dueño (owner) y el nombre del repositorio (repoName) de la URL.
                // Ejemplo: de "[https://github.com/owner/repo-name](https://github.com/owner/repo-name)" a "owner" y "repo-name".
                var uri = new Uri(repositorio.RepositorioUrl);
                // Split AbsolutePath para manejar URLs con posible '/' al final.
                var segments = uri.AbsolutePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

                if (segments.Length >= 2)
                {
                    string owner = segments[0];
                    string repoName = segments[1];

                    // Obtener información del repositorio desde la API de GitHub.
                    githubRepoInfo = await githubClient.Repository.Get(owner, repoName);

                    // Opcional: Autocompletar o validar el NombreRepositorio con el nombre de GitHub.
                    if (string.IsNullOrWhiteSpace(repositorio.NombreRepositorio))
                    {
                        repositorio.NombreRepositorio = githubRepoInfo.Name;
                    }
                    else if (!repositorio.NombreRepositorio.Equals(githubRepoInfo.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError("NombreRepositorio", $"El nombre del repositorio en GitHub es '{githubRepoInfo.Name}'. Por favor, corríjalo.");
                    }

                    // Aquí podrías asignar otras propiedades del repositorio de GitHub a tu modelo si existen,
                    // por ejemplo: repositorio.Descripcion = githubRepoInfo.Description;
                    // repositorio.Estrellas = githubRepoInfo.StargazersCount;
                }
                else
                {
                    ModelState.AddModelError("RepositorioUrl", "URL de GitHub mal formada (debe ser [https://github.com/usuario/repositorio](https://github.com/usuario/repositorio)).");
                }
            }
            catch (NotFoundException) // Excepción si el repositorio no existe en GitHub.
            {
                ModelState.AddModelError("RepositorioUrl", "El repositorio no fue encontrado en GitHub. Verifique la URL.");
            }
            catch (ApiException ex) // Excepciones de la API de GitHub (ej. límite de solicitudes excedido).
            {
                ModelState.AddModelError("RepositorioUrl", $"Error al contactar GitHub: {ex.Message}. Intente más tarde.");
                // Es recomendable registrar este error en un sistema de logs real.
                // Console.WriteLine($"GitHub API Error: {ex}");
            }
            catch (Exception ex) // Otras excepciones generales al procesar la URL.
            {
                ModelState.AddModelError("RepositorioUrl", "Ocurrió un error inesperado al procesar la URL del repositorio.");
                // Es recomendable registrar este error.
                // Console.WriteLine($"URL Processing Error: {ex}");
            }

            // === FIN DE LA LÓGICA DE INTEGRACIÓN Y VALIDACIÓN CON GITHUB ===

            // Si el modelo sigue siendo válido después de todas las validaciones (incluidas las de GitHub).
            if (ModelState.IsValid)
            {
                // Asignar FechaCreacion a la fecha y hora actual, ya que no se envía desde el formulario.
                repositorio.FechaCreacion = DateTime.Now;

                _context.Add(repositorio); // Añade el nuevo repositorio al contexto de la base de datos.
                await _context.SaveChangesAsync(); // Guarda los cambios en la base de datos.
                return RedirectToAction(nameof(Index)); // Redirige a la vista Index.
            }

            // Si hay errores (después de cualquier validación), recarga los SelectList y devuelve la vista
            // para que el usuario pueda corregir los errores.
            ViewData["ProyectoId"] = new SelectList(_context.Proyectos, "ProyectoId", "NombreProyecto", repositorio.ProyectoId);
            return View(repositorio);
        }

        // GET: Repositorios/Edit/5 (Muestra el formulario para editar un repositorio existente)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Busca el repositorio por su ID.
            var repositorio = await _context.Repositorios.FindAsync(id);
            if (repositorio == null)
            {
                return NotFound();
            }
            // Carga los proyectos para el SelectList, mostrando el NombreProyecto, y preselecciona el proyecto actual.
            ViewData["ProyectoId"] = new SelectList(_context.Proyectos, "ProyectoId", "NombreProyecto", repositorio.ProyectoId);
            return View(repositorio);
        }

        // POST: Repositorios/Edit/5 (Procesa el envío del formulario de edición)
        [HttpPost]
        [ValidateAntiForgeryToken]
        // [Bind] especifica qué propiedades del modelo deben ser vinculadas desde el formulario
        public async Task<IActionResult> Edit(int id, [Bind("RepositorioId,ProyectoId,NombreRepositorio,RepositorioUrl,FechaCreacion")] Repositorio repositorio)
        {
            // Verifica que el ID de la ruta coincida con el ID del modelo enviado.
            if (id != repositorio.RepositorioId)
            {
                return NotFound();
            }

            // === INICIO DE LA LÓGICA DE INTEGRACIÓN Y VALIDACIÓN CON GITHUB (Similar al Create) ===

            // 1. Validaciones iniciales de la URL (obligatorio, formato URL, es de GitHub).
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
                ModelState.AddModelError("RepositorioUrl", "La URL debe ser de GitHub (ej. [https://github.com/usuario/repo](https://github.com/usuario/repo)).");
            }

            if (!ModelState.IsValid)
            {
                ViewData["ProyectoId"] = new SelectList(_context.Proyectos, "ProyectoId", "NombreProyecto", repositorio.ProyectoId);
                return View(repositorio);
            }

            // 2. Cliente de GitHub
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

                    if (string.IsNullOrWhiteSpace(repositorio.NombreRepositorio))
                    {
                        repositorio.NombreRepositorio = githubRepoInfo.Name;
                    }
                    else if (!repositorio.NombreRepositorio.Equals(githubRepoInfo.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError("NombreRepositorio", $"El nombre del repositorio en GitHub es '{githubRepoInfo.Name}'. Por favor, corríjalo.");
                    }
                }
                else
                {
                    ModelState.AddModelError("RepositorioUrl", "URL de GitHub mal formada (debe ser [https://github.com/usuario/repositorio](https://github.com/usuario/repositorio)).");
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

            // === FIN DE LA LÓGICA DE INTEGRACIÓN CON GITHUB ===

            // Si el modelo sigue siendo válido después de todas las validaciones.
            if (ModelState.IsValid)
            {
                try
                {
                    // Al editar, la FechaCreacion normalmente no se modifica.
                    // Para evitar que se sobrescriba con un valor nulo o incorrecto si no viene del formulario,
                    // recuperamos la entidad original sin seguimiento y le asignamos su FechaCreacion original.
                    var existingRepo = await _context.Repositorios.AsNoTracking().FirstOrDefaultAsync(r => r.RepositorioId == id);
                    if (existingRepo != null)
                    {
                        repositorio.FechaCreacion = existingRepo.FechaCreacion; // Preserva la fecha original.
                    }
                    else
                    {
                        // Si el registro original no se encuentra (caso inusual aquí), asigna la fecha actual.
                        repositorio.FechaCreacion = DateTime.Now;
                    }

                    _context.Update(repositorio); // Marca la entidad como modificada.
                    await _context.SaveChangesAsync(); // Guarda los cambios.
                }
                catch (DbUpdateConcurrencyException) // Manejo de errores de concurrencia.
                {
                    if (!RepositorioExists(repositorio.RepositorioId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw; // Lanza la excepción si es un problema diferente de concurrencia.
                    }
                }
                return RedirectToAction(nameof(Index)); // Redirige a la vista Index.
            }
            // Si hay errores, recarga los SelectList y devuelve la vista.
            ViewData["ProyectoId"] = new SelectList(_context.Proyectos, "ProyectoId", "NombreProyecto", repositorio.ProyectoId);
            return View(repositorio);
        }

        // GET: Repositorios/Delete/5 (Muestra la confirmación de eliminación)
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Busca el repositorio para eliminar, incluyendo el proyecto asociado.
            var repositorio = await _context.Repositorios
                .Include(r => r.Proyecto)
                .FirstOrDefaultAsync(m => m.RepositorioId == id);
            if (repositorio == null)
            {
                return NotFound();
            }

            return View(repositorio);
        }

        // POST: Repositorios/Delete/5 (Confirma y realiza la eliminación)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Busca el repositorio a eliminar.
            var repositorio = await _context.Repositorios.FindAsync(id);
            if (repositorio != null)
            {
                _context.Repositorios.Remove(repositorio); // Marca la entidad para eliminación.
            }

            await _context.SaveChangesAsync(); // Guarda los cambios en la base de datos.
            return RedirectToAction(nameof(Index)); // Redirige a la vista Index.
        }

        // Método auxiliar para verificar si un repositorio existe por su ID.
        private bool RepositorioExists(int id)
        {
            return _context.Repositorios.Any(e => e.RepositorioId == id);
        }
    }
}