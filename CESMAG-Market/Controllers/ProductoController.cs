using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Tiendavirtual_Figueroa.Data;
using Tiendavirtual_Figueroa.Models;

namespace Tiendavirtual_Figueroa.Controllers
{
    public class ProductoController : Controller
    {
        private readonly TiendaContext _context;

        public ProductoController(TiendaContext context)
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

        // CORRECCIÓN: Index ahora acepta filtros por búsqueda, categoría y estado
        // En un P2P el listado principal es el catálogo del marketplace, no el inventario
        public IActionResult Index(string? busqueda, int? categoriaId, string? estado, string? facultad)
        {
            var query = _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.Usuario)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(busqueda))
                query = query.Where(p => p.Nombre.Contains(busqueda) || p.Descripcion.Contains(busqueda));

            if (categoriaId.HasValue)
                query = query.Where(p => p.CategoriaId == categoriaId);

            if (!string.IsNullOrWhiteSpace(estado))
                query = query.Where(p => p.Estado == estado);

            // CORRECCIÓN: filtro por facultad del vendedor (clave del marketplace académico)
            if (!string.IsNullOrWhiteSpace(facultad))
                query = query.Where(p => p.Usuario.Facultad == facultad);

            // Pasar datos para los dropdowns de filtro en la vista
            ViewBag.Categorias = _context.Categorias.ToList();
            ViewBag.BusquedaActual = busqueda;
            ViewBag.CategoriaActual = categoriaId;
            ViewBag.EstadoActual = estado;
            ViewBag.FacultadActual = facultad;

            return View(query.ToList());
        }

        public IActionResult Create()
        {
            ViewBag.Categorias = _context.Categorias.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Producto producto, IFormFile? imagen)
        {
            var usuarioIdStr = HttpContext.Session.GetString("UsuarioId");

            // CORRECCIÓN: el superadmin (Id=0) no tiene registro en la BD
            // por lo tanto no puede ser vendedor en el marketplace
            if (string.IsNullOrEmpty(usuarioIdStr) || usuarioIdStr == "0")
            {
                TempData["Error"] = "El administrador no puede publicar productos. " +
                                    "Inicia sesión con una cuenta de estudiante.";
                return RedirectToAction("Index");
            }

            producto.UsuarioId = int.Parse(usuarioIdStr);
            producto.Estado = "Disponible"; // todo producto nuevo empieza como Disponible

            if (imagen != null)
            {
                var carpeta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                if (!Directory.Exists(carpeta))
                    Directory.CreateDirectory(carpeta);

                var nombreArchivo = Guid.NewGuid().ToString() + Path.GetExtension(imagen.FileName);
                var ruta = Path.Combine(carpeta, nombreArchivo);
                using (var stream = new FileStream(ruta, FileMode.Create))
                {
                    imagen.CopyTo(stream);
                }
                producto.ImagenUrl = "/images/" + nombreArchivo;
            }

            // CORRECCIÓN: se excluye UsuarioId y Usuario del ModelState porque ya se asignaron manualmente
            ModelState.Remove("Usuario");
            ModelState.Remove("Categoria");

            if (ModelState.IsValid)
            {
                _context.Productos.Add(producto);
                _context.SaveChanges();

                // CORRECCIÓN: crear el Anuncio vinculado al producto recién publicado
                var anuncio = new Anuncio
                {
                    ProductoId = producto.Id,
                    VendedorId = producto.UsuarioId,
                    FechaPublicacion = DateTime.Now,
                    Estado = "Disponible"
                };
                _context.Anuncios.Add(anuncio);
                _context.SaveChanges();

                TempData["Exito"] = $"Producto \"{producto.Nombre}\" publicado correctamente.";
                return RedirectToAction("Index");
            }

            ViewBag.Categorias = _context.Categorias.ToList();
            return View(producto);
        }

        public IActionResult Edit(int id)
        {
            var producto = _context.Productos.Find(id);
            if (producto == null) return NotFound();

            // CORRECCIÓN: solo el vendedor dueño o el admin puede editar el producto
            var usuarioIdStr = HttpContext.Session.GetString("UsuarioId");
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "admin" && (string.IsNullOrEmpty(usuarioIdStr) || producto.UsuarioId != int.Parse(usuarioIdStr)))
            {
                TempData["Error"] = "No tienes permiso para editar este producto.";
                return RedirectToAction("Index");
            }

            ViewBag.Categorias = _context.Categorias.ToList();
            return View(producto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Producto producto, IFormFile? imagen)
        {
            var productoBD = _context.Productos.Find(producto.Id);
            if (productoBD == null) return NotFound();

            // CORRECCIÓN: verificar propiedad antes de editar
            var usuarioIdStr = HttpContext.Session.GetString("UsuarioId");
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "admin" && (string.IsNullOrEmpty(usuarioIdStr) || productoBD.UsuarioId != int.Parse(usuarioIdStr)))
            {
                TempData["Error"] = "No tienes permiso para editar este producto.";
                return RedirectToAction("Index");
            }

            productoBD.Nombre = producto.Nombre;
            productoBD.Descripcion = producto.Descripcion;
            productoBD.Precio = producto.Precio;
            productoBD.CategoriaId = producto.CategoriaId;
            // CORRECCIÓN: Estado también es editable (el vendedor puede marcar como Vendido)
            productoBD.Estado = producto.Estado;

            if (imagen != null)
            {
                var carpeta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                if (!Directory.Exists(carpeta))
                    Directory.CreateDirectory(carpeta);

                var nombreArchivo = Guid.NewGuid().ToString() + Path.GetExtension(imagen.FileName);
                var ruta = Path.Combine(carpeta, nombreArchivo);
                using (var stream = new FileStream(ruta, FileMode.Create))
                {
                    imagen.CopyTo(stream);
                }
                productoBD.ImagenUrl = "/images/" + nombreArchivo;
            }

            _context.SaveChanges();
            TempData["Exito"] = $"Producto \"{productoBD.Nombre}\" actualizado correctamente.";
            return RedirectToAction("Index");
        }

        public IActionResult Delete(int id)
        {
            var producto = _context.Productos.Find(id);
            if (producto == null) return NotFound();

            // CORRECCIÓN: puede eliminar el admin O el propio vendedor dueño del producto
            var usuarioIdStr = HttpContext.Session.GetString("UsuarioId");
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "admin" && (string.IsNullOrEmpty(usuarioIdStr) || producto.UsuarioId != int.Parse(usuarioIdStr)))
            {
                TempData["Error"] = "No tienes permiso para eliminar este producto.";
                return RedirectToAction("Index");
            }

            _context.Productos.Remove(producto);
            _context.SaveChanges();
            TempData["Exito"] = $"Producto \"{producto.Nombre}\" eliminado correctamente.";
            return RedirectToAction("Index");
        }

        // CORRECCIÓN: acción nueva para cambiar estado Disponible/Vendido sin abrir el formulario Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CambiarEstado(int id)
        {
            var producto = _context.Productos.Find(id);
            if (producto == null) return NotFound();

            var usuarioIdStr = HttpContext.Session.GetString("UsuarioId");
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "admin" && (string.IsNullOrEmpty(usuarioIdStr) || producto.UsuarioId != int.Parse(usuarioIdStr)))
            {
                TempData["Error"] = "No tienes permiso para cambiar el estado de este producto.";
                return RedirectToAction("Index");
            }

            producto.Estado = producto.Estado == "Disponible" ? "Vendido" : "Disponible";

            // Sincronizar estado del Anuncio asociado
            var anuncio = _context.Anuncios.FirstOrDefault(a => a.ProductoId == id);
            if (anuncio != null)
                anuncio.Estado = producto.Estado;

            _context.SaveChanges();
            TempData["Exito"] = $"Estado del producto actualizado a \"{producto.Estado}\".";
            return RedirectToAction("Index");
        }

        // CORRECCIÓN: eliminadas las acciones AgregarCarrito, Carrito y Comprar
        // En un marketplace P2P el contacto es directo (WhatsApp/correo del vendedor)
        // No hay flujo de compra dentro de la plataforma
    }
}
