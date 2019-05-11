using Escademy.Dal;
using Escademy.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Escademy.ViewModels
{
    public class CoachingVM : BaseVM
    {
        public int AccountId { get; set; }
        public decimal SalaryUSD { get; set; }
        public string Game { get; set; }
        public int GameId { get; set; }
        public int ServiceTypeId { get; set; }
        public string Abbreviation { get; set; }
        public string Picture { get; set; }
        public string Description { get; set; }
        public string Title { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Country { get; set; }
        public string UserPicture { get; set; }
        public string UserDescription { get; set; }

        public List<esc_profilegamesPricing> Pricings { get; set; }
        public List<esc_faq> Faqs { get; set; }
        public List<PriceHourModel> PriceHourModel { get; set; }
        public List<esc_profilegamesFiles> Files { get; set; }
        //public List<CoachingFAQ> FAQElements { get; set; }
    }
}