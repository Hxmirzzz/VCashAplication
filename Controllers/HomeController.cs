using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VCashApp.Models;
using Microsoft.AspNetCore.Authorization; 
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using Serilog;
using Serilog.Context;

namespace VCashApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ILogger<HomeController> logger,
                              SignInManager<ApplicationUser> signInManager,
                              UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _signInManager = signInManager;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost] // Será llamada por AJAX (POST)
        [Authorize] // Solo usuarios autenticados pueden extender su sesión
        [ValidateAntiForgeryToken] // Protección CSRF
        public async Task<IActionResult> ExtendSession()
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "Desconocida";
            using (LogContext.PushProperty("IpAddress", ipAddress))
            {
                var currentUser = await _userManager.GetUserAsync(User); // Obtener el usuario actual
                if (currentUser == null)
                {
                    Log.Warning("| Usuario: No autenticado | Ip: {Ip} | Acción: Extender Sesión | Resultado: Usuario no encontrado o no autenticado |", ipAddress);
                    return Json(new { success = false, message = "No se pudo identificar al usuario." });
                }

                try
                {
                    // Re-iniciar sesión al usuario. Esto emitirá una nueva cookie de autenticación
                    // con la SlidingExpiration reiniciada.
                    await _signInManager.SignInAsync(currentUser, isPersistent: true); // isPersistent debe ser true si la cookie debe recordar al usuario
                                                                                       // Esto depende de cómo manejas "Recordarme".
                                                                                       // Para sliding expiration, isPersistent: true es necesario.

                    Log.Information("| Usuario: {User} | Ip: {Ip} | Acción: Extender Sesión | Resultado: Sesión extendida exitosamente |", currentUser.UserName, ipAddress);
                    return Json(new { success = true, message = "Su sesión ha sido extendida." });
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "| Usuario: {User} | Ip: {Ip} | Acción: Error al extender sesión | Mensaje: {ErrorMessage} |", currentUser.UserName, ipAddress, ex.Message);
                    return Json(new { success = false, message = "Error al extender la sesión." });
                }
            }
        }
    }
}
