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
            // Verifica se l'utente è loggato
            if (!User.Identity.IsAuthenticated)
            {
                // Se non è loggato, reindirizza alla pagina di login
                return RedirectToAction("Index", "Login");
            }

            // Recupera l'IdUtente dal claim (utente autenticato)
            Guid idUtente = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)); // Assumendo che l'utente sia autenticato

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

                if(stockDisponibile <= 0)
                {
                    TempData["ErrorMessage"] = "Il prodotto non è disponibile!";
                }

                // Verifica se il prodotto è già nel carrello per quell'utente
                string checkQuery = "SELECT COUNT(*) FROM Carrello WHERE IdProdotto = @IdProdotto AND IdUtente = @IdUtente";
                await using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@IdProdotto", idProdotto);
                    checkCommand.Parameters.AddWithValue("@IdUtente", idUtente);
                    int count = (int)await checkCommand.ExecuteScalarAsync();

                    if (count > 0)
                    {
                        // Se il prodotto è già nel carrello per quell'utente, aggiorna la quantità
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
                        // Se il prodotto non è nel carrello per quell'utente, inseriscilo
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
            }

            return RedirectToAction("Details", new { id = idProdotto });
        }


        //ricerca con filtri
        public async Task<IActionResult> Ricerca(string query, string filtro, string categoria)
        {
            var risultati = new ProductViewModel()
            {
                Products = new List<Product>()
            };

            query = query ?? "";
            categoria = categoria ?? "";

            await using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string sqlQuery = @"SELECT Prodotti.IdProdotto, Prodotti.Dettaglio, Prodotti.Descrizione, 
                            Categorie.NomeCategoria, Prodotti.URLImmagine, Prodotti.Prezzo 
                            FROM Prodotti 
                            INNER JOIN Categorie ON Prodotti.IdCategoria = Categorie.IdCategoria
                            WHERE (1 = 1)";

                if (!string.IsNullOrEmpty(query))
                {
                    sqlQuery += " AND (Prodotti.Dettaglio LIKE @Query OR Prodotti.Descrizione LIKE @Query)";
                }

                if (!string.IsNullOrEmpty(categoria))
                {
                    sqlQuery += " AND Categorie.NomeCategoria = @Categoria";
                }

                switch (filtro)
                {
                    case "prezzo_asc":
                        sqlQuery += " ORDER BY Prodotti.Prezzo ASC";
                        break;
                    case "prezzo_desc":
                        sqlQuery += " ORDER BY Prodotti.Prezzo DESC";
                        break;
                    case "alfabetico_asc":
                        sqlQuery += " ORDER BY Prodotti.Dettaglio ASC";
                        break;
                    case "alfabetico_desc":
                        sqlQuery += " ORDER BY Prodotti.Dettaglio DESC";
                        break;
                    
                    default:
                        sqlQuery += " ORDER BY Prodotti.Dettaglio ASC";
                        break;
                }

                await using (var command = new SqlCommand(sqlQuery, connection))
                {
                    if (!string.IsNullOrEmpty(query))
                    {
                        command.Parameters.AddWithValue("@Query", "%" + query + "%");
                    }
                    if (!string.IsNullOrEmpty(categoria))
                    {
                        command.Parameters.AddWithValue("@Categoria", categoria);
                    }

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

                    ViewBag.ProductsList = productList.Products;
                }
            }

            ViewBag.Filtro = filtro;
            ViewBag.Query = query;
            ViewBag.Categoria = categoria;

            return View(risultati);
        }

        //ricerca quando si preme l'immagine
        public async Task<IActionResult> FiltraPerCategoria(string categoria)
        {
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


