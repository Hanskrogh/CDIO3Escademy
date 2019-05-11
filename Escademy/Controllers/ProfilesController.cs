using Escademy.Models;
using MySql.Data.MySqlClient;
using System.Web.Mvc;

namespace Escademy.Controllers
{
    public class ProfilesController : Controller
    {
        // GET: Profiles
        
        public ActionResult Index()
        {
            return RedirectToAction("Profile");
        }

        public ActionResult EditProfile()
        {
            return View();
        } 

        public new ActionResult Profile(int id=0)
        {
            ViewBag.id = id;
            using (var conn = new MySqlConnection(ConnectionString.Get("EscademyMDB")))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM esc_accounts WHERE id=@userId";
                    cmd.Parameters.AddWithValue("@userId", (int)ViewBag.id);
                    var reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        var user = new User()
                        {
                            Id = reader.GetInt32("Id"),
                            Email = reader.GetString("Email"),
                            FirstName = reader.GetString("FirstName"),
                            LastName = reader.GetString("LastName"),
                            Level = reader.GetInt32("Level"),
                            Picture = reader.GetString("ProfilePicture"),
                            Description = reader.GetString("Description"),
                            Country = reader.GetString("Country")
                        };

                        if (user.Picture.Length > 0)
                        {
                            user.Picture = $"data:image/png;base64,{user.Picture}";
                        } else
                        {
                            user.Picture = "/Content/Images/portrait_2.png";
                        }


                        ViewBag.user = user;
                    } else { ViewBag.user = null; }

                }
                conn.Close();
            }
            
            return View();
        }

        public ActionResult Earnings()
        {
            return View();
        }
    }
}