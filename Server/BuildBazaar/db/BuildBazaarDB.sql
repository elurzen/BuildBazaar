-- phpMyAdmin SQL Dump
-- version 5.2.1
-- https://www.phpmyadmin.net/
--
-- Host: db
-- Generation Time: Jun 26, 2025 at 01:52 AM
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
-- Database: `BuildBazaarDB`
--
CREATE DATABASE IF NOT EXISTS `BuildBazaarDB` DEFAULT CHARACTER SET latin1 COLLATE latin1_swedish_ci;
USE `BuildBazaarDB`;

-- --------------------------------------------------------

--
-- Table structure for table `Builds`
--

CREATE TABLE `Builds` (
  `buildID` bigint(20) UNSIGNED NOT NULL,
  `buildName` varchar(100) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  `userID` bigint(20) UNSIGNED NOT NULL,
  `isPublic` tinyint(1) NOT NULL DEFAULT '0',
  `gameID` bigint(20) UNSIGNED NOT NULL,
  `classID` bigint(20) UNSIGNED NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- --------------------------------------------------------

--
-- Table structure for table `BuildTagLinks`
--

CREATE TABLE `BuildTagLinks` (
  `buildID` bigint(20) UNSIGNED NOT NULL,
  `tagID` bigint(20) UNSIGNED NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- --------------------------------------------------------

--
-- Table structure for table `BuildUrls`
--

CREATE TABLE `BuildUrls` (
  `buildUrlID` bigint(20) UNSIGNED NOT NULL,
  `buildID` bigint(20) UNSIGNED NOT NULL,
  `buildUrl` varchar(1000) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  `buildUrlName` varchar(100) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  `userID` bigint(20) UNSIGNED NOT NULL,
  `isPublic` tinyint(1) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- --------------------------------------------------------

--
-- Table structure for table `Classes`
--

CREATE TABLE `Classes` (
  `classID` bigint(20) UNSIGNED NOT NULL,
  `className` varchar(100) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  `gameID` bigint(20) UNSIGNED NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- --------------------------------------------------------

--
-- Table structure for table `Games`
--

CREATE TABLE `Games` (
  `gameID` bigint(20) UNSIGNED NOT NULL,
  `gameName` varchar(100) CHARACTER SET ascii COLLATE ascii_bin NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

--
-- Dumping data for table `Games`
--

INSERT INTO `Games` (`gameID`, `gameName`) VALUES
(1, 'Path of Exile'),
(2, 'Path of Exile 2');

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
  `userID` bigint(20) UNSIGNED NOT NULL,
  `isPublic` tinyint(4) NOT NULL
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
  `filePath` varchar(300) NOT NULL,
  `userID` bigint(20) UNSIGNED NOT NULL,
  `isPublic` tinyint(4) NOT NULL
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
-- Table structure for table `Tags`
--

CREATE TABLE `Tags` (
  `tagID` bigint(20) UNSIGNED NOT NULL,
  `tagName` varchar(100) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  `gameID` bigint(100) UNSIGNED NOT NULL
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
  ADD PRIMARY KEY (`buildID`),
  ADD KEY `BuildsClassFK` (`classID`),
  ADD KEY `BuildsUserFK` (`userID`);

--
-- Indexes for table `BuildTagLinks`
--
ALTER TABLE `BuildTagLinks`
  ADD PRIMARY KEY (`buildID`,`tagID`),
  ADD KEY `BuildTagLinkTagFK` (`tagID`);

--
-- Indexes for table `BuildUrls`
--
ALTER TABLE `BuildUrls`
  ADD PRIMARY KEY (`buildUrlID`),
  ADD KEY `BuildUrlsBuildFK` (`buildID`),
  ADD KEY `BulidUrlsUserFK` (`userID`);

--
-- Indexes for table `Classes`
--
ALTER TABLE `Classes`
  ADD PRIMARY KEY (`classID`),
  ADD KEY `ClassesGameFK` (`gameID`);

--
-- Indexes for table `Games`
--
ALTER TABLE `Games`
  ADD PRIMARY KEY (`gameID`);

--
-- Indexes for table `Images`
--
ALTER TABLE `Images`
  ADD PRIMARY KEY (`imageID`),
  ADD KEY `ImagesBuildFK` (`buildID`),
  ADD KEY `ImagesTypeFK` (`typeID`),
  ADD KEY `ImagesUserFK` (`userID`);

--
-- Indexes for table `ImageTypes`
--
ALTER TABLE `ImageTypes`
  ADD PRIMARY KEY (`typeID`);

--
-- Indexes for table `Notes`
--
ALTER TABLE `Notes`
  ADD PRIMARY KEY (`noteID`),
  ADD KEY `NotesBuildFK` (`buildID`),
  ADD KEY `NotesUserFK` (`userID`);

--
-- Indexes for table `PasswordResets`
--
ALTER TABLE `PasswordResets`
  ADD PRIMARY KEY (`passwordResetID`),
  ADD KEY `PasswordResetsUserFK` (`userID`);

--
-- Indexes for table `Tags`
--
ALTER TABLE `Tags`
  ADD PRIMARY KEY (`tagID`),
  ADD UNIQUE KEY `tagName_3` (`tagName`,`gameID`),
  ADD KEY `TagsGameIDFK` (`gameID`);

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
-- AUTO_INCREMENT for table `Classes`
--
ALTER TABLE `Classes`
  MODIFY `classID` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `Games`
--
ALTER TABLE `Games`
  MODIFY `gameID` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=3;

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
-- AUTO_INCREMENT for table `Tags`
--
ALTER TABLE `Tags`
  MODIFY `tagID` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `Users`
--
ALTER TABLE `Users`
  MODIFY `userID` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- Constraints for dumped tables
--

--
-- Constraints for table `Builds`
--
ALTER TABLE `Builds`
  ADD CONSTRAINT `BuildsClassFK` FOREIGN KEY (`classID`) REFERENCES `Classes` (`classID`),
  ADD CONSTRAINT `BuildsUserFK` FOREIGN KEY (`userID`) REFERENCES `Users` (`userID`) ON UPDATE CASCADE;

--
-- Constraints for table `BuildTagLinks`
--
ALTER TABLE `BuildTagLinks`
  ADD CONSTRAINT `BuildTagLinkBuildFK` FOREIGN KEY (`buildID`) REFERENCES `Builds` (`buildID`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `BuildTagLinkTagFK` FOREIGN KEY (`tagID`) REFERENCES `Tags` (`tagID`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Constraints for table `BuildUrls`
--
ALTER TABLE `BuildUrls`
  ADD CONSTRAINT `BuildUrlsBuildFK` FOREIGN KEY (`buildID`) REFERENCES `Builds` (`buildID`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `BulidUrlsUserFK` FOREIGN KEY (`userID`) REFERENCES `Users` (`userID`) ON UPDATE CASCADE;

--
-- Constraints for table `Classes`
--
ALTER TABLE `Classes`
  ADD CONSTRAINT `ClassesGameFK` FOREIGN KEY (`gameID`) REFERENCES `Games` (`gameID`) ON UPDATE CASCADE;

--
-- Constraints for table `Images`
--
ALTER TABLE `Images`
  ADD CONSTRAINT `ImagesBuildFK` FOREIGN KEY (`buildID`) REFERENCES `Builds` (`buildID`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `ImagesTypeFK` FOREIGN KEY (`typeID`) REFERENCES `ImageTypes` (`typeID`) ON UPDATE CASCADE,
  ADD CONSTRAINT `ImagesUserFK` FOREIGN KEY (`userID`) REFERENCES `Users` (`userID`) ON UPDATE CASCADE;

--
-- Constraints for table `Notes`
--
ALTER TABLE `Notes`
  ADD CONSTRAINT `NotesBuildFK` FOREIGN KEY (`buildID`) REFERENCES `Builds` (`buildID`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `NotesUserFK` FOREIGN KEY (`userID`) REFERENCES `Users` (`userID`) ON UPDATE CASCADE;

--
-- Constraints for table `PasswordResets`
--
ALTER TABLE `PasswordResets`
  ADD CONSTRAINT `PasswordResetsUserFK` FOREIGN KEY (`userID`) REFERENCES `Users` (`userID`) ON UPDATE CASCADE;

--
-- Constraints for table `Tags`
--
ALTER TABLE `Tags`
  ADD CONSTRAINT `TagsGameIDFK` FOREIGN KEY (`gameID`) REFERENCES `Games` (`gameID`) ON DELETE CASCADE ON UPDATE CASCADE;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
