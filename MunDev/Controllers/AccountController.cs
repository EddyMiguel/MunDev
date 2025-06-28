using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.Cookies;
using MunDev.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MunDev.Models;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic; 
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

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);

            if (usuario == null || !BCrypt.Net.BCrypt.Verify(password, usuario.ContrasenaHash)) 
            {
                ModelState.AddModelError(string.Empty, "Credenciales inválidas.");
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.UsuarioId.ToString()),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Name, usuario.NombreUsuario)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true, 
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30) 
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
          
            name = string.IsNullOrEmpty(name) ? email?.Split('@')[0] : name;

            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError(string.Empty, "No se pudo obtener el email del proveedor externo.");
                
                return View(nameof(Login));
            }

            var usuarioExistente = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);

            if (usuarioExistente == null)
            {
               
                var baseUserName = name; 
                int counter = 0;
                string finalUserName = baseUserName;

               
                while (await _context.Usuarios.AnyAsync(u => u.NombreUsuario == finalUserName))
                {
                    counter++;
                    finalUserName = $"{baseUserName}{counter}";
                   
                }

                var nuevoUsuario = new Usuario
                {
                    Email = email,
                    NombreUsuario = finalUserName, 
                    ContrasenaHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()), // Contraseña hasheada aleatoria
                    Activo = true
                };

                _context.Usuarios.Add(nuevoUsuario);
                await _context.SaveChangesAsync();
                usuarioExistente = nuevoUsuario; // Asigna el nuevo usuario al objeto existente para el flujo de autenticación
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuarioExistente.UsuarioId.ToString()),
                new Claim(ClaimTypes.Email, usuarioExistente.Email),
                new Claim(ClaimTypes.Name, usuarioExistente.NombreUsuario)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = authenticateResult.Properties.IsPersistent,
                ExpiresUtc = authenticateResult.Properties.ExpiresUtc
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

            

            return LocalRedirect(returnUrl ?? "/");
        }

        [HttpPost]
        [ValidateAntiForgeryToken] 
        public async Task<IActionResult> Logout()
        {
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
