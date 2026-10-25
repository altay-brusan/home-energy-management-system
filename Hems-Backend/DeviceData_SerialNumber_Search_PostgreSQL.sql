-- DeviceData tablosunda JSON içindeki DeviceSerialNumber ile arama script'i
-- PostgreSQL için optimize edilmiş sorgular

-- 1. Temel JSON içindeki DeviceSerialNumber ile arama
SELECT 
    dd."Id",
    dd."DataType",
    dd."DeviceId",
    dd."RequestDate",
    dd."Data",
    dd."Created",
    dd."CreatedBy",
    dd."Data"->>'DeviceSerialNumber' as "DeviceSerialNumber",
    dd."Data"->>'DeviceModel' as "DeviceModel",
    dd."Data"->>'FirmwareVersion' as "FirmwareVersion"
FROM "DeviceData" dd
WHERE dd."Data"->>'DeviceSerialNumber' = 'Test1234'  -- JSON içindeki DeviceSerialNumber ile arama
ORDER BY dd."RequestDate" DESC;

-- 2. JSON DeviceSerialNumber ile DataType'a göre filtreleme
SELECT 
    dd."Id",
    dd."DataType",
    dd."DeviceId",
    dd."RequestDate",
    dd."Data",
    dd."Data"->>'DeviceSerialNumber' as "DeviceSerialNumber",
    dd."Data"->>'DeviceModel' as "DeviceModel"
FROM "DeviceData" dd
WHERE dd."Data"->>'DeviceSerialNumber' = 'Test1234'
  AND dd."DataType" = 'BatteryData'  -- DataType filtresi
ORDER BY dd."RequestDate" DESC;

-- 3. JSON DeviceSerialNumber ile tarih aralığında arama
SELECT 
    dd."Id",
    dd."DataType",
    dd."DeviceId",
    dd."RequestDate",
    dd."Data",
    dd."Data"->>'DeviceSerialNumber' as "DeviceSerialNumber",
    dd."Data"->>'DateTime' as "DataDateTime"
FROM "DeviceData" dd
WHERE dd."Data"->>'DeviceSerialNumber' = 'Test1234'
  AND dd."RequestDate" >= '2025-10-01'::timestamp
  AND dd."RequestDate" <= '2025-10-31'::timestamp
ORDER BY dd."RequestDate" DESC;

-- 4. Pagination ile JSON DeviceSerialNumber arama (sayfalama)
SELECT 
    dd."Id",
    dd."DataType",
    dd."DeviceId",
    dd."RequestDate",
    dd."Data",
    dd."Data"->>'DeviceSerialNumber' as "DeviceSerialNumber"
FROM "DeviceData" dd
WHERE dd."Data"->>'DeviceSerialNumber' = 'Test1234'
ORDER BY dd."RequestDate" DESC
LIMIT 10 OFFSET 0;  -- İlk 10 kayıt

-- 5. Serial Number ile JSON data içinde arama (PostgreSQL JSONB özelliği)
SELECT 
    dd.id,
    dd."DataType",
    dd."DeviceId",
    dd."RequestDate",
    dd."Data",
    d."SerialNumber"
FROM "DeviceData" dd
INNER JOIN "Devices" d ON dd."DeviceId" = d."Id"
WHERE d."SerialNumber" = 'SERIAL123456'
  AND dd."Data" @> '{"power": 100}'::jsonb  -- JSON içinde belirli değer arama
ORDER BY dd."RequestDate" DESC;

-- 6. Serial Number ile aggregate fonksiyonlar (toplam, ortalama vb.)
SELECT 
    d."SerialNumber",
    d."Manufacturer",
    COUNT(dd.id) as "TotalRecords",
    MIN(dd."RequestDate") as "FirstRecord",
    MAX(dd."RequestDate") as "LastRecord",
    AVG((dd."Data"->>'power')::numeric) as "AvgPower"  -- JSON'dan power değeri alıp ortalama hesapla
FROM "DeviceData" dd
INNER JOIN "Devices" d ON dd."DeviceId" = d."Id"
WHERE d."SerialNumber" = 'SERIAL123456'
GROUP BY d."SerialNumber", d."Manufacturer";

-- 7. Serial Number ile LIKE arama (kısmi eşleşme)
SELECT 
    dd.id,
    dd."DataType",
    dd."DeviceId",
    dd."RequestDate",
    dd."Data",
    d."SerialNumber",
    d."Manufacturer"
FROM "DeviceData" dd
INNER JOIN "Devices" d ON dd."DeviceId" = d."Id"
WHERE d."SerialNumber" LIKE 'SERIAL%'  -- SERIAL ile başlayan serial number'lar
ORDER BY dd."RequestDate" DESC;

-- 8. Serial Number ile IN operatörü (birden fazla serial number)
SELECT 
    dd.id,
    dd."DataType",
    dd."DeviceId",
    dd."RequestDate",
    dd."Data",
    d."SerialNumber",
    d."Manufacturer"
FROM "DeviceData" dd
INNER JOIN "Devices" d ON dd."DeviceId" = d."Id"
WHERE d."SerialNumber" IN ('SERIAL123456', 'SERIAL789012', 'SERIAL345678')
ORDER BY d."SerialNumber", dd."RequestDate" DESC;

-- 9. Serial Number ile EXISTS subquery (performanslı arama)
SELECT 
    dd.id,
    dd."DataType",
    dd."DeviceId",
    dd."RequestDate",
    dd."Data"
FROM "DeviceData" dd
WHERE EXISTS (
    SELECT 1 
    FROM "Devices" d 
    WHERE d."Id" = dd."DeviceId" 
      AND d."SerialNumber" = 'SERIAL123456'
)
ORDER BY dd."RequestDate" DESC;

-- 10. Serial Number ile CTE (Common Table Expression) kullanımı
WITH DeviceSerialData AS (
    SELECT 
        dd.*,
        d."SerialNumber",
        d."Manufacturer",
        d."ModelNumber"
    FROM "DeviceData" dd
    INNER JOIN "Devices" d ON dd."DeviceId" = d."Id"
    WHERE d."SerialNumber" = 'SERIAL123456'
)
SELECT 
    id,
    "DataType",
    "DeviceId",
    "RequestDate",
    "Data",
    "SerialNumber",
    "Manufacturer"
FROM DeviceSerialData
ORDER BY "RequestDate" DESC;

-- 11. Index kullanımı için öneriler
-- Bu sorgular için aşağıdaki index'ler performansı artıracaktır:
-- CREATE INDEX CONCURRENTLY "IX_Devices_SerialNumber" ON "Devices" ("SerialNumber");
-- CREATE INDEX CONCURRENTLY "IX_DeviceData_DeviceId_RequestDate" ON "DeviceData" ("DeviceId", "RequestDate");

-- 12. Serial Number ile JSON path arama
SELECT 
    dd.id,
    dd."DataType",
    dd."DeviceId",
    dd."RequestDate",
    dd."Data",
    d."SerialNumber"
FROM "DeviceData" dd
INNER JOIN "Devices" d ON dd."DeviceId" = d."Id"
WHERE d."SerialNumber" = 'SERIAL123456'
  AND dd."Data" #> '{sensors,temperature}' IS NOT NULL  -- JSON path ile arama
ORDER BY dd."RequestDate" DESC;

-- 5. BatteryData ile JSON içindeki DeviceSerialNumber arama
SELECT 
    dd."Id",
    dd."DataType",
    dd."DeviceId",
    dd."RequestDate",
    dd."Data",
    dd."Data"->>'DeviceSerialNumber' as "DeviceSerialNumber",
    dd."Data"->>'SOH' as "StateOfHealth",
    dd."Data"->>'BatteryCurrent' as "BatteryCurrent",
    dd."Data"->>'BatteryPackVoltage' as "BatteryPackVoltage",
    dd."Data"->>'MaxCellVoltage' as "MaxCellVoltage",
    dd."Data"->>'MinCellVoltage' as "MinCellVoltage",
    dd."Data"->>'MaxCellTemperature' as "MaxCellTemperature",
    dd."Data"->>'MinCellTemperature' as "MinCellTemperature",
    dd."Data"->>'DeviceModel' as "DeviceModel",
    dd."Data"->>'FirmwareVersion' as "FirmwareVersion"
FROM "DeviceData" dd
WHERE dd."Data"->>'DeviceSerialNumber' = 'Test1234'  -- JSON içindeki DeviceSerialNumber ile arama
ORDER BY dd."RequestDate" DESC;

-- 6. BatteryData ile SOH (State of Health) değerine göre filtreleme
SELECT 
    dd."Id",
    dd."DataType",
    dd."DeviceId",
    dd."RequestDate",
    dd."Data",
    dd."Data"->>'DeviceSerialNumber' as "DeviceSerialNumber",
    (dd."Data"->>'SOH')::numeric as "StateOfHealth",
    (dd."Data"->>'BatteryCurrent')::numeric as "BatteryCurrent"
FROM "DeviceData" dd
WHERE dd."Data"->>'DeviceSerialNumber' = 'Test1234'
  AND (dd."Data"->>'SOH')::numeric > 80.0  -- SOH 80'den büyük olanlar
ORDER BY dd."RequestDate" DESC;

-- 7. BatteryData ile Cell bilgilerini ayrı ayrı alma
SELECT 
    dd."Id",
    dd."DataType",
    dd."DeviceId",
    dd."RequestDate",
    dd."Data",
    dd."Data"->>'DeviceSerialNumber' as "DeviceSerialNumber",
    jsonb_array_elements(dd."Data"->'SOC') as "CellData"
FROM "DeviceData" dd
WHERE dd."Data"->>'DeviceSerialNumber' = 'Test1234'
ORDER BY dd."RequestDate" DESC;

-- 8. BatteryData ile Cell detaylarını ayrıntılı alma
SELECT 
    dd."Id",
    dd."DataType",
    dd."DeviceId",
    dd."RequestDate",
    dd."Data",
    dd."Data"->>'DeviceSerialNumber' as "DeviceSerialNumber",
    cell_data->>'CellNumber' as "CellNumber",
    (cell_data->>'CellVoltage')::numeric as "CellVoltage",
    (cell_data->>'CellTemperature')::numeric as "CellTemperature"
FROM "DeviceData" dd
CROSS JOIN LATERAL jsonb_array_elements(dd."Data"->'SOC') as cell_data
WHERE dd."Data"->>'DeviceSerialNumber' = 'Test1234'
ORDER BY dd."RequestDate" DESC, (cell_data->>'CellNumber')::int;

-- 9. BatteryData ile Battery Current'a göre filtreleme
SELECT 
    dd."Id",
    dd."DataType",
    dd."DeviceId",
    dd."RequestDate",
    dd."Data",
    dd."Data"->>'DeviceSerialNumber' as "DeviceSerialNumber",
    (dd."Data"->>'BatteryCurrent')::numeric as "BatteryCurrent",
    (dd."Data"->>'BatteryPackVoltage')::numeric as "BatteryPackVoltage"
FROM "DeviceData" dd
WHERE dd."Data"->>'DeviceSerialNumber' = 'Test1234'
  AND (dd."Data"->>'BatteryCurrent')::numeric > 5.0  -- Battery Current 5'ten büyük olanlar
ORDER BY dd."RequestDate" DESC;

-- 10. BatteryData ile tarih aralığında arama (JSON içindeki DateTime ile)
SELECT 
    dd."Id",
    dd."DataType",
    dd."DeviceId",
    dd."RequestDate",
    dd."Data",
    dd."Data"->>'DeviceSerialNumber' as "DeviceSerialNumber",
    dd."Data"->>'DateTime' as "DataDateTime",
    (dd."Data"->>'SOH')::numeric as "StateOfHealth"
FROM "DeviceData" dd
WHERE dd."Data"->>'DeviceSerialNumber' = 'Test1234'
  AND dd."Data"->>'DateTime' >= '2025-10-01'
  AND dd."Data"->>'DateTime' <= '2025-10-31'
ORDER BY dd."RequestDate" DESC;

-- 11. BatteryData ile aggregate fonksiyonlar (ortalama, min, max)
SELECT 
    dd."Data"->>'DeviceSerialNumber' as "DeviceSerialNumber",
    COUNT(*) as "TotalRecords",
    AVG((dd."Data"->>'SOH')::numeric) as "AvgSOH",
    MIN((dd."Data"->>'SOH')::numeric) as "MinSOH",
    MAX((dd."Data"->>'SOH')::numeric) as "MaxSOH",
    AVG((dd."Data"->>'BatteryCurrent')::numeric) as "AvgBatteryCurrent",
    AVG((dd."Data"->>'BatteryPackVoltage')::numeric) as "AvgBatteryPackVoltage"
FROM "DeviceData" dd
WHERE dd."Data"->>'DeviceSerialNumber' = 'Test1234'
GROUP BY dd."Data"->>'DeviceSerialNumber';

-- 12. BatteryData ile Cell Voltage analizi
SELECT 
    dd."Id",
    dd."DataType",
    dd."DeviceId",
    dd."RequestDate",
    dd."Data",
    dd."Data"->>'DeviceSerialNumber' as "DeviceSerialNumber",
    (dd."Data"->>'MaxCellVoltage')::numeric as "MaxCellVoltage",
    (dd."Data"->>'MinCellVoltage')::numeric as "MinCellVoltage",
    (dd."Data"->>'MaxCellVoltage')::numeric - (dd."Data"->>'MinCellVoltage')::numeric as "VoltageDifference"
FROM "DeviceData" dd
WHERE dd."Data"->>'DeviceSerialNumber' = 'Test1234'
ORDER BY dd."RequestDate" DESC;

-- 13. BatteryData ile Cell Temperature analizi
SELECT 
    dd."Id",
    dd."DataType",
    dd."DeviceId",
    dd."RequestDate",
    dd."Data",
    dd."Data"->>'DeviceSerialNumber' as "DeviceSerialNumber",
    (dd."Data"->>'MaxCellTemperature')::numeric as "MaxCellTemperature",
    (dd."Data"->>'MinCellTemperature')::numeric as "MinCellTemperature",
    (dd."Data"->>'MaxCellTemperature')::numeric - (dd."Data"->>'MinCellTemperature')::numeric as "TemperatureDifference"
FROM "DeviceData" dd
WHERE dd."Data"->>'DeviceSerialNumber' = 'Test1234'
ORDER BY dd."RequestDate" DESC;

-- 14. BatteryData ile LIKE arama (kısmi DeviceSerialNumber eşleşme)
SELECT 
    dd."Id",
    dd."DataType",
    dd."DeviceId",
    dd."RequestDate",
    dd."Data",
    dd."Data"->>'DeviceSerialNumber' as "DeviceSerialNumber",
    dd."Data"->>'DeviceModel' as "DeviceModel"
FROM "DeviceData" dd
WHERE dd."Data"->>'DeviceSerialNumber' LIKE 'Test%'  -- Test ile başlayan serial number'lar
ORDER BY dd."RequestDate" DESC;

-- 15. BatteryData ile IN operatörü (birden fazla DeviceSerialNumber)
SELECT 
    dd."Id",
    dd."DataType",
    dd."DeviceId",
    dd."RequestDate",
    dd."Data",
    dd."Data"->>'DeviceSerialNumber' as "DeviceSerialNumber",
    dd."Data"->>'DeviceModel' as "DeviceModel"
FROM "DeviceData" dd
WHERE dd."Data"->>'DeviceSerialNumber' IN ('Test1234', 'Test5678', 'Test9999')
ORDER BY dd."Data"->>'DeviceSerialNumber', dd."RequestDate" DESC;

-- 16. Index önerileri (JSON içindeki DeviceSerialNumber için)
-- Bu sorgular için aşağıdaki index'ler performansı artıracaktır:
-- CREATE INDEX CONCURRENTLY "IX_DeviceData_Data_DeviceSerialNumber" ON "DeviceData" USING GIN (("Data"->>'DeviceSerialNumber'));
-- CREATE INDEX CONCURRENTLY "IX_DeviceData_DataType_RequestDate" ON "DeviceData" ("DataType", "RequestDate");
-- CREATE INDEX CONCURRENTLY "IX_DeviceData_RequestDate" ON "DeviceData" ("RequestDate");
