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
                    "Categorie.NomeCategoria, Prodotti.URLImmagine, Prodotti.Prezzo, Prodotti.Stock " +
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
                            details.Quantita = reader.GetInt32(6);

                            if (details.Quantita == 0)
                            {
                                details.OutOfStock = true;
                            }
                            else
                            {
                                details.OutOfStock = false;
                            }
                        }
                    }
                }
                return View(details);
            }
        }

        [HttpPost]
        public async Task<IActionResult> AggiungiAlCarrello(Guid idProdotto, int quantita)
        {
            await using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Verifica la disponibilità in stock
                string checkStockQuery = "SELECT Stock FROM Prodotti WHERE IdProdotto = @IdProdotto";
                int stockDisponibile = 0;
                await using (SqlCommand checkStockCommand = new SqlCommand(checkStockQuery, connection))
                {
                    checkStockCommand.Parameters.AddWithValue("@IdProdotto", idProdotto);
                    stockDisponibile = (int)await checkStockCommand.ExecuteScalarAsync();
                }

                // Se lo stock è insufficiente, aggiungi l'errore in ViewData
                if (stockDisponibile < quantita)
                {
                    TempData["ErrorMessage"] = "Non ci sono abbastanza pezzi disponibili per questo prodotto.";
                    return RedirectToAction("Details", new { id = idProdotto });
                }

                // Verifica se il prodotto è già nel carrello
                string checkQuery = "SELECT COUNT(*) FROM Carrello WHERE IdProdotto = @IdProdotto";
                await using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@IdProdotto", idProdotto);
                    int count = (int)await checkCommand.ExecuteScalarAsync();

                    if (count > 0)
                    {
                        // Se il prodotto è già nel carrello, aggiorna la quantità
                        string updateQuery = "UPDATE Carrello SET Quantita = Quantita + @Quantita WHERE IdProdotto = @IdProdotto";
                        await using (SqlCommand updateCommand = new SqlCommand(updateQuery, connection))
                        {
                            updateCommand.Parameters.AddWithValue("@Quantita", quantita);
                            updateCommand.Parameters.AddWithValue("@IdProdotto", idProdotto);
                            await updateCommand.ExecuteNonQueryAsync();
                        }
                    }
                    else
                    {
                        // Se il prodotto non è nel carrello, inseriscilo
                        string insertQuery = "INSERT INTO Carrello (IdProdotto, Quantita) VALUES (@IdProdotto, @Quantita)";
                        await using (SqlCommand insertCommand = new SqlCommand(insertQuery, connection))
                        {
                            insertCommand.Parameters.AddWithValue("@IdProdotto", idProdotto);
                            insertCommand.Parameters.AddWithValue("@Quantita", quantita);
                            await insertCommand.ExecuteNonQueryAsync();
                        }
                    }
                }
            }

            return RedirectToAction("VisualizzaCarrello", "Cart");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}


