-- phpMyAdmin SQL Dump
-- version 5.2.1
-- https://www.phpmyadmin.net/
--
-- Host: 127.0.0.1
-- Generation Time: Jun 12, 2025 at 12:50 PM
-- Server version: 10.4.32-MariaDB
-- PHP Version: 8.2.12

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Database: `workday`
--

-- --------------------------------------------------------

--
-- Table structure for table `analytics`
--

CREATE TABLE `analytics` (
  `id` int(11) NOT NULL,
  `name` varchar(100) NOT NULL,
  `date` int(11) NOT NULL,
  `data` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL CHECK (json_valid(`data`))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `facebook`
--

CREATE TABLE `facebook` (
  `id` bigint(20) NOT NULL,
  `facebook` varchar(100) NOT NULL,
  `user` bigint(20) NOT NULL,
  `friends` varchar(5000) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1 COLLATE=latin1_swedish_ci;

-- --------------------------------------------------------

--
-- Table structure for table `forms`
--

CREATE TABLE `forms` (
  `id` int(11) NOT NULL,
  `email` varchar(100) NOT NULL,
  `registered` int(11) NOT NULL,
  `size` int(11) NOT NULL,
  `data` blob NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `forms`
--

INSERT INTO `forms` (`id`, `email`, `registered`, `size`, `data`) VALUES
(1, 'Anonymous', 2147483647, 413, 0x7b0a20202020225175657374696f6e73223a205b0a20202020202020207b0a202020202020202020202020224944223a20302c0a20202020202020202020202022526573706f6e7365223a2022486f6c6120646f6e2070657069746f220a20202020202020207d2c0a20202020202020207b0a202020202020202020202020224944223a20312c0a20202020202020202020202022526573706f6e7365223a2022486f6c6120646f6e206a6f7365220a20202020202020207d2c0a20202020202020207b0a202020202020202020202020224944223a20322c0a20202020202020202020202022526573706f6e7365223a20225061736f20757374656420706f72206d692063617361220a20202020202020207d2c0a20202020202020207b0a202020202020202020202020224944223a20332c0a20202020202020202020202022526573706f6e7365223a2022506f72207375206361736120796f20706173652e5c6e5c6e4164696f7320646f6e2070657069746f2e5c6e5c6e4164696f7320646f6e204a6f73652e220a20202020202020207d0a202020205d0a7d);

-- --------------------------------------------------------

--
-- Table structure for table `profile`
--

CREATE TABLE `profile` (
  `id` bigint(20) NOT NULL,
  `user` bigint(20) NOT NULL,
  `name` varchar(100) NOT NULL,
  `address` varchar(500) NOT NULL,
  `description` varchar(5000) NOT NULL,
  `data` text NOT NULL,
  `data2` text NOT NULL,
  `data3` text NOT NULL,
  `data4` text NOT NULL,
  `data5` text NOT NULL,
  `autorun` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1 COLLATE=latin1_swedish_ci;

-- --------------------------------------------------------

--
-- Table structure for table `users`
--

CREATE TABLE `users` (
  `id` bigint(20) NOT NULL,
  `email` varchar(100) NOT NULL,
  `nickname` varchar(100) NOT NULL,
  `password` varchar(500) NOT NULL,
  `platform` varchar(1000) NOT NULL,
  `registerdate` int(11) NOT NULL,
  `lastlogin` int(11) NOT NULL,
  `admin` int(11) NOT NULL,
  `level` int(11) NOT NULL,
  `code` varchar(100) NOT NULL,
  `validated` int(11) NOT NULL,
  `ip` varchar(300) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1 COLLATE=latin1_swedish_ci;

--
-- Indexes for dumped tables
--

--
-- Indexes for table `analytics`
--
ALTER TABLE `analytics`
  ADD PRIMARY KEY (`id`);

--
-- Indexes for table `facebook`
--
ALTER TABLE `facebook`
  ADD PRIMARY KEY (`id`);

--
-- Indexes for table `forms`
--
ALTER TABLE `forms`
  ADD PRIMARY KEY (`id`);

--
-- Indexes for table `profile`
--
ALTER TABLE `profile`
  ADD PRIMARY KEY (`id`);

--
-- Indexes for table `users`
--
ALTER TABLE `users`
  ADD PRIMARY KEY (`id`),
  ADD KEY `id` (`id`);
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
