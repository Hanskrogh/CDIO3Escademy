using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Escademy.ViewModels
{
    public class PriceHourModel
    {
        public string Hour { get; set; }
        public string Price { get; set; }

        public static List<PriceHourModel> GetPriceHours()
        {
            List<PriceHourModel> listOfHours = new List<PriceHourModel>();
            listOfHours.Add(new PriceHourModel() { Hour = "1", Price = "" });
            listOfHours.Add(new PriceHourModel() { Hour = "2", Price = "" });
            listOfHours.Add(new PriceHourModel() { Hour = "3", Price = "" });
            listOfHours.Add(new PriceHourModel() { Hour = "4", Price = "" });
            listOfHours.Add(new PriceHourModel() { Hour = "5", Price = "" });
            listOfHours.Add(new PriceHourModel() { Hour = "6", Price = "" });
            listOfHours.Add(new PriceHourModel() { Hour = "7", Price = "" });
            listOfHours.Add(new PriceHourModel() { Hour = "8", Price = "" });
            listOfHours.Add(new PriceHourModel() { Hour = "9", Price = "" });
            listOfHours.Add(new PriceHourModel() { Hour = "10", Price = "" });
            listOfHours.Add(new PriceHourModel() { Hour = "11", Price = "" });
            listOfHours.Add(new PriceHourModel() { Hour = "12", Price = "" });
            return listOfHours;
        }
    }
}