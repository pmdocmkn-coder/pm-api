-- ============================================
-- Migration: Add Divisions Table
-- Date: 2026-02-18
-- Description: Create Divisions master data table
-- ============================================

CREATE TABLE IF NOT EXISTS `Divisions` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `Code` VARCHAR(50) NOT NULL,
    `Name` VARCHAR(200) NOT NULL,
    `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
    `CreatedBy` INT NOT NULL,
    `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `UpdatedAt` DATETIME(6) NULL,
    `UpdatedBy` INT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE INDEX `IX_Divisions_Code` (`Code`),
    INDEX `IX_Divisions_CreatedBy` (`CreatedBy`),
    INDEX `IX_Divisions_UpdatedBy` (`UpdatedBy`),
    CONSTRAINT `FK_Divisions_Users_CreatedBy` FOREIGN KEY (`CreatedBy`) REFERENCES `Users` (`UserId`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Divisions_Users_UpdatedBy` FOREIGN KEY (`UpdatedBy`) REFERENCES `Users` (`UserId`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================
-- Seed Division Permissions
-- ============================================

INSERT IGNORE INTO `Permissions` (`PermissionName`, `Description`, `Module`, `CreatedAt`)
VALUES
    ('divisi.view', 'View divisions', 'Division', NOW()),
    ('divisi.create', 'Create divisions', 'Division', NOW()),
    ('divisi.update', 'Update divisions', 'Division', NOW()),
    ('divisi.delete', 'Delete divisions', 'Division', NOW());

-- ============================================
-- Assign Division Permissions to SuperAdmin (RoleId=1) and Admin (RoleId=2)
-- ============================================

INSERT IGNORE INTO `RolePermissions` (`RoleId`, `PermissionId`)
SELECT 1, `PermissionId` FROM `Permissions` WHERE `PermissionName` LIKE 'divisi.%';

INSERT IGNORE INTO `RolePermissions` (`RoleId`, `PermissionId`)
SELECT 2, `PermissionId` FROM `Permissions` WHERE `PermissionName` LIKE 'divisi.%';
