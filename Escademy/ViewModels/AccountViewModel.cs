using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Escademy.ViewModels
{
    public class AccountViewModel
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public int Level { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int verified { get; set; }
        public string ProfilePicture { get; set; }
        public string Description { get; set; }
        public string Country { get; set; }
    }
}