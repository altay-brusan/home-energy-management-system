-- =====================================================================
-- MIGRATION SQL Script: Hide duplicate generator parameters
-- These parameters are duplicates from GeneratorAndSmartLoad that overlap
-- with BatteryChargeSource configuration. They should be hidden from the UI.
--
-- Created: 2026-01-21
-- =====================================================================

BEGIN;

-- Hide duplicate parameters in GeneratorSettings group
-- These are duplicates of parameters in GeneratorCharging (under ChargeSources)
UPDATE "Parameters"
SET "Visible" = false, "LastModified" = NOW()
WHERE "Name" IN (
    'GeneratorCharge',        -- Duplicate of GeneratorChargingEnabled
    'GeneratorGridCharge',    -- Duplicate of GridChargingEnabled
    'GeneratorStartSOC',      -- Duplicate of GeneratorChargingStartSOC
    'GeneratorChargeCurrent'  -- Duplicate of GeneratorChargingCurrent
)
AND "Visible" = true;

-- NOTE: GeneratorPeakShavingEnabled and GeneratorPeakShavingLimitPower are NOT duplicates
-- They only exist in GeneratorAndSmartLoadConfig and should remain visible

COMMIT;

-- Verification query
SELECT "Name", "Visible", "LastModified"
FROM "Parameters"
WHERE "Name" IN (
    'GeneratorCharge',
    'GeneratorGridCharge',
    'GeneratorStartSOC',
    'GeneratorChargeCurrent'
);



  UPDATE "Parameters"
  SET "Description" = 'Generator port connection mode',
      "LastModified" = NOW()
  WHERE "Name" = 'SmartLoadEnabled';
