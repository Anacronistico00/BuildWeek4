using BuildWeek4.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

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

            return View(carrello);
        }

        // Aggiungi prodotto al carrello
        [HttpPost]
        public async Task<IActionResult> AggiungiAlCarrello(Guid idProdotto, int quantita)
        {
            await using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = "INSERT INTO Carrello (IdProdotto, Quantita) VALUES (@IdProdotto, @Quantita)";
                await using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@IdProdotto", idProdotto);
                    command.Parameters.AddWithValue("@Quantita", quantita);

                    await command.ExecuteNonQueryAsync();
                }
            }

            return RedirectToAction("VisualizzaCarrello");
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

        // Acquisto
        [HttpPost]
        public async Task<IActionResult> Acquista()
        {

            await using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Svuota il carrello
                string query = "DELETE FROM Carrello";

                await using (SqlCommand command = new SqlCommand(query, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
            }

            TempData["AcquistoCompletato"] = "Grazie per aver acquistato! Il tuo ordine è stato completato.";
            return RedirectToAction("VisualizzaCarrello");
        }
    }

}
