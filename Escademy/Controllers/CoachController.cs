using Escademy.Models;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Web;
using System.Web.Http;

namespace Escademy.Controllers
{
    public class CoachController : ApiController
    {
        // GET: api/Coach
        //public IEnumerable<string> Get()
        //{
        //    return new string[] { "value1", "value2" };
        //}

        // GET: api/Coach/5
        public object Get(int id)
        {
            List<GameCoaching> coachings = new List<GameCoaching>();

            using (var conn = new MySqlConnection(ConnectionString.Get("EscademyMDB")))
            {
                conn.Open();

                // Load Game Coachings
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM esc_profilegames WHERE accountId=@accId";
                    cmd.Parameters.AddWithValue("@accId", id);

                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        coachings.Add(new GameCoaching()
                        {
                            AccountId = id,
                            GameId = reader.GetInt32("gameId"),
                            SalaryUSD = reader.GetDecimal("SalaryUSD"),
                            Description = reader.GetString("Description")
                            //TODO load more properties..
                        });
                    }
                }

                // Load FAQ's
                foreach (var coaching in coachings)
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT * FROM esc_faq where accountId=@accId AND gameId=@gameId";
                        cmd.Parameters.AddWithValue("@accId", coaching.AccountId);
                        cmd.Parameters.AddWithValue("@gameId", coaching.GameId);

                        var reader = cmd.ExecuteReader();
                        var faqList = new List<CoachingFAQ>();
                        while (reader.Read())
                        {
                            faqList.Add(new CoachingFAQ() {
                                Title = reader.GetString("Title"),
                                Description = reader.GetString("Description")
                            });
                        }
                        coaching.FAQElements = faqList.ToArray();
                    }
                }


                conn.Close();
            }

            return Json(new
            {
                count = coachings.Count,
                coachings
            });
        }

        // POST: api/Coach
        //public object Post()
        //{
        //    var httpRequest = HttpContext.Current.Request;
        //    if (httpRequest.Files.Count > 0)
        //    {
        //        var docfiles = new List<string>();
        //        foreach (string file in httpRequest.Files)
        //        {
        //            var postedFile = httpRequest.Files[file];
        //            var filePath = HttpContext.Current.Server.MapPath("~/" + postedFile.FileName);
        //            postedFile.SaveAs(filePath);
        //            docfiles.Add(filePath);
        //        }
        //    }
        //    return Json("");
        //}

        // POST: api/Coach
        public object Post([FromBody]GameCoaching coaching)
        {
            bool auth = false;
            bool success = false;
            string error = "";
            int error_code = -1;

            if (EnsureUserRights(UserLevel.Coach))
            {
                var user = GetCurrentUser();
                auth = true;

                using (var conn = new MySqlConnection(ConnectionString.Get("EscademyMDB")))
                {
                    conn.Open();

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "INSERT INTO esc_profilegames(accountId, gameId, SalaryUSD, Description) VALUES (@accId, @gameId, @salary, @description)";
                        cmd.Parameters.AddWithValue("@accId", user.Id);
                        cmd.Parameters.AddWithValue("@gameId", coaching.GameId);
                        cmd.Parameters.AddWithValue("@salary", coaching.SalaryUSD);
                        cmd.Parameters.AddWithValue("@description", coaching.Description);

                        try
                        {
                            cmd.ExecuteNonQuery();
                            success = true;
                        }
                        catch (MySqlException mse)
                        {
                            if (mse.Number == 1062)  // DUPLICATE_ENTRY
                            {
                                error = "You are already coaching in this game! Please edit or delete that coaching and try again.";
                            }
                            else
                            {
                                error = "Unknown Error Occured.";
                            }

                            error_code = mse.Number;
                        }
                    }

                    if (success)
                    {
                        foreach (var FAQElement in coaching.FAQElements)
                        {
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.CommandText = "INSERT INTO esc_faq(accountId, gameId, Title, Description) VALUES (@accId, @gameId, @title, @desc)";
                                cmd.Parameters.AddWithValue("@accId", user.Id);
                                cmd.Parameters.AddWithValue("@gameId", coaching.GameId);
                                cmd.Parameters.AddWithValue("@title", FAQElement.Title);
                                cmd.Parameters.AddWithValue("@desc", FAQElement.Description);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }

                    conn.Close();
                }
            }

            return Json(new { Auth = auth ? "OK" : "NO_AUTH", success, error, error_code });
        }

        // PUT: api/Coach/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE: api/Coach/5
        public void Delete(int id)
        {
        }



        private bool EnsureUserRights(UserLevel rights)
        {
            if (HttpContext.Current.Session["user"] != null)
            {
                if ((HttpContext.Current.Session["user"] as User).HasRole(rights)) // REQUIRES ADMIN ROLE
                {
                    return true;
                }
            }
            return false;
        }
        private User GetCurrentUser()
        {
            return (User)HttpContext.Current.Session["user"];
        }
    }
}
