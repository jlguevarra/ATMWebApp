using System;
using System.Data;
using MySql.Data.MySqlClient;

namespace ATMWebApp.Models
{
    public class DatabaseHelper
    {
        private string connectionString = "server=localhost;database=atm_db;user=root;password=;";

        public bool ValidateUser(string cardNumber, string pin)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM users WHERE card_number = @cardNumber AND pin = @pin";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@cardNumber", cardNumber);
                    cmd.Parameters.AddWithValue("@pin", pin);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        return reader.HasRows; // Returns true if user exists
                    }
                }
            }
        }
    }
}
