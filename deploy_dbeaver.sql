-- ============================================================
-- DEPLOY SCRIPT - VERSI SIMPEL UNTUK DBEAVER
-- Jalankan satu per satu (select tiap blok lalu Ctrl+Enter)
-- ============================================================

-- STEP 1: Buat tabel TelegramQueues (jika belum ada)
CREATE TABLE IF NOT EXISTS `TelegramQueues` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `ChatId` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `Message` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Status` varchar(20) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'Pending',
    `RetryCount` int NOT NULL DEFAULT 0,
    `MaxRetry` int NOT NULL DEFAULT 3,
    `ErrorMessage` varchar(1000) CHARACTER SET utf8mb4 NULL,
    `CreatedAt` datetime(6) NOT NULL DEFAULT (UTC_TIMESTAMP()),
    `SentAt` datetime(6) NULL,
    CONSTRAINT `PK_TelegramQueues` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

-- STEP 2: Buat index TelegramQueues (jika belum ada)
CREATE INDEX IF NOT EXISTS `IX_TelegramQueue_Status` ON `TelegramQueues` (`Status`);

-- STEP 3: Rename kolom PicPhone -> PicTelegramId (jika PicPhone masih ada)
-- NOTE: Jalankan ini hanya jika kolom PicPhone masih ada, skip jika error
ALTER TABLE `OperationalDocuments` 
    CHANGE COLUMN IF EXISTS `PicPhone` `PicTelegramId` varchar(100) CHARACTER SET utf8mb4 NULL;

-- STEP 4: Tambah kolom FollowUpRemark ke OperationalDocuments
ALTER TABLE `OperationalDocuments`
    ADD COLUMN IF NOT EXISTS `FollowUpRemark` longtext CHARACTER SET utf8mb4 NULL;

-- STEP 5: Tambah kolom IsWarranty ke RadioRepairJobs
ALTER TABLE `RadioRepairJobs`
    ADD COLUMN IF NOT EXISTS `IsWarranty` tinyint(1) NOT NULL DEFAULT FALSE;

-- STEP 6: Catat semua migration ke history
INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260710062719_MigrateToTelegram', '8.0.11');

INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260710073913_AddFollowUpRemark', '8.0.11');

INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260710093229_AddIsWarrantyToRadioRepairJob', '8.0.11');

-- STEP 7: Verifikasi hasil
SELECT MigrationId FROM `__EFMigrationsHistory`
WHERE MigrationId LIKE '20260710%'
ORDER BY MigrationId;
