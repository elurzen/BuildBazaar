-- phpMyAdmin SQL Dump
-- version 5.2.1
-- https://www.phpmyadmin.net/
--
-- Host: db
-- Generation Time: Sep 05, 2024 at 01:43 PM
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

CREATE TABLE IF NOT EXISTS `Builds` (
  `buildID` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT,
  `buildName` varchar(100) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  `userID` bigint(20) UNSIGNED NOT NULL,
  PRIMARY KEY (`buildID`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- --------------------------------------------------------

--
-- Table structure for table `BuildUrls`
--

CREATE TABLE IF NOT EXISTS `BuildUrls` (
  `buildUrlID` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT,
  `buildID` bigint(20) UNSIGNED NOT NULL,
  `buildUrl` varchar(1000) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  `buildUrlName` varchar(100) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  PRIMARY KEY (`buildUrlID`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- --------------------------------------------------------

--
-- Table structure for table `Images`
--

CREATE TABLE IF NOT EXISTS `Images` (
  `imageID` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT,
  `filePath` varchar(300) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  `imageOrder` int(10) NOT NULL,
  `typeID` bigint(20) UNSIGNED NOT NULL,
  `buildID` bigint(20) UNSIGNED NOT NULL,
  `userID` bigint(20) UNSIGNED NOT NULL,
  PRIMARY KEY (`imageID`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- --------------------------------------------------------

--
-- Table structure for table `ImageTypes`
--

CREATE TABLE IF NOT EXISTS `ImageTypes` (
  `typeID` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT,
  `typeName` varchar(100) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  PRIMARY KEY (`typeID`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=latin1;

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

CREATE TABLE IF NOT EXISTS `Notes` (
  `noteID` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT,
  `buildID` bigint(20) UNSIGNED NOT NULL,
  `filePath` varchar(300) NOT NULL,
  PRIMARY KEY (`noteID`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- --------------------------------------------------------

--
-- Table structure for table `Users`
--

CREATE TABLE IF NOT EXISTS `Users` (
  `userID` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT,
  `userName` varchar(20) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  `email` varchar(100) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  `password` varchar(100) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  PRIMARY KEY (`userID`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
