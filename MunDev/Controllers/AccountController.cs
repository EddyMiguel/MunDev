using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.Cookies;
using MunDev.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MunDev.Models;

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
        public IActionResult Login(string returnUrl = "/")
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public IActionResult ExternalLogin(string provider, string returnUrl = null)
        {
            var redirecrUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new {returnUrl});
            var properties = new AuthenticationProperties { RedirectUri = redirecrUrl };

            return Challenge(properties, provider);
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            if (remoteError != null)
            {
                ModelState.AddModelError(string.Empty, $"Error del proveedor externo: {remoteError}");
                return View(nameof(Login));
            }

            var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!authenticateResult.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Error al autenticarse con el porveedor externo,");
                return View(nameof(Login));
            }

            var email = authenticateResult.Principal.FindFirst(ClaimTypes.Email)?.Value;
            var name = authenticateResult.Principal.FindFirst(ClaimTypes.Name)?.Value;
            var externalId = authenticateResult.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError(string.Empty, "No se pudo obtener el email.");
                return View(nameof(Login));
            }

            var usuarioExistente = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);

            if (usuarioExistente == null)
            {
                var nuevoUsuario = new Usuario
                {
                    Email = email,
                    NombreUsuario = name ?? email.Split('@')[0]
                };

                _context.Usuarios.Add(nuevoUsuario);
                await _context.SaveChangesAsync();
                usuarioExistente = nuevoUsuario;
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
        public async Task<IActionResult> Logout()
            { await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme); 
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
