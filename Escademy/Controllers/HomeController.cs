using Escademy.Models;
using System;
using System.Collections.Generic;
using System.Web.Mvc;


namespace Escademy.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult Coaches(string filter)
        {
            ViewBag.filterCategory = filter;

            return View();
        }

        [HttpGet]
        public ActionResult _GetCoaches(int id)
        {
            var users = new List<User>();
            var gameCoaches = new List<GameCoaching>();
            try
            {
                using (var conn = new MySql.Data.MySqlClient.MySqlConnection(ConnectionString.Get("EscademyMDB")))
                {
                    conn.Open();

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = $"SELECT a.FirstName, a.Id, a.ProfilePicture, a.Created_at, p.accountId, p.title, p.SalaryUSD, p.gameId, g.Game, g.Abbreviation, g.Picture FROM esc_profilegames p JOIN esc_accounts a ON a.Id = p.accountId JOIN esc_games g ON p.gameId = g.Id Where g.Id = {id}";
                        var reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            gameCoaches.Add(new GameCoaching()
                            {
                                AccountId = reader.GetInt32("accountId"),
                                SalaryUSD = reader.GetDecimal("SalaryUSD"),
                                Game = reader.GetString("Game"),
                                Picture = reader.GetString("Picture"),
                                Abbreviation = reader.GetString("Abbreviation"),
                                GameId = reader.GetInt32("gameId"),
                                Title = reader.GetString("Title")
                            });

                            users.Add(new User()
                            {
                                Id = reader.GetInt32("Id"),
                                FirstName = reader.GetString("FirstName"),
                                Picture = reader.GetString("ProfilePicture")
                            });
                        }
                    }

                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            ViewBag.gameCoaches = gameCoaches;
            ViewBag.users = users;
            return PartialView("_GetCoaches");
        }

        public ActionResult About()
        {
            return View();
        }
        public ActionResult Contact()
        {
            return View();
        }
        public ActionResult Chat()
        {
            if (Session["user"] != null)
            {
                return View();
            }
            else return RedirectToAction("Login", "Auth");
        }
    }
}