using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication;

namespace BuildWeek4.Controllers
{
    public class LoginController : Controller
    {
        private readonly string _connectionString;

        public LoginController()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false, true)
                .Build();

            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Compila tutti i campi.";
                return View();
            }

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Cerca l'utente con l'email e la password
                string sql = "SELECT IdUtente, IsAdmin FROM Utenti WHERE Email = @Email AND UserPassword = @Password";

                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@Password", password);

                    var reader = await command.ExecuteReaderAsync();

                    if (reader.Read())
                    {
                        // Recupera l'ID utente e il valore di IsAdmin
                        var userId = reader.GetGuid(0);
                        var isAdmin = reader.GetBoolean(1);

                        // Crea i claims
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, email),
                            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                            new Claim("IsAdmin", isAdmin ? "1" : "0")
                        };

                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                        // Effettua il login dell'utente con i claims
                        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);

                        // Reindirizza alla Home dopo il login
                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        ViewBag.Error = "Email o password errati.";
                        return View();
                    }
                }
            }
        }

        public IActionResult Logout()
        {
            // Esegui il logout e reindirizza alla pagina di login
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Login");
        }
    }
}
