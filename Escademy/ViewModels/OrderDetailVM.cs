using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Escademy.ViewModels
{
    public class OrderDetailVM
    {
        public string GamePicture { get; set; }
        public string BuyerName { get; set; }
        public string BuyerEmail { get; set; }
        public string FirstLetter_BuyerName { get; set; }
        public decimal Price { get; set;}
        public string OrderDate { get; set; }
        public string OrderStatus { get; set; }
    }
}