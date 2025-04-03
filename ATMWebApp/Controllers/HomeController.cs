using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ATMWebApp.Models;
using MySql.Data.MySqlClient;
using System.Text;
using System.Text.RegularExpressions;

namespace ATMWebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly string connectionString = "server=localhost;database=u520535046_atm_db;user=u520535046_atm_db;password=;Atmcard246";
        

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
            if (string.IsNullOrEmpty(pin))
            {
                TempData["Error"] = "Please enter your PIN.";
                return RedirectToAction("PinEntry");
            }

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                // ✅ Validate both card number and PIN
                string query = "SELECT card_number FROM users WHERE pin = @pin LIMIT 1";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@pin", pin);

                    object result = cmd.ExecuteScalar(); // Get card number if exists

                    if (result != null)
                    {
                        string cardNumber = result.ToString();
                        HttpContext.Session.SetString("CardNumber", cardNumber); // Store session
                        return RedirectToAction("MainMenu"); // Redirect to main menu
                    }
                    else
                    {
                        TempData["Error"] = "Invalid PIN. Please try again.";
                    }
                }
            }

            return RedirectToAction("PinEntry");
        }

        public IActionResult MainMenu()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("CardNumber")))
            {
                return RedirectToAction("PinEntry");
            }
            return View();
        }

        public IActionResult Withdraw()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("CardNumber")))
            {
                return RedirectToAction("PinEntry");
            }
            return View();
        }
        public IActionResult WithdrawFunds(decimal amount, string accountType)
        {
            string cardNumber = HttpContext.Session.GetString("CardNumber");

            if (string.IsNullOrEmpty(cardNumber))
            {
                return RedirectToAction("PinEntry");
            }

            if (amount <= 0)
            {
                TempData["Error"] = "Withdrawal amount must be greater than zero.";
                return RedirectToAction("Withdraw"); // Redirect to Withdraw page with error message
            }

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                string balanceQuery = accountType == "Savings"
                    ? "SELECT savings_balance FROM users WHERE card_number = @cardNumber"
                    : "SELECT current_balance FROM users WHERE card_number = @cardNumber";

                MySqlCommand balanceCmd = new MySqlCommand(balanceQuery, conn);
                balanceCmd.Parameters.AddWithValue("@cardNumber", cardNumber);
                object balanceResult = balanceCmd.ExecuteScalar();

                if (balanceResult == null)
                {
                    TempData["Error"] = "Account not found.";
                    return RedirectToAction("Withdraw");
                }

                decimal balance = Convert.ToDecimal(balanceResult);

                if (balance < amount)
                {
                    TempData["Error"] = "Insufficient balance.";
                    return RedirectToAction("Withdraw");
                }

                // Update balance in the database
                string updateQuery = accountType == "Savings"
                    ? "UPDATE users SET savings_balance = savings_balance - @amount WHERE card_number = @cardNumber"
                    : "UPDATE users SET current_balance = current_balance - @amount WHERE card_number = @cardNumber";

                MySqlCommand updateCmd = new MySqlCommand(updateQuery, conn);
                updateCmd.Parameters.AddWithValue("@amount", amount);
                updateCmd.Parameters.AddWithValue("@cardNumber", cardNumber);
                updateCmd.ExecuteNonQuery();

                // Log transaction after successful withdrawal with account type
                string transactionQuery = "INSERT INTO transactions (card_number, date, type, amount, balance, account_type) VALUES (@cardNumber, @date, @type, @amount, @balance, @accountType)";
                MySqlCommand transactionCmd = new MySqlCommand(transactionQuery, conn);
                transactionCmd.Parameters.AddWithValue("@cardNumber", cardNumber);
                transactionCmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                transactionCmd.Parameters.AddWithValue("@type", "Withdrawal");
                transactionCmd.Parameters.AddWithValue("@amount", -amount);
                transactionCmd.Parameters.AddWithValue("@balance", balance - amount);
                transactionCmd.Parameters.AddWithValue("@accountType", accountType); // Save Current/Savings account type
                transactionCmd.ExecuteNonQuery();

                TempData["Message"] = "Withdrawal successful!";
            }

            return RedirectToAction("Withdraw"); // Redirect back to the Withdraw page
        }

        private (decimal, decimal) GetBalanceFromDatabase(string cardNumber)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT current_balance, savings_balance FROM users WHERE card_number = @cardNumber";

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@cardNumber", cardNumber);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read()) // Check if data exists
                        {
                            decimal currentBalance = reader.GetDecimal(0);
                            decimal savingsBalance = reader.GetDecimal(1);
                            return (currentBalance, savingsBalance); // Return tuple
                        }
                    }
                }
            }

            return (0, 0); // Return (0,0) if no data is found
        }

        public IActionResult CheckBalance()
        {
            string cardNumber = HttpContext.Session.GetString("CardNumber");

            if (string.IsNullOrEmpty(cardNumber))
            {
                return RedirectToAction("PinEntry"); // Redirect if not logged in
            }

            var balances = GetBalanceFromDatabase(cardNumber);

            ViewBag.CurrentBalance = balances.Item1; // Current balance
            ViewBag.SavingsBalance = balances.Item2; // Savings balance

            return View();
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
        public IActionResult TransferFunds(string recipientCard, decimal amount, string accountType)
        {
            string senderCard = HttpContext.Session.GetString("CardNumber");

            if (string.IsNullOrEmpty(senderCard))
            {
                return RedirectToAction("PinEntry");
            }

            // Prevent self-transfer
            if (senderCard == recipientCard)
            {
                TempData["Error"] = "You cannot transfer funds to your own account!";
                return RedirectToAction("TransferFunds");
            }

            // Define transfer limits
            decimal minTransfer = 100;    // Minimum transfer limit
            decimal maxTransfer = 50000;  // Maximum transfer limit

            if (amount < minTransfer || amount > maxTransfer)
            {
                TempData["Error"] = $"Transfer amount must be between ₱{minTransfer} and ₱{maxTransfer}.";
                return RedirectToAction("TransferFunds");
            }

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                string balanceField = accountType == "Savings" ? "savings_balance" : "current_balance";

                // Fetch sender's balance
                string senderBalanceQuery = $"SELECT {balanceField} FROM users WHERE card_number = @cardNumber";
                MySqlCommand senderBalanceCmd = new MySqlCommand(senderBalanceQuery, conn);
                senderBalanceCmd.Parameters.AddWithValue("@cardNumber", senderCard);
                object senderBalanceResult = senderBalanceCmd.ExecuteScalar();

                if (senderBalanceResult == null)
                {
                    TempData["Error"] = "Sender account not found.";
                    return RedirectToAction("TransferFunds");
                }

                decimal senderBalance = Convert.ToDecimal(senderBalanceResult);

                // Check if sender has enough funds
                if (senderBalance < amount)
                {
                    TempData["Error"] = "Insufficient funds.";
                    return RedirectToAction("TransferFunds");
                }

                // Fetch recipient's balance
                string recipientBalanceQuery = $"SELECT {balanceField} FROM users WHERE card_number = @recipientCard";
                MySqlCommand recipientBalanceCmd = new MySqlCommand(recipientBalanceQuery, conn);
                recipientBalanceCmd.Parameters.AddWithValue("@recipientCard", recipientCard);
                object recipientBalanceResult = recipientBalanceCmd.ExecuteScalar();

                if (recipientBalanceResult == null)
                {
                    TempData["Error"] = "Recipient account not found.";
                    return RedirectToAction("TransferFunds");
                }

                decimal recipientBalance = Convert.ToDecimal(recipientBalanceResult);

                // Begin database transaction
                using (MySqlTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // Deduct from sender
                        string updateSenderQuery = $"UPDATE users SET {balanceField} = {balanceField} - @amount WHERE card_number = @cardNumber";
                        MySqlCommand updateSenderCmd = new MySqlCommand(updateSenderQuery, conn, transaction);
                        updateSenderCmd.Parameters.AddWithValue("@amount", amount);
                        updateSenderCmd.Parameters.AddWithValue("@cardNumber", senderCard);
                        updateSenderCmd.ExecuteNonQuery();

                        // Add to recipient
                        string updateRecipientQuery = $"UPDATE users SET {balanceField} = {balanceField} + @amount WHERE card_number = @recipientCard";
                        MySqlCommand updateRecipientCmd = new MySqlCommand(updateRecipientQuery, conn, transaction);
                        updateRecipientCmd.Parameters.AddWithValue("@amount", amount);
                        updateRecipientCmd.Parameters.AddWithValue("@recipientCard", recipientCard);
                        updateRecipientCmd.ExecuteNonQuery();

                        // Get updated balances
                        senderBalanceCmd = new MySqlCommand(senderBalanceQuery, conn, transaction);
                        senderBalanceCmd.Parameters.AddWithValue("@cardNumber", senderCard);
                        senderBalance = Convert.ToDecimal(senderBalanceCmd.ExecuteScalar());

                        recipientBalanceCmd = new MySqlCommand(recipientBalanceQuery, conn, transaction);
                        recipientBalanceCmd.Parameters.AddWithValue("@recipientCard", recipientCard);
                        recipientBalance = Convert.ToDecimal(recipientBalanceCmd.ExecuteScalar());

                        // Log transaction for sender
                        string senderTransactionQuery = "INSERT INTO transactions (card_number, date, type, amount, balance, account_type) VALUES (@cardNumber, @date, @type, @amount, @balance, @accountType)";
                        MySqlCommand senderTransactionCmd = new MySqlCommand(senderTransactionQuery, conn, transaction);
                        senderTransactionCmd.Parameters.AddWithValue("@cardNumber", senderCard);
                        senderTransactionCmd.Parameters.AddWithValue("@date", DateTime.Now);
                        senderTransactionCmd.Parameters.AddWithValue("@type", "Transfer");
                        senderTransactionCmd.Parameters.AddWithValue("@amount", -amount); // Negative for sender
                        senderTransactionCmd.Parameters.AddWithValue("@balance", senderBalance);
                        senderTransactionCmd.Parameters.AddWithValue("@accountType", accountType);
                        senderTransactionCmd.ExecuteNonQuery();

                        // Log transaction for recipient
                        string recipientTransactionQuery = "INSERT INTO transactions (card_number, date, type, amount, balance, account_type) VALUES (@cardNumber, @date, @type, @amount, @balance, @accountType)";
                        MySqlCommand recipientTransactionCmd = new MySqlCommand(recipientTransactionQuery, conn, transaction);
                        recipientTransactionCmd.Parameters.AddWithValue("@cardNumber", recipientCard);
                        recipientTransactionCmd.Parameters.AddWithValue("@date", DateTime.Now);
                        recipientTransactionCmd.Parameters.AddWithValue("@type", "Transfer");
                        recipientTransactionCmd.Parameters.AddWithValue("@amount", amount);
                        recipientTransactionCmd.Parameters.AddWithValue("@balance", recipientBalance);
                        recipientTransactionCmd.Parameters.AddWithValue("@accountType", accountType);
                        recipientTransactionCmd.ExecuteNonQuery();

                        // Commit transaction
                        transaction.Commit();

                        TempData["Message"] = "Transfer successful!";
                        return RedirectToAction("TransferFunds");
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        TempData["Error"] = "Transfer failed. Please try again.";
                        return RedirectToAction("TransferFunds");
                    }
                }
            }
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

            // ✅ Ensure new PIN is exactly 6 digits
            if (!Regex.IsMatch(newPin, @"^\d{6}$"))
            {
                TempData["Error"] = "New PIN must be exactly 6 digits!";
                return RedirectToAction("ChangePin");
            }

            if (newPin != confirmNewPin)
            {
                TempData["Error"] = "New PINs do not match!";
                return RedirectToAction("ChangePin");
            }

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                // ✅ Retrieve current PIN from the database
                string getPinQuery = "SELECT pin FROM users WHERE card_number = @cardNumber";
                string storedPin;

                using (var cmd = new MySqlCommand(getPinQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@cardNumber", cardNumber);
                    storedPin = cmd.ExecuteScalar()?.ToString();
                }

                // ✅ Check if the current PIN is correct
                if (storedPin != currentPin)
                {
                    TempData["Error"] = "Current PIN is incorrect!";
                    return RedirectToAction("ChangePin");
                }

                // ✅ Prevent setting the same PIN
                if (storedPin == newPin)
                {
                    TempData["Error"] = "New PIN must be different from the current PIN!";
                    return RedirectToAction("ChangePin");
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
                return RedirectToAction("PinEntry");
            }

            List<Transaction> transactions = new List<Transaction>();

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT date, type, amount, balance, account_type FROM transactions WHERE card_number = @cardNumber ORDER BY date DESC LIMIT 10";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@cardNumber", cardNumber);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        transactions.Add(new Transaction
                        {
                            Date = reader.GetDateTime("date"),
                            Type = reader.GetString("type"),
                            Amount = reader.GetDecimal("amount"),
                            Balance = reader.GetDecimal("balance"),
                            AccountType = reader.GetString("account_type") // Get account type
                        });
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
        public IActionResult DepositFunds(decimal amount, string accountType)
        {
            string cardNumber = HttpContext.Session.GetString("CardNumber");

            if (string.IsNullOrEmpty(cardNumber))
            {
                return RedirectToAction("PinEntry");
            }

            // Validate account type
            if (accountType != "Savings" && accountType != "Current")
            {
                TempData["Message"] = "Invalid account type selected.";
                return RedirectToAction("DepositCash");
            }

            // Define deposit limits
            decimal minDeposit = 100;     // Minimum deposit amount
            decimal maxDeposit = 100000;  // Maximum deposit amount

            if (amount < minDeposit || amount > maxDeposit)
            {
                TempData["Message"] = $"Deposit amount must be between ₱{minDeposit} and ₱{maxDeposit}.";
                return RedirectToAction("DepositCash");
            }

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                // Fetch current balance
                string balanceField = accountType == "Savings" ? "savings_balance" : "current_balance";
                string balanceQuery = $"SELECT {balanceField} FROM users WHERE card_number = @cardNumber";

                MySqlCommand balanceCmd = new MySqlCommand(balanceQuery, conn);
                balanceCmd.Parameters.AddWithValue("@cardNumber", cardNumber);
                object balanceResult = balanceCmd.ExecuteScalar();

                if (balanceResult == null)
                {
                    TempData["Message"] = "Account not found.";
                    return RedirectToAction("DepositCash");
                }

                decimal balance = Convert.ToDecimal(balanceResult);

                using (MySqlTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // Update balance
                        string updateQuery = $"UPDATE users SET {balanceField} = {balanceField} + @amount WHERE card_number = @cardNumber";
                        MySqlCommand updateCmd = new MySqlCommand(updateQuery, conn, transaction);
                        updateCmd.Parameters.AddWithValue("@amount", amount);
                        updateCmd.Parameters.AddWithValue("@cardNumber", cardNumber);
                        updateCmd.ExecuteNonQuery();

                        // Get updated balance
                        balanceCmd = new MySqlCommand(balanceQuery, conn, transaction);
                        balanceCmd.Parameters.AddWithValue("@cardNumber", cardNumber);
                        balance = Convert.ToDecimal(balanceCmd.ExecuteScalar());

                        // Log transaction
                        string transactionQuery = "INSERT INTO transactions (card_number, date, type, amount, balance, account_type) VALUES (@cardNumber, @date, @type, @amount, @balance, @accountType)";
                        MySqlCommand transactionCmd = new MySqlCommand(transactionQuery, conn, transaction);
                        transactionCmd.Parameters.AddWithValue("@cardNumber", cardNumber);
                        transactionCmd.Parameters.AddWithValue("@date", DateTime.Now);
                        transactionCmd.Parameters.AddWithValue("@type", "Deposit");
                        transactionCmd.Parameters.AddWithValue("@amount", amount);
                        transactionCmd.Parameters.AddWithValue("@balance", balance);
                        transactionCmd.Parameters.AddWithValue("@accountType", accountType);
                        transactionCmd.ExecuteNonQuery();

                        // Commit transaction
                        transaction.Commit();

                        TempData["Message"] = "Deposit successful!";
                        return RedirectToAction("DepositCash");
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        TempData["Message"] = "Deposit failed. Please try again.";
                        return RedirectToAction("DepositCash");
                    }
                }
            }
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




   
