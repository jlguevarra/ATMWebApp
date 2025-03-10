namespace ATMWebApp.Models
{
    public class Transaction
    {
        public string Date { get; set; }
        public string Type { get; set; }
        public decimal Amount { get; set; }
        public decimal Balance { get; set; }
    }
}
