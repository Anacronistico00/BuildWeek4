using BuildWeek4.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace BuildWeek4.Controllers
{
    public class CartController : Controller
    {
        private readonly string _connectionString;

        public CartController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // Visualizza il carrello
        public async Task<IActionResult> VisualizzaCarrello()
        {
            List<Cart> carrello = new List<Cart>();

            // Ottieni l'ID dell'utente loggato
            Guid idUtente = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)); // Assumendo che l'ID utente sia nel claim

            await using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Aggiungi il filtro per l'utente loggato nella query
                string query = "SELECT Carrello.IdProdotto, Prodotti.Dettaglio, Prodotti.Prezzo, Carrello.Quantita " +
                               "FROM Carrello " +
                               "INNER JOIN Prodotti ON Carrello.IdProdotto = Prodotti.IdProdotto " +
                               "WHERE Carrello.IdUtente = @IdUtente";

                await using (SqlCommand command = new SqlCommand(query, connection))
                {
                    // Aggiungi il parametro per l'ID utente
                    command.Parameters.AddWithValue("@IdUtente", idUtente);

                    await using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            carrello.Add(new Cart
                            {
                                IdProdotto = reader.GetGuid(0),
                                Dettaglio = reader.GetString(1),
                                Prezzo = reader.GetDecimal(2),
                                Quantita = reader.GetInt32(3)
                            });
                        }
                    }
                }

                // Creazione della lista di prodotti
                var productList = new ProductViewModel()
                {
                    Products = new List<Product>()
                };

                // Query per recuperare tutti i prodotti
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

            // Passa la lista del carrello alla vista
            return View(carrello);
        }



        // Rimuovi prodotto dal carrello
        [HttpPost]
        public async Task<IActionResult> RimuoviDalCarrello(Guid idProdotto)
        {
            await using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string query = "DELETE FROM Carrello WHERE IdProdotto = @IdProdotto";

                await using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@IdProdotto", idProdotto);
                    await command.ExecuteNonQueryAsync();
                }
            }

            return RedirectToAction("VisualizzaCarrello");
        }

        // Svuota il carrello
        [HttpPost]
        public async Task<IActionResult> SvuotaCarrello()
        {
            await using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string query = "DELETE FROM Carrello";

                await using (SqlCommand command = new SqlCommand(query, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
            }

            TempData["CarrelloSvuotato"] = "Il carrello è stato svuotato";
            return RedirectToAction("VisualizzaCarrello");
        }

        [HttpPost]
        public async Task<IActionResult> Acquista()
        {
            List<Cart> carrello = new List<Cart>();

            await using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string query = "SELECT Carrello.IdProdotto, Prodotti.Dettaglio, Prodotti.Prezzo, Carrello.Quantita " +
                               "FROM Carrello " +
                               "INNER JOIN Prodotti ON Carrello.IdProdotto = Prodotti.IdProdotto";

                await using (SqlCommand command = new SqlCommand(query, connection))
                {
                    await using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            carrello.Add(new Cart
                            {
                                IdProdotto = reader.GetGuid(0),
                                Dettaglio = reader.GetString(1),
                                Prezzo = reader.GetDecimal(2),
                                Quantita = reader.GetInt32(3)
                            });
                        }
                    }
                }
            }

            // Apre la connessione al database
            await using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                foreach (var prodotto in carrello)
                {
                    // Verifica lo stock disponibile per ogni prodotto
                    string checkStockQuery = "SELECT Stock FROM Prodotti WHERE IdProdotto = @IdProdotto";
                    int stockDisponibile = 0;

                    await using (SqlCommand checkStockCommand = new SqlCommand(checkStockQuery, connection))
                    {
                        checkStockCommand.Parameters.AddWithValue("@IdProdotto", prodotto.IdProdotto);
                        stockDisponibile = (int)await checkStockCommand.ExecuteScalarAsync();
                    }

                    // Se lo stock non è sufficiente, mostra un errore e interrompi l'acquisto
                    if (stockDisponibile < prodotto.Quantita)
                    {
                        TempData["ErroreCarrello"] = $"Non ci sono abbastanza pezzi disponibili per il prodotto {prodotto.Dettaglio}. Disponibile: {stockDisponibile}.";
                        return RedirectToAction("VisualizzaCarrello");
                    }

                    // Aggiorna lo stock, sottraendo la quantità acquistata
                    string updateStockQuery = "UPDATE Prodotti SET Stock = Stock - @Quantita WHERE IdProdotto = @IdProdotto";

                    await using (SqlCommand updateStockCommand = new SqlCommand(updateStockQuery, connection))
                    {
                        updateStockCommand.Parameters.AddWithValue("@IdProdotto", prodotto.IdProdotto);
                        updateStockCommand.Parameters.AddWithValue("@Quantita", prodotto.Quantita);
                        await updateStockCommand.ExecuteNonQueryAsync();
                    }
                }

                // Svuota il carrello
                string deleteCarrelloQuery = "DELETE FROM Carrello";
                await using (SqlCommand deleteCarrelloCommand = new SqlCommand(deleteCarrelloQuery, connection))
                {
                    await deleteCarrelloCommand.ExecuteNonQueryAsync();
                }
            }

            // Mostra il messaggio di conferma dell'acquisto
            TempData["AcquistoCompletato"] = "Grazie per aver acquistato! Il tuo ordine è stato completato.";
            return RedirectToAction("VisualizzaCarrello");
        }

    }

}
