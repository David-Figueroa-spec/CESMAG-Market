using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Tiendavirtual_Figueroa.Data;
using Tiendavirtual_Figueroa.Helpers;
using Tiendavirtual_Figueroa.Models;

namespace Tiendavirtual_Figueroa.Controllers
{
    public class UsuarioController : Controller
    {
        private readonly TiendaContext _context;

        // Dominio institucional permitido para registro
        private const string DominioInstitucional = "@unicesmag.edu.co";

        public UsuarioController(TiendaContext context)
        {
            _context = context;
        }

        // CORRECCIÓN: OnActionExecuting protege solo las acciones de gestión (admin)
        // El registro (Register) debe ser público para que nuevos estudiantes puedan inscribirse
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Permitir acceso sin sesión SOLO a Register
            var actionName = context.ActionDescriptor.RouteValues["action"];
            if (actionName == "Register" || actionName == "RegisterConfirm")
            {
                base.OnActionExecuting(context);
                return;
            }

            if (HttpContext.Session.GetString("Usuario") == null)
            {
                context.Result = RedirectToAction("Index", "Login");
            }
            base.OnActionExecuting(context);
        }

        // CORRECCIÓN: acción nueva de auto-registro para estudiantes
        // Antes solo había Create (accesible solo con sesión activa = imposible registrarse)
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(Usuario usuario)
        {
            // CORRECCIÓN: validar dominio institucional
            if (!usuario.Correo.EndsWith(DominioInstitucional, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("Correo", $"Debes usar tu correo institucional ({DominioInstitucional})");
            }

            // CORRECCIÓN: verificar que el correo no esté ya registrado
            if (_context.Usuarios.Any(u => u.Correo == usuario.Correo))
            {
                ModelState.AddModelError("Correo", "Este correo ya está registrado.");
            }

            if (ModelState.IsValid)
            {
                usuario.Rol = "estudiante";      // los auto-registros siempre son estudiantes
                usuario.Validado = false;         // requiere validación del admin
                usuario.Calificacion = 0.0;
                usuario.Clave = HashHelper.ObtenerHash(usuario.Clave);

                _context.Usuarios.Add(usuario);
                _context.SaveChanges();

                TempData["Exito"] = "Registro exitoso. Un administrador validará tu cuenta pronto.";
                return RedirectToAction("Index", "Login");
            }

            return View(usuario);
        }

        // --- Acciones de gestión (requieren sesión activa) ---

        public IActionResult Index()
        {
            // CORRECCIÓN: solo el admin puede ver la lista de todos los usuarios
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "admin")
            {
                TempData["Error"] = "Acceso restringido a administradores.";
                return RedirectToAction("Index", "Home");
            }

            var usuarios = _context.Usuarios.ToList();
            return View(usuarios);
        }

        // Crear usuario desde el panel admin (puede asignar cualquier rol)
        public IActionResult Create()
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "admin")
                return RedirectToAction("Index", "Home");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Usuario usuario)
        {
            if (ModelState.IsValid)
            {
                usuario.Clave = HashHelper.ObtenerHash(usuario.Clave);
                _context.Usuarios.Add(usuario);
                _context.SaveChanges();
                TempData["Exito"] = $"Usuario \"{usuario.Nombre}\" creado correctamente.";
                return RedirectToAction("Index");
            }
            return View(usuario);
        }

        public IActionResult Edit(int id)
        {
            var usuario = _context.Usuarios.Find(id);
            if (usuario == null) return NotFound();

            // Solo el admin o el propio usuario pueden editar
            var usuarioIdStr = HttpContext.Session.GetString("UsuarioId");
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "admin" && (string.IsNullOrEmpty(usuarioIdStr) || usuario.Id != int.Parse(usuarioIdStr)))
            {
                TempData["Error"] = "No tienes permiso para editar este perfil.";
                return RedirectToAction("Index");
            }

            usuario.Clave = string.Empty; // no mostrar el hash en el formulario
            return View(usuario);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Usuario usuario)
        {
            // CORRECCIÓN: validar dominio institucional también al editar
            if (!usuario.Correo.EndsWith(DominioInstitucional, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("Correo", $"Debes usar tu correo institucional ({DominioInstitucional})");
            }

            if (string.IsNullOrEmpty(usuario.Clave))
            {
                ModelState.Remove("Clave");
                var claveActual = _context.Usuarios
                    .AsNoTracking()
                    .Where(u => u.Id == usuario.Id)
                    .Select(u => u.Clave)
                    .FirstOrDefault();
                usuario.Clave = claveActual ?? string.Empty;
            }
            else
            {
                usuario.Clave = HashHelper.ObtenerHash(usuario.Clave);
            }

            if (ModelState.IsValid)
            {
                _context.Usuarios.Update(usuario);
                _context.SaveChanges();
                TempData["Exito"] = "Perfil actualizado correctamente.";
                return RedirectToAction("Index");
            }
            return View(usuario);
        }

        // CORRECCIÓN: acción nueva para que el admin valide/invalide un usuario institucional
        public IActionResult ToggleValidado(int id)
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "admin")
                return RedirectToAction("Index", "Home");

            var usuario = _context.Usuarios.Find(id);
            if (usuario != null)
            {
                usuario.Validado = !usuario.Validado;
                _context.SaveChanges();
                TempData["Exito"] = usuario.Validado
                    ? $"Usuario \"{usuario.Nombre}\" validado correctamente."
                    : $"Usuario \"{usuario.Nombre}\" marcado como no validado.";
            }
            return RedirectToAction("Index");
        }

        public IActionResult Delete(int id)
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "admin")
            {
                TempData["Error"] = "Solo el administrador puede eliminar usuarios.";
                return RedirectToAction("Index");
            }

            var usuario = _context.Usuarios.Find(id);
            if (usuario != null)
            {
                _context.Usuarios.Remove(usuario);
                _context.SaveChanges();
                TempData["Exito"] = $"Usuario \"{usuario.Nombre}\" eliminado correctamente.";
            }
            return RedirectToAction("Index");
        }
    }
}
