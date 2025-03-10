using System.ComponentModel.DataAnnotations;

namespace ATMWebApp.Models
{
    public class Account
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string CardNumber { get; set; }

        [Required]
        public string Pin { get; set; }

        public decimal Balance { get; set; }
    }
}
