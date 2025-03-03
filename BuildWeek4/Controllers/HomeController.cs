using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using BuildWeek4.Models;
using Microsoft.Data.SqlClient;

namespace BuildWeek4.Controllers
{
    [AutoValidateAntiforgeryToken]
    public class HomeController : Controller
    {
        private readonly string _connectionString;

        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;            
            //creo un'istanza della configurazione, per leggere la stringa di connessione al database
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false, true) //il secondo parametro indica l'obligatoreità del file, il terzo parametro indica se il file deve essere ricaricato e letto nuovamente quando viene modificato durante l'esecuzione del programa
                .Build();

            //Lettura della configurazione dal file appsettings.json
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<IActionResult> Index()
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
    }

}

