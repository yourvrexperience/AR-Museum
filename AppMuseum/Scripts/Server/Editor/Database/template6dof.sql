-- phpMyAdmin SQL Dump
-- version 5.2.3
-- https://www.phpmyadmin.net/
--
-- Host: mysql.yourvrexperience.com
-- Generation Time: Aug 25, 2026 at 12:44 AM
-- Server version: 8.0.41-0ubuntu0.24.04.1
-- PHP Version: 8.5.4

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Database: `template6dof`
--

-- --------------------------------------------------------

--
-- Table structure for table `analytics`
--

CREATE TABLE `analytics` (
  `id` int NOT NULL,
  `name` varchar(100) NOT NULL,
  `email` varchar(128) NOT NULL,
  `age` int NOT NULL,
  `language` varchar(16) NOT NULL,
  `level` int NOT NULL,
  `date` int NOT NULL,
  `data` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL
) ;

-- --------------------------------------------------------

--
-- Table structure for table `forms`
--

CREATE TABLE `forms` (
  `id` int NOT NULL,
  `email` varchar(100) NOT NULL,
  `registered` int NOT NULL,
  `size` int NOT NULL,
  `data` blob NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Table structure for table `poimaps`
--

CREATE TABLE `poimaps` (
  `id` int NOT NULL,
  `age` int NOT NULL,
  `level` int NOT NULL,
  `positions` blob NOT NULL,
  `secrets` blob NOT NULL,
  `narration` mediumblob NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Table structure for table `poimaps_backup`
--

CREATE TABLE `poimaps_backup` (
  `id` int NOT NULL,
  `age` int NOT NULL,
  `level` int NOT NULL,
  `positions` blob NOT NULL,
  `secrets` blob NOT NULL,
  `narration` mediumblob NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Table structure for table `poimaps_edition`
--

CREATE TABLE `poimaps_edition` (
  `id` int NOT NULL,
  `age` int NOT NULL,
  `level` int NOT NULL,
  `positions` blob NOT NULL,
  `secrets` blob NOT NULL,
  `narration` mediumblob NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Table structure for table `profile`
--

CREATE TABLE `profile` (
  `id` bigint NOT NULL,
  `user` bigint NOT NULL,
  `name` varchar(100) NOT NULL,
  `address` varchar(500) NOT NULL,
  `description` varchar(5000) NOT NULL,
  `data` mediumtext NOT NULL,
  `data2` mediumtext NOT NULL,
  `data3` mediumtext NOT NULL,
  `data4` mediumtext NOT NULL,
  `data5` mediumtext NOT NULL,
  `autorun` int NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- --------------------------------------------------------

--
-- Table structure for table `speech`
--

CREATE TABLE `speech` (
  `id` int NOT NULL,
  `secret` int NOT NULL,
  `text` varchar(2048) COLLATE utf8mb3_unicode_ci NOT NULL,
  `age` int NOT NULL,
  `floor` int NOT NULL,
  `poi` int NOT NULL,
  `segment` int NOT NULL,
  `language` varchar(10) COLLATE utf8mb3_unicode_ci NOT NULL,
  `size` int NOT NULL,
  `data` mediumblob NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COLLATE=utf8mb3_unicode_ci;

-- --------------------------------------------------------

--
-- Table structure for table `speech_backup`
--

CREATE TABLE `speech_backup` (
  `id` int NOT NULL,
  `secret` int NOT NULL,
  `text` varchar(2048) COLLATE utf8mb3_unicode_ci NOT NULL,
  `age` int NOT NULL,
  `floor` int NOT NULL,
  `poi` int NOT NULL,
  `segment` int NOT NULL,
  `language` varchar(10) COLLATE utf8mb3_unicode_ci NOT NULL,
  `size` int NOT NULL,
  `data` mediumblob NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COLLATE=utf8mb3_unicode_ci;

-- --------------------------------------------------------

--
-- Table structure for table `speech_edition`
--

CREATE TABLE `speech_edition` (
  `id` int NOT NULL,
  `secret` int NOT NULL,
  `text` varchar(2048) COLLATE utf8mb3_unicode_ci NOT NULL,
  `age` int NOT NULL,
  `floor` int NOT NULL,
  `poi` int NOT NULL,
  `segment` int NOT NULL,
  `language` varchar(10) COLLATE utf8mb3_unicode_ci NOT NULL,
  `size` int NOT NULL,
  `data` mediumblob NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COLLATE=utf8mb3_unicode_ci;

-- --------------------------------------------------------

--
-- Table structure for table `useripaddress`
--

CREATE TABLE `useripaddress` (
  `id` int NOT NULL,
  `address` varchar(200) COLLATE utf8mb3_unicode_ci NOT NULL,
  `allowed` int NOT NULL,
  `email` varchar(200) COLLATE utf8mb3_unicode_ci NOT NULL,
  `accounts` int NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COLLATE=utf8mb3_unicode_ci;

-- --------------------------------------------------------

--
-- Table structure for table `users`
--

CREATE TABLE `users` (
  `id` bigint NOT NULL,
  `email` varchar(100) NOT NULL,
  `nickname` varchar(100) NOT NULL,
  `password` varchar(500) NOT NULL,
  `platform` varchar(1000) NOT NULL,
  `registerdate` int NOT NULL,
  `lastlogin` int NOT NULL,
  `admin` int NOT NULL,
  `level` int NOT NULL,
  `code` varchar(100) NOT NULL,
  `validated` int NOT NULL,
  `ip` varchar(300) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- --------------------------------------------------------

--
-- Table structure for table `version`
--

CREATE TABLE `version` (
  `id` int NOT NULL,
  `version_dev` int NOT NULL,
  `version_prod` int NOT NULL,
  `levels` int NOT NULL,
  `secrets_dev` int NOT NULL,
  `secrets_prod` int NOT NULL,
  `development` blob NOT NULL,
  `production` blob NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

--
-- Indexes for dumped tables
--

--
-- Indexes for table `analytics`
--
ALTER TABLE `analytics`
  ADD PRIMARY KEY (`id`),
  ADD KEY `idx_analytics_name_date` (`name`,`date`),
  ADD KEY `idx_analytics_email` (`email`),
  ADD KEY `idx_analytics_date` (`date`);

--
-- Indexes for table `forms`
--
ALTER TABLE `forms`
  ADD PRIMARY KEY (`id`);

--
-- Indexes for table `poimaps`
--
ALTER TABLE `poimaps`
  ADD PRIMARY KEY (`id`);

--
-- Indexes for table `poimaps_backup`
--
ALTER TABLE `poimaps_backup`
  ADD PRIMARY KEY (`id`);

--
-- Indexes for table `poimaps_edition`
--
ALTER TABLE `poimaps_edition`
  ADD PRIMARY KEY (`id`);

--
-- Indexes for table `profile`
--
ALTER TABLE `profile`
  ADD PRIMARY KEY (`id`);

--
-- Indexes for table `speech`
--
ALTER TABLE `speech`
  ADD PRIMARY KEY (`id`);

--
-- Indexes for table `speech_backup`
--
ALTER TABLE `speech_backup`
  ADD PRIMARY KEY (`id`);

--
-- Indexes for table `speech_edition`
--
ALTER TABLE `speech_edition`
  ADD PRIMARY KEY (`id`);

--
-- Indexes for table `useripaddress`
--
ALTER TABLE `useripaddress`
  ADD PRIMARY KEY (`id`);

--
-- Indexes for table `users`
--
ALTER TABLE `users`
  ADD PRIMARY KEY (`id`),
  ADD KEY `id` (`id`);

--
-- Indexes for table `version`
--
ALTER TABLE `version`
  ADD PRIMARY KEY (`id`);
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
