-- Seed mock health system data: sites, providers, conditions, allergies, medications,
-- immunizations, social history, and a couple of patients with linked records.
-- 
-- Usage (from repo root, assuming POSTGRES_CONNECTION_STRING points at mockhealthsystem_db):
--   psql "$POSTGRES_CONNECTION_STRING" -f backend/scripts/seed-mock-data.sql
-- Or, if you prefer psql flags:
--   psql -h localhost -p 5432 -U mockhealthsystem_user -d mockhealthsystem_db -f backend/scripts/seed-mock-data.sql

-- SITES ----------------------------------------------------------------------

INSERT INTO "Sites" ("Id", "Uid", "Name") VALUES
  (1, '11111111-1111-1111-1111-111111111111', 'Central Research Hospital'),
  (2, '22222222-2222-2222-2222-222222222222', 'Community Clinic East')
ON CONFLICT ("Id") DO NOTHING;

-- PROVIDER TYPES -------------------------------------------------------------

INSERT INTO "ProviderTypes" ("Id", "Name") VALUES
  (1, 'Primary Care'),
  (2, 'Cardiology')
ON CONFLICT ("Id") DO NOTHING;

-- PROVIDERS ------------------------------------------------------------------

INSERT INTO "Providers" ("Id", "ProviderName", "Title", "FirstName", "MiddleName", "LastName", "ProviderTypeId") VALUES
  (1, 'Dr. Jane Smith', 'MD', 'Jane', NULL, 'Smith', 1),
  (2, 'Dr. Robert Chen', 'MD', 'Robert', NULL, 'Chen', 2)
ON CONFLICT ("Id") DO NOTHING;

-- CONDITIONS -----------------------------------------------------------------

INSERT INTO "Conditions" ("Id", "Name", "Icd10Code", "Icd9Code") VALUES
  (1, 'Hypertension', 'I10', NULL),
  (2, 'Type 2 diabetes mellitus', 'E11.9', NULL),
  (3, 'Hyperlipidemia', 'E78.5', NULL)
ON CONFLICT ("Id") DO NOTHING;

-- ALLERGIES ------------------------------------------------------------------

INSERT INTO "Allergies" ("Id", "Name") VALUES
  (1, 'Penicillin'),
  (2, 'Peanuts')
ON CONFLICT ("Id") DO NOTHING;

-- MEDICATION ROUTES ----------------------------------------------------------

INSERT INTO "MedicationRoutes" ("Id", "Name") VALUES
  (1, 'Oral'),
  (2, 'Injection')
ON CONFLICT ("Id") DO NOTHING;

-- MEDICATIONS ----------------------------------------------------------------

INSERT INTO "Medications" ("Id", "Name") VALUES
  (1, 'Metformin'),
  (2, 'Lisinopril'),
  (3, 'Atorvastatin')
ON CONFLICT ("Id") DO NOTHING;

-- IMMUNIZATION TYPES & IMMUNIZATIONS ----------------------------------------

INSERT INTO "ImmunizationTypes" ("Id", "Name") VALUES
  (1, 'Influenza vaccine'),
  (2, 'COVID-19 mRNA vaccine')
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO "Immunizations" ("Id", "Name") VALUES
  (1, 'Influenza, quadrivalent'),
  (2, 'COVID-19 (mRNA), latest formulation')
ON CONFLICT ("Id") DO NOTHING;

-- RELATIONS (FAMILY) --------------------------------------------------------

INSERT INTO "Relations" ("Id", "Name") VALUES
  (1, 'Mother'),
  (2, 'Father'),
  (3, 'Sibling')
ON CONFLICT ("Id") DO NOTHING;

-- SOCIAL HISTORY LOOKUPS -----------------------------------------------------

INSERT INTO "ConditionTypes" ("Id", "Name") VALUES
  (1, 'Smoking'),
  (2, 'Alcohol')
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO "SocialHistories" ("Id", "Name", "CategoryId") VALUES
  (1, 'Current every day smoker', 1),
  (2, 'Former smoker', 1),
  (3, 'Drinks alcohol socially', 2)
ON CONFLICT ("Id") DO NOTHING;

-- PATIENTS -------------------------------------------------------------------

INSERT INTO "Patients" (
  "Id", "PrimarySiteId", "DisplayName", "Status", "StatusReason",
  "FirstName", "MiddleName", "LastName", "PreferredName", "Title",
  "Country", "Address1", "City", "State", "Zip",
  "DoNotMail", "RecruitmentTextOptIn", "PhoneTypeToText", "Fax",
  "DateOfBirth", "GenderCode", "Race", "Ethnicity", "NativeLanguage",
  "MaritalStatus", "WeightValue", "WeightUnit", "HeightValue", "HeightUnit",
  "Ssn", "Mrn", "ImportId", "ImportSourceId", "ImportPatientId", "Uid",
  "PrimaryEmailAddress", "PrimaryDoNotEmail", "SecondaryEmailAddress", "SecondaryDoNotEmail",
  "GuardianJson", "PrimaryInsuranceJson", "SecondaryInsuranceJson", "CustomFieldsJson",
  "ManagedMedicare", "CaregiverId", "Caregiver"
) VALUES
  (
    1, 1, 'John Doe', 'Active', NULL,
    'John', NULL, 'Doe', 'John', 'Mr.',
    'USA', '123 Main St', 'Springfield', 'IL', '62701',
    FALSE, TRUE, 'Phone1', NULL,
    DATE '1980-05-15', 'M', 'White', 'Not Hispanic or Latino', 'English',
    'Married', 95.0, 'kgs', 178.0, 'cms',
    '123-45-6789', 'MRN-0001', NULL, NULL, NULL, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    'john.doe@example.com', FALSE, NULL, FALSE,
    NULL, NULL, NULL, NULL,
    FALSE, NULL, FALSE
  ),
  (
    2, 2, 'Maria Lopez', 'Active', NULL,
    'Maria', 'Isabel', 'Lopez', 'Maria', 'Ms.',
    'USA', '456 Oak Ave', 'Riverton', 'CA', '94065',
    FALSE, TRUE, 'Phone1', NULL,
    DATE '1990-09-20', 'F', 'White', 'Hispanic or Latino', 'Spanish',
    'Single', 68.0, 'kgs', 165.0, 'cms',
    '987-65-4321', 'MRN-0002', NULL, NULL, NULL, 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
    'maria.lopez@example.com', FALSE, NULL, FALSE,
    NULL, NULL, NULL, NULL,
    FALSE, NULL, FALSE
  )
ON CONFLICT ("Id") DO NOTHING;

-- PATIENT PHONES -------------------------------------------------------------

INSERT INTO "PatientPhones" ("Id", "PatientId", "Slot", "Number", "RawNumber", "OutOfService") VALUES
  (1, 1, 1, '(217) 555-0100', '2175550100', FALSE),
  (2, 2, 1, '(415) 555-0200', '4155550200', FALSE)
ON CONFLICT ("Id") DO NOTHING;

-- PATIENT PROVIDERS ----------------------------------------------------------

INSERT INTO "PatientProviders" ("Id", "PatientId", "ProviderId", "Comment", "StartDate", "EndDate") VALUES
  (1, 1, 1, 'Primary care physician', DATE '2020-01-01', NULL),
  (2, 2, 2, 'Cardiologist', DATE '2022-03-15', NULL)
ON CONFLICT ("Id") DO NOTHING;

-- PATIENT CONDITIONS ---------------------------------------------------------

INSERT INTO "PatientConditions" ("Id", "PatientId", "ConditionId", "StartDate", "EndDate", "AgeAtOnset", "Comment") VALUES
  (1, 1, 1, DATE '2015-06-01', NULL, '35', 'Diagnosed during routine exam'),
  (2, 1, 3, DATE '2018-09-15', NULL, '38', 'Elevated LDL'),
  (3, 2, 2, DATE '2020-11-10', NULL, '30', 'Diagnosed after screening')
ON CONFLICT ("Id") DO NOTHING;

-- PATIENT ALLERGIES ---------------------------------------------------------

INSERT INTO "PatientAllergies" ("Id", "PatientId", "AllergyId", "Reaction", "Comment", "StartDate", "EndDate") VALUES
  (1, 1, 1, 'Rash', 'Mild rash as child', DATE '1990-01-01', NULL),
  (2, 2, 2, 'Anaphylaxis', 'Carries EpiPen', DATE '2000-01-01', NULL)
ON CONFLICT ("Id") DO NOTHING;

-- PATIENT PROCEDURES --------------------------------------------------------

INSERT INTO "PatientProcedures" ("Id", "PatientId", "ProcedureId", "Name", "Comment", "CptCode", "ProcedureBy", "Date") VALUES
  (1, 1, NULL, 'Colonoscopy', 'Screening', '45378', 'Dr. Jane Smith', DATE '2021-02-10'),
  (2, 2, NULL, 'Echocardiogram', 'Baseline cardiac function', '93306', 'Dr. Robert Chen', DATE '2023-01-05')
ON CONFLICT ("Id") DO NOTHING;

-- PATIENT MEDICATIONS -------------------------------------------------------

INSERT INTO "PatientMedications" ("Id", "PatientId", "MedicationId", "RouteId", "Dosage", "StartDate", "EndDate", "Comment") VALUES
  (1, 1, 2, 1, '10 mg once daily', DATE '2016-01-01', NULL, 'Blood pressure control'),
  (2, 1, 3, 1, '20 mg once daily', DATE '2018-10-01', NULL, 'Cholesterol management'),
  (3, 2, 1, 1, '500 mg twice daily', DATE '2021-01-15', NULL, 'Glycemic control')
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO "PatientMedicationConditions" ("PatientMedicationId", "PatientConditionId") VALUES
  (1, 1),
  (2, 2),
  (3, 3)
ON CONFLICT ("PatientMedicationId", "PatientConditionId") DO NOTHING;

-- PATIENT IMMUNIZATIONS -----------------------------------------------------

INSERT INTO "PatientImmunizations" ("Id", "PatientId", "ImmunizationId", "ImmunizationTypeId", "Name", "Comment", "Location", "Date") VALUES
  (1, 1, 1, 1, 'Influenza, quadrivalent', 'Annual flu shot', 'Left deltoid', DATE '2023-10-15'),
  (2, 2, 2, 2, 'COVID-19 (mRNA)', 'Booster dose', 'Right deltoid', DATE '2024-02-20')
ON CONFLICT ("Id") DO NOTHING;

-- PATIENT FAMILY HISTORY ----------------------------------------------------

INSERT INTO "PatientFamilyHistories" ("Id", "PatientId", "ConditionId", "FamilyMemberId", "RelationName", "AgeAtOnset", "Comment", "StartDate", "EndDate") VALUES
  (1, 1, 2, 1, 'Mother', '45', 'Type 2 diabetes in mother', DATE '2000-01-01', NULL),
  (2, 2, 1, 2, 'Father', '50', 'Hypertension in father', DATE '2010-01-01', NULL)
ON CONFLICT ("Id") DO NOTHING;

-- PATIENT SOCIAL HISTORY ----------------------------------------------------

INSERT INTO "PatientSocialHistoryEntries" ("Id", "PatientId", "SocialHistoryId", "Comment") VALUES
  (1, 1, 3, 'Occasional social drinking, denies binge episodes'),
  (2, 2, 2, 'Quit smoking 5 years ago, 5 pack-year history')
ON CONFLICT ("Id") DO NOTHING;

-- Reset identity sequences so the next generated Id is above existing data.
-- Required when seeding with explicit IDs; otherwise "Generate patients" hits duplicate key.
SELECT setval(pg_get_serial_sequence('"Sites"', 'Id'), (SELECT COALESCE(MAX("Id"), 1) FROM "Sites"));
SELECT setval(pg_get_serial_sequence('"ProviderTypes"', 'Id'), (SELECT COALESCE(MAX("Id"), 1) FROM "ProviderTypes"));
SELECT setval(pg_get_serial_sequence('"Providers"', 'Id'), (SELECT COALESCE(MAX("Id"), 1) FROM "Providers"));
SELECT setval(pg_get_serial_sequence('"Conditions"', 'Id'), (SELECT COALESCE(MAX("Id"), 1) FROM "Conditions"));
SELECT setval(pg_get_serial_sequence('"ConditionTypes"', 'Id'), (SELECT COALESCE(MAX("Id"), 1) FROM "ConditionTypes"));
SELECT setval(pg_get_serial_sequence('"Allergies"', 'Id'), (SELECT COALESCE(MAX("Id"), 1) FROM "Allergies"));
SELECT setval(pg_get_serial_sequence('"Devices"', 'Id'), (SELECT COALESCE(MAX("Id"), 1) FROM "Devices"));
SELECT setval(pg_get_serial_sequence('"MedicationRoutes"', 'Id'), (SELECT COALESCE(MAX("Id"), 1) FROM "MedicationRoutes"));
SELECT setval(pg_get_serial_sequence('"Medications"', 'Id'), (SELECT COALESCE(MAX("Id"), 1) FROM "Medications"));
SELECT setval(pg_get_serial_sequence('"ImmunizationTypes"', 'Id'), (SELECT COALESCE(MAX("Id"), 1) FROM "ImmunizationTypes"));
SELECT setval(pg_get_serial_sequence('"Immunizations"', 'Id'), (SELECT COALESCE(MAX("Id"), 1) FROM "Immunizations"));
SELECT setval(pg_get_serial_sequence('"Procedures"', 'Id'), (SELECT COALESCE(MAX("Id"), 1) FROM "Procedures"));
SELECT setval(pg_get_serial_sequence('"Relations"', 'Id'), (SELECT COALESCE(MAX("Id"), 1) FROM "Relations"));
SELECT setval(pg_get_serial_sequence('"SocialHistories"', 'Id'), (SELECT COALESCE(MAX("Id"), 1) FROM "SocialHistories"));
SELECT setval(pg_get_serial_sequence('"Patients"', 'Id'), (SELECT COALESCE(MAX("Id"), 1) FROM "Patients"));
SELECT setval(pg_get_serial_sequence('"PatientPhones"', 'Id'), (SELECT COALESCE(MAX("Id"), 1) FROM "PatientPhones"));
SELECT setval(pg_get_serial_sequence('"PatientProviders"', 'Id'), (SELECT COALESCE(MAX("Id"), 1) FROM "PatientProviders"));
SELECT setval(pg_get_serial_sequence('"PatientConditions"', 'Id'), (SELECT COALESCE(MAX("Id"), 1) FROM "PatientConditions"));
SELECT setval(pg_get_serial_sequence('"PatientAllergies"', 'Id'), (SELECT COALESCE(MAX("Id"), 1) FROM "PatientAllergies"));
SELECT setval(pg_get_serial_sequence('"PatientProcedures"', 'Id'), (SELECT COALESCE(MAX("Id"), 1) FROM "PatientProcedures"));
SELECT setval(pg_get_serial_sequence('"PatientMedications"', 'Id'), (SELECT COALESCE(MAX("Id"), 1) FROM "PatientMedications"));
SELECT setval(pg_get_serial_sequence('"PatientImmunizations"', 'Id'), (SELECT COALESCE(MAX("Id"), 1) FROM "PatientImmunizations"));
SELECT setval(pg_get_serial_sequence('"PatientFamilyHistories"', 'Id'), (SELECT COALESCE(MAX("Id"), 1) FROM "PatientFamilyHistories"));
SELECT setval(pg_get_serial_sequence('"PatientSocialHistoryEntries"', 'Id'), (SELECT COALESCE(MAX("Id"), 1) FROM "PatientSocialHistoryEntries"));

