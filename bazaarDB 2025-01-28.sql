-- phpMyAdmin SQL Dump
-- version 5.2.1
-- https://www.phpmyadmin.net/
--
-- Host: db
-- Generation Time: Jan 28, 2025 at 04:45 PM
-- Server version: 5.7.44
-- PHP Version: 8.2.20

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Database: `bazaarDB`
--
CREATE DATABASE IF NOT EXISTS `bazaarDB` DEFAULT CHARACTER SET latin1 COLLATE latin1_swedish_ci;
USE `bazaarDB`;

-- --------------------------------------------------------

--
-- Table structure for table `Builds`
--

CREATE TABLE `Builds` (
  `buildID` bigint(20) UNSIGNED NOT NULL,
  `buildName` varchar(100) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  `userID` bigint(20) UNSIGNED NOT NULL,
  `isPublic` tinyint(1) NOT NULL DEFAULT '0'
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- --------------------------------------------------------

--
-- Table structure for table `BuildUrls`
--

CREATE TABLE `BuildUrls` (
  `buildUrlID` bigint(20) UNSIGNED NOT NULL,
  `buildID` bigint(20) UNSIGNED NOT NULL,
  `buildUrl` varchar(1000) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  `buildUrlName` varchar(100) CHARACTER SET ascii COLLATE ascii_bin NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- --------------------------------------------------------

--
-- Table structure for table `Images`
--

CREATE TABLE `Images` (
  `imageID` bigint(20) UNSIGNED NOT NULL,
  `filePath` varchar(300) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  `imageOrder` int(10) NOT NULL,
  `typeID` bigint(20) UNSIGNED NOT NULL,
  `buildID` bigint(20) UNSIGNED NOT NULL,
  `userID` bigint(20) UNSIGNED NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- --------------------------------------------------------

--
-- Table structure for table `ImageTypes`
--

CREATE TABLE `ImageTypes` (
  `typeID` bigint(20) UNSIGNED NOT NULL,
  `typeName` varchar(100) CHARACTER SET ascii COLLATE ascii_bin NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

--
-- Dumping data for table `ImageTypes`
--

INSERT INTO `ImageTypes` (`typeID`, `typeName`) VALUES
(1, 'Build Thumbnail'),
(2, 'Reference Image');

-- --------------------------------------------------------

--
-- Table structure for table `Notes`
--

CREATE TABLE `Notes` (
  `noteID` bigint(20) UNSIGNED NOT NULL,
  `buildID` bigint(20) UNSIGNED NOT NULL,
  `filePath` varchar(300) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- --------------------------------------------------------

--
-- Table structure for table `PasswordResets`
--

CREATE TABLE `PasswordResets` (
  `passwordResetID` bigint(20) UNSIGNED NOT NULL,
  `userID` bigint(20) UNSIGNED NOT NULL,
  `resetToken` varchar(200) NOT NULL,
  `expiration` datetime NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- --------------------------------------------------------

--
-- Table structure for table `Users`
--

CREATE TABLE `Users` (
  `userID` bigint(20) UNSIGNED NOT NULL,
  `userName` varchar(20) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  `email` varchar(100) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  `password` varchar(100) CHARACTER SET ascii COLLATE ascii_bin NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

--
-- Indexes for dumped tables
--

--
-- Indexes for table `Builds`
--
ALTER TABLE `Builds`
  ADD PRIMARY KEY (`buildID`);

--
-- Indexes for table `BuildUrls`
--
ALTER TABLE `BuildUrls`
  ADD PRIMARY KEY (`buildUrlID`);

--
-- Indexes for table `Images`
--
ALTER TABLE `Images`
  ADD PRIMARY KEY (`imageID`);

--
-- Indexes for table `ImageTypes`
--
ALTER TABLE `ImageTypes`
  ADD PRIMARY KEY (`typeID`);

--
-- Indexes for table `Notes`
--
ALTER TABLE `Notes`
  ADD PRIMARY KEY (`noteID`);

--
-- Indexes for table `PasswordResets`
--
ALTER TABLE `PasswordResets`
  ADD PRIMARY KEY (`passwordResetID`);

--
-- Indexes for table `Users`
--
ALTER TABLE `Users`
  ADD PRIMARY KEY (`userID`);

--
-- AUTO_INCREMENT for dumped tables
--

--
-- AUTO_INCREMENT for table `Builds`
--
ALTER TABLE `Builds`
  MODIFY `buildID` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `BuildUrls`
--
ALTER TABLE `BuildUrls`
  MODIFY `buildUrlID` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `Images`
--
ALTER TABLE `Images`
  MODIFY `imageID` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `ImageTypes`
--
ALTER TABLE `ImageTypes`
  MODIFY `typeID` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=3;

--
-- AUTO_INCREMENT for table `Notes`
--
ALTER TABLE `Notes`
  MODIFY `noteID` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `PasswordResets`
--
ALTER TABLE `PasswordResets`
  MODIFY `passwordResetID` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `Users`
--
ALTER TABLE `Users`
  MODIFY `userID` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
