using System.Security.Cryptography;
using System.Text;

namespace Tiendavirtual_Figueroa.Helpers
{
    // CORRECCIÓN: este archivo no existía en el proyecto pero era referenciado
    // por LoginController y UsuarioController → error de compilación
    public static class HashHelper
    {
        public static string ObtenerHash(string texto)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(texto);
            var hash  = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hash).ToLower();
        }
    }
}
