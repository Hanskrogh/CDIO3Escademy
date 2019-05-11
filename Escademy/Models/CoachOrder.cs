using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Escademy.Models
{
    public class CoachOrder
    {
        public int Id { get; set; }
        public string txn_id { get; set; }
        public decimal mc_gross { get; set; }
        public string mc_currency { get; set; }
        public string payer_mail { get; set; }
        public int payer_account_id { get; set; }
        public int receiver_id { get; set; }
        public int game_id { get; set; }
        public bool success { get; set; }
        public DateTime date { get; set; }
        public int quantity { get; set; }
    }
}