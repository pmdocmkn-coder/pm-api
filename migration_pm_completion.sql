START TRANSACTION;

ALTER TABLE `PmScheduleTasks` ADD `CompletedAt` datetime(6) NULL;

ALTER TABLE `PmScheduleTasks` ADD `CompletedByUserId` int NULL;

ALTER TABLE `PmScheduleTasks` ADD `IsCompleted` tinyint(1) NOT NULL DEFAULT FALSE;

ALTER TABLE `PmScheduleTasks` ADD `Remarks` varchar(1000) CHARACTER SET utf8mb4 NULL;

CREATE INDEX `IX_PmScheduleTasks_CompletedByUserId` ON `PmScheduleTasks` (`CompletedByUserId`);

ALTER TABLE `PmScheduleTasks` ADD CONSTRAINT `FK_PmScheduleTasks_Users_CompletedByUserId` FOREIGN KEY (`CompletedByUserId`) REFERENCES `Users` (`UserId`) ON DELETE RESTRICT;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260713053155_AddPmScheduleCompletion', '8.0.11');

COMMIT;

