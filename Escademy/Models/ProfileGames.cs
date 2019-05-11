using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Escademy.Models
{
    public class ProfileGames
    {
        public int accountId { get; set; }
        public int gameId { get; set; }
        public decimal SalaryUSD { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
    }
}