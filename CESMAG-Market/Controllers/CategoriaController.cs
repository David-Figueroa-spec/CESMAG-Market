using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Tiendavirtual_Figueroa.Data;
using Tiendavirtual_Figueroa.Models;

namespace Tiendavirtual_Figueroa.Controllers
{
    public class CategoriaController : Controller
    {
        private readonly TiendaContext _context;

        public CategoriaController(TiendaContext context)
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

        // Index es visible para todos (estudiantes necesitan ver las categorías al filtrar)
        public IActionResult Index()
        {
            var categorias = _context.Categorias.ToList();
            return View(categorias);
        }

        // CORRECCIÓN: Create/Edit/Delete restringidos al admin
        // Antes cualquier usuario autenticado podía crear, editar y eliminar categorías
        public IActionResult Create()
        {
            if (HttpContext.Session.GetString("Rol") != "admin")
            {
                TempData["Error"] = "Solo el administrador puede gestionar categorías.";
                return RedirectToAction("Index");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Categoria categoria)
        {
            if (HttpContext.Session.GetString("Rol") != "admin")
                return RedirectToAction("Index");

            if (ModelState.IsValid)
            {
                _context.Categorias.Add(categoria);
                _context.SaveChanges();
                TempData["Exito"] = $"Categoría \"{categoria.Nombre}\" creada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            return View(categoria);
        }

        public IActionResult Edit(int id)
        {
            if (HttpContext.Session.GetString("Rol") != "admin")
            {
                TempData["Error"] = "Solo el administrador puede gestionar categorías.";
                return RedirectToAction("Index");
            }

            var categoria = _context.Categorias.Find(id);
            if (categoria == null) return NotFound();
            return View(categoria);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Categoria categoria)
        {
            if (HttpContext.Session.GetString("Rol") != "admin")
                return RedirectToAction("Index");

            if (ModelState.IsValid)
            {
                _context.Categorias.Update(categoria);
                _context.SaveChanges();
                TempData["Exito"] = $"Categoría \"{categoria.Nombre}\" actualizada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            return View(categoria);
        }

        // CORRECCIÓN: faltaba completamente la verificación de rol admin en Delete
        // Antes cualquier usuario autenticado podía eliminar categorías con solo conocer la URL
        public IActionResult Delete(int id)
        {
            if (HttpContext.Session.GetString("Rol") != "admin")
            {
                TempData["Error"] = "Solo el administrador puede eliminar categorías.";
                return RedirectToAction("Index");
            }

            var categoria = _context.Categorias.Find(id);
            if (categoria != null)
            {
                // CORRECCIÓN: verificar si hay productos usando esta categoría antes de eliminar
                bool tieneProductos = _context.Productos.Any(p => p.CategoriaId == id);
                if (tieneProductos)
                {
                    TempData["Error"] = "No se puede eliminar la categoría porque tiene productos asociados.";
                    return RedirectToAction("Index");
                }

                _context.Categorias.Remove(categoria);
                _context.SaveChanges();
                TempData["Exito"] = $"Categoría \"{categoria.Nombre}\" eliminada correctamente.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
