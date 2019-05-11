using Escademy.Models;
using System.Web;
using System.Web.Http;

namespace Escademy.Controllers
{
    public class TransactionController : ApiController
    {
        [HttpGet]
        public object GetBalance()
        {
            decimal balance = 0.0M;
            bool auth = false;

            if (EnsureUserRights(UserLevel.Coach))
            {
                auth = true;
                var user = GetCurrentUser();

                using (var conn = new MySql.Data.MySqlClient.MySqlConnection(ConnectionString.Get("EscademyMDB")))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT Status, Amount FROM esc_Transactions WHERE ReceiverId=@accId";
                        cmd.Parameters.AddWithValue("@accId", user.Id);
                        var reader = cmd.ExecuteReader();

                        while (reader.Read())
                        {
                            if (reader.GetString("Status") == "Pending")
                            {
                                balance += reader.GetDecimal("Amount");
                            }
                        }
                    }
                    conn.Close();
                }
            }

            return Json(new { Auth = auth ? "OK" : "NO_AUTH", Balance = balance });
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
