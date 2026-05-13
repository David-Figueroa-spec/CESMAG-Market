using Microsoft.AspNetCore.Mvc;
using Tiendavirtual_Figueroa.Data;
using Tiendavirtual_Figueroa.Helpers;
using Tiendavirtual_Figueroa.Models;

namespace Tiendavirtual_Figueroa.Controllers
{
    public class LoginController : Controller
    {
        private readonly TiendaContext _context;
        private readonly IConfiguration _configuration;

        public LoginController(TiendaContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // CORRECCIÓN: faltaba ValidateAntiForgeryToken en el POST
        public IActionResult Index(string correo, string clave)
        {
            // --- SUPERUSUARIO (definido en appsettings.json, no en BD) ---
            var superCorreo = _configuration["SuperUsuario:Correo"];
            var superClave  = _configuration["SuperUsuario:Clave"];
            var superNombre = _configuration["SuperUsuario:Nombre"];
            var superRol    = _configuration["SuperUsuario:Rol"];

            if (correo == superCorreo && clave == superClave)
            {
                HttpContext.Session.SetString("Usuario",   superNombre!);
                HttpContext.Session.SetString("Rol",       superRol!);
                // CORRECCIÓN: UsuarioId 0 identifica al superusuario en sesión
                HttpContext.Session.SetString("UsuarioId", "0");
                return RedirectToAction("Index", "Home");
            }

            // --- USUARIOS NORMALES (desde BD, clave hasheada con SHA256) ---
            var claveHash = HashHelper.ObtenerHash(clave);
            var usuario = _context.Usuarios
                .FirstOrDefault(u => u.Correo == correo && u.Clave == claveHash);

            if (usuario != null)
            {
                // CORRECCIÓN: bloquear usuarios no validados por el admin
                if (!usuario.Validado)
                {
                    ViewBag.Error = "Tu cuenta aún no ha sido validada por un administrador.";
                    return View();
                }

                HttpContext.Session.SetString("Usuario",   usuario.Nombre);
                HttpContext.Session.SetString("Rol",       usuario.Rol);
                // CORRECCIÓN: guardar el ID del usuario en sesión
                // Era imposible saber quién era el vendedor al publicar un producto sin esto
                HttpContext.Session.SetString("UsuarioId", usuario.Id.ToString());
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Credenciales incorrectas.";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }
    }
}
