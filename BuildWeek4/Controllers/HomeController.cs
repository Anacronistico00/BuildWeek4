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

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false, true)
                .Build();


            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<IActionResult> Index()
        {
            var productList = new ProductViewModel()
            { 
                Products = new List<Product>() 
            };

            await using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string query = "SELECT Prodotti.IdProdotto, Prodotti.Dettaglio, Prodotti.Descrizione, Categorie.NomeCategoria, " +
                    "Prodotti.URLImmagine, Prodotti.Prezzo FROM Prodotti " +
                    "INNER JOIN Categorie ON Prodotti.IdCategoria = Categorie.IdCategoria";
                await using (SqlCommand command = new SqlCommand(query, connection))
                {
                    await using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while(await reader.ReadAsync())
                        {
                            productList.Products.Add(
                                new Product()
                                {
                                    IdProdotto = reader.GetGuid(0),
                                    Dettaglio = reader.GetString(1),
                                    Descrizione = reader.GetString(2),
                                    Categoria = reader.GetString(3),
                                    URLImmagine = reader.GetString(4),
                                    Prezzo = reader.GetDecimal(5)
                                });
                        }
                    }
                }
            }

                return View(productList);
        }
        
        public async Task<IActionResult> Details(Guid id)
        {
            var details = new Details();

            await using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                await using (SqlCommand command = new SqlCommand("SELECT Prodotti.IdProdotto, Prodotti.Dettaglio, Prodotti.Descrizione, " +
                    "Categorie.NomeCategoria, Prodotti.URLImmagine, Prodotti.Prezzo " +
                    "FROM Prodotti " +
                    "INNER JOIN " +
                    "Categorie ON Prodotti.IdCategoria = Categorie.IdCategoria WHERE Prodotti.IdProdotto = @IdProdotto", connection))
                {
                    command.Parameters.AddWithValue("@IdProdotto", id);
                    await using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            details.IdProdotto = reader.GetGuid(0);
                            details.Dettaglio = reader.GetString(1);
                            details.Descrizione = reader.GetString(2);
                            details.Categoria = reader.GetString(3);
                            details.URLImmagine = reader.GetString(4);
                            details.Prezzo = reader.GetDecimal(5);
                        }
                    }
                }
            }
            return View(details);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

}

