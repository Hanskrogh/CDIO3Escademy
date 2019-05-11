using Escademy.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Escademy.Controllers
{
    public class BaseController : Controller
    {
        public User GetCurrentUser()
        {
            return (User)Session["user"];
        }
    }
}