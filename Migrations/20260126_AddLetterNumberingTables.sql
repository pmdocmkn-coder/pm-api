-- Migration: AddLetterNumberingTables
-- Created: 2026-01-26

-- Create DocumentTypes table
CREATE TABLE IF NOT EXISTS `DocumentTypes` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Code` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `Name` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `Description` varchar(500) CHARACTER SET utf8mb4 NULL,
    `IsActive` tinyint(1) NOT NULL DEFAULT 1,
    `CreatedAt` datetime(6) NOT NULL DEFAULT UTC_TIMESTAMP(),
    `UpdatedAt` datetime(6) NULL,
    `CreatedBy` int NOT NULL,
    `UpdatedBy` int NULL,
    CONSTRAINT `PK_DocumentTypes` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_DocumentTypes_Users_CreatedBy` FOREIGN KEY (`CreatedBy`) REFERENCES `Users` (`UserId`) ON DELETE RESTRICT,
    CONSTRAINT `FK_DocumentTypes_Users_UpdatedBy` FOREIGN KEY (`UpdatedBy`) REFERENCES `Users` (`UserId`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

-- Create Companies table
CREATE TABLE IF NOT EXISTS `Companies` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Code` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `Name` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `Address` varchar(500) CHARACTER SET utf8mb4 NULL,
    `IsActive` tinyint(1) NOT NULL DEFAULT 1,
    `CreatedAt` datetime(6) NOT NULL DEFAULT UTC_TIMESTAMP(),
    `UpdatedAt` datetime(6) NULL,
    `CreatedBy` int NOT NULL,
    `UpdatedBy` int NULL,
    CONSTRAINT `PK_Companies` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Companies_Users_CreatedBy` FOREIGN KEY (`CreatedBy`) REFERENCES `Users` (`UserId`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Companies_Users_UpdatedBy` FOREIGN KEY (`UpdatedBy`) REFERENCES `Users` (`UserId`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

-- Create LetterNumbers table
CREATE TABLE IF NOT EXISTS `LetterNumbers` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `FormattedNumber` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `SequenceNumber` int NOT NULL,
    `Year` int NOT NULL,
    `Month` int NOT NULL,
    `LetterDate` date NOT NULL,
    `Subject` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
    `Recipient` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `AttachmentUrl` varchar(1000) CHARACTER SET utf8mb4 NULL,
    `Status` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `CompanyId` int NOT NULL,
    `DocumentTypeId` int NOT NULL,
    `CreatedAt` datetime(6) NOT NULL DEFAULT UTC_TIMESTAMP(),
    `UpdatedAt` datetime(6) NULL,
    `CreatedBy` int NOT NULL,
    `UpdatedBy` int NULL,
    CONSTRAINT `PK_LetterNumbers` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_LetterNumbers_Companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `Companies` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_LetterNumbers_DocumentTypes_DocumentTypeId` FOREIGN KEY (`DocumentTypeId`) REFERENCES `DocumentTypes` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_LetterNumbers_Users_CreatedBy` FOREIGN KEY (`CreatedBy`) REFERENCES `Users` (`UserId`) ON DELETE RESTRICT,
    CONSTRAINT `FK_LetterNumbers_Users_UpdatedBy` FOREIGN KEY (`UpdatedBy`) REFERENCES `Users` (`UserId`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

-- Create indexes for DocumentTypes
CREATE UNIQUE INDEX `IX_DocumentTypes_Code` ON `DocumentTypes` (`Code`);
CREATE INDEX `IX_DocumentTypes_IsActive` ON `DocumentTypes` (`IsActive`);
CREATE INDEX `IX_DocumentTypes_CreatedBy` ON `DocumentTypes` (`CreatedBy`);
CREATE INDEX `IX_DocumentTypes_UpdatedBy` ON `DocumentTypes` (`UpdatedBy`);

-- Create indexes for Companies
CREATE UNIQUE INDEX `IX_Companies_Code` ON `Companies` (`Code`);
CREATE INDEX `IX_Companies_IsActive` ON `Companies` (`IsActive`);
CREATE INDEX `IX_Companies_CreatedBy` ON `Companies` (`CreatedBy`);
CREATE INDEX `IX_Companies_UpdatedBy` ON `Companies` (`UpdatedBy`);

-- Create indexes for LetterNumbers
CREATE UNIQUE INDEX `IX_LetterNumber_UniqueSequence` ON `LetterNumbers` (`CompanyId`, `DocumentTypeId`, `Year`, `SequenceNumber`);
CREATE INDEX `IX_LetterNumber_YearMonth` ON `LetterNumbers` (`Year`, `Month`);
CREATE INDEX `IX_LetterNumber_LetterDate` ON `LetterNumbers` (`LetterDate`);
CREATE INDEX `IX_LetterNumber_Status` ON `LetterNumbers` (`Status`);
CREATE INDEX `IX_LetterNumber_FormattedNumber` ON `LetterNumbers` (`FormattedNumber`);
CREATE INDEX `IX_LetterNumbers_CompanyId` ON `LetterNumbers` (`CompanyId`);
CREATE INDEX `IX_LetterNumbers_DocumentTypeId` ON `LetterNumbers` (`DocumentTypeId`);
CREATE INDEX `IX_LetterNumbers_CreatedBy` ON `LetterNumbers` (`CreatedBy`);
CREATE INDEX `IX_LetterNumbers_UpdatedBy` ON `LetterNumbers` (`UpdatedBy`);

-- Insert migration history
INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260126041740_AddLetterNumberingTables', '8.0.0');

-- Sample data (optional)
-- Insert sample document types
INSERT INTO `DocumentTypes` (`Code`, `Name`, `Description`, `IsActive`, `CreatedBy`)
VALUES 
    ('BAO', 'Berita Acara', 'Berita Acara Operasional', 1, 1),
    ('SKT', 'Surat Keterangan', 'Surat Keterangan', 1, 1),
    ('SPT', 'Surat Perintah Tugas', 'Surat Perintah Tugas', 1, 1);

-- Insert sample companies
INSERT INTO `Companies` (`Code`, `Name`, `Address`, `IsActive`, `CreatedBy`)
VALUES 
    ('KPC', 'PT. Kaltim Prima Coal', 'Sangatta, Kalimantan Timur', 1, 1),
    ('MKN', 'PT. Mahakam', 'Samarinda, Kalimantan Timur', 1, 1);
