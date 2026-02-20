-- ============================================
-- Migration: Add EmployeeId & Division to Users + Digital Signature to Gatepasses
-- Date: 2025-02-13
-- ============================================

-- Phase A: User Profile fields
ALTER TABLE `Users` ADD COLUMN `EmployeeId` VARCHAR(50) NULL AFTER `PhotoUrl`;
ALTER TABLE `Users` ADD COLUMN `Division` VARCHAR(100) NULL AFTER `EmployeeId`;

-- Phase C: Digital Signature fields on Gatepasses
ALTER TABLE `Gatepasses` ADD COLUMN `SignedByUserId` INT NULL AFTER `Status`;
ALTER TABLE `Gatepasses` ADD COLUMN `SignedAt` DATETIME NULL AFTER `SignedByUserId`;
ALTER TABLE `Gatepasses` ADD COLUMN `VerificationToken` VARCHAR(32) NULL AFTER `SignedAt`;

-- Foreign key for SignedByUser
ALTER TABLE `Gatepasses` ADD CONSTRAINT `FK_Gatepasses_Users_SignedByUserId`
    FOREIGN KEY (`SignedByUserId`) REFERENCES `Users`(`UserId`) ON DELETE RESTRICT;

-- Index for faster verification token lookup
CREATE INDEX `IX_Gatepass_VerificationToken` ON `Gatepasses` (`VerificationToken`);
