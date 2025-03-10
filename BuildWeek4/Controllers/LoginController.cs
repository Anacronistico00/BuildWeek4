﻿using Microsoft.AspNetCore.Mvc;
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


        //Registra un nuovo utente
        [HttpGet]
        public IActionResult Registrati()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Registrati(string nome, string email, string password)
        {
            if (string.IsNullOrEmpty(nome) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Compila tutti i campi.";
                return View();
            }

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Verifica se l'email è già registrata
                string checkSql = "SELECT COUNT(*) FROM Utenti WHERE Email = @Email";
                using (var checkCommand = new SqlCommand(checkSql, connection))
                {
                    checkCommand.Parameters.AddWithValue("@Email", email);
                    int count = (int)await checkCommand.ExecuteScalarAsync();

                    if (count > 0)
                    {
                        ViewBag.Error = "Email già registrata.";
                        return View();
                    }
                }

                // Inserisci il nuovo utente
                string insertSql = "INSERT INTO Utenti (IdUtente, Nome, Email, UserPassword, IsAdmin) VALUES (@IdUtente, @Nome, @Email, @Password, 0)";
                using (var insertCommand = new SqlCommand(insertSql, connection))
                {
                    insertCommand.Parameters.AddWithValue("@IdUtente", Guid.NewGuid());
                    insertCommand.Parameters.AddWithValue("@Nome", nome);
                    insertCommand.Parameters.AddWithValue("@Email", email);
                    insertCommand.Parameters.AddWithValue("@Password", password);

                    await insertCommand.ExecuteNonQueryAsync();
                }

                ViewBag.Success = "Registrazione completata! Ora puoi effettuare il login.";
                return RedirectToAction("Index");
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

