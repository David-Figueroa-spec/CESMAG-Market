using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Tiendavirtual_Figueroa.Data;
using Tiendavirtual_Figueroa.Models;

namespace Tiendavirtual_Figueroa.Controllers
{
    // CORRECCIÓN: controlador nuevo requerido para gestionar publicaciones del marketplace
    public class AnuncioController : Controller
    {
        private readonly TiendaContext _context;

        public AnuncioController(TiendaContext context)
        {
            _context = context;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (HttpContext.Session.GetString("Usuario") == null)
            {
                context.Result = RedirectToAction("Index", "Login");
            }
            base.OnActionExecuting(context);
        }

        // Historial de publicaciones del estudiante autenticado
        public IActionResult MisAnuncios()
        {
            var usuarioIdStr = HttpContext.Session.GetString("UsuarioId");
            if (string.IsNullOrEmpty(usuarioIdStr))
                return RedirectToAction("Index", "Login");

            int usuarioId = int.Parse(usuarioIdStr);

            var anuncios = _context.Anuncios
                .Include(a => a.Producto)
                    .ThenInclude(p => p.Categoria)
                .Where(a => a.VendedorId == usuarioId)
                .OrderByDescending(a => a.FechaPublicacion)
                .ToList();

            return View(anuncios);
        }

        // Admin: ver todos los anuncios del sistema
        public IActionResult Index()
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "admin")
            {
                TempData["Error"] = "Acceso restringido a administradores.";
                return RedirectToAction("Index", "Home");
            }

            var anuncios = _context.Anuncios
                .Include(a => a.Producto)
                .Include(a => a.Vendedor)
                .OrderByDescending(a => a.FechaPublicacion)
                .ToList();

            return View(anuncios);
        }

        // Cambiar estado del anuncio (y del producto asociado) entre Disponible/Vendido
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CambiarEstado(int id)
        {
            var anuncio = _context.Anuncios
                .Include(a => a.Producto)
                .FirstOrDefault(a => a.Id == id);

            if (anuncio == null) return NotFound();

            // Solo el vendedor o el admin pueden cambiar el estado
            var usuarioIdStr = HttpContext.Session.GetString("UsuarioId");
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "admin" && (string.IsNullOrEmpty(usuarioIdStr) || anuncio.VendedorId != int.Parse(usuarioIdStr)))
            {
                TempData["Error"] = "No tienes permiso para modificar este anuncio.";
                return RedirectToAction("MisAnuncios");
            }

            string nuevoEstado = anuncio.Estado == "Disponible" ? "Vendido" : "Disponible";
            anuncio.Estado = nuevoEstado;
            anuncio.Producto.Estado = nuevoEstado; // sincronizar con el producto

            _context.SaveChanges();
            TempData["Exito"] = $"Anuncio marcado como \"{nuevoEstado}\".";
            return RedirectToAction("MisAnuncios");
        }

        // Admin: eliminar un anuncio inapropiado (moderación)
        public IActionResult Delete(int id)
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "admin")
            {
                TempData["Error"] = "Acceso restringido a administradores.";
                return RedirectToAction("Index", "Home");
            }

            var anuncio = _context.Anuncios.Find(id);
            if (anuncio != null)
            {
                _context.Anuncios.Remove(anuncio);
                _context.SaveChanges();
                TempData["Exito"] = "Anuncio eliminado correctamente.";
            }

            return RedirectToAction("Index");
        }
    }
}
