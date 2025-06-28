using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.Cookies;
using MunDev.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MunDev.Models;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic; // Necesario para List<Claim>
using BCrypt.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.Google;

namespace MunDev.Controllers
{
    public class AccountController : Controller
    {
        private readonly MunDevContext _context;

        public AccountController(MunDevContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous] // Permitir acceso sin autenticación
        public IActionResult Login(string returnUrl = "/")
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // NUEVO/OPCIONAL: Método POST para el login local (si tienes usuarios con contraseña en tu DB)
        // Si solo usas login externo, puedes omitir este método POST.
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);

            if (usuario == null || !BCrypt.Net.BCrypt.Verify(password, usuario.ContrasenaHash)) // Verifica la contraseña hasheada
            {
                ModelState.AddModelError(string.Empty, "Credenciales inválidas.");
                return View();
            }

            // Si las credenciales son válidas, crea los claims y el principal
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.UsuarioId.ToString()),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Name, usuario.NombreUsuario)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true, // Puedes hacerlo configurable (recordar usuario)
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30) // Tiempo de expiración de la cookie
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            return LocalRedirect(returnUrl ?? "/");
        }


        [HttpPost]
        [AllowAnonymous] // Permitir acceso sin autenticación
        public IActionResult ExternalLogin(string provider, string returnUrl = null)
        {
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };

            // Aquí se genera el desafío para el proveedor externo (ej. Google).
            // ASP.NET Core maneja la redirección al proveedor.
            return Challenge(properties, provider);
        }

        [HttpGet]
        [AllowAnonymous] // Permitir acceso sin autenticación
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            if (remoteError != null)
            {
                ModelState.AddModelError(string.Empty, $"Error del proveedor externo: {remoteError}");
                return View(nameof(Login));
            }

            var authenticateResult = await HttpContext.AuthenticateAsync("Google"); 

            if (!authenticateResult.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Error al autenticarse con el proveedor externo (Google).");
                return View(nameof(Login));
            }

            // Obtener claims del principal autenticado por el proveedor externo
            var email = authenticateResult.Principal.FindFirst(ClaimTypes.Email)?.Value;
            var name = authenticateResult.Principal.FindFirst(ClaimTypes.Name)?.Value;
            // var externalId = authenticateResult.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value; // Este sería el ID del usuario en el proveedor externo

            // Si el nombre no viene del proveedor, intentar usar el email.
            name = string.IsNullOrEmpty(name) ? email?.Split('@')[0] : name;

            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError(string.Empty, "No se pudo obtener el email del proveedor externo.");
                // No es necesario SignOutAsync aquí. El proceso fallará y la cookie externa se limpia sola.
                return View(nameof(Login));
            }

            var usuarioExistente = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);

            if (usuarioExistente == null)
            {
                // El usuario no existe por email, es un nuevo registro.
                // Generar un NombreUsuario único si la columna tiene una restricción UNIQUE.
                var baseUserName = name; // Nombre proporcionado por el proveedor o derivado del email
                int counter = 0;
                string finalUserName = baseUserName;

                // Bucle para asegurar que el NombreUsuario sea único
                while (await _context.Usuarios.AnyAsync(u => u.NombreUsuario == finalUserName))
                {
                    counter++;
                    finalUserName = $"{baseUserName}{counter}"; // Añade un número para hacerlo único
                    // También podrías añadir un GUID para mayor unicidad:
                    // finalUserName = $"{baseUserName.Substring(0, Math.Min(baseUserName.Length, 20))}_{Guid.NewGuid().ToString().Substring(0, 8)}";
                }

                var nuevoUsuario = new Usuario
                {
                    Email = email,
                    NombreUsuario = finalUserName, // Usar el nombre de usuario final único
                    ContrasenaHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()), // Contraseña hasheada aleatoria
                    Activo = true
                };

                _context.Usuarios.Add(nuevoUsuario);
                await _context.SaveChangesAsync();
                usuarioExistente = nuevoUsuario; // Asigna el nuevo usuario al objeto existente para el flujo de autenticación
            }

            // Una vez que tenemos el usuario de nuestra DB, lo autenticamos con nuestro esquema de cookies.
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuarioExistente.UsuarioId.ToString()),
                new Claim(ClaimTypes.Email, usuarioExistente.Email),
                new Claim(ClaimTypes.Name, usuarioExistente.NombreUsuario)
                // Puedes añadir más claims aquí, como roles, etc.
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                // Mantiene la persistencia y expiración del login del proveedor externo si es que las hay.
                // Si authenticateResult.Properties no tiene IsPersistent, usar false por defecto.
                IsPersistent = authenticateResult.Properties.IsPersistent,
                ExpiresUtc = authenticateResult.Properties.ExpiresUtc
            };

            // Realiza el SignIn con tu esquema de autenticación de cookies, que es la sesión principal de tu app.
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

            // === CORRECCIÓN CRÍTICA: ELIMINAR ESTA LÍNEA ===
            // await HttpContext.SignOutAsync(authenticateResult.Properties.Items["LoginProvider"] ?? CookieAuthenticationDefaults.AuthenticationScheme);
            // Esta línea causa el error porque intenta cerrar sesión de un esquema (Google) que no lo soporta.
            // La autenticación externa es transitoria y no necesita un SignOut explícito aquí.

            return LocalRedirect(returnUrl ?? "/");
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // Buena práctica para POSTs
        public async Task<IActionResult> Logout()
        {
            // Solo cierra sesión del esquema de cookies, que es tu sesión local principal.
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [AllowAnonymous] // Permitir acceso sin autenticación
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
