using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using BuildWeek4.Models;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

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

        private async Task<int> GetCartItemCount(Guid idUtente)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Query per ottenere il numero di articoli nel carrello
                string query = "SELECT SUM(Quantita) FROM Carrello WHERE IdUtente = @IdUtente";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@IdUtente", idUtente);

                    var result = await command.ExecuteScalarAsync();
                    return result != DBNull.Value ? Convert.ToInt32(result) : 0;
                }
            }
        }

        public async Task<IActionResult> Index()
        {
            var productList = new ProductViewModel()
            {
                Products = new List<Product>()
            };

            // Recupero il numero di articoli nel carrello
            if (User.Identity.IsAuthenticated)
            {
                Guid idUtente = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                ViewBag.CartCount = await GetCartItemCount(idUtente);
            }
            else
            {
                ViewBag.CartCount = 0;
            }

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
                        while (await reader.ReadAsync())
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


        [HttpGet]
        public IActionResult Profilo()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Login");
            }

            var email = User.Identity.Name;
            var isAdmin = User.Claims.FirstOrDefault(c => c.Type == "IsAdmin")?.Value == "1";

            ViewBag.Email = email;
            ViewBag.IsAdmin = isAdmin;

            return View();
        }


        public async Task<IActionResult> Details(Guid id)
        {

            if (User.Identity.IsAuthenticated)
            {
                Guid idUtente = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                ViewBag.CartCount = await GetCartItemCount(idUtente);
            }
            else
            {
                ViewBag.CartCount = 0;
            }

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

                var productList = new ProductViewModel()
                {
                    Products = new List<Product>()
                };

                await using (SqlCommand command = new SqlCommand("SELECT Prodotti.IdProdotto, Prodotti.Dettaglio, Prodotti.Descrizione, Categorie.NomeCategoria, " +
                    "Prodotti.URLImmagine, Prodotti.Prezzo FROM Prodotti " +
                    "INNER JOIN Categorie ON Prodotti.IdCategoria = Categorie.IdCategoria", connection))
                {
                    await using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
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

                // Assegna la lista dei prodotti a ViewBag
                ViewBag.ProductsList = productList.Products;
            }

            return View(details);
            }
        }


        [HttpPost]
        public async Task<IActionResult> AggiungiAlCarrello(Guid idProdotto, int quantita)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Login");
            }

            try
            {
                Guid idUtente = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                await using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Controlla la disponibilità
                    string checkStockQuery = "SELECT Stock FROM Prodotti WHERE IdProdotto = @IdProdotto";
                    int stockDisponibile = 0;
                    await using (SqlCommand checkStockCommand = new SqlCommand(checkStockQuery, connection))
                    {
                        checkStockCommand.Parameters.AddWithValue("@IdProdotto", idProdotto);
                        object stockResult = await checkStockCommand.ExecuteScalarAsync();
                        stockDisponibile = stockResult != DBNull.Value ? Convert.ToInt32(stockResult) : 0;
                    }

                    if (stockDisponibile <= 0)
                    {
                        TempData["ErrorMessage"] = "Prodotto esaurito!";
                        return RedirectToAction("Details", new { id = idProdotto });
                    }

                    // Controlla se il prodotto è già nel carrello
                    string checkQuery = "SELECT Quantita FROM Carrello WHERE IdProdotto = @IdProdotto AND IdUtente = @IdUtente";
                    int quantitaAttuale = 0;
                    await using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@IdProdotto", idProdotto);
                        checkCommand.Parameters.AddWithValue("@IdUtente", idUtente);
                        object result = await checkCommand.ExecuteScalarAsync();
                        quantitaAttuale = result != DBNull.Value ? Convert.ToInt32(result) : 0;
                    }

                    if (quantitaAttuale > 0)
                    {
                        // Se già presente, aggiorna
                        string updateQuery = "UPDATE Carrello SET Quantita = Quantita + @Quantita WHERE IdProdotto = @IdProdotto AND IdUtente = @IdUtente";
                        await using (SqlCommand updateCommand = new SqlCommand(updateQuery, connection))
                        {
                            updateCommand.Parameters.AddWithValue("@Quantita", quantita);
                            updateCommand.Parameters.AddWithValue("@IdProdotto", idProdotto);
                            updateCommand.Parameters.AddWithValue("@IdUtente", idUtente);
                            await updateCommand.ExecuteNonQueryAsync();
                        }
                    }
                    else
                    {
                        // Inserisce il nuovo prodotto
                        string insertQuery = "INSERT INTO Carrello (IdProdotto, IdUtente, Quantita) VALUES (@IdProdotto, @IdUtente, @Quantita)";
                        await using (SqlCommand insertCommand = new SqlCommand(insertQuery, connection))
                        {
                            insertCommand.Parameters.AddWithValue("@IdProdotto", idProdotto);
                            insertCommand.Parameters.AddWithValue("@IdUtente", idUtente);
                            insertCommand.Parameters.AddWithValue("@Quantita", quantita);
                            await insertCommand.ExecuteNonQueryAsync();
                        }
                    }
                }

                // Aggiorna il contatore del carrello
                TempData["CartUpdated"] = true;
                return RedirectToAction("Details", new { id = idProdotto });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Errore: " + ex.Message;
                return RedirectToAction("Details", new { id = idProdotto });
            }
        }



        public async Task<IActionResult> Ricerca(string query)
        {

            if (User.Identity.IsAuthenticated)
            {
                Guid idUtente = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                ViewBag.CartCount = await GetCartItemCount(idUtente);
            }
            else
            {
                ViewBag.CartCount = 0;
            }

            var risultati = new ProductViewModel()
            {
                Products = new List<Product>()
            };

            if (string.IsNullOrEmpty(query))
            {
                return View(risultati); // Nessun risultato se la query è vuota
            }

            await using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sqlQuery = @"SELECT Prodotti.IdProdotto, Prodotti.Dettaglio, Prodotti.Descrizione, 
                            Categorie.NomeCategoria, Prodotti.URLImmagine, Prodotti.Prezzo 
                            FROM Prodotti 
                            INNER JOIN Categorie ON Prodotti.IdCategoria = Categorie.IdCategoria
                            WHERE Prodotti.Dettaglio LIKE @Query OR Prodotti.Descrizione LIKE @Query";

                await using (SqlCommand command = new SqlCommand(sqlQuery, connection))
                {
                    command.Parameters.AddWithValue("@Query", "%" + query + "%");

                    await using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            risultati.Products.Add(new Product()
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
                var productList = new ProductViewModel()
                {
                    Products = new List<Product>()
                };

                await using (SqlCommand command = new SqlCommand("SELECT Prodotti.IdProdotto, Prodotti.Dettaglio, Prodotti.Descrizione, Categorie.NomeCategoria, " +
                    "Prodotti.URLImmagine, Prodotti.Prezzo FROM Prodotti " +
                    "INNER JOIN Categorie ON Prodotti.IdCategoria = Categorie.IdCategoria", connection))
                {
                    await using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
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

                    // Assegna la lista dei prodotti a ViewBag
                    ViewBag.ProductsList = productList.Products;
                }
            }

            return View(risultati);
        }

        //ricerca quando si preme l'immagine
        public async Task<IActionResult> FiltraPerCategoria(string categoria)
        {

            if (User.Identity.IsAuthenticated)
            {
                Guid idUtente = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                ViewBag.CartCount = await GetCartItemCount(idUtente);
            }
            else
            {
                ViewBag.CartCount = 0;
            }

            var risultati = new ProductViewModel()
            {
                Products = new List<Product>()
            };

            if (string.IsNullOrEmpty(categoria))
            {
                return RedirectToAction("Index"); // Se la categoria è vuota, torno alla home
            }

            await using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sqlQuery = @"SELECT Prodotti.IdProdotto, Prodotti.Dettaglio, Prodotti.Descrizione, 
                    Categorie.NomeCategoria, Prodotti.URLImmagine, Prodotti.Prezzo 
                    FROM Prodotti 
                    INNER JOIN Categorie ON Prodotti.IdCategoria = Categorie.IdCategoria
                    WHERE Categorie.NomeCategoria = @Categoria";

                await using (SqlCommand command = new SqlCommand(sqlQuery, connection))
                {
                    command.Parameters.AddWithValue("@Categoria", categoria);

                    await using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            risultati.Products.Add(new Product()
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
                var productList = new ProductViewModel()
                {
                    Products = new List<Product>()
                };

                await using (SqlCommand command = new SqlCommand("SELECT Prodotti.IdProdotto, Prodotti.Dettaglio, Prodotti.Descrizione, Categorie.NomeCategoria, " +
                    "Prodotti.URLImmagine, Prodotti.Prezzo FROM Prodotti " +
                    "INNER JOIN Categorie ON Prodotti.IdCategoria = Categorie.IdCategoria", connection))
                {
                    await using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
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

                    // Assegna la lista dei prodotti a ViewBag
                    ViewBag.ProductsList = productList.Products;
                }
            }

            ViewBag.Categoria = categoria;
            return View("Ricerca", risultati);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}


