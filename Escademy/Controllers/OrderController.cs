using Escademy.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Escademy.Controllers
{
    public class OrderController : Controller
    {
        public ActionResult New(int accountId=0, int gameId=0, int quantity=1)
        {
            if (EnsureUserRights(UserLevel.Default))
            {
                ViewBag.payerid = GetCurrentUser().Id;
                LoadOrder(accountId, gameId, quantity);
                return View();
            }
            return RedirectToAction("Login", "Auth");
        }
        public ActionResult Confirm(int accountId = 0, int gameId = 0, int quantity = 1)
        {
            if (EnsureUserRights(UserLevel.Default))
            {
                ViewBag.payerid = GetCurrentUser().Id;
                LoadOrder(accountId, gameId, quantity);
                return View();
            }
            return RedirectToAction("Login", "Auth");
        }

        public ActionResult Details()
        {
            return View();
        }


        private bool EnsureUserRights(UserLevel rights)
        {
            if (Session["user"] != null)
            {
                if ((Session["user"] as User).HasRole(rights))
                {
                    return true;
                }
            }
            return false;
        }
        private User GetCurrentUser()
        {
            return (User)Session["user"];
        }


        private void LoadOrder(int accountId, int gameId, int quantity)
        {
            ViewBag.accountId = accountId;
            ViewBag.gameId = gameId;
            ViewBag.quantity = quantity;

            using (var conn = new MySqlConnection(ConnectionString.Get("EscademyMDB")))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT p.*, g.Picture, a.Created_at, a.FirstName, a.Description userdesc, a.ProfilePicture, a.Country FROM esc_profilegames p JOIN esc_accounts a ON a.Id=p.accountId JOIN esc_games g ON g.Id=p.gameId WHERE accountId=@accId AND gameId=@gameId";
                    cmd.Parameters.AddWithValue("@accId", accountId);
                    cmd.Parameters.AddWithValue("@gameId", gameId);

                    var reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        ViewBag.coach = new GameCoaching()
                        {
                            AccountId = accountId,
                            GameId = gameId,
                            Description = reader.GetString("Description"),
                            Title = reader.GetString("Title"),
                            SalaryUSD = reader.GetDecimal("SalaryUSD"),
                            Picture = reader.GetString("Picture")
                        };

                        ViewBag.user = new User()
                        {
                            Country = reader.GetString("Country"),
                            Description = reader.GetString("userdesc"),
                            FirstName = reader.GetString("FirstName"),
                            Picture = reader.GetString("ProfilePicture")
                        };


                        var dt = reader.GetDateTime("Created_at");
                        ViewBag.date = $"{dt.ToString("MMM", CultureInfo.InvariantCulture)} {dt.Year}";
                    }
                }
                conn.Close();
            }
        }
    }
}