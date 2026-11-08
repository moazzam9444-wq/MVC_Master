BEGIN TRANSACTION;
IF DB_NAME() <> N'Kicks' THROW 51000, 'This reviewed upgrade is restricted to Kicks.', 1;
IF EXISTS (SELECT 1 FROM [dbo].[amenities] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: amenities. Stop for a data-preserving migration review.', 1;
IF EXISTS (SELECT 1 FROM [dbo].[booking_teams] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: booking_teams. Stop for a data-preserving migration review.', 1;
IF EXISTS (SELECT 1 FROM [dbo].[bookings] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: bookings. Stop for a data-preserving migration review.', 1;
IF EXISTS (SELECT 1 FROM [dbo].[challenge_requests] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: challenge_requests. Stop for a data-preserving migration review.', 1;
IF EXISTS (SELECT 1 FROM [dbo].[challenges] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: challenges. Stop for a data-preserving migration review.', 1;
IF EXISTS (SELECT 1 FROM [dbo].[feedback] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: feedback. Stop for a data-preserving migration review.', 1;
IF EXISTS (SELECT 1 FROM [dbo].[field_amenities] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: field_amenities. Stop for a data-preserving migration review.', 1;
IF EXISTS (SELECT 1 FROM [dbo].[field_images] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: field_images. Stop for a data-preserving migration review.', 1;
IF EXISTS (SELECT 1 FROM [dbo].[fields] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: fields. Stop for a data-preserving migration review.', 1;
IF EXISTS (SELECT 1 FROM [dbo].[game_participants] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: game_participants. Stop for a data-preserving migration review.', 1;
IF EXISTS (SELECT 1 FROM [dbo].[meetings] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: meetings. Stop for a data-preserving migration review.', 1;
IF EXISTS (SELECT 1 FROM [dbo].[messages] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: messages. Stop for a data-preserving migration review.', 1;
IF EXISTS (SELECT 1 FROM [dbo].[open_games] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: open_games. Stop for a data-preserving migration review.', 1;
IF EXISTS (SELECT 1 FROM [dbo].[ownership_documents] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: ownership_documents. Stop for a data-preserving migration review.', 1;
IF EXISTS (SELECT 1 FROM [dbo].[payments] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: payments. Stop for a data-preserving migration review.', 1;
IF EXISTS (SELECT 1 FROM [dbo].[registration_players] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: registration_players. Stop for a data-preserving migration review.', 1;
IF EXISTS (SELECT 1 FROM [dbo].[settings] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: settings. Stop for a data-preserving migration review.', 1;
IF EXISTS (SELECT 1 FROM [dbo].[team_members] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: team_members. Stop for a data-preserving migration review.', 1;
IF EXISTS (SELECT 1 FROM [dbo].[team_requests] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: team_requests. Stop for a data-preserving migration review.', 1;
IF EXISTS (SELECT 1 FROM [dbo].[teams] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: teams. Stop for a data-preserving migration review.', 1;
IF EXISTS (SELECT 1 FROM [dbo].[tournament_draw_entries] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: tournament_draw_entries. Stop for a data-preserving migration review.', 1;
IF EXISTS (SELECT 1 FROM [dbo].[tournament_match_slots] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: tournament_match_slots. Stop for a data-preserving migration review.', 1;
IF EXISTS (SELECT 1 FROM [dbo].[tournament_matches] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: tournament_matches. Stop for a data-preserving migration review.', 1;
IF EXISTS (SELECT 1 FROM [dbo].[tournament_registrations] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: tournament_registrations. Stop for a data-preserving migration review.', 1;
IF EXISTS (SELECT 1 FROM [dbo].[tournaments] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: tournaments. Stop for a data-preserving migration review.', 1;
IF EXISTS (SELECT 1 FROM [dbo].[users] WITH (TABLOCKX,HOLDLOCK)) THROW 51001, 'Pre-upgrade table is populated: users. Stop for a data-preserving migration review.', 1;

ALTER TABLE [dbo].[users] DROP CONSTRAINT [UQ_users_email];

ALTER TABLE [dbo].[users] DROP CONSTRAINT [CK_users_password_iterations];

ALTER TABLE [dbo].[users] DROP CONSTRAINT [CK_users_requested_role_enum];

ALTER TABLE [dbo].[users] DROP CONSTRAINT [CK_users_role_enum];

ALTER TABLE [dbo].[users] DROP CONSTRAINT [CK_users_status_enum];

ALTER TABLE [dbo].[tournaments] DROP CONSTRAINT [CK_tournaments_capacity];

ALTER TABLE [dbo].[tournaments] DROP CONSTRAINT [CK_tournaments_duration_minutes];

ALTER TABLE [dbo].[tournaments] DROP CONSTRAINT [CK_tournaments_entry_fee];

ALTER TABLE [dbo].[tournaments] DROP CONSTRAINT [CK_tournaments_system_enum];

ALTER TABLE [dbo].[tournament_registrations] DROP CONSTRAINT [UQ_tournament_registrations_tournament_id_captain_id];

ALTER TABLE [dbo].[tournament_registrations] DROP CONSTRAINT [UQ_tournament_registrations_tournament_id_team_id];

DROP INDEX [IX_tournament_registrations_0] ON [dbo].[tournament_registrations];

ALTER TABLE [dbo].[tournament_registrations] DROP CONSTRAINT [CK_tournament_registrations_status_enum];

ALTER TABLE [dbo].[tournament_matches] DROP CONSTRAINT [UQ_tournament_matches_tournament_id_round_number_match_number];

DROP INDEX [IX_tournament_matches_0] ON [dbo].[tournament_matches];

ALTER TABLE [dbo].[tournament_matches] DROP CONSTRAINT [CK_tournament_matches_match_number];

ALTER TABLE [dbo].[tournament_matches] DROP CONSTRAINT [CK_tournament_matches_round_number];

DROP INDEX [IX_tournament_match_slots_0] ON [dbo].[tournament_match_slots];

DROP INDEX [IX_tournament_match_slots_1] ON [dbo].[tournament_match_slots];

ALTER TABLE [dbo].[tournament_match_slots] DROP CONSTRAINT [CK_tournament_match_slots_rule_1];

ALTER TABLE [dbo].[tournament_match_slots] DROP CONSTRAINT [CK_tournament_match_slots_slot_number];

ALTER TABLE [dbo].[tournament_draw_entries] DROP CONSTRAINT [UQ_tournament_draw_entries_tournament_id_seed_position];

DROP INDEX [IX_tournament_draw_entries_0] ON [dbo].[tournament_draw_entries];

ALTER TABLE [dbo].[tournament_draw_entries] DROP CONSTRAINT [CK_tournament_draw_entries_seed_position];

ALTER TABLE [dbo].[teams] DROP CONSTRAINT [UQ_teams_captain_id];

ALTER TABLE [dbo].[teams] DROP CONSTRAINT [UQ_teams_name];

ALTER TABLE [dbo].[teams] DROP CONSTRAINT [CK_teams_capacity];

ALTER TABLE [dbo].[teams] DROP CONSTRAINT [CK_teams_format_enum];

ALTER TABLE [dbo].[teams] DROP CONSTRAINT [CK_teams_level_enum];

DROP INDEX [IX_team_requests_0] ON [dbo].[team_requests];

ALTER TABLE [dbo].[team_requests] DROP CONSTRAINT [CK_team_requests_position_enum];

ALTER TABLE [dbo].[team_requests] DROP CONSTRAINT [CK_team_requests_request_type_enum];

ALTER TABLE [dbo].[team_requests] DROP CONSTRAINT [CK_team_requests_status_enum];

ALTER TABLE [dbo].[team_members] DROP CONSTRAINT [CK_team_members_position_enum];

ALTER TABLE [dbo].[settings] DROP CONSTRAINT [CK_settings_commission_rate];

ALTER TABLE [dbo].[settings] DROP CONSTRAINT [CK_settings_id];

ALTER TABLE [dbo].[settings] DROP CONSTRAINT [CK_settings_listing_fee];

DROP INDEX [IX_registration_players_0] ON [dbo].[registration_players];

ALTER TABLE [dbo].[registration_players] DROP CONSTRAINT [CK_registration_players_position_enum];

DROP INDEX [IX_payments_0] ON [dbo].[payments];

ALTER TABLE [dbo].[payments] DROP CONSTRAINT [CK_payments_amount];

ALTER TABLE [dbo].[payments] DROP CONSTRAINT [CK_payments_commission];

ALTER TABLE [dbo].[payments] DROP CONSTRAINT [CK_payments_commission_rate];

ALTER TABLE [dbo].[payments] DROP CONSTRAINT [CK_payments_owner_net];

ALTER TABLE [dbo].[payments] DROP CONSTRAINT [CK_payments_rule_1];

ALTER TABLE [dbo].[payments] DROP CONSTRAINT [CK_payments_rule_2];

ALTER TABLE [dbo].[payments] DROP CONSTRAINT [CK_payments_status_enum];

ALTER TABLE [dbo].[ownership_documents] DROP CONSTRAINT [CK_ownership_documents_byte_size];

ALTER TABLE [dbo].[open_games] DROP CONSTRAINT [CK_open_games_capacity];

ALTER TABLE [dbo].[open_games] DROP CONSTRAINT [CK_open_games_duration_minutes];

ALTER TABLE [dbo].[open_games] DROP CONSTRAINT [CK_open_games_player_fee];

ALTER TABLE [dbo].[messages] DROP CONSTRAINT [CK_messages_rule_1];

ALTER TABLE [dbo].[meetings] DROP CONSTRAINT [UQ_meetings_room_id];

ALTER TABLE [dbo].[game_participants] DROP CONSTRAINT [UQ_game_participants_game_id_user_id];

DROP INDEX [IX_game_participants_0] ON [dbo].[game_participants];

ALTER TABLE [dbo].[game_participants] DROP CONSTRAINT [CK_game_participants_position_enum];

ALTER TABLE [dbo].[game_participants] DROP CONSTRAINT [CK_game_participants_status_enum];

ALTER TABLE [dbo].[fields] DROP CONSTRAINT [CK_fields_format_enum];

ALTER TABLE [dbo].[fields] DROP CONSTRAINT [CK_fields_hourly_price];

ALTER TABLE [dbo].[fields] DROP CONSTRAINT [CK_fields_latitude];

ALTER TABLE [dbo].[fields] DROP CONSTRAINT [CK_fields_location_precision_enum];

ALTER TABLE [dbo].[fields] DROP CONSTRAINT [CK_fields_longitude];

ALTER TABLE [dbo].[field_images] DROP CONSTRAINT [UQ_field_images_field_id_sort_order];

DROP INDEX [IX_field_images_0] ON [dbo].[field_images];

ALTER TABLE [dbo].[field_images] DROP CONSTRAINT [CK_field_images_sort_order];

DROP INDEX [IX_field_amenities_0] ON [dbo].[field_amenities];

ALTER TABLE [dbo].[feedback] DROP CONSTRAINT [CK_feedback_rating];

ALTER TABLE [dbo].[challenges] DROP CONSTRAINT [CK_challenges_cost_agreement_enum];

ALTER TABLE [dbo].[challenges] DROP CONSTRAINT [CK_challenges_format_enum];

ALTER TABLE [dbo].[challenges] DROP CONSTRAINT [CK_challenges_rule_1];

ALTER TABLE [dbo].[challenges] DROP CONSTRAINT [CK_challenges_status_enum];

DROP INDEX [IX_challenge_requests_0] ON [dbo].[challenge_requests];

ALTER TABLE [dbo].[challenge_requests] DROP CONSTRAINT [CK_challenge_requests_status_enum];

DROP INDEX [IX_bookings_0] ON [dbo].[bookings];

DROP INDEX [IX_bookings_1] ON [dbo].[bookings];

ALTER TABLE [dbo].[bookings] DROP CONSTRAINT [CK_bookings_price];

ALTER TABLE [dbo].[bookings] DROP CONSTRAINT [CK_bookings_starts_at];

ALTER TABLE [dbo].[bookings] DROP CONSTRAINT [CK_bookings_status_enum];

DROP INDEX [IX_booking_teams_0] ON [dbo].[booking_teams];

ALTER TABLE [dbo].[amenities] DROP CONSTRAINT [UQ_amenities_name];

DECLARE @var nvarchar(max);
SELECT @var = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[users]') AND [c].[name] = N'password_algorithm');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [dbo].[users] DROP CONSTRAINT ' + @var + ';');
ALTER TABLE [dbo].[users] DROP COLUMN [password_algorithm];

DECLARE @var1 nvarchar(max);
SELECT @var1 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[users]') AND [c].[name] = N'password_iterations');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[users] DROP CONSTRAINT ' + @var1 + ';');
ALTER TABLE [dbo].[users] DROP COLUMN [password_iterations];

DECLARE @var2 nvarchar(max);
SELECT @var2 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[users]') AND [c].[name] = N'password_salt');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[users] DROP CONSTRAINT ' + @var2 + ';');
ALTER TABLE [dbo].[users] DROP COLUMN [password_salt];

DECLARE @var3 nvarchar(max);
SELECT @var3 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[users]') AND [c].[name] = N'role');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[users] DROP CONSTRAINT ' + @var3 + ';');
ALTER TABLE [dbo].[users] DROP COLUMN [role];

EXEC sp_rename N'[dbo].[payments].[IX_payments_5]', N'IX_payments_7', 'INDEX';

EXEC sp_rename N'[dbo].[payments].[IX_payments_4]', N'IX_payments_6', 'INDEX';

EXEC sp_rename N'[dbo].[payments].[IX_payments_3]', N'IX_payments_5', 'INDEX';

EXEC sp_rename N'[dbo].[payments].[IX_payments_2]', N'IX_payments_4', 'INDEX';

EXEC sp_rename N'[dbo].[payments].[IX_payments_1]', N'IX_payments_3', 'INDEX';

EXEC sp_rename N'[dbo].[challenge_requests].[IX_challenge_requests_1]', N'IX_challenge_requests_2', 'INDEX';

DECLARE @var4 nvarchar(max);
SELECT @var4 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[users]') AND [c].[name] = N'status');
IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[users] DROP CONSTRAINT ' + @var4 + ';');
ALTER TABLE [dbo].[users] ADD DEFAULT 'Pending' FOR [status];

DECLARE @var5 nvarchar(max);
SELECT @var5 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[users]') AND [c].[name] = N'password_hash');
IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[users] DROP CONSTRAINT ' + @var5 + ';');
ALTER TABLE [dbo].[users] ALTER COLUMN [password_hash] nvarchar(max) NULL;

DECLARE @var6 nvarchar(max);
SELECT @var6 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[users]') AND [c].[name] = N'created_at');
IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[users] DROP CONSTRAINT ' + @var6 + ';');
ALTER TABLE [dbo].[users] ADD DEFAULT (SYSUTCDATETIME()) FOR [created_at];

ALTER TABLE [dbo].[users] ADD [access_failed_count] int NOT NULL DEFAULT 0;

ALTER TABLE [dbo].[users] ADD [address] nvarchar(300) NULL;

ALTER TABLE [dbo].[users] ADD [concurrency_stamp] nvarchar(max) NULL;

ALTER TABLE [dbo].[users] ADD [email_confirmed] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [dbo].[users] ADD [lockout_enabled] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [dbo].[users] ADD [lockout_end] datetimeoffset NULL;

ALTER TABLE [dbo].[users] ADD [normalized_email] nvarchar(254) NULL;

ALTER TABLE [dbo].[users] ADD [normalized_user_name] nvarchar(256) NULL;

ALTER TABLE [dbo].[users] ADD [phone_number_confirmed] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [dbo].[users] ADD [security_stamp] nvarchar(max) NULL;

ALTER TABLE [dbo].[users] ADD [two_factor_enabled] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [dbo].[users] ADD [user_name] nvarchar(256) NULL;

DECLARE @var7 nvarchar(max);
SELECT @var7 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[tournaments]') AND [c].[name] = N'system');
IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[tournaments] DROP CONSTRAINT ' + @var7 + ';');
ALTER TABLE [dbo].[tournaments] ADD DEFAULT 'Single elimination' FOR [system];

DECLARE @var8 nvarchar(max);
SELECT @var8 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[tournaments]') AND [c].[name] = N'created_at');
IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[tournaments] DROP CONSTRAINT ' + @var8 + ';');
ALTER TABLE [dbo].[tournaments] ADD DEFAULT (SYSUTCDATETIME()) FOR [created_at];

ALTER TABLE [dbo].[tournaments] ADD [cancelled_at] datetimeoffset(0) NULL;

ALTER TABLE [dbo].[tournaments] ADD [cancelled_by] int NULL;

ALTER TABLE [dbo].[tournaments] ADD [status] varchar(24) NOT NULL DEFAULT 'Scheduled';

DECLARE @var9 nvarchar(max);
SELECT @var9 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[tournament_registrations]') AND [c].[name] = N'status');
IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[tournament_registrations] DROP CONSTRAINT ' + @var9 + ';');
ALTER TABLE [dbo].[tournament_registrations] ADD DEFAULT 'Registered' FOR [status];

DECLARE @var10 nvarchar(max);
SELECT @var10 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[tournament_registrations]') AND [c].[name] = N'created_at');
IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[tournament_registrations] DROP CONSTRAINT ' + @var10 + ';');
ALTER TABLE [dbo].[tournament_registrations] ADD DEFAULT (SYSUTCDATETIME()) FOR [created_at];

ALTER TABLE [dbo].[tournament_registrations] ADD [withdrawn_at] datetimeoffset(0) NULL;

ALTER TABLE [dbo].[tournament_registrations] ADD [withdrawn_by] int NULL;

DECLARE @var11 nvarchar(max);
SELECT @var11 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[tournament_matches]') AND [c].[name] = N'is_bye');
IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[tournament_matches] DROP CONSTRAINT ' + @var11 + ';');
ALTER TABLE [dbo].[tournament_matches] ADD DEFAULT CAST(0 AS bit) FOR [is_bye];

DECLARE @var12 nvarchar(max);
SELECT @var12 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[teams]') AND [c].[name] = N'level');
IF @var12 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[teams] DROP CONSTRAINT ' + @var12 + ';');
ALTER TABLE [dbo].[teams] ADD DEFAULT 'Intermediate' FOR [level];

DECLARE @var13 nvarchar(max);
SELECT @var13 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[teams]') AND [c].[name] = N'is_recruiting');
IF @var13 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[teams] DROP CONSTRAINT ' + @var13 + ';');
ALTER TABLE [dbo].[teams] ADD DEFAULT CAST(1 AS bit) FOR [is_recruiting];

DECLARE @var14 nvarchar(max);
SELECT @var14 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[teams]') AND [c].[name] = N'format');
IF @var14 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[teams] DROP CONSTRAINT ' + @var14 + ';');
ALTER TABLE [dbo].[teams] ADD DEFAULT '5v5' FOR [format];

DECLARE @var15 nvarchar(max);
SELECT @var15 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[teams]') AND [c].[name] = N'created_at');
IF @var15 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[teams] DROP CONSTRAINT ' + @var15 + ';');
ALTER TABLE [dbo].[teams] ADD DEFAULT (SYSUTCDATETIME()) FOR [created_at];

DECLARE @var16 nvarchar(max);
SELECT @var16 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[teams]') AND [c].[name] = N'color');
IF @var16 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[teams] DROP CONSTRAINT ' + @var16 + ';');
ALTER TABLE [dbo].[teams] ADD DEFAULT 'forest' FOR [color];

DECLARE @var17 nvarchar(max);
SELECT @var17 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[teams]') AND [c].[name] = N'capacity');
IF @var17 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[teams] DROP CONSTRAINT ' + @var17 + ';');
ALTER TABLE [dbo].[teams] ADD DEFAULT 12 FOR [capacity];

DECLARE @var18 nvarchar(max);
SELECT @var18 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[team_requests]') AND [c].[name] = N'status');
IF @var18 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[team_requests] DROP CONSTRAINT ' + @var18 + ';');
ALTER TABLE [dbo].[team_requests] ADD DEFAULT 'Pending' FOR [status];

DECLARE @var19 nvarchar(max);
SELECT @var19 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[team_requests]') AND [c].[name] = N'request_type');
IF @var19 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[team_requests] DROP CONSTRAINT ' + @var19 + ';');
ALTER TABLE [dbo].[team_requests] ADD DEFAULT 'join' FOR [request_type];

DECLARE @var20 nvarchar(max);
SELECT @var20 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[team_requests]') AND [c].[name] = N'position');
IF @var20 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[team_requests] DROP CONSTRAINT ' + @var20 + ';');
ALTER TABLE [dbo].[team_requests] ADD DEFAULT 'Midfielder' FOR [position];

DECLARE @var21 nvarchar(max);
SELECT @var21 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[team_requests]') AND [c].[name] = N'created_at');
IF @var21 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[team_requests] DROP CONSTRAINT ' + @var21 + ';');
ALTER TABLE [dbo].[team_requests] ADD DEFAULT (SYSUTCDATETIME()) FOR [created_at];

DECLARE @var22 nvarchar(max);
SELECT @var22 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[team_members]') AND [c].[name] = N'position');
IF @var22 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[team_members] DROP CONSTRAINT ' + @var22 + ';');
ALTER TABLE [dbo].[team_members] ADD DEFAULT 'Midfielder' FOR [position];

DECLARE @var23 nvarchar(max);
SELECT @var23 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[team_members]') AND [c].[name] = N'joined_at');
IF @var23 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[team_members] DROP CONSTRAINT ' + @var23 + ';');
ALTER TABLE [dbo].[team_members] ADD DEFAULT (SYSUTCDATETIME()) FOR [joined_at];

DECLARE @var24 nvarchar(max);
SELECT @var24 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[settings]') AND [c].[name] = N'listing_fee');
IF @var24 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[settings] DROP CONSTRAINT ' + @var24 + ';');
ALTER TABLE [dbo].[settings] ADD DEFAULT 5.0 FOR [listing_fee];

DECLARE @var25 nvarchar(max);
SELECT @var25 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[settings]') AND [c].[name] = N'commission_rate');
IF @var25 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[settings] DROP CONSTRAINT ' + @var25 + ';');
ALTER TABLE [dbo].[settings] ADD DEFAULT 10.0 FOR [commission_rate];

DECLARE @var26 nvarchar(max);
SELECT @var26 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[settings]') AND [c].[name] = N'id');
IF @var26 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[settings] DROP CONSTRAINT ' + @var26 + ';');
ALTER TABLE [dbo].[settings] ADD DEFAULT 1 FOR [id];

DECLARE @var27 nvarchar(max);
SELECT @var27 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[registration_players]') AND [c].[name] = N'position');
IF @var27 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[registration_players] DROP CONSTRAINT ' + @var27 + ';');
ALTER TABLE [dbo].[registration_players] ADD DEFAULT 'Midfielder' FOR [position];

DECLARE @var28 nvarchar(max);
SELECT @var28 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[payments]') AND [c].[name] = N'status');
IF @var28 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[payments] DROP CONSTRAINT ' + @var28 + ';');
ALTER TABLE [dbo].[payments] ADD DEFAULT 'Paid (demo)' FOR [status];

DECLARE @var29 nvarchar(max);
SELECT @var29 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[payments]') AND [c].[name] = N'owner_id');
IF @var29 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[payments] DROP CONSTRAINT ' + @var29 + ';');
ALTER TABLE [dbo].[payments] ALTER COLUMN [owner_id] int NULL;

DECLARE @var30 nvarchar(max);
SELECT @var30 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[payments]') AND [c].[name] = N'is_demo');
IF @var30 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[payments] DROP CONSTRAINT ' + @var30 + ';');
ALTER TABLE [dbo].[payments] ADD DEFAULT CAST(1 AS bit) FOR [is_demo];

DECLARE @var31 nvarchar(max);
SELECT @var31 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[payments]') AND [c].[name] = N'created_at');
IF @var31 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[payments] DROP CONSTRAINT ' + @var31 + ';');
ALTER TABLE [dbo].[payments] ADD DEFAULT (SYSUTCDATETIME()) FOR [created_at];

ALTER TABLE [dbo].[payments] ADD [platform_admin_id] int NOT NULL DEFAULT 0;

ALTER TABLE [dbo].[payments] ADD [refunded_at] datetimeoffset(0) NULL;

ALTER TABLE [dbo].[payments] ADD [share_id] int NULL;

DECLARE @var32 nvarchar(max);
SELECT @var32 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[ownership_documents]') AND [c].[name] = N'created_at');
IF @var32 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[ownership_documents] DROP CONSTRAINT ' + @var32 + ';');
ALTER TABLE [dbo].[ownership_documents] ADD DEFAULT (SYSUTCDATETIME()) FOR [created_at];

DECLARE @var33 nvarchar(max);
SELECT @var33 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[open_games]') AND [c].[name] = N'created_at');
IF @var33 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[open_games] DROP CONSTRAINT ' + @var33 + ';');
ALTER TABLE [dbo].[open_games] ADD DEFAULT (SYSUTCDATETIME()) FOR [created_at];

ALTER TABLE [dbo].[open_games] ADD [status] varchar(24) NOT NULL DEFAULT 'Scheduled';

DECLARE @var34 nvarchar(max);
SELECT @var34 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[messages]') AND [c].[name] = N'created_at');
IF @var34 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[messages] DROP CONSTRAINT ' + @var34 + ';');
ALTER TABLE [dbo].[messages] ADD DEFAULT (SYSUTCDATETIME()) FOR [created_at];

DECLARE @var35 nvarchar(max);
SELECT @var35 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[game_participants]') AND [c].[name] = N'status');
IF @var35 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[game_participants] DROP CONSTRAINT ' + @var35 + ';');
ALTER TABLE [dbo].[game_participants] ADD DEFAULT 'Joined' FOR [status];

DECLARE @var36 nvarchar(max);
SELECT @var36 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[game_participants]') AND [c].[name] = N'position');
IF @var36 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[game_participants] DROP CONSTRAINT ' + @var36 + ';');
ALTER TABLE [dbo].[game_participants] ADD DEFAULT 'Midfielder' FOR [position];

DECLARE @var37 nvarchar(max);
SELECT @var37 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[game_participants]') AND [c].[name] = N'created_at');
IF @var37 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[game_participants] DROP CONSTRAINT ' + @var37 + ';');
ALTER TABLE [dbo].[game_participants] ADD DEFAULT (SYSUTCDATETIME()) FOR [created_at];

DECLARE @var38 nvarchar(max);
SELECT @var38 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[fields]') AND [c].[name] = N'location_precision');
IF @var38 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[fields] DROP CONSTRAINT ' + @var38 + ';');
ALTER TABLE [dbo].[fields] ADD DEFAULT 'unknown' FOR [location_precision];

DECLARE @var39 nvarchar(max);
SELECT @var39 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[fields]') AND [c].[name] = N'is_archived');
IF @var39 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[fields] DROP CONSTRAINT ' + @var39 + ';');
ALTER TABLE [dbo].[fields] ADD DEFAULT CAST(0 AS bit) FOR [is_archived];

DECLARE @var40 nvarchar(max);
SELECT @var40 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[fields]') AND [c].[name] = N'format');
IF @var40 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[fields] DROP CONSTRAINT ' + @var40 + ';');
ALTER TABLE [dbo].[fields] ADD DEFAULT '5v5' FOR [format];

DECLARE @var41 nvarchar(max);
SELECT @var41 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[fields]') AND [c].[name] = N'created_at');
IF @var41 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[fields] DROP CONSTRAINT ' + @var41 + ';');
ALTER TABLE [dbo].[fields] ADD DEFAULT (SYSUTCDATETIME()) FOR [created_at];

ALTER TABLE [dbo].[fields] ADD [rating] decimal(3,2) NOT NULL DEFAULT 0.0;

DECLARE @var42 nvarchar(max);
SELECT @var42 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[field_images]') AND [c].[name] = N'sort_order');
IF @var42 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[field_images] DROP CONSTRAINT ' + @var42 + ';');
ALTER TABLE [dbo].[field_images] ADD DEFAULT 0 FOR [sort_order];

DECLARE @var43 nvarchar(max);
SELECT @var43 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[feedback]') AND [c].[name] = N'created_at');
IF @var43 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[feedback] DROP CONSTRAINT ' + @var43 + ';');
ALTER TABLE [dbo].[feedback] ADD DEFAULT (SYSUTCDATETIME()) FOR [created_at];

DECLARE @var44 nvarchar(max);
SELECT @var44 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[challenges]') AND [c].[name] = N'status');
IF @var44 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[challenges] DROP CONSTRAINT ' + @var44 + ';');
ALTER TABLE [dbo].[challenges] ADD DEFAULT 'Open' FOR [status];

DECLARE @var45 nvarchar(max);
SELECT @var45 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[challenges]') AND [c].[name] = N'format');
IF @var45 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[challenges] DROP CONSTRAINT ' + @var45 + ';');
ALTER TABLE [dbo].[challenges] ADD DEFAULT '5v5' FOR [format];

DECLARE @var46 nvarchar(max);
SELECT @var46 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[challenges]') AND [c].[name] = N'created_at');
IF @var46 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[challenges] DROP CONSTRAINT ' + @var46 + ';');
ALTER TABLE [dbo].[challenges] ADD DEFAULT (SYSUTCDATETIME()) FOR [created_at];

DECLARE @var47 nvarchar(max);
SELECT @var47 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[challenges]') AND [c].[name] = N'cost_agreement');
IF @var47 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[challenges] DROP CONSTRAINT ' + @var47 + ';');
ALTER TABLE [dbo].[challenges] ADD DEFAULT 'Half each' FOR [cost_agreement];

ALTER TABLE [dbo].[challenges] ADD [field_id] int NULL;

ALTER TABLE [dbo].[challenges] ADD [payment_mode] varchar(24) NOT NULL DEFAULT 'Split checkout';

ALTER TABLE [dbo].[challenges] ADD [quoted_price] decimal(12,2) NULL;

DECLARE @var48 nvarchar(max);
SELECT @var48 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[challenge_requests]') AND [c].[name] = N'status');
IF @var48 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[challenge_requests] DROP CONSTRAINT ' + @var48 + ';');
ALTER TABLE [dbo].[challenge_requests] ADD DEFAULT 'Pending' FOR [status];

DECLARE @var49 nvarchar(max);
SELECT @var49 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[challenge_requests]') AND [c].[name] = N'created_at');
IF @var49 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[challenge_requests] DROP CONSTRAINT ' + @var49 + ';');
ALTER TABLE [dbo].[challenge_requests] ADD DEFAULT (SYSUTCDATETIME()) FOR [created_at];

DECLARE @var50 nvarchar(max);
SELECT @var50 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[bookings]') AND [c].[name] = N'status');
IF @var50 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[bookings] DROP CONSTRAINT ' + @var50 + ';');
ALTER TABLE [dbo].[bookings] ADD DEFAULT 'Confirmed' FOR [status];

DECLARE @var51 nvarchar(max);
SELECT @var51 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[bookings]') AND [c].[name] = N'created_at');
IF @var51 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[bookings] DROP CONSTRAINT ' + @var51 + ';');
ALTER TABLE [dbo].[bookings] ADD DEFAULT (SYSUTCDATETIME()) FOR [created_at];

ALTER TABLE [dbo].[bookings] ADD [cancellation_reason] varchar(24) NULL;

ALTER TABLE [dbo].[bookings] ADD [cancelled_at] datetimeoffset(0) NULL;

ALTER TABLE [dbo].[bookings] ADD [cancelled_by] int NULL;

ALTER TABLE [dbo].[bookings] ADD [commission] decimal(12,2) NOT NULL DEFAULT 0.0;

ALTER TABLE [dbo].[bookings] ADD [commission_rate] decimal(5,2) NOT NULL DEFAULT 0.0;

ALTER TABLE [dbo].[bookings] ADD [is_split] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [dbo].[bookings] ADD [owner_id] int NOT NULL DEFAULT 0;

ALTER TABLE [dbo].[bookings] ADD [owner_net] decimal(12,2) NOT NULL DEFAULT 0.0;

ALTER TABLE [dbo].[bookings] ADD [payment_deadline] datetimeoffset(0) NULL;

DECLARE @var52 nvarchar(max);
SELECT @var52 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[tournaments]') AND [c].[name] = N'ends_at');
IF @var52 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[tournaments] DROP CONSTRAINT ' + @var52 + ';');
ALTER TABLE [dbo].[tournaments] DROP COLUMN [ends_at];
ALTER TABLE [dbo].[tournaments] ADD [ends_at] AS DATEADD(MINUTE, [duration_minutes], [starts_at]) PERSISTED;

DECLARE @var53 nvarchar(max);
SELECT @var53 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[open_games]') AND [c].[name] = N'ends_at');
IF @var53 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[open_games] DROP CONSTRAINT ' + @var53 + ';');
ALTER TABLE [dbo].[open_games] DROP COLUMN [ends_at];
ALTER TABLE [dbo].[open_games] ADD [ends_at] AS DATEADD(MINUTE, [duration_minutes], [starts_at]) PERSISTED;

DECLARE @var54 nvarchar(max);
SELECT @var54 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[bookings]') AND [c].[name] = N'ends_at');
IF @var54 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[bookings] DROP CONSTRAINT ' + @var54 + ';');
ALTER TABLE [dbo].[bookings] DROP COLUMN [ends_at];
ALTER TABLE [dbo].[bookings] ADD [ends_at] AS DATEADD(HOUR, 1, [starts_at]) PERSISTED;

ALTER TABLE [dbo].[booking_teams] ADD CONSTRAINT [UQ_booking_teams_booking_id_team_id_captain_id] UNIQUE ([booking_id], [team_id], [captain_id]);

CREATE TABLE [dbo].[AspNetRoles] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(256) NOT NULL,
    [NormalizedName] nvarchar(256) NOT NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_AspNetRoles_name] CHECK ([Name] IN (N'player',N'owner',N'admin'))
);

CREATE TABLE [dbo].[AspNetUserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[users] ([id])
);

CREATE TABLE [dbo].[AspNetUserLogins] (
    [LoginProvider] nvarchar(450) NOT NULL,
    [ProviderKey] nvarchar(450) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] int NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[users] ([id])
);

CREATE TABLE [dbo].[AspNetUserTokens] (
    [UserId] int NOT NULL,
    [LoginProvider] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[users] ([id])
);

CREATE TABLE [dbo].[booking_shares] (
    [id] int NOT NULL IDENTITY,
    [booking_id] int NOT NULL,
    [team_id] int NOT NULL,
    [payer_id] int NOT NULL,
    [amount] decimal(12,2) NOT NULL,
    [commission] decimal(12,2) NOT NULL,
    [payment_state] varchar(24) NOT NULL DEFAULT 'Unpaid',
    [paid_at] datetimeoffset(0) NULL,
    CONSTRAINT [PK_booking_shares] PRIMARY KEY ([id]),
    CONSTRAINT [CK_booking_shares_amount] CHECK ([amount] >= 0),
    CONSTRAINT [CK_booking_shares_commission] CHECK ([commission] >= 0),
    CONSTRAINT [CK_booking_shares_payment_state_enum] CHECK ([payment_state] IN (N'Unpaid', N'Held', N'Paid', N'Refunded')),
    CONSTRAINT [CK_booking_shares_rule_1] CHECK ([commission] <= [amount]),
    CONSTRAINT [FK_booking_shares_booking_id_0] FOREIGN KEY ([booking_id]) REFERENCES [dbo].[bookings] ([id]),
    CONSTRAINT [FK_booking_shares_booking_id_team_id_payer_id_3] FOREIGN KEY ([booking_id], [team_id], [payer_id]) REFERENCES [dbo].[booking_teams] ([booking_id], [team_id], [captain_id]),
    CONSTRAINT [FK_booking_shares_payer_id_2] FOREIGN KEY ([payer_id]) REFERENCES [dbo].[users] ([id]),
    CONSTRAINT [FK_booking_shares_team_id_1] FOREIGN KEY ([team_id]) REFERENCES [dbo].[teams] ([id])
);

CREATE TABLE [dbo].[team_recruiting_positions] (
    [team_id] int NOT NULL,
    [position] varchar(24) NOT NULL,
    CONSTRAINT [PK_team_recruiting_positions] PRIMARY KEY ([team_id], [position]),
    CONSTRAINT [CK_team_recruiting_positions_position_enum] CHECK ([position] IN (N'Goalkeeper', N'Defender', N'Midfielder', N'Forward')),
    CONSTRAINT [FK_team_recruiting_positions_team_id_0] FOREIGN KEY ([team_id]) REFERENCES [dbo].[teams] ([id])
);

CREATE TABLE [dbo].[AspNetRoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] int NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[AspNetRoles] ([Id])
);

CREATE TABLE [dbo].[AspNetUserRoles] (
    [UserId] int NOT NULL,
    [RoleId] int NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[AspNetRoles] ([Id]),
    CONSTRAINT [FK_AspNetUserRoles_users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[users] ([id])
);

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ConcurrencyStamp', N'Name', N'NormalizedName') AND [object_id] = OBJECT_ID(N'[dbo].[AspNetRoles]'))
    SET IDENTITY_INSERT [dbo].[AspNetRoles] ON;
INSERT INTO [dbo].[AspNetRoles] ([Id], [ConcurrencyStamp], [Name], [NormalizedName])
VALUES (1, N'kicks-player-v1', N'player', N'PLAYER'),
(2, N'kicks-owner-v1', N'owner', N'OWNER'),
(3, N'kicks-admin-v1', N'admin', N'ADMIN');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ConcurrencyStamp', N'Name', N'NormalizedName') AND [object_id] = OBJECT_ID(N'[dbo].[AspNetRoles]'))
    SET IDENTITY_INSERT [dbo].[AspNetRoles] OFF;

CREATE UNIQUE INDEX [EmailIndex] ON [dbo].[users] ([normalized_email]) WHERE [normalized_email] IS NOT NULL;

CREATE UNIQUE INDEX [UQ_users_email] ON [dbo].[users] ([email]);

CREATE UNIQUE INDEX [UserNameIndex] ON [dbo].[users] ([normalized_user_name]) WHERE [normalized_user_name] IS NOT NULL;

ALTER TABLE [dbo].[users] ADD CONSTRAINT [CK_users_requested_role_enum] CHECK ([requested_role] IN (N'player', N'owner'));

ALTER TABLE [dbo].[users] ADD CONSTRAINT [CK_users_status_enum] CHECK ([status] IN (N'Pending', N'Active', N'Rejected', N'Inactive'));

CREATE INDEX [IX_tournaments_2] ON [dbo].[tournaments] ([cancelled_by]);

ALTER TABLE [dbo].[tournaments] ADD CONSTRAINT [CK_tournaments_capacity] CHECK ([capacity] BETWEEN 2 AND 64);

ALTER TABLE [dbo].[tournaments] ADD CONSTRAINT [CK_tournaments_duration_minutes] CHECK ([duration_minutes] > 0);

ALTER TABLE [dbo].[tournaments] ADD CONSTRAINT [CK_tournaments_entry_fee] CHECK ([entry_fee] >= 0);

ALTER TABLE [dbo].[tournaments] ADD CONSTRAINT [CK_tournaments_rule_1] CHECK ([status] <> 'Cancelled' OR ([cancelled_by] IS NOT NULL AND [cancelled_at] IS NOT NULL));

ALTER TABLE [dbo].[tournaments] ADD CONSTRAINT [CK_tournaments_status_enum] CHECK ([status] IN (N'Scheduled', N'Cancelled'));

ALTER TABLE [dbo].[tournaments] ADD CONSTRAINT [CK_tournaments_system_enum] CHECK ([system] IN (N'Single elimination', N'Group stage + knockout'));

CREATE INDEX [IX_tournament_registrations_3] ON [dbo].[tournament_registrations] ([withdrawn_by]);

CREATE UNIQUE INDEX [UQ_tournament_registrations_tournament_id_captain_id] ON [dbo].[tournament_registrations] ([tournament_id], [captain_id]);

CREATE UNIQUE INDEX [UQ_tournament_registrations_tournament_id_team_id] ON [dbo].[tournament_registrations] ([tournament_id], [team_id]);

ALTER TABLE [dbo].[tournament_registrations] ADD CONSTRAINT [CK_tournament_registrations_status_enum] CHECK ([status] IN (N'Registered', N'Withdrawn'));

CREATE INDEX [IX_tournament_matches_tournament_id_winner_registration_id] ON [dbo].[tournament_matches] ([tournament_id], [winner_registration_id]);

CREATE UNIQUE INDEX [UQ_tournament_matches_tournament_id_round_number_match_number] ON [dbo].[tournament_matches] ([tournament_id], [round_number], [match_number]);

ALTER TABLE [dbo].[tournament_matches] ADD CONSTRAINT [CK_tournament_matches_match_number] CHECK ([match_number] BETWEEN 1 AND 32);

ALTER TABLE [dbo].[tournament_matches] ADD CONSTRAINT [CK_tournament_matches_round_number] CHECK ([round_number] BETWEEN 1 AND 6);

CREATE INDEX [IX_tournament_match_slots_tournament_id_match_id] ON [dbo].[tournament_match_slots] ([tournament_id], [match_id]);

CREATE INDEX [IX_tournament_match_slots_tournament_id_registration_id] ON [dbo].[tournament_match_slots] ([tournament_id], [registration_id]);

CREATE INDEX [IX_tournament_match_slots_tournament_id_source_match_id] ON [dbo].[tournament_match_slots] ([tournament_id], [source_match_id]);

ALTER TABLE [dbo].[tournament_match_slots] ADD CONSTRAINT [CK_tournament_match_slots_rule_1] CHECK ([source_match_id] IS NULL OR [source_match_id] <> [match_id]);

ALTER TABLE [dbo].[tournament_match_slots] ADD CONSTRAINT [CK_tournament_match_slots_slot_number] CHECK ([slot_number] IN (1,2));

CREATE INDEX [IX_tournament_draw_entries_tournament_id_registration_id] ON [dbo].[tournament_draw_entries] ([tournament_id], [registration_id]);

CREATE UNIQUE INDEX [UQ_tournament_draw_entries_tournament_id_seed_position] ON [dbo].[tournament_draw_entries] ([tournament_id], [seed_position]);

ALTER TABLE [dbo].[tournament_draw_entries] ADD CONSTRAINT [CK_tournament_draw_entries_seed_position] CHECK ([seed_position] BETWEEN 1 AND 64);

CREATE UNIQUE INDEX [UQ_teams_captain_id] ON [dbo].[teams] ([captain_id]);

CREATE UNIQUE INDEX [UQ_teams_name] ON [dbo].[teams] ([name]);

ALTER TABLE [dbo].[teams] ADD CONSTRAINT [CK_teams_capacity] CHECK ([capacity] BETWEEN 5 AND 30);

ALTER TABLE [dbo].[teams] ADD CONSTRAINT [CK_teams_format_enum] CHECK ([format] IN (N'5v5', N'6v6', N'7v7'));

ALTER TABLE [dbo].[teams] ADD CONSTRAINT [CK_teams_level_enum] CHECK ([level] IN (N'Casual', N'Intermediate', N'Competitive'));

CREATE UNIQUE INDEX [IX_team_requests_0] ON [dbo].[team_requests] ([team_id], [user_id], [request_type]) WHERE [status] = 'Pending';

ALTER TABLE [dbo].[team_requests] ADD CONSTRAINT [CK_team_requests_position_enum] CHECK ([position] IN (N'Goalkeeper', N'Defender', N'Midfielder', N'Forward'));

ALTER TABLE [dbo].[team_requests] ADD CONSTRAINT [CK_team_requests_request_type_enum] CHECK ([request_type] IN (N'join', N'leave'));

ALTER TABLE [dbo].[team_requests] ADD CONSTRAINT [CK_team_requests_status_enum] CHECK ([status] IN (N'Pending', N'Accepted', N'Declined', N'Cancelled'));

ALTER TABLE [dbo].[team_members] ADD CONSTRAINT [CK_team_members_position_enum] CHECK ([position] IN (N'Goalkeeper', N'Defender', N'Midfielder', N'Forward'));

ALTER TABLE [dbo].[settings] ADD CONSTRAINT [CK_settings_commission_rate] CHECK ([commission_rate] BETWEEN 0 AND 100);

ALTER TABLE [dbo].[settings] ADD CONSTRAINT [CK_settings_id] CHECK ([id] = 1);

ALTER TABLE [dbo].[settings] ADD CONSTRAINT [CK_settings_listing_fee] CHECK ([listing_fee] >= 0);

ALTER TABLE [dbo].[registration_players] ADD CONSTRAINT [CK_registration_players_position_enum] CHECK ([position] IN (N'Goalkeeper', N'Defender', N'Midfielder', N'Forward'));

CREATE UNIQUE INDEX [IX_payments_0] ON [dbo].[payments] ([share_id]) WHERE [share_id] IS NOT NULL;

CREATE INDEX [IX_payments_1] ON [dbo].[payments] ([payer_id], [created_at]);

CREATE INDEX [IX_payments_2] ON [dbo].[payments] ([platform_admin_id], [status], [created_at]);

ALTER TABLE [dbo].[payments] ADD CONSTRAINT [CK_payments_amount] CHECK ([amount] >= 0);

ALTER TABLE [dbo].[payments] ADD CONSTRAINT [CK_payments_commission] CHECK ([commission] >= 0);

ALTER TABLE [dbo].[payments] ADD CONSTRAINT [CK_payments_commission_rate] CHECK ([commission_rate] BETWEEN 0 AND 100);

ALTER TABLE [dbo].[payments] ADD CONSTRAINT [CK_payments_owner_net] CHECK ([owner_net] >= 0);

ALTER TABLE [dbo].[payments] ADD CONSTRAINT [CK_payments_rule_1] CHECK ([amount] > 0);

ALTER TABLE [dbo].[payments] ADD CONSTRAINT [CK_payments_rule_2] CHECK ([commission] + [owner_net] = [amount]);

ALTER TABLE [dbo].[payments] ADD CONSTRAINT [CK_payments_rule_3] CHECK (CASE WHEN [field_id] IS NULL THEN 0 ELSE 1 END + CASE WHEN [booking_id] IS NULL THEN 0 ELSE 1 END + CASE WHEN [share_id] IS NULL THEN 0 ELSE 1 END + CASE WHEN [game_participant_id] IS NULL THEN 0 ELSE 1 END + CASE WHEN [registration_id] IS NULL THEN 0 ELSE 1 END = 1);

ALTER TABLE [dbo].[payments] ADD CONSTRAINT [CK_payments_rule_4] CHECK ([registration_id] IS NULL OR ([commission_rate] = 100 AND [commission] = [amount] AND [owner_net] = 0 AND [owner_id] IS NULL));

ALTER TABLE [dbo].[payments] ADD CONSTRAINT [CK_payments_rule_5] CHECK ([status] <> 'Held (demo)' OR [share_id] IS NOT NULL);

ALTER TABLE [dbo].[payments] ADD CONSTRAINT [CK_payments_status_enum] CHECK ([status] IN (N'Held (demo)', N'Paid (demo)', N'Refunded (demo)'));

ALTER TABLE [dbo].[ownership_documents] ADD CONSTRAINT [CK_ownership_documents_byte_size] CHECK ([byte_size] BETWEEN 1 AND 1048576);

ALTER TABLE [dbo].[open_games] ADD CONSTRAINT [CK_open_games_capacity] CHECK ([capacity] >= 2);

ALTER TABLE [dbo].[open_games] ADD CONSTRAINT [CK_open_games_duration_minutes] CHECK ([duration_minutes] > 0);

ALTER TABLE [dbo].[open_games] ADD CONSTRAINT [CK_open_games_player_fee] CHECK ([player_fee] >= 0);

ALTER TABLE [dbo].[open_games] ADD CONSTRAINT [CK_open_games_status_enum] CHECK ([status] IN (N'Scheduled', N'Cancelled'));

ALTER TABLE [dbo].[messages] ADD CONSTRAINT [CK_messages_rule_1] CHECK ([sender_id] <> [recipient_id]);

CREATE UNIQUE INDEX [UQ_game_participants_game_id_user_id] ON [dbo].[game_participants] ([game_id], [user_id]);

ALTER TABLE [dbo].[game_participants] ADD CONSTRAINT [CK_game_participants_position_enum] CHECK ([position] IN (N'Goalkeeper', N'Defender', N'Midfielder', N'Forward'));

ALTER TABLE [dbo].[game_participants] ADD CONSTRAINT [CK_game_participants_status_enum] CHECK ([status] IN (N'Joined', N'Left'));

ALTER TABLE [dbo].[fields] ADD CONSTRAINT [CK_fields_format_enum] CHECK ([format] IN (N'5v5', N'6v6', N'7v7'));

ALTER TABLE [dbo].[fields] ADD CONSTRAINT [CK_fields_hourly_price] CHECK ([hourly_price] >= 0);

ALTER TABLE [dbo].[fields] ADD CONSTRAINT [CK_fields_latitude] CHECK ([latitude] BETWEEN -90 AND 90);

ALTER TABLE [dbo].[fields] ADD CONSTRAINT [CK_fields_location_precision_enum] CHECK ([location_precision] IN (N'exact', N'area', N'city', N'unknown'));

ALTER TABLE [dbo].[fields] ADD CONSTRAINT [CK_fields_longitude] CHECK ([longitude] BETWEEN -180 AND 180);

ALTER TABLE [dbo].[fields] ADD CONSTRAINT [CK_fields_rating] CHECK ([rating] BETWEEN 0 AND 5);

CREATE UNIQUE INDEX [UQ_field_images_field_id_sort_order] ON [dbo].[field_images] ([field_id], [sort_order]);

ALTER TABLE [dbo].[field_images] ADD CONSTRAINT [CK_field_images_sort_order] CHECK ([sort_order] >= 0);

ALTER TABLE [dbo].[feedback] ADD CONSTRAINT [CK_feedback_rating] CHECK ([rating] BETWEEN 1 AND 5);

CREATE INDEX [IX_challenges_3] ON [dbo].[challenges] ([field_id]);

ALTER TABLE [dbo].[challenges] ADD CONSTRAINT [CK_challenges_cost_agreement_enum] CHECK ([cost_agreement] IN (N'Half each', N'Host team pays', N'Visiting team pays'));

ALTER TABLE [dbo].[challenges] ADD CONSTRAINT [CK_challenges_format_enum] CHECK ([format] IN (N'5v5', N'6v6', N'7v7'));

ALTER TABLE [dbo].[challenges] ADD CONSTRAINT [CK_challenges_payment_mode_enum] CHECK ([payment_mode] IN (N'Split checkout', N'Legacy full'));

ALTER TABLE [dbo].[challenges] ADD CONSTRAINT [CK_challenges_quoted_price] CHECK ([quoted_price] >= 0);

ALTER TABLE [dbo].[challenges] ADD CONSTRAINT [CK_challenges_rule_1] CHECK ([opponent_team_id] IS NULL OR [opponent_team_id] <> [host_team_id]);

ALTER TABLE [dbo].[challenges] ADD CONSTRAINT [CK_challenges_rule_2] CHECK ([payment_mode] <> 'Split checkout' OR ([field_id] IS NOT NULL AND [quoted_price] IS NOT NULL AND [cost_agreement] = 'Half each'));

ALTER TABLE [dbo].[challenges] ADD CONSTRAINT [CK_challenges_status_enum] CHECK ([status] IN (N'Open', N'Agreed', N'Awaiting payment', N'Booked', N'Expired', N'Cancelled'));

CREATE UNIQUE INDEX [IX_challenge_requests_0] ON [dbo].[challenge_requests] ([challenge_id], [team_id]) WHERE [status] = 'Pending';

CREATE UNIQUE INDEX [IX_challenge_requests_1] ON [dbo].[challenge_requests] ([challenge_id]) WHERE [status] = 'Accepted';

ALTER TABLE [dbo].[challenge_requests] ADD CONSTRAINT [CK_challenge_requests_status_enum] CHECK ([status] IN (N'Pending', N'Accepted', N'Declined', N'Withdrawn'));

CREATE UNIQUE INDEX [IX_bookings_0] ON [dbo].[bookings] ([field_id], [starts_at]) WHERE [status] IN ('Pending','Confirmed');

CREATE UNIQUE INDEX [IX_bookings_1] ON [dbo].[bookings] ([challenge_id]) WHERE [challenge_id] IS NOT NULL;

CREATE INDEX [IX_bookings_3] ON [dbo].[bookings] ([owner_id]);

CREATE INDEX [IX_bookings_4] ON [dbo].[bookings] ([cancelled_by]);

ALTER TABLE [dbo].[bookings] ADD CONSTRAINT [CK_bookings_cancellation_reason_enum] CHECK ([cancellation_reason] IN (N'Cancelled', N'Expired'));

ALTER TABLE [dbo].[bookings] ADD CONSTRAINT [CK_bookings_commission] CHECK ([commission] >= 0);

ALTER TABLE [dbo].[bookings] ADD CONSTRAINT [CK_bookings_commission_rate] CHECK ([commission_rate] BETWEEN 0 AND 100);

ALTER TABLE [dbo].[bookings] ADD CONSTRAINT [CK_bookings_owner_net] CHECK ([owner_net] >= 0);

ALTER TABLE [dbo].[bookings] ADD CONSTRAINT [CK_bookings_price] CHECK ([price] >= 0);

ALTER TABLE [dbo].[bookings] ADD CONSTRAINT [CK_bookings_rule_1] CHECK ([commission] + [owner_net] = [price]);

ALTER TABLE [dbo].[bookings] ADD CONSTRAINT [CK_bookings_rule_2] CHECK ([is_split] = 0 OR ([challenge_id] IS NOT NULL AND [payment_deadline] IS NOT NULL));

ALTER TABLE [dbo].[bookings] ADD CONSTRAINT [CK_bookings_rule_3] CHECK ([payment_deadline] IS NULL OR [payment_deadline] <= [starts_at]);

ALTER TABLE [dbo].[bookings] ADD CONSTRAINT [CK_bookings_rule_4] CHECK (DATEPART(TZOFFSET,[starts_at]) = 180 AND DATEPART(HOUR,[starts_at]) BETWEEN 10 AND 22);

ALTER TABLE [dbo].[bookings] ADD CONSTRAINT [CK_bookings_starts_at] CHECK (DATEPART(MINUTE,[starts_at]) = 0 AND DATEPART(SECOND,[starts_at]) = 0);

ALTER TABLE [dbo].[bookings] ADD CONSTRAINT [CK_bookings_status_enum] CHECK ([status] IN (N'Pending', N'Confirmed', N'Completed', N'Cancelled'));

CREATE UNIQUE INDEX [UQ_amenities_name] ON [dbo].[amenities] ([name]);

CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [dbo].[AspNetRoleClaims] ([RoleId]);

CREATE UNIQUE INDEX [RoleNameIndex] ON [dbo].[AspNetRoles] ([NormalizedName]);

CREATE INDEX [IX_AspNetUserClaims_UserId] ON [dbo].[AspNetUserClaims] ([UserId]);

CREATE INDEX [IX_AspNetUserLogins_UserId] ON [dbo].[AspNetUserLogins] ([UserId]);

CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [dbo].[AspNetUserRoles] ([RoleId]);

CREATE UNIQUE INDEX [UX_AspNetUserRoles_UserId] ON [dbo].[AspNetUserRoles] ([UserId]);

CREATE INDEX [IX_booking_shares_1] ON [dbo].[booking_shares] ([team_id]);

CREATE INDEX [IX_booking_shares_2] ON [dbo].[booking_shares] ([payer_id]);

CREATE UNIQUE INDEX [IX_booking_shares_booking_id_team_id_payer_id] ON [dbo].[booking_shares] ([booking_id], [team_id], [payer_id]);

CREATE UNIQUE INDEX [UQ_booking_shares_booking_id_payer_id] ON [dbo].[booking_shares] ([booking_id], [payer_id]);

CREATE UNIQUE INDEX [UQ_booking_shares_booking_id_team_id] ON [dbo].[booking_shares] ([booking_id], [team_id]);

ALTER TABLE [dbo].[bookings] ADD CONSTRAINT [FK_bookings_cancelled_by_4] FOREIGN KEY ([cancelled_by]) REFERENCES [dbo].[users] ([id]);

ALTER TABLE [dbo].[bookings] ADD CONSTRAINT [FK_bookings_owner_id_2] FOREIGN KEY ([owner_id]) REFERENCES [dbo].[users] ([id]);

ALTER TABLE [dbo].[challenges] ADD CONSTRAINT [FK_challenges_field_id_3] FOREIGN KEY ([field_id]) REFERENCES [dbo].[fields] ([id]);

ALTER TABLE [dbo].[payments] ADD CONSTRAINT [FK_payments_platform_admin_id_1] FOREIGN KEY ([platform_admin_id]) REFERENCES [dbo].[users] ([id]);

ALTER TABLE [dbo].[payments] ADD CONSTRAINT [FK_payments_share_id_5] FOREIGN KEY ([share_id]) REFERENCES [dbo].[booking_shares] ([id]);

ALTER TABLE [dbo].[tournament_registrations] ADD CONSTRAINT [FK_tournament_registrations_withdrawn_by_3] FOREIGN KEY ([withdrawn_by]) REFERENCES [dbo].[users] ([id]);

ALTER TABLE [dbo].[tournaments] ADD CONSTRAINT [FK_tournaments_cancelled_by_2] FOREIGN KEY ([cancelled_by]) REFERENCES [dbo].[users] ([id]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261107153123_UpgradeKicksIdentityAndDomain', N'10.0.14');

COMMIT;
GO

