using BuildWeek4.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace BuildWeek4.Controllers
{
    [AutoValidateAntiforgeryToken]
    public class AdminController : Controller
    {
        private readonly string _connectionString;

        public AdminController()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false, true)
                .Build();


            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }
        public async Task<IActionResult> Index()
        {
            var productList = new ProductViewModel()
            { Products = new List<Product>() };
            await using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string query = "SELECT Prodotti.IdProdotto, Prodotti.Dettaglio, Prodotti.Descrizione, Categorie.NomeCategoria,Prodotti.URLImmagine, Prodotti.Prezzo FROM Prodotti\r\n    INNER JOIN\r\n    Categorie ON Prodotti.IdCategoria = Categorie.IdCategoria";
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

        public async Task<IActionResult> AddProduct()
        {
            var model = new AddProductModel()
            {
                Categorie = await GetCategories()
            };
            return View(model);
        }

        private async Task<List<Categoria>> GetCategories()
        {
            List<Categoria> categories = new List<Categoria>();

            await using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string query = "SELECT * FROM Categorie";
                await using (SqlCommand command = new SqlCommand(query, connection))
                {
                    await using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            categories.Add(new Categoria()
                            {
                                IdCategoria = reader.GetGuid(0),
                                NomeCategoria = reader.GetString(1)
                            });
                        }
                    }
                }
            }
            return categories;
        }

        [HttpPost]
        public async Task<IActionResult> Create(AddProductModel addProductModel)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("AddProduct");
            }

            await using (SqlConnection connection =  new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = "INSERT INTO Prodotti (IdProdotto, URLImmagine, Prezzo, Dettaglio, Descrizione, IdCategoria) VALUES (@IdProdotto, @URLImmagine, @Prezzo, @Dettaglio, @Descrizione, @IdCategoria)";
                
                await using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@IdProdotto", Guid.NewGuid());
                    command.Parameters.AddWithValue("@URLImmagine", addProductModel.URLImmagine);
                    command.Parameters.AddWithValue("@Prezzo", addProductModel.Prezzo);
                    command.Parameters.AddWithValue("@Dettaglio", addProductModel.Dettaglio);
                    command.Parameters.AddWithValue("@Descrizione", addProductModel.Descrizione);
                    command.Parameters.AddWithValue("@IdCategoria", addProductModel.IdCategoria);
                    await command.ExecuteNonQueryAsync();
                }
            }
            return RedirectToAction("Index");
        }

        [HttpGet("home/edit/{id:guid}")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var editProduct = new EditProduct();

            await using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string query = "SELECT * FROM Prodotti WHERE IdProdotto = @IdProdotto";
                await using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@IdProdotto", id);
                    await using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            editProduct.IdProdotto = Guid.Parse(reader["IdProdotto"].ToString());
                            editProduct.URLImmagine = reader.GetString(1);
                            editProduct.Dettaglio = reader.GetString(3);
                            editProduct.Prezzo = reader.GetDecimal(2);
                            editProduct.Descrizione = reader.GetString(4);
                            editProduct.IdCategoria = reader.GetGuid(5);
                        }
                    }
                }
            }
            ViewBag.Categoria = await GetCategories();
            return View(editProduct);
        }

        [HttpPost("home/edit/save/{id:guid}")]
        public async Task<IActionResult> SaveEdit(Guid id, EditProduct editProduct)
        {
            await using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string query = "UPDATE Prodotti SET Dettaglio = @Dettaglio, URLImmagine = @URLImmagine, Prezzo = @Prezzo, Descrizione = @Descrizione, IdCategoria = @IdCategoria WHERE IdProdotto = @IdProdotto";
                await using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Dettaglio", editProduct.Dettaglio);
                    command.Parameters.AddWithValue("@URLImmagine", editProduct.URLImmagine);
                    command.Parameters.AddWithValue("@Prezzo", editProduct.Prezzo);
                    command.Parameters.AddWithValue("@Descrizione", editProduct.Descrizione);
                    command.Parameters.AddWithValue("@IdCategoria", editProduct.IdCategoria);
                    command.Parameters.AddWithValue("@IdProdotto", id);
                    await command.ExecuteNonQueryAsync();
                }


            }
            return RedirectToAction("Index");
        }

        [HttpGet("home/delete/{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string query = "DELETE FROM Prodotti WHERE IdProdotto = @IdProdotto";
                await using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@IdProdotto", id);
                    await command.ExecuteNonQueryAsync();
                }
            }
            return RedirectToAction("Index");
        }
    }
}
