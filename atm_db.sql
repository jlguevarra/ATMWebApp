-- phpMyAdmin SQL Dump
-- version 5.2.1
-- https://www.phpmyadmin.net/
--
-- Host: 127.0.0.1
-- Generation Time: Mar 10, 2025 at 03:42 AM
-- Server version: 10.4.32-MariaDB
-- PHP Version: 8.0.30

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Database: `atm_db`
--

-- --------------------------------------------------------

--
-- Table structure for table `transactions`
--

CREATE TABLE `transactions` (
  `id` int(11) NOT NULL,
  `card_number` varchar(16) NOT NULL,
  `date` datetime DEFAULT current_timestamp(),
  `type` enum('Deposit','Withdrawal','Transfer') NOT NULL,
  `amount` decimal(10,2) NOT NULL,
  `balance` decimal(10,2) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `transactions`
--

INSERT INTO `transactions` (`id`, `card_number`, `date`, `type`, `amount`, `balance`) VALUES
(1, '1234567812345678', '2025-03-05 21:04:48', 'Deposit', 5000.00, 5000.00),
(2, '1234567812345678', '2025-03-04 21:04:48', 'Withdrawal', 1000.00, 4000.00),
(3, '1234567812345678', '2025-03-03 21:04:48', 'Deposit', 2000.00, 6000.00),
(4, '1234567812345678', '2025-03-02 21:04:48', 'Transfer', 500.00, 5500.00),
(5, '1234567812345678', '2025-03-01 21:04:48', 'Withdrawal', 1500.00, 4000.00),
(6, '8765432112345678', '2025-03-05 21:17:01', 'Deposit', 2000.00, 2000.00),
(7, '8765432112345678', '2025-03-04 21:17:01', 'Withdrawal', 500.00, 1500.00),
(8, '8765432112345678', '2025-03-03 21:17:01', 'Deposit', 1000.00, 2000.00),
(9, '8765432112345678', '2025-03-02 21:17:01', 'Withdrawal', 200.00, 1000.00),
(10, '8765432112345678', '2025-03-01 21:17:01', 'Deposit', 1000.00, 1200.00),
(11, '1234567812345678', '2025-03-05 23:00:57', 'Withdrawal', 100.00, 0.00),
(12, '1234567812345678', '2025-03-05 23:02:19', 'Withdrawal', 100.00, 0.00),
(13, '1234567812345678', '2025-03-05 23:02:52', 'Withdrawal', 100.00, 0.00),
(14, '1234567812345678', '2025-03-05 23:03:22', 'Withdrawal', 100.00, 0.00),
(15, '1234567812345678', '2025-03-05 23:05:58', 'Withdrawal', 100.00, 0.00),
(16, '1234567812345678', '2025-03-05 23:12:42', 'Withdrawal', 100.00, 5800.00),
(17, '1234567812345678', '2025-03-05 23:12:58', 'Withdrawal', 200.00, 5600.00),
(18, '1234567812345678', '2025-03-05 23:13:05', 'Withdrawal', 300.00, 5300.00),
(19, '1234567812345678', '2025-03-05 23:13:08', 'Withdrawal', 400.00, 4900.00),
(20, '1234567812345678', '2025-03-05 23:13:13', 'Withdrawal', 900.00, 4000.00),
(21, '1234567812345678', '2025-03-05 23:51:04', 'Withdrawal', 100.00, 3900.00),
(22, '8765432112345678', '2025-03-05 23:51:38', 'Withdrawal', 100.00, 2000.00);

-- --------------------------------------------------------

--
-- Table structure for table `users`
--

CREATE TABLE `users` (
  `id` int(11) NOT NULL,
  `card_number` varchar(16) NOT NULL,
  `pin` varchar(4) NOT NULL,
  `balance` decimal(18,2) DEFAULT 0.00
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `users`
--

INSERT INTO `users` (`id`, `card_number`, `pin`, `balance`) VALUES
(1, '1234567812345678', '1234', 3900.00),
(2, '8765432112345678', '4321', 2000.00);

--
-- Indexes for dumped tables
--

--
-- Indexes for table `transactions`
--
ALTER TABLE `transactions`
  ADD PRIMARY KEY (`id`);

--
-- Indexes for table `users`
--
ALTER TABLE `users`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `card_number` (`card_number`);

--
-- AUTO_INCREMENT for dumped tables
--

--
-- AUTO_INCREMENT for table `transactions`
--
ALTER TABLE `transactions`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=23;

--
-- AUTO_INCREMENT for table `users`
--
ALTER TABLE `users`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=3;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
