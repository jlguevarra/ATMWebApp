using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ATMWebApp.Models;
using MySql.Data.MySqlClient;

namespace ATMWebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly string connectionString = "server=localhost;database=atm_db;user=root;password=;";
        

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult PinEntry()
        {
            return View();
        }

        [HttpPost]
        public IActionResult PinEntry(string pin)
        {
            string cardNumber = GetCardNumberFromDatabase(pin);

            if (!string.IsNullOrEmpty(cardNumber))
            {
                HttpContext.Session.SetString("CardNumber", cardNumber);
                return RedirectToAction("MainMenu");
            }
            else
            {
                ViewBag.ErrorMessage = "Invalid PIN.";
                return View();
            }
        }

        public IActionResult MainMenu()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("CardNumber")))
            {
                return RedirectToAction("PinEntry");
            }
            return View();
        }

        // ✅ Withdraw GET Request
        public IActionResult Withdraw()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("CardNumber")))
            {
                return RedirectToAction("PinEntry");
            }
            return View();
        }

        // ✅ Withdraw POST Request (Processing Withdrawal)
        [HttpPost]
        public IActionResult Withdraw(decimal amount)
        {
            string cardNumber = HttpContext.Session.GetString("CardNumber");

            if (string.IsNullOrEmpty(cardNumber))
            {
                return RedirectToAction("PinEntry");
            }

            if (amount <= 0)
            {
                ViewBag.Message = "Withdrawal amount must be greater than zero.";
                return View();
            }

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                // 1️⃣ Get current balance
                string checkBalanceQuery = "SELECT balance FROM users WHERE card_number = @cardNumber";
                MySqlCommand checkCmd = new MySqlCommand(checkBalanceQuery, conn);
                checkCmd.Parameters.AddWithValue("@cardNumber", cardNumber);
                object result = checkCmd.ExecuteScalar();

                if (result == null)
                {
                    ViewBag.Message = "Account not found.";
                    return View();
                }

                decimal currentBalance = Convert.ToDecimal(result);

                // 2️⃣ Check if sufficient balance
                if (currentBalance < amount)
                {
                    ViewBag.Message = "Insufficient balance.";
                    return View();
                }

                decimal newBalance = currentBalance - amount;

                // 3️⃣ Update the balance
                string updateBalanceQuery = "UPDATE users SET balance = @newBalance WHERE card_number = @cardNumber";
                MySqlCommand updateCmd = new MySqlCommand(updateBalanceQuery, conn);
                updateCmd.Parameters.AddWithValue("@newBalance", newBalance);
                updateCmd.Parameters.AddWithValue("@cardNumber", cardNumber);
                updateCmd.ExecuteNonQuery();

                // 4️⃣ Log transaction
                string insertTransactionQuery = "INSERT INTO transactions (card_number, type, amount, balance) VALUES (@cardNumber, 'Withdrawal', @amount, @newBalance)";
                MySqlCommand insertCmd = new MySqlCommand(insertTransactionQuery, conn);
                insertCmd.Parameters.AddWithValue("@cardNumber", cardNumber);
                insertCmd.Parameters.AddWithValue("@amount", amount);
                insertCmd.Parameters.AddWithValue("@newBalance", newBalance); // Corrected variable
                insertCmd.ExecuteNonQuery();

                ViewBag.Message = $"Withdrawal successful! New balance: {newBalance:C}";
            }

            return View();
        }



        public IActionResult CheckBalance()
        {
            string cardNumber = HttpContext.Session.GetString("CardNumber");

            if (string.IsNullOrEmpty(cardNumber))
            {
                return RedirectToAction("PinEntry"); // Redirect if not logged in
            }

            decimal balance = GetBalanceFromDatabase(cardNumber);

            if (balance < 0)
            {
                TempData["Error"] = "Error retrieving balance.";
                return RedirectToAction("MainMenu");
            }

            ViewBag.Balance = balance;
            return View();
        }
        private decimal GetBalanceFromDatabase(string cardNumber)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT balance FROM users WHERE card_number = @cardNumber";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@cardNumber", cardNumber);
                    object result = cmd.ExecuteScalar();
                    return result != null ? Convert.ToDecimal(result) : -1; // Return -1 if no balance found
                }
            }
        }

        public IActionResult TransferFunds()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("CardNumber")))
            {
                return RedirectToAction("PinEntry");
            }
            return View();
        }

        [HttpPost]
        public IActionResult TransferFunds(string recipient, decimal amount)
        {
            string senderCardNumber = HttpContext.Session.GetString("CardNumber");

            if (string.IsNullOrEmpty(senderCardNumber))
            {
                return RedirectToAction("PinEntry"); // Redirect if not logged in
            }

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                // ✅ Check if recipient exists
                string recipientQuery = "SELECT balance FROM users WHERE card_number = @recipient";
                using (var recipientCmd = new MySqlCommand(recipientQuery, connection))
                {
                    recipientCmd.Parameters.AddWithValue("@recipient", recipient);
                    var recipientBalanceObj = recipientCmd.ExecuteScalar();

                    if (recipientBalanceObj == null)
                    {
                        TempData["Error"] = "Recipient card number not found!";
                        return RedirectToAction("TransferFunds");
                    }

                    // ✅ Get sender's balance
                    string senderQuery = "SELECT balance FROM users WHERE card_number = @senderCardNumber";
                    using (var senderCmd = new MySqlCommand(senderQuery, connection))
                    {
                        senderCmd.Parameters.AddWithValue("@senderCardNumber", senderCardNumber);
                        var senderBalanceObj = senderCmd.ExecuteScalar();

                        if (senderBalanceObj != null)
                        {
                            decimal senderBalance = Convert.ToDecimal(senderBalanceObj);

                            // ✅ Check if sender has enough funds
                            if (amount > 0 && amount <= senderBalance)
                            {
                                // ✅ Deduct from sender
                                string deductQuery = "UPDATE users SET balance = balance - @amount WHERE card_number = @senderCardNumber";
                                using (var deductCmd = new MySqlCommand(deductQuery, connection))
                                {
                                    deductCmd.Parameters.AddWithValue("@amount", amount);
                                    deductCmd.Parameters.AddWithValue("@senderCardNumber", senderCardNumber);
                                    deductCmd.ExecuteNonQuery();
                                }

                                // ✅ Add to recipient
                                string addQuery = "UPDATE users SET balance = balance + @amount WHERE card_number = @recipient";
                                using (var addCmd = new MySqlCommand(addQuery, connection))
                                {
                                    addCmd.Parameters.AddWithValue("@amount", amount);
                                    addCmd.Parameters.AddWithValue("@recipient", recipient);
                                    addCmd.ExecuteNonQuery();
                                }

                                TempData["Message"] = $"Successfully transferred {amount:C} to {recipient}.";
                            }
                            else
                            {
                                TempData["Error"] = "Insufficient funds or invalid amount!";
                            }
                        }
                    }
                }
            }

            return RedirectToAction("TransferFunds");
        }


        public IActionResult ChangePin()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("CardNumber")))
            {
                return RedirectToAction("PinEntry");
            }
            return View();
        }
        [HttpPost]
        public IActionResult ChangePin(string currentPin, string newPin, string confirmNewPin)
        {
            string cardNumber = HttpContext.Session.GetString("CardNumber");

            if (string.IsNullOrEmpty(cardNumber))
            {
                return RedirectToAction("PinEntry"); // Redirect to login if session expired
            }

            if (newPin != confirmNewPin)
            {
                TempData["Error"] = "New PINs do not match!";
                return RedirectToAction("ChangePin");
            }

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                // ✅ Verify current PIN
                string query = "SELECT COUNT(*) FROM users WHERE card_number = @cardNumber AND pin = @currentPin";
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@cardNumber", cardNumber);
                    cmd.Parameters.AddWithValue("@currentPin", currentPin);
                    int count = Convert.ToInt32(cmd.ExecuteScalar());

                    if (count == 0)
                    {
                        TempData["Error"] = "Current PIN is incorrect!";
                        return RedirectToAction("ChangePin");
                    }
                }

                // ✅ Update PIN
                string updateQuery = "UPDATE users SET pin = @newPin WHERE card_number = @cardNumber";
                using (var updateCmd = new MySqlCommand(updateQuery, connection))
                {
                    updateCmd.Parameters.AddWithValue("@newPin", newPin);
                    updateCmd.Parameters.AddWithValue("@cardNumber", cardNumber);
                    updateCmd.ExecuteNonQuery();
                }

                TempData["Message"] = "PIN successfully changed!";
            }

            return RedirectToAction("ChangePin");
        }


      
        public IActionResult MiniStatement()
        {
            string cardNumber = HttpContext.Session.GetString("CardNumber");

            if (string.IsNullOrEmpty(cardNumber))
            {
                return RedirectToAction("PinEntry"); // Redirect to login if session expired
            }

            List<Transaction> transactions = new List<Transaction>();

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                // ✅ Get the last 5 transactions for the user
                string query = "SELECT date, type, amount, balance FROM transactions WHERE card_number = @cardNumber ORDER BY date DESC LIMIT 5";
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@cardNumber", cardNumber);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            transactions.Add(new Transaction
                            {
                                Date = reader.GetDateTime("date").ToString("yyyy-MM-dd HH:mm:ss"),
                                Type = reader.GetString("type"),
                                Amount = reader.GetDecimal("amount"),
                                Balance = reader.GetDecimal("balance")
                            });
                        }
                    }
                }
            }

            ViewBag.Transactions = transactions;
            return View();
        }



        public IActionResult DepositCash()
        {
            // Check if the user is authenticated (CardNumber stored in session)
            string cardNumber = HttpContext.Session.GetString("CardNumber");

            if (string.IsNullOrEmpty(cardNumber))
            {
                return RedirectToAction("PinEntry");
            }

            return View();
        }

        [HttpPost]
        public IActionResult Deposit(decimal amount)
        {
            // Retrieve card number from session
            string cardNumber = HttpContext.Session.GetString("CardNumber");

            if (string.IsNullOrEmpty(cardNumber))
            {
                return RedirectToAction("PinEntry");
            }

            if (amount <= 0)
            {
                TempData["Error"] = "Deposit amount must be greater than zero.";
                return RedirectToAction("DepositCash");
            }

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();

            
                string checkAccountQuery = "SELECT balance FROM users WHERE card_number = @cardNumber";
                using (MySqlCommand checkCmd = new MySqlCommand(checkAccountQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@cardNumber", cardNumber);
                    object result = checkCmd.ExecuteScalar();

                    if (result == null)
                    {
                        TempData["Error"] = "Account not found.";
                        return RedirectToAction("DepositCash");
                    }

                    // Get current balance
                    decimal currentBalance = Convert.ToDecimal(result);
                    decimal newBalance = currentBalance + amount;

                    // Update the balance
                    string updateQuery = "UPDATE users SET balance = @newBalance WHERE card_number = @cardNumber";
                    using (MySqlCommand updateCmd = new MySqlCommand(updateQuery, conn))
                    {
                        updateCmd.Parameters.AddWithValue("@newBalance", newBalance);
                        updateCmd.Parameters.AddWithValue("@cardNumber", cardNumber);
                        updateCmd.ExecuteNonQuery();
                    }

                    TempData["Message"] = $"Deposit successful! New balance: {newBalance:C}";
                }
            }

            return RedirectToAction("DepositCash");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("PinEntry");
        }

        private string GetCardNumberFromDatabase(string pin)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT card_number FROM users WHERE pin = @pin";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@pin", pin);
                    object result = cmd.ExecuteScalar();
                    return result?.ToString();
                }
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}




   
