CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    ALTER DATABASE CHARACTER SET utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `CallRecords` (
        `CallRecordId` int NOT NULL AUTO_INCREMENT,
        `CallDate` date NOT NULL,
        `CallTime` time(0) NOT NULL,
        `CallCloseReason` int NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        CONSTRAINT `PK_CallRecords` PRIMARY KEY (`CallRecordId`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `CallSummaries` (
        `CallSummaryId` int NOT NULL AUTO_INCREMENT,
        `SummaryDate` date NOT NULL,
        `HourGroup` int NOT NULL,
        `TotalQty` int NOT NULL,
        `TEBusyCount` int NOT NULL,
        `SysBusyCount` int NOT NULL,
        `OthersCount` int NOT NULL,
        `TEBusyPercent` decimal(5,2) NOT NULL,
        `SysBusyPercent` decimal(5,2) NOT NULL,
        `OthersPercent` decimal(5,2) NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `UpdatedAt` datetime(6) NULL,
        CONSTRAINT `PK_CallSummaries` PRIMARY KEY (`CallSummaryId`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `CctvKpcs` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Severity` varchar(20) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'Low',
        `Camera` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
        `IpCamera` varchar(50) CHARACTER SET utf8mb4 NULL,
        `Model` varchar(200) CHARACTER SET utf8mb4 NULL,
        `Brand` varchar(100) CHARACTER SET utf8mb4 NULL,
        `ExplicitLocation` varchar(500) CHARACTER SET utf8mb4 NULL,
        `FotoKoordinat` varchar(1000) CHARACTER SET utf8mb4 NULL,
        `Remarks` varchar(500) CHARACTER SET utf8mb4 NULL,
        `IsActive` tinyint(1) NOT NULL DEFAULT TRUE,
        `CreatedAt` datetime(6) NOT NULL DEFAULT UTC_TIMESTAMP(),
        `UpdatedAt` datetime(6) NULL,
        CONSTRAINT `PK_CctvKpcs` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `FileImportHistories` (
        `ImportHistoryId` int NOT NULL AUTO_INCREMENT,
        `FileName` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `ImportDate` datetime(6) NOT NULL,
        `RecordCount` int NOT NULL,
        `Status` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `ErrorMessage` longtext CHARACTER SET utf8mb4 NULL,
        CONSTRAINT `PK_FileImportHistories` PRIMARY KEY (`ImportHistoryId`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `FleetStatistics` (
        `FleetStatisticId` int NOT NULL AUTO_INCREMENT,
        `CallDate` date NOT NULL,
        `CallerFleet` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `CalledFleet` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `CallCount` int NOT NULL,
        `TotalDuration` int NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `UpdatedAt` datetime(6) NULL,
        CONSTRAINT `PK_FleetStatistics` PRIMARY KEY (`FleetStatisticId`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `InternalLinks` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `LinkName` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
        `LinkGroup` varchar(255) CHARACTER SET utf8mb4 NULL,
        `Direction` int NOT NULL,
        `IpAddress` varchar(100) CHARACTER SET utf8mb4 NULL,
        `Device` varchar(200) CHARACTER SET utf8mb4 NULL,
        `Type` varchar(100) CHARACTER SET utf8mb4 NULL,
        `UsedFrequency` varchar(100) CHARACTER SET utf8mb4 NULL,
        `RslNearEnd` decimal(10,2) NULL,
        `ServiceType` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `IsActive` tinyint(1) NOT NULL DEFAULT TRUE,
        `CreatedAt` datetime(6) NOT NULL DEFAULT UTC_TIMESTAMP(),
        CONSTRAINT `PK_InternalLinks` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `KpiDocuments` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `PeriodMonth` datetime(6) NOT NULL,
        `AreaGroup` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `DocumentName` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `DataSource` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `GroupTag` varchar(100) CHARACTER SET utf8mb4 NULL,
        `DateReceived` datetime(6) NULL,
        `DateSubmittedToReviewer` datetime(6) NULL,
        `DateApproved` datetime(6) NULL,
        `DateSubmittedToRqm` datetime(6) NULL,
        `Remarks` varchar(500) CHARACTER SET utf8mb4 NULL,
        `RemarksSubmittedToReviewer` varchar(500) CHARACTER SET utf8mb4 NULL,
        `RemarksApproved` varchar(500) CHARACTER SET utf8mb4 NULL,
        `RemarksSubmittedToRqm` varchar(500) CHARACTER SET utf8mb4 NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `UpdatedAt` datetime(6) NULL,
        `CreatedBy` int NOT NULL,
        `UpdatedBy` int NULL,
        CONSTRAINT `PK_KpiDocuments` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `NecTowers` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Name` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `Location` varchar(200) CHARACTER SET utf8mb4 NULL,
        CONSTRAINT `PK_NecTowers` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `Notifications` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `RecipientUserId` int NULL,
        `RecipientRoleName` varchar(100) CHARACTER SET utf8mb4 NULL,
        `Title` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
        `Message` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
        `Category` varchar(50) CHARACTER SET utf8mb4 NULL,
        `LinkUrl` varchar(500) CHARACTER SET utf8mb4 NULL,
        `ReferenceId` int NULL,
        `ReferenceType` varchar(100) CHARACTER SET utf8mb4 NULL,
        `IsRead` tinyint(1) NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `ReadAt` datetime(6) NULL,
        CONSTRAINT `PK_Notifications` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `OperationalDocuments` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Name` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `Type` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `ReferenceNumber` varchar(100) CHARACTER SET utf8mb4 NULL,
        `GroupName` varchar(200) CHARACTER SET utf8mb4 NULL,
        `ValidFrom` datetime(6) NOT NULL,
        `ValidUntil` datetime(6) NOT NULL,
        `PicName` varchar(255) CHARACTER SET utf8mb4 NULL,
        `PicPhone` varchar(200) CHARACTER SET utf8mb4 NULL,
        `FileLink` varchar(1000) CHARACTER SET utf8mb4 NULL,
        `FollowUpStatus` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `CreatedAt` datetime(6) NOT NULL DEFAULT UTC_TIMESTAMP(),
        `UpdatedAt` datetime(6) NULL,
        CONSTRAINT `PK_OperationalDocuments` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `OperationalDocumentTypes` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Name` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `Description` varchar(255) CHARACTER SET utf8mb4 NULL,
        `IsActive` tinyint(1) NOT NULL,
        `CreatedAt` datetime(6) NOT NULL DEFAULT UTC_TIMESTAMP(),
        `UpdatedAt` datetime(6) NULL,
        CONSTRAINT `PK_OperationalDocumentTypes` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `Permissions` (
        `PermissionId` int NOT NULL AUTO_INCREMENT,
        `PermissionName` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `Description` varchar(255) CHARACTER SET utf8mb4 NULL,
        `Group` varchar(50) CHARACTER SET utf8mb4 NULL,
        `CreatedAt` datetime(6) NOT NULL DEFAULT UTC_TIMESTAMP(),
        CONSTRAINT `PK_Permissions` PRIMARY KEY (`PermissionId`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `PmSites` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Name` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `OrderIndex` int NOT NULL,
        CONSTRAINT `PK_PmSites` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `Radios` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Category` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `SerialNumber` varchar(100) CHARACTER SET utf8mb4 NULL,
        `Type` varchar(100) CHARACTER SET utf8mb4 NULL,
        `Department` varchar(100) CHARACTER SET utf8mb4 NULL,
        `Division` varchar(100) CHARACTER SET utf8mb4 NULL,
        `Company` varchar(100) CHARACTER SET utf8mb4 NULL,
        `Channel` varchar(100) CHARACTER SET utf8mb4 NULL,
        `Tanggal` datetime(6) NULL,
        `NomorAset` varchar(100) CHARACTER SET utf8mb4 NULL,
        `NomorUnit` varchar(100) CHARACTER SET utf8mb4 NULL,
        `NomorLv` varchar(100) CHARACTER SET utf8mb4 NULL,
        `IsTrunking` tinyint(1) NOT NULL,
        `IsConventional` tinyint(1) NOT NULL,
        `Fleet` varchar(200) CHARACTER SET utf8mb4 NULL,
        `RadioId` varchar(100) CHARACTER SET utf8mb4 NULL,
        `IsScrap` tinyint(1) NOT NULL,
        `ScrapJobNumber` varchar(100) CHARACTER SET utf8mb4 NULL,
        `DateScrapped` datetime(6) NULL,
        `Remarks` varchar(1000) CHARACTER SET utf8mb4 NULL,
        `Mark` varchar(1000) CHARACTER SET utf8mb4 NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `UpdatedAt` datetime(6) NULL,
        CONSTRAINT `PK_Radios` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `Roles` (
        `RoleId` int NOT NULL AUTO_INCREMENT,
        `RoleName` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `Description` varchar(255) CHARACTER SET utf8mb4 NULL,
        `IsActive` tinyint(1) NOT NULL,
        `CreatedAt` datetime(6) NOT NULL DEFAULT UTC_TIMESTAMP(),
        CONSTRAINT `PK_Roles` PRIMARY KEY (`RoleId`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `SwrSites` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Name` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `Location` varchar(255) CHARACTER SET utf8mb4 NULL,
        `Type` longtext CHARACTER SET utf8mb4 NOT NULL,
        CONSTRAINT `PK_SwrSites` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `WarehousePartCatalog` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `PartCode` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `PartName` varchar(250) CHARACTER SET utf8mb4 NOT NULL,
        `OwnerId` varchar(100) CHARACTER SET utf8mb4 NULL,
        `Category` varchar(100) CHARACTER SET utf8mb4 NULL,
        `Unit` varchar(50) CHARACTER SET utf8mb4 NULL,
        `Description` varchar(1000) CHARACTER SET utf8mb4 NULL,
        `IsActive` tinyint(1) NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `UpdatedAt` datetime(6) NULL,
        CONSTRAINT `PK_WarehousePartCatalog` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `InternalLinkHistories` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `InternalLinkId` int NOT NULL,
        `Date` datetime(6) NOT NULL,
        `RslNearEnd` decimal(10,2) NULL,
        `Uptime` int NULL,
        `Notes` text CHARACTER SET utf8mb4 NULL,
        `ScreenshotBase64` longtext CHARACTER SET utf8mb4 NULL,
        `Status` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `CreatedAt` datetime(6) NOT NULL DEFAULT UTC_TIMESTAMP(),
        CONSTRAINT `PK_InternalLinkHistories` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_InternalLinkHistories_InternalLinks_InternalLinkId` FOREIGN KEY (`InternalLinkId`) REFERENCES `InternalLinks` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `NecLinks` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `LinkName` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
        `NearEndTowerId` int NOT NULL,
        `FarEndTowerId` int NOT NULL,
        `ExpectedRslMin` decimal(65,30) NOT NULL,
        `ExpectedRslMax` decimal(65,30) NOT NULL,
        CONSTRAINT `PK_NecLinks` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_NecLinks_NecTowers_FarEndTowerId` FOREIGN KEY (`FarEndTowerId`) REFERENCES `NecTowers` (`Id`) ON DELETE CASCADE,
        CONSTRAINT `FK_NecLinks_NecTowers_NearEndTowerId` FOREIGN KEY (`NearEndTowerId`) REFERENCES `NecTowers` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `OperationalDocumentNotificationHistories` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `OperationalDocumentId` int NOT NULL,
        `NotifiedAt` datetime(6) NOT NULL DEFAULT UTC_TIMESTAMP(),
        `DaysRemaining` int NOT NULL,
        CONSTRAINT `PK_OperationalDocumentNotificationHistories` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_OperationalDocumentNotificationHistories_OperationalDocument~` FOREIGN KEY (`OperationalDocumentId`) REFERENCES `OperationalDocuments` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `PmSchedules` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Year` int NOT NULL,
        `PmSiteId` int NOT NULL,
        `DeviceName` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        CONSTRAINT `PK_PmSchedules` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_PmSchedules_PmSites_PmSiteId` FOREIGN KEY (`PmSiteId`) REFERENCES `PmSites` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `RadioHistories` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `RadioId` int NOT NULL,
        `Action` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `Details` varchar(2000) CHARACTER SET utf8mb4 NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `CreatedBy` varchar(100) CHARACTER SET utf8mb4 NULL,
        CONSTRAINT `PK_RadioHistories` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_RadioHistories_Radios_RadioId` FOREIGN KEY (`RadioId`) REFERENCES `Radios` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `RolePermissions` (
        `RolePermissionId` int NOT NULL AUTO_INCREMENT,
        `RoleId` int NOT NULL,
        `PermissionId` int NOT NULL,
        `CreatedAt` datetime(6) NOT NULL DEFAULT UTC_TIMESTAMP(),
        CONSTRAINT `PK_RolePermissions` PRIMARY KEY (`RolePermissionId`),
        CONSTRAINT `FK_RolePermissions_Permissions_PermissionId` FOREIGN KEY (`PermissionId`) REFERENCES `Permissions` (`PermissionId`) ON DELETE CASCADE,
        CONSTRAINT `FK_RolePermissions_Roles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `Roles` (`RoleId`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `Users` (
        `UserId` int NOT NULL AUTO_INCREMENT,
        `Username` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `PasswordHash` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `FullName` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
        `Email` varchar(200) CHARACTER SET utf8mb4 NULL,
        `PhotoUrl` varchar(500) CHARACTER SET utf8mb4 NULL,
        `EmployeeId` varchar(50) CHARACTER SET utf8mb4 NULL,
        `Division` varchar(100) CHARACTER SET utf8mb4 NULL,
        `IsActive` tinyint(1) NOT NULL DEFAULT FALSE,
        `RoleId` int NOT NULL,
        `LastLogin` datetime(6) NULL,
        `CreatedAt` datetime(6) NOT NULL DEFAULT UTC_TIMESTAMP(),
        `UpdatedAt` datetime(6) NULL,
        CONSTRAINT `PK_Users` PRIMARY KEY (`UserId`),
        CONSTRAINT `FK_Users_Roles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `Roles` (`RoleId`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `SwrChannels` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `ChannelName` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `SwrSiteId` int NOT NULL,
        `ExpectedSwrMax` decimal(4,2) NOT NULL DEFAULT 1.5,
        `ExpectedPwrMax` decimal(6,2) NULL,
        CONSTRAINT `PK_SwrChannels` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_SwrChannels_SwrSites_SwrSiteId` FOREIGN KEY (`SwrSiteId`) REFERENCES `SwrSites` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `NecRslHistories` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `NecLinkId` int NOT NULL,
        `Date` datetime(6) NOT NULL,
        `RslNearEnd` decimal(10,2) NULL,
        `RslFarEnd` decimal(10,2) NULL,
        `Notes` text CHARACTER SET utf8mb4 NULL,
        `Status` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `CreatedAt` datetime(6) NOT NULL DEFAULT UTC_TIMESTAMP(),
        CONSTRAINT `PK_NecRslHistories` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_NecRslHistories_NecLinks_NecLinkId` FOREIGN KEY (`NecLinkId`) REFERENCES `NecLinks` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `PmScheduleTasks` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `PmScheduleId` int NOT NULL,
        `Month` int NOT NULL,
        `Week` int NOT NULL,
        CONSTRAINT `PK_PmScheduleTasks` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_PmScheduleTasks_PmSchedules_PmScheduleId` FOREIGN KEY (`PmScheduleId`) REFERENCES `PmSchedules` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `ActivityLogs` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Module` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `EntityId` int NULL,
        `Action` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `UserId` int NOT NULL,
        `Description` varchar(1000) CHARACTER SET utf8mb4 NOT NULL,
        `Timestamp` datetime(6) NOT NULL DEFAULT UTC_TIMESTAMP(),
        CONSTRAINT `PK_ActivityLogs` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_ActivityLogs_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`UserId`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `Companies` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Code` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `Name` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
        `Address` varchar(500) CHARACTER SET utf8mb4 NULL,
        `IsActive` tinyint(1) NOT NULL DEFAULT TRUE,
        `CreatedBy` int NOT NULL,
        `CreatedAt` datetime(6) NOT NULL DEFAULT UTC_TIMESTAMP(),
        `UpdatedAt` datetime(6) NULL,
        `UpdatedBy` int NULL,
        CONSTRAINT `PK_Companies` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_Companies_Users_CreatedBy` FOREIGN KEY (`CreatedBy`) REFERENCES `Users` (`UserId`) ON DELETE RESTRICT,
        CONSTRAINT `FK_Companies_Users_UpdatedBy` FOREIGN KEY (`UpdatedBy`) REFERENCES `Users` (`UserId`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `Divisions` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Code` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `Name` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
        `IsActive` tinyint(1) NOT NULL DEFAULT TRUE,
        `CreatedBy` int NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `UpdatedAt` datetime(6) NULL,
        `UpdatedBy` int NULL,
        CONSTRAINT `PK_Divisions` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_Divisions_Users_CreatedBy` FOREIGN KEY (`CreatedBy`) REFERENCES `Users` (`UserId`) ON DELETE RESTRICT,
        CONSTRAINT `FK_Divisions_Users_UpdatedBy` FOREIGN KEY (`UpdatedBy`) REFERENCES `Users` (`UserId`) ON DELETE SET NULL
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `DocumentTypes` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Code` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `Name` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
        `Description` varchar(500) CHARACTER SET utf8mb4 NULL,
        `IsActive` tinyint(1) NOT NULL DEFAULT TRUE,
        `CreatedBy` int NOT NULL,
        `CreatedAt` datetime(6) NOT NULL DEFAULT UTC_TIMESTAMP(),
        `UpdatedAt` datetime(6) NULL,
        `UpdatedBy` int NULL,
        CONSTRAINT `PK_DocumentTypes` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_DocumentTypes_Users_CreatedBy` FOREIGN KEY (`CreatedBy`) REFERENCES `Users` (`UserId`) ON DELETE RESTRICT,
        CONSTRAINT `FK_DocumentTypes_Users_UpdatedBy` FOREIGN KEY (`UpdatedBy`) REFERENCES `Users` (`UserId`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `Gatepasses` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `FormattedNumber` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `SequenceNumber` int NOT NULL,
        `Year` int NOT NULL,
        `Month` int NOT NULL,
        `Destination` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
        `PicName` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `PicContact` varchar(50) CHARACTER SET utf8mb4 NULL,
        `GatepassDate` date NOT NULL,
        `SignatureQRCode` varchar(200) CHARACTER SET utf8mb4 NULL,
        `Notes` text CHARACTER SET utf8mb4 NULL,
        `Status` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
        `SignedByUserId` int NULL,
        `SignedAt` datetime(6) NULL,
        `VerificationToken` longtext CHARACTER SET utf8mb4 NULL,
        `CreatedBy` int NOT NULL,
        `CreatedAt` datetime(6) NOT NULL DEFAULT UTC_TIMESTAMP(),
        `UpdatedAt` datetime(6) NULL,
        `UpdatedBy` int NULL,
        CONSTRAINT `PK_Gatepasses` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_Gatepasses_Users_CreatedBy` FOREIGN KEY (`CreatedBy`) REFERENCES `Users` (`UserId`) ON DELETE RESTRICT,
        CONSTRAINT `FK_Gatepasses_Users_SignedByUserId` FOREIGN KEY (`SignedByUserId`) REFERENCES `Users` (`UserId`) ON DELETE RESTRICT,
        CONSTRAINT `FK_Gatepasses_Users_UpdatedBy` FOREIGN KEY (`UpdatedBy`) REFERENCES `Users` (`UserId`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `InspeksiTemuanKpcs` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Ruang` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
        `Temuan` longtext CHARACTER SET utf8mb4 NOT NULL,
        `KategoriTemuan` varchar(200) CHARACTER SET utf8mb4 NULL,
        `Inspector` varchar(200) CHARACTER SET utf8mb4 NULL,
        `Severity` varchar(50) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'Medium',
        `TanggalTemuan` datetime(6) NOT NULL,
        `NoFollowUp` varchar(100) CHARACTER SET utf8mb4 NULL,
        `PerbaikanDilakukan` longtext CHARACTER SET utf8mb4 NULL,
        `TanggalPerbaikan` datetime(6) NULL,
        `TanggalSelesaiPerbaikan` datetime(6) NULL,
        `PicPelaksana` varchar(200) CHARACTER SET utf8mb4 NULL,
        `Status` varchar(50) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'Open',
        `TanggalTargetSelesai` datetime(6) NULL,
        `TanggalClosed` datetime(6) NULL,
        `Keterangan` longtext CHARACTER SET utf8mb4 NULL,
        `FotoTemuanUrls` longtext CHARACTER SET utf8mb4 NULL,
        `FotoHasilUrls` longtext CHARACTER SET utf8mb4 NULL,
        `CreatedBy` int NOT NULL,
        `CreatedAt` datetime(6) NOT NULL DEFAULT UTC_TIMESTAMP(),
        `UpdatedBy` int NULL,
        `UpdatedAt` datetime(6) NULL,
        `IsDeleted` tinyint(1) NOT NULL DEFAULT FALSE,
        `DeletedAt` datetime(6) NULL,
        `DeletedBy` int NULL,
        CONSTRAINT `PK_InspeksiTemuanKpcs` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_InspeksiTemuanKpcs_Users_CreatedBy` FOREIGN KEY (`CreatedBy`) REFERENCES `Users` (`UserId`) ON DELETE RESTRICT,
        CONSTRAINT `FK_InspeksiTemuanKpcs_Users_DeletedBy` FOREIGN KEY (`DeletedBy`) REFERENCES `Users` (`UserId`) ON DELETE RESTRICT,
        CONSTRAINT `FK_InspeksiTemuanKpcs_Users_UpdatedBy` FOREIGN KEY (`UpdatedBy`) REFERENCES `Users` (`UserId`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `PasswordResetTokens` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `UserId` int NOT NULL,
        `Token` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `ExpiresAt` datetime(6) NOT NULL,
        `IsUsed` tinyint(1) NOT NULL DEFAULT FALSE,
        `CreatedAt` datetime(6) NOT NULL DEFAULT UTC_TIMESTAMP(),
        CONSTRAINT `PK_PasswordResetTokens` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_PasswordResetTokens_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`UserId`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `RadioGrafirs` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `NoAsset` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `SerialNumber` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `TypeRadio` varchar(100) CHARACTER SET utf8mb4 NULL,
        `Div` varchar(50) CHARACTER SET utf8mb4 NULL,
        `Dept` varchar(100) CHARACTER SET utf8mb4 NULL,
        `FleetId` varchar(50) CHARACTER SET utf8mb4 NULL,
        `Tanggal` datetime(6) NULL,
        `Status` varchar(50) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'Active',
        `CreatedAt` datetime(6) NOT NULL DEFAULT UTC_TIMESTAMP(),
        `UpdatedAt` datetime(6) NULL,
        `CreatedBy` int NULL,
        `UpdatedBy` int NULL,
        CONSTRAINT `PK_RadioGrafirs` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_RadioGrafirs_Users_CreatedBy` FOREIGN KEY (`CreatedBy`) REFERENCES `Users` (`UserId`),
        CONSTRAINT `FK_RadioGrafirs_Users_UpdatedBy` FOREIGN KEY (`UpdatedBy`) REFERENCES `Users` (`UserId`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `RepairJobCustomStatuses` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Label` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `Color` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `SortOrder` int NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        `CreatedByUserId` int NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `UpdatedAt` datetime(6) NULL,
        CONSTRAINT `PK_RepairJobCustomStatuses` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_RepairJobCustomStatuses_Users_CreatedByUserId` FOREIGN KEY (`CreatedByUserId`) REFERENCES `Users` (`UserId`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `WorkshopTechnicians` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Name` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `UserId` int NULL,
        `IsActive` tinyint(1) NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `UpdatedAt` datetime(6) NULL,
        `IsDeleted` tinyint(1) NOT NULL,
        `DeletedAt` datetime(6) NULL,
        `DeletedByUserId` int NULL,
        CONSTRAINT `PK_WorkshopTechnicians` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_WorkshopTechnicians_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`UserId`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `SwrHistories` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `SwrChannelId` int NOT NULL,
        `Date` date NOT NULL,
        `Fpwr` decimal(6,2) NULL,
        `Vswr` decimal(4,2) NOT NULL,
        `Notes` text CHARACTER SET utf8mb4 NULL,
        `Status` longtext CHARACTER SET utf8mb4 NOT NULL,
        `CreatedAt` datetime(6) NOT NULL DEFAULT UTC_TIMESTAMP(),
        CONSTRAINT `PK_SwrHistories` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_SwrHistories_SwrChannels_SwrChannelId` FOREIGN KEY (`SwrChannelId`) REFERENCES `SwrChannels` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `Quotations` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `FormattedNumber` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `SequenceNumber` int NOT NULL,
        `Year` int NOT NULL,
        `Month` int NOT NULL,
        `CustomerId` int NOT NULL,
        `CustomerName` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
        `Description` text CHARACTER SET utf8mb4 NOT NULL,
        `QuotationDate` date NOT NULL,
        `Notes` text CHARACTER SET utf8mb4 NULL,
        `Status` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
        `CreatedBy` int NOT NULL,
        `CreatedAt` datetime(6) NOT NULL DEFAULT UTC_TIMESTAMP(),
        `UpdatedAt` datetime(6) NULL,
        `UpdatedBy` int NULL,
        `Nominal` decimal(65,30) NULL,
        CONSTRAINT `PK_Quotations` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_Quotations_Companies_CustomerId` FOREIGN KEY (`CustomerId`) REFERENCES `Companies` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_Quotations_Users_CreatedBy` FOREIGN KEY (`CreatedBy`) REFERENCES `Users` (`UserId`) ON DELETE RESTRICT,
        CONSTRAINT `FK_Quotations_Users_UpdatedBy` FOREIGN KEY (`UpdatedBy`) REFERENCES `Users` (`UserId`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `LetterNumbers` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `FormattedNumber` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `SequenceNumber` int NOT NULL,
        `DocumentTypeId` int NOT NULL,
        `CompanyId` int NOT NULL,
        `Year` int NOT NULL,
        `Month` int NOT NULL,
        `LetterDate` date NOT NULL,
        `Subject` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
        `Recipient` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
        `AttachmentUrl` varchar(1000) CHARACTER SET utf8mb4 NULL,
        `Status` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `CreatedBy` int NOT NULL,
        `CreatedAt` datetime(6) NOT NULL DEFAULT UTC_TIMESTAMP(),
        `UpdatedAt` datetime(6) NULL,
        `UpdatedBy` int NULL,
        CONSTRAINT `PK_LetterNumbers` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_LetterNumbers_Companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `Companies` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_LetterNumbers_DocumentTypes_DocumentTypeId` FOREIGN KEY (`DocumentTypeId`) REFERENCES `DocumentTypes` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_LetterNumbers_Users_CreatedBy` FOREIGN KEY (`CreatedBy`) REFERENCES `Users` (`UserId`) ON DELETE RESTRICT,
        CONSTRAINT `FK_LetterNumbers_Users_UpdatedBy` FOREIGN KEY (`UpdatedBy`) REFERENCES `Users` (`UserId`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `GatepassItems` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `GatepassId` int NOT NULL,
        `ItemName` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
        `Quantity` int NOT NULL DEFAULT 1,
        `Unit` varchar(50) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'unit',
        `Description` varchar(500) CHARACTER SET utf8mb4 NULL,
        `SerialNumber` varchar(100) CHARACTER SET utf8mb4 NULL,
        CONSTRAINT `PK_GatepassItems` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_GatepassItems_Gatepasses_GatepassId` FOREIGN KEY (`GatepassId`) REFERENCES `Gatepasses` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `RadioConventionals` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `UnitNumber` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `RadioId` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `SerialNumber` varchar(100) CHARACTER SET utf8mb4 NULL,
        `Dept` varchar(100) CHARACTER SET utf8mb4 NULL,
        `Fleet` varchar(50) CHARACTER SET utf8mb4 NULL,
        `RadioType` varchar(100) CHARACTER SET utf8mb4 NULL,
        `Frequency` varchar(50) CHARACTER SET utf8mb4 NULL,
        `Status` varchar(50) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'Active',
        `GrafirId` int NULL,
        `CreatedAt` datetime(6) NOT NULL DEFAULT UTC_TIMESTAMP(),
        `UpdatedAt` datetime(6) NULL,
        `CreatedBy` int NULL,
        `UpdatedBy` int NULL,
        CONSTRAINT `PK_RadioConventionals` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_RadioConventionals_RadioGrafirs_GrafirId` FOREIGN KEY (`GrafirId`) REFERENCES `RadioGrafirs` (`Id`) ON DELETE SET NULL,
        CONSTRAINT `FK_RadioConventionals_Users_CreatedBy` FOREIGN KEY (`CreatedBy`) REFERENCES `Users` (`UserId`),
        CONSTRAINT `FK_RadioConventionals_Users_UpdatedBy` FOREIGN KEY (`UpdatedBy`) REFERENCES `Users` (`UserId`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `RadioTrunkings` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `UnitNumber` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `Dept` varchar(100) CHARACTER SET utf8mb4 NULL,
        `Fleet` varchar(50) CHARACTER SET utf8mb4 NULL,
        `RadioId` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `SerialNumber` varchar(100) CHARACTER SET utf8mb4 NULL,
        `DateProgram` datetime(6) NULL,
        `RadioType` varchar(100) CHARACTER SET utf8mb4 NULL,
        `JobNumber` varchar(50) CHARACTER SET utf8mb4 NULL,
        `Status` varchar(50) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'Active',
        `Initiator` varchar(100) CHARACTER SET utf8mb4 NULL,
        `Firmware` varchar(100) CHARACTER SET utf8mb4 NULL,
        `ChannelApply` varchar(500) CHARACTER SET utf8mb4 NULL,
        `Remarks` varchar(500) CHARACTER SET utf8mb4 NULL,
        `GrafirId` int NULL,
        `CreatedAt` datetime(6) NOT NULL DEFAULT UTC_TIMESTAMP(),
        `UpdatedAt` datetime(6) NULL,
        `CreatedBy` int NULL,
        `UpdatedBy` int NULL,
        CONSTRAINT `PK_RadioTrunkings` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_RadioTrunkings_RadioGrafirs_GrafirId` FOREIGN KEY (`GrafirId`) REFERENCES `RadioGrafirs` (`Id`) ON DELETE SET NULL,
        CONSTRAINT `FK_RadioTrunkings_Users_CreatedBy` FOREIGN KEY (`CreatedBy`) REFERENCES `Users` (`UserId`),
        CONSTRAINT `FK_RadioTrunkings_Users_UpdatedBy` FOREIGN KEY (`UpdatedBy`) REFERENCES `Users` (`UserId`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `RadioRepairJobs` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `JobNumber` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
        `HelpdeskTicketNumber` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `RadioId` int NULL,
        `RadioSerialNumber` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `BatterySerialNumber` varchar(100) CHARACTER SET utf8mb4 NULL,
        `EquipmentName` varchar(100) CHARACTER SET utf8mb4 NULL,
        `UnitNumber` varchar(100) CHARACTER SET utf8mb4 NULL,
        `RadioOwnerLabel` varchar(200) CHARACTER SET utf8mb4 NULL,
        `OwnerDivision` varchar(100) CHARACTER SET utf8mb4 NULL,
        `OwnerDepartment` varchar(100) CHARACTER SET utf8mb4 NULL,
        `DamageDescription` varchar(2000) CHARACTER SET utf8mb4 NOT NULL,
        `EquipmentTagType` int NULL,
        `OriginFrom` varchar(100) CHARACTER SET utf8mb4 NULL,
        `RepairDataDescription` varchar(2000) CHARACTER SET utf8mb4 NULL,
        `RepairedByName` varchar(100) CHARACTER SET utf8mb4 NULL,
        `FrequencyError` varchar(100) CHARACTER SET utf8mb4 NULL,
        `AfReading` varchar(100) CHARACTER SET utf8mb4 NULL,
        `PowerReading` varchar(100) CHARACTER SET utf8mb4 NULL,
        `VoltageOutNoLoad` varchar(100) CHARACTER SET utf8mb4 NULL,
        `VoltageOutWithLoad` varchar(100) CHARACTER SET utf8mb4 NULL,
        `PhysicalCondition` varchar(100) CHARACTER SET utf8mb4 NULL,
        `DisplayCondition` varchar(100) CHARACTER SET utf8mb4 NULL,
        `Status` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `AssignedTechnicianUserId` int NOT NULL,
        `OpenedByUserId` int NOT NULL,
        `OpenedAt` datetime(6) NOT NULL,
        `ClosedAt` datetime(6) NULL,
        `WorkshopTechnicianId` int NULL,
        `CurrentHandoverId` int NULL,
        `CustomStatusId` int NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `UpdatedAt` datetime(6) NULL,
        `IsDeleted` tinyint(1) NOT NULL,
        `DeletedAt` datetime(6) NULL,
        `DeletedByUserId` int NULL,
        `AccumulatedProgressDurationMinutes` int NOT NULL,
        `CurrentProgressStartedAt` datetime(6) NULL,
        CONSTRAINT `PK_RadioRepairJobs` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_RadioRepairJobs_Radios_RadioId` FOREIGN KEY (`RadioId`) REFERENCES `Radios` (`Id`) ON DELETE SET NULL,
        CONSTRAINT `FK_RadioRepairJobs_RepairJobCustomStatuses_CustomStatusId` FOREIGN KEY (`CustomStatusId`) REFERENCES `RepairJobCustomStatuses` (`Id`),
        CONSTRAINT `FK_RadioRepairJobs_Users_AssignedTechnicianUserId` FOREIGN KEY (`AssignedTechnicianUserId`) REFERENCES `Users` (`UserId`) ON DELETE RESTRICT,
        CONSTRAINT `FK_RadioRepairJobs_Users_OpenedByUserId` FOREIGN KEY (`OpenedByUserId`) REFERENCES `Users` (`UserId`) ON DELETE RESTRICT,
        CONSTRAINT `FK_RadioRepairJobs_WorkshopTechnicians_WorkshopTechnicianId` FOREIGN KEY (`WorkshopTechnicianId`) REFERENCES `WorkshopTechnicians` (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `RadioConventionalHistories` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `RadioConventionalId` int NOT NULL,
        `PreviousUnitNumber` varchar(50) CHARACTER SET utf8mb4 NULL,
        `PreviousDept` varchar(100) CHARACTER SET utf8mb4 NULL,
        `PreviousFleet` varchar(50) CHARACTER SET utf8mb4 NULL,
        `NewUnitNumber` varchar(50) CHARACTER SET utf8mb4 NULL,
        `NewDept` varchar(100) CHARACTER SET utf8mb4 NULL,
        `NewFleet` varchar(50) CHARACTER SET utf8mb4 NULL,
        `ChangeType` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `Notes` longtext CHARACTER SET utf8mb4 NULL,
        `ChangedAt` datetime(6) NOT NULL DEFAULT UTC_TIMESTAMP(),
        `ChangedBy` int NULL,
        CONSTRAINT `PK_RadioConventionalHistories` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_RadioConventionalHistories_RadioConventionals_RadioConventio~` FOREIGN KEY (`RadioConventionalId`) REFERENCES `RadioConventionals` (`Id`) ON DELETE CASCADE,
        CONSTRAINT `FK_RadioConventionalHistories_Users_ChangedBy` FOREIGN KEY (`ChangedBy`) REFERENCES `Users` (`UserId`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `RadioScraps` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `ScrapCategory` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `TypeRadio` varchar(100) CHARACTER SET utf8mb4 NULL,
        `SerialNumber` varchar(100) CHARACTER SET utf8mb4 NULL,
        `JobNumber` varchar(50) CHARACTER SET utf8mb4 NULL,
        `DateScrap` datetime(6) NOT NULL,
        `Remarks` longtext CHARACTER SET utf8mb4 NULL,
        `SourceTrunkingId` int NULL,
        `SourceConventionalId` int NULL,
        `SourceGrafirId` int NULL,
        `CreatedAt` datetime(6) NOT NULL DEFAULT UTC_TIMESTAMP(),
        `CreatedBy` int NULL,
        CONSTRAINT `PK_RadioScraps` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_RadioScraps_RadioConventionals_SourceConventionalId` FOREIGN KEY (`SourceConventionalId`) REFERENCES `RadioConventionals` (`Id`) ON DELETE SET NULL,
        CONSTRAINT `FK_RadioScraps_RadioGrafirs_SourceGrafirId` FOREIGN KEY (`SourceGrafirId`) REFERENCES `RadioGrafirs` (`Id`) ON DELETE SET NULL,
        CONSTRAINT `FK_RadioScraps_RadioTrunkings_SourceTrunkingId` FOREIGN KEY (`SourceTrunkingId`) REFERENCES `RadioTrunkings` (`Id`) ON DELETE SET NULL,
        CONSTRAINT `FK_RadioScraps_Users_CreatedBy` FOREIGN KEY (`CreatedBy`) REFERENCES `Users` (`UserId`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `RadioTrunkingHistories` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `RadioTrunkingId` int NOT NULL,
        `PreviousUnitNumber` varchar(50) CHARACTER SET utf8mb4 NULL,
        `PreviousDept` varchar(100) CHARACTER SET utf8mb4 NULL,
        `PreviousFleet` varchar(50) CHARACTER SET utf8mb4 NULL,
        `NewUnitNumber` varchar(50) CHARACTER SET utf8mb4 NULL,
        `NewDept` varchar(100) CHARACTER SET utf8mb4 NULL,
        `NewFleet` varchar(50) CHARACTER SET utf8mb4 NULL,
        `ChangeType` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `Notes` longtext CHARACTER SET utf8mb4 NULL,
        `ChangedAt` datetime(6) NOT NULL DEFAULT UTC_TIMESTAMP(),
        `ChangedBy` int NULL,
        CONSTRAINT `PK_RadioTrunkingHistories` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_RadioTrunkingHistories_RadioTrunkings_RadioTrunkingId` FOREIGN KEY (`RadioTrunkingId`) REFERENCES `RadioTrunkings` (`Id`) ON DELETE CASCADE,
        CONSTRAINT `FK_RadioTrunkingHistories_Users_ChangedBy` FOREIGN KEY (`ChangedBy`) REFERENCES `Users` (`UserId`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `RadioHandovers` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `HandoverNumber` varchar(30) CHARACTER SET utf8mb4 NOT NULL,
        `HandoverType` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `RadioRepairJobId` int NOT NULL,
        `RadioId` int NULL,
        `RadioSerialNumber` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `BatterySerialNumber` varchar(100) CHARACTER SET utf8mb4 NULL,
        `EquipmentName` varchar(100) CHARACTER SET utf8mb4 NULL,
        `UnitNumber` varchar(100) CHARACTER SET utf8mb4 NULL,
        `RadioOwnerLabel` varchar(200) CHARACTER SET utf8mb4 NULL,
        `OwnerDivision` varchar(100) CHARACTER SET utf8mb4 NULL,
        `OwnerDepartment` varchar(100) CHARACTER SET utf8mb4 NULL,
        `RadioPhotoBase64` longtext CHARACTER SET utf8mb4 NULL,
        `HandedOverSignatureBase64` longtext CHARACTER SET utf8mb4 NULL,
        `ReceiverSignatureBase64` longtext CHARACTER SET utf8mb4 NULL,
        `Remarks` varchar(1000) CHARACTER SET utf8mb4 NULL,
        `EquipmentTagType` int NOT NULL,
        `NoJobErp` varchar(100) CHARACTER SET utf8mb4 NULL,
        `OriginFrom` varchar(200) CHARACTER SET utf8mb4 NULL,
        `RepairDataDescription` varchar(2000) CHARACTER SET utf8mb4 NULL,
        `RepairedByName` varchar(200) CHARACTER SET utf8mb4 NULL,
        `FrequencyError` varchar(100) CHARACTER SET utf8mb4 NULL,
        `AfReading` varchar(100) CHARACTER SET utf8mb4 NULL,
        `PowerReading` varchar(100) CHARACTER SET utf8mb4 NULL,
        `VoltageOutNoLoad` varchar(100) CHARACTER SET utf8mb4 NULL,
        `VoltageOutWithLoad` varchar(100) CHARACTER SET utf8mb4 NULL,
        `PhysicalCondition` varchar(500) CHARACTER SET utf8mb4 NULL,
        `DisplayCondition` varchar(500) CHARACTER SET utf8mb4 NULL,
        `HandedOverByUserId` int NOT NULL,
        `ReceivedByUserId` int NOT NULL,
        `PicReceiverName` varchar(200) CHARACTER SET utf8mb4 NULL,
        `WorkshopTechnicianId` int NULL,
        `HandedOverByWorkshopTechnicianId` int NULL,
        `HandoverAt` datetime(6) NOT NULL,
        `SignedAt` datetime(6) NULL,
        `Status` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `UpdatedAt` datetime(6) NULL,
        `IsDeleted` tinyint(1) NOT NULL,
        `DeletedAt` datetime(6) NULL,
        `DeletedByUserId` int NULL,
        CONSTRAINT `PK_RadioHandovers` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_RadioHandovers_RadioRepairJobs_RadioRepairJobId` FOREIGN KEY (`RadioRepairJobId`) REFERENCES `RadioRepairJobs` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_RadioHandovers_Radios_RadioId` FOREIGN KEY (`RadioId`) REFERENCES `Radios` (`Id`),
        CONSTRAINT `FK_RadioHandovers_Users_HandedOverByUserId` FOREIGN KEY (`HandedOverByUserId`) REFERENCES `Users` (`UserId`) ON DELETE CASCADE,
        CONSTRAINT `FK_RadioHandovers_Users_ReceivedByUserId` FOREIGN KEY (`ReceivedByUserId`) REFERENCES `Users` (`UserId`) ON DELETE CASCADE,
        CONSTRAINT `FK_RadioHandovers_WorkshopTechnicians_HandedOverByWorkshopTechn~` FOREIGN KEY (`HandedOverByWorkshopTechnicianId`) REFERENCES `WorkshopTechnicians` (`Id`),
        CONSTRAINT `FK_RadioHandovers_WorkshopTechnicians_WorkshopTechnicianId` FOREIGN KEY (`WorkshopTechnicianId`) REFERENCES `WorkshopTechnicians` (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `RadioRepairJobStatusLogs` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `JobId` int NOT NULL,
        `FromStatus` varchar(50) CHARACTER SET utf8mb4 NULL,
        `ToStatus` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `Note` varchar(500) CHARACTER SET utf8mb4 NULL,
        `UserId` int NOT NULL,
        `WorkshopTechnicianName` varchar(100) CHARACTER SET utf8mb4 NULL,
        `At` datetime(6) NOT NULL,
        CONSTRAINT `PK_RadioRepairJobStatusLogs` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_RadioRepairJobStatusLogs_RadioRepairJobs_JobId` FOREIGN KEY (`JobId`) REFERENCES `RadioRepairJobs` (`Id`) ON DELETE CASCADE,
        CONSTRAINT `FK_RadioRepairJobStatusLogs_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`UserId`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `WarehousePartBorrows` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `BorrowNumber` varchar(30) CHARACTER SET utf8mb4 NOT NULL,
        `BorrowedByUserId` int NOT NULL,
        `BorrowerName` varchar(200) CHARACTER SET utf8mb4 NULL,
        `Purpose` varchar(1000) CHARACTER SET utf8mb4 NULL,
        `RelatedRepairJobId` int NULL,
        `TicketNumber` varchar(100) CHARACTER SET utf8mb4 NULL,
        `Status` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `RequestedAt` datetime(6) NOT NULL,
        `ApprovedByUserId` int NULL,
        `ApprovedAt` datetime(6) NULL,
        `ApprovalNote` varchar(500) CHARACTER SET utf8mb4 NULL,
        `RejectedByUserId` int NULL,
        `RejectedAt` datetime(6) NULL,
        `RejectionReason` varchar(500) CHARACTER SET utf8mb4 NULL,
        `IssuedAt` datetime(6) NULL,
        `IssuedByUserId` int NULL,
        `IssuerSignatureBase64` longtext CHARACTER SET utf8mb4 NULL,
        `ReceiverSignatureBase64` longtext CHARACTER SET utf8mb4 NULL,
        `ReturnIssuerSignatureBase64` longtext CHARACTER SET utf8mb4 NULL,
        `ReturnReceiverSignatureBase64` longtext CHARACTER SET utf8mb4 NULL,
        `ReturnedAt` datetime(6) NULL,
        `ReturnCondition` varchar(200) CHARACTER SET utf8mb4 NULL,
        `ReturnNote` varchar(500) CHARACTER SET utf8mb4 NULL,
        `ReturnedByName` varchar(200) CHARACTER SET utf8mb4 NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `UpdatedAt` datetime(6) NULL,
        `IsActive` tinyint(1) NOT NULL,
        CONSTRAINT `PK_WarehousePartBorrows` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_WarehousePartBorrows_RadioRepairJobs_RelatedRepairJobId` FOREIGN KEY (`RelatedRepairJobId`) REFERENCES `RadioRepairJobs` (`Id`) ON DELETE SET NULL,
        CONSTRAINT `FK_WarehousePartBorrows_Users_BorrowedByUserId` FOREIGN KEY (`BorrowedByUserId`) REFERENCES `Users` (`UserId`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `RadioHandoverAccessories` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `RadioHandoverId` int NOT NULL,
        `AccessoryCode` varchar(50) CHARACTER SET utf8mb4 NULL,
        `ItemName` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
        `Quantity` int NOT NULL,
        `Unit` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
        `Description` varchar(500) CHARACTER SET utf8mb4 NULL,
        `SerialNumber` varchar(100) CHARACTER SET utf8mb4 NULL,
        CONSTRAINT `PK_RadioHandoverAccessories` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_RadioHandoverAccessories_RadioHandovers_RadioHandoverId` FOREIGN KEY (`RadioHandoverId`) REFERENCES `RadioHandovers` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `RadioHandoverPhotos` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `RadioHandoverId` int NOT NULL,
        `SortOrder` int NOT NULL,
        `PhotoBase64` longtext CHARACTER SET utf8mb4 NOT NULL,
        CONSTRAINT `PK_RadioHandoverPhotos` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_RadioHandoverPhotos_RadioHandovers_RadioHandoverId` FOREIGN KEY (`RadioHandoverId`) REFERENCES `RadioHandovers` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `WarehousePartBorrowItems` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `BorrowId` int NOT NULL,
        `PartDescription` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
        `PartCode` varchar(100) CHARACTER SET utf8mb4 NULL,
        `Unit` varchar(50) CHARACTER SET utf8mb4 NULL,
        `Quantity` int NOT NULL,
        CONSTRAINT `PK_WarehousePartBorrowItems` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_WarehousePartBorrowItems_WarehousePartBorrows_BorrowId` FOREIGN KEY (`BorrowId`) REFERENCES `WarehousePartBorrows` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE TABLE `WarehousePartBorrowStatusLogs` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `BorrowId` int NOT NULL,
        `FromStatus` varchar(50) CHARACTER SET utf8mb4 NULL,
        `ToStatus` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `Note` varchar(500) CHARACTER SET utf8mb4 NULL,
        `UserId` int NOT NULL,
        `At` datetime(6) NOT NULL,
        CONSTRAINT `PK_WarehousePartBorrowStatusLogs` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_WarehousePartBorrowStatusLogs_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`UserId`) ON DELETE CASCADE,
        CONSTRAINT `FK_WarehousePartBorrowStatusLogs_WarehousePartBorrows_BorrowId` FOREIGN KEY (`BorrowId`) REFERENCES `WarehousePartBorrows` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_ActivityLog_Module_Time` ON `ActivityLogs` (`Module`, `Timestamp`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_ActivityLog_UserId` ON `ActivityLogs` (`UserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_CallRecord_CloseReason` ON `CallRecords` (`CallCloseReason`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_CallRecord_Date` ON `CallRecords` (`CallDate`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_CallRecord_DateTime` ON `CallRecords` (`CallDate`, `CallTime`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_CallSummary_DateHour` ON `CallSummaries` (`SummaryDate`, `HourGroup`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_CctvKpc_Brand` ON `CctvKpcs` (`Brand`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_CctvKpc_IsActive` ON `CctvKpcs` (`IsActive`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_CctvKpc_Severity` ON `CctvKpcs` (`Severity`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_Companies_Code` ON `Companies` (`Code`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_Companies_CreatedBy` ON `Companies` (`CreatedBy`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_Companies_IsActive` ON `Companies` (`IsActive`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_Companies_UpdatedBy` ON `Companies` (`UpdatedBy`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_Divisions_Code` ON `Divisions` (`Code`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_Divisions_CreatedBy` ON `Divisions` (`CreatedBy`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_Divisions_UpdatedBy` ON `Divisions` (`UpdatedBy`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_DocumentTypes_Code` ON `DocumentTypes` (`Code`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_DocumentTypes_CreatedBy` ON `DocumentTypes` (`CreatedBy`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_DocumentTypes_IsActive` ON `DocumentTypes` (`IsActive`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_DocumentTypes_UpdatedBy` ON `DocumentTypes` (`UpdatedBy`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_Gatepass_FormattedNumber` ON `Gatepasses` (`FormattedNumber`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_Gatepass_Status` ON `Gatepasses` (`Status`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_Gatepass_UniqueSequence` ON `Gatepasses` (`Year`, `SequenceNumber`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_Gatepass_YearMonth` ON `Gatepasses` (`Year`, `Month`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_Gatepasses_CreatedBy` ON `Gatepasses` (`CreatedBy`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_Gatepasses_SignedByUserId` ON `Gatepasses` (`SignedByUserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_Gatepasses_UpdatedBy` ON `Gatepasses` (`UpdatedBy`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_GatepassItem_GatepassId` ON `GatepassItems` (`GatepassId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_InspeksiTemuanKpc_Deleted_Status` ON `InspeksiTemuanKpcs` (`IsDeleted`, `Status`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_InspeksiTemuanKpc_Ruang` ON `InspeksiTemuanKpcs` (`Ruang`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_InspeksiTemuanKpc_Tanggal` ON `InspeksiTemuanKpcs` (`TanggalTemuan`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_InspeksiTemuanKpcs_CreatedBy` ON `InspeksiTemuanKpcs` (`CreatedBy`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_InspeksiTemuanKpcs_DeletedBy` ON `InspeksiTemuanKpcs` (`DeletedBy`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_InspeksiTemuanKpcs_UpdatedBy` ON `InspeksiTemuanKpcs` (`UpdatedBy`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_InternalLinkHistory_Date` ON `InternalLinkHistories` (`Date`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_InternalLinkHistory_LinkDate` ON `InternalLinkHistories` (`InternalLinkId`, `Date`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_InternalLink_LinkName` ON `InternalLinks` (`LinkName`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_LetterNumber_FormattedNumber` ON `LetterNumbers` (`FormattedNumber`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_LetterNumber_LetterDate` ON `LetterNumbers` (`LetterDate`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_LetterNumber_Status` ON `LetterNumbers` (`Status`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_LetterNumber_UniqueSequence` ON `LetterNumbers` (`CompanyId`, `DocumentTypeId`, `Year`, `SequenceNumber`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_LetterNumber_YearMonth` ON `LetterNumbers` (`Year`, `Month`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_LetterNumbers_CreatedBy` ON `LetterNumbers` (`CreatedBy`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_LetterNumbers_DocumentTypeId` ON `LetterNumbers` (`DocumentTypeId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_LetterNumbers_UpdatedBy` ON `LetterNumbers` (`UpdatedBy`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_NecLinks_FarEndTowerId` ON `NecLinks` (`FarEndTowerId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_NecLinks_LinkName` ON `NecLinks` (`LinkName`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_NecLinks_NearEndTowerId` ON `NecLinks` (`NearEndTowerId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_NecRslHistory_Date` ON `NecRslHistories` (`Date`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_NecRslHistory_LinkDate` ON `NecRslHistories` (`NecLinkId`, `Date`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_NecTowers_Name` ON `NecTowers` (`Name`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_Notifications_IsRead` ON `Notifications` (`IsRead`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_Notifications_RecipientRoleName` ON `Notifications` (`RecipientRoleName`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_Notifications_RecipientUserId` ON `Notifications` (`RecipientUserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_OperationalDocumentNotificationHistories_DaysRemaining` ON `OperationalDocumentNotificationHistories` (`DaysRemaining`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_OperationalDocumentNotificationHistories_OperationalDocument~` ON `OperationalDocumentNotificationHistories` (`OperationalDocumentId`, `NotifiedAt`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_OperationalDocuments_FollowUpStatus` ON `OperationalDocuments` (`FollowUpStatus`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_OperationalDocuments_Type` ON `OperationalDocuments` (`Type`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_OperationalDocuments_ValidUntil` ON `OperationalDocuments` (`ValidUntil`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_OperationalDocumentTypes_Name` ON `OperationalDocumentTypes` (`Name`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_PasswordResetToken_Token` ON `PasswordResetTokens` (`Token`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_PasswordResetToken_User_Used` ON `PasswordResetTokens` (`UserId`, `IsUsed`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_Permission_PermissionName` ON `Permissions` (`PermissionName`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_PmSchedules_PmSiteId` ON `PmSchedules` (`PmSiteId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_PmScheduleTasks_PmScheduleId` ON `PmScheduleTasks` (`PmScheduleId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_Quotation_CustomerId` ON `Quotations` (`CustomerId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_Quotation_FormattedNumber` ON `Quotations` (`FormattedNumber`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_Quotation_Status` ON `Quotations` (`Status`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_Quotation_UniqueSequence` ON `Quotations` (`Year`, `SequenceNumber`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_Quotation_YearMonth` ON `Quotations` (`Year`, `Month`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_Quotations_CreatedBy` ON `Quotations` (`CreatedBy`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_Quotations_UpdatedBy` ON `Quotations` (`UpdatedBy`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_RadioConventionalHistories_ChangedBy` ON `RadioConventionalHistories` (`ChangedBy`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_RadioConventionalHistory_ChangedAt` ON `RadioConventionalHistories` (`ChangedAt`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_RadioConventionalHistory_RadioId` ON `RadioConventionalHistories` (`RadioConventionalId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_RadioConventional_RadioId` ON `RadioConventionals` (`RadioId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_RadioConventional_UnitNumber` ON `RadioConventionals` (`UnitNumber`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_RadioConventionals_CreatedBy` ON `RadioConventionals` (`CreatedBy`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_RadioConventionals_GrafirId` ON `RadioConventionals` (`GrafirId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_RadioConventionals_UpdatedBy` ON `RadioConventionals` (`UpdatedBy`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_RadioGrafir_NoAsset` ON `RadioGrafirs` (`NoAsset`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_RadioGrafir_SerialNumber` ON `RadioGrafirs` (`SerialNumber`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_RadioGrafirs_CreatedBy` ON `RadioGrafirs` (`CreatedBy`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_RadioGrafirs_UpdatedBy` ON `RadioGrafirs` (`UpdatedBy`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_RadioHandoverAccessories_RadioHandoverId` ON `RadioHandoverAccessories` (`RadioHandoverId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_RadioHandoverPhotos_RadioHandoverId_SortOrder` ON `RadioHandoverPhotos` (`RadioHandoverId`, `SortOrder`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_RadioHandovers_HandedOverByUserId` ON `RadioHandovers` (`HandedOverByUserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_RadioHandovers_HandedOverByWorkshopTechnicianId` ON `RadioHandovers` (`HandedOverByWorkshopTechnicianId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_RadioHandovers_HandoverNumber` ON `RadioHandovers` (`HandoverNumber`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_RadioHandovers_IsDeleted_HandoverAt` ON `RadioHandovers` (`IsDeleted`, `HandoverAt`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_RadioHandovers_RadioId` ON `RadioHandovers` (`RadioId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_RadioHandovers_RadioRepairJobId` ON `RadioHandovers` (`RadioRepairJobId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_RadioHandovers_ReceivedByUserId` ON `RadioHandovers` (`ReceivedByUserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_RadioHandovers_WorkshopTechnicianId` ON `RadioHandovers` (`WorkshopTechnicianId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_RadioHistories_RadioId` ON `RadioHistories` (`RadioId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_RadioRepairJobs_AssignedTechnicianUserId` ON `RadioRepairJobs` (`AssignedTechnicianUserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_RadioRepairJobs_CustomStatusId` ON `RadioRepairJobs` (`CustomStatusId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_RadioRepairJobs_IsDeleted_HelpdeskTicketNumber_RadioSerialNu~` ON `RadioRepairJobs` (`IsDeleted`, `HelpdeskTicketNumber`, `RadioSerialNumber`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_RadioRepairJobs_JobNumber` ON `RadioRepairJobs` (`JobNumber`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_RadioRepairJobs_OpenedByUserId` ON `RadioRepairJobs` (`OpenedByUserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_RadioRepairJobs_RadioId` ON `RadioRepairJobs` (`RadioId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_RadioRepairJobs_WorkshopTechnicianId` ON `RadioRepairJobs` (`WorkshopTechnicianId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_RadioRepairJobStatusLogs_JobId` ON `RadioRepairJobStatusLogs` (`JobId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_RadioRepairJobStatusLogs_UserId` ON `RadioRepairJobStatusLogs` (`UserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_RadioScrap_Category` ON `RadioScraps` (`ScrapCategory`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_RadioScrap_Category_Date` ON `RadioScraps` (`ScrapCategory`, `DateScrap`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_RadioScrap_DateScrap` ON `RadioScraps` (`DateScrap`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_RadioScraps_CreatedBy` ON `RadioScraps` (`CreatedBy`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_RadioScraps_SourceConventionalId` ON `RadioScraps` (`SourceConventionalId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_RadioScraps_SourceGrafirId` ON `RadioScraps` (`SourceGrafirId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_RadioScraps_SourceTrunkingId` ON `RadioScraps` (`SourceTrunkingId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_RadioTrunkingHistories_ChangedBy` ON `RadioTrunkingHistories` (`ChangedBy`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_RadioTrunkingHistory_ChangedAt` ON `RadioTrunkingHistories` (`ChangedAt`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_RadioTrunkingHistory_RadioId` ON `RadioTrunkingHistories` (`RadioTrunkingId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_RadioTrunking_RadioId` ON `RadioTrunkings` (`RadioId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_RadioTrunking_SerialNumber` ON `RadioTrunkings` (`SerialNumber`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_RadioTrunking_UnitNumber` ON `RadioTrunkings` (`UnitNumber`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_RadioTrunkings_CreatedBy` ON `RadioTrunkings` (`CreatedBy`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_RadioTrunkings_GrafirId` ON `RadioTrunkings` (`GrafirId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_RadioTrunkings_UpdatedBy` ON `RadioTrunkings` (`UpdatedBy`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_RepairJobCustomStatuses_CreatedByUserId` ON `RepairJobCustomStatuses` (`CreatedByUserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_RolePermission_RoleId_PermissionId` ON `RolePermissions` (`RoleId`, `PermissionId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_RolePermissions_PermissionId` ON `RolePermissions` (`PermissionId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_Role_RoleName` ON `Roles` (`RoleName`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_SwrChannels_SwrSiteId_ChannelName` ON `SwrChannels` (`SwrSiteId`, `ChannelName`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_SwrHistories_Date` ON `SwrHistories` (`Date`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_SwrHistories_SwrChannelId_Date` ON `SwrHistories` (`SwrChannelId`, `Date`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_SwrSites_Name` ON `SwrSites` (`Name`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_Users_RoleId` ON `Users` (`RoleId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_WarehousePartBorrowItems_BorrowId` ON `WarehousePartBorrowItems` (`BorrowId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_WarehousePartBorrows_BorrowedByUserId` ON `WarehousePartBorrows` (`BorrowedByUserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_WarehousePartBorrows_BorrowNumber` ON `WarehousePartBorrows` (`BorrowNumber`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_WarehousePartBorrows_RelatedRepairJobId` ON `WarehousePartBorrows` (`RelatedRepairJobId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_WarehousePartBorrowStatusLogs_BorrowId` ON `WarehousePartBorrowStatusLogs` (`BorrowId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_WarehousePartBorrowStatusLogs_UserId` ON `WarehousePartBorrowStatusLogs` (`UserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    CREATE INDEX `IX_WorkshopTechnicians_UserId` ON `WorkshopTechnicians` (`UserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710010113_InitialCreate') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260710010113_InitialCreate', '8.0.11');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710062719_MigrateToTelegram') THEN

    DROP TABLE `WhatsAppQueues`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710062719_MigrateToTelegram') THEN

    ALTER TABLE `OperationalDocuments` RENAME COLUMN `PicPhone` TO `PicTelegramId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710062719_MigrateToTelegram') THEN

    CREATE TABLE `TelegramQueues` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `ChatId` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `Message` longtext CHARACTER SET utf8mb4 NOT NULL,
        `Status` varchar(20) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'Pending',
        `RetryCount` int NOT NULL,
        `MaxRetry` int NOT NULL,
        `ErrorMessage` varchar(1000) CHARACTER SET utf8mb4 NULL,
        `CreatedAt` datetime(6) NOT NULL DEFAULT UTC_TIMESTAMP(),
        `SentAt` datetime(6) NULL,
        CONSTRAINT `PK_TelegramQueues` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710062719_MigrateToTelegram') THEN

    CREATE INDEX `IX_TelegramQueue_Status` ON `TelegramQueues` (`Status`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710062719_MigrateToTelegram') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260710062719_MigrateToTelegram', '8.0.11');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710073913_AddFollowUpRemark') THEN

    ALTER TABLE `OperationalDocuments` ADD `FollowUpRemark` longtext CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710073913_AddFollowUpRemark') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260710073913_AddFollowUpRemark', '8.0.11');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710093229_AddIsWarrantyToRadioRepairJob') THEN

    ALTER TABLE `RadioRepairJobs` ADD `IsWarranty` tinyint(1) NOT NULL DEFAULT FALSE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260710093229_AddIsWarrantyToRadioRepairJob') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260710093229_AddIsWarrantyToRadioRepairJob', '8.0.11');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260713053155_AddPmScheduleCompletion') THEN

    ALTER TABLE `PmScheduleTasks` ADD `CompletedAt` datetime(6) NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260713053155_AddPmScheduleCompletion') THEN

    ALTER TABLE `PmScheduleTasks` ADD `CompletedByUserId` int NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260713053155_AddPmScheduleCompletion') THEN

    ALTER TABLE `PmScheduleTasks` ADD `IsCompleted` tinyint(1) NOT NULL DEFAULT FALSE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260713053155_AddPmScheduleCompletion') THEN

    ALTER TABLE `PmScheduleTasks` ADD `Remarks` varchar(1000) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260713053155_AddPmScheduleCompletion') THEN

    CREATE INDEX `IX_PmScheduleTasks_CompletedByUserId` ON `PmScheduleTasks` (`CompletedByUserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260713053155_AddPmScheduleCompletion') THEN

    ALTER TABLE `PmScheduleTasks` ADD CONSTRAINT `FK_PmScheduleTasks_Users_CompletedByUserId` FOREIGN KEY (`CompletedByUserId`) REFERENCES `Users` (`UserId`) ON DELETE RESTRICT;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260713053155_AddPmScheduleCompletion') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260713053155_AddPmScheduleCompletion', '8.0.11');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

