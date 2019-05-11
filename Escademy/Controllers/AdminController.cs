using Escademy.Models;
using System.Web.Mvc;

namespace Escademy.Controllers
{
    public class AdminController : Controller
    {
        // GET: Admin
        public ActionResult Index()
        {
            User user = null;
            if (Session["user"] != null)
            {
                user = (User)Session["user"];
            }

            if (user != null && user.HasRole(UserLevel.Admin))
            {
                return View();
            } else
            {
                return RedirectToAction("Login", "Auth"); // no permission
            }
        }
    }
}