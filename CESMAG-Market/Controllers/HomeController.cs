using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tiendavirtual_Figueroa.Data;
using Tiendavirtual_Figueroa.Models;

namespace Tiendavirtual_Figueroa.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly TiendaContext _context;

        // CORRECCIÓN: se inyecta TiendaContext para mostrar productos recientes en home
        public HomeController(ILogger<HomeController> logger, TiendaContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("Usuario") == null)
                return RedirectToAction("Index", "Login");

            // CORRECCIÓN: la página de inicio muestra los 8 productos disponibles más recientes
            // Antes devolvía una vista vacía sin datos útiles para el marketplace
            var productosRecientes = _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.Usuario)
                .Where(p => p.Estado == "Disponible")
                .OrderByDescending(p => p.Id)
                .Take(8)
                .ToList();

            ViewBag.TotalDisponibles = _context.Productos.Count(p => p.Estado == "Disponible");
            ViewBag.TotalVendidos    = _context.Productos.Count(p => p.Estado == "Vendido");

            return View(productosRecientes);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
