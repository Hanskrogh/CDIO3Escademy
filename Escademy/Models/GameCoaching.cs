namespace Escademy.Models
{
    public class GameCoaching
    {
        public int AccountId { get; set; }
        public decimal SalaryUSD { get; set; }
        public string Game { get; set; }
        public int GameId { get; set; }
        public string Abbreviation { get; set; }
        public string Picture { get; set; }
        public string Description { get; set; }
        public string Title { get; set; }

        public CoachingFAQ[] FAQElements { get; set; }
    }
}