-- =====================================================
-- Netherlands Country and Cities Seed Script
-- Run this in DBeaver or any PostgreSQL client
-- =====================================================

DO $$
DECLARE
    netherlands_id UUID;
BEGIN
    -- Generate UUID for Netherlands
    netherlands_id := gen_random_uuid();

    -- Insert Netherlands
    INSERT INTO "Countries" ("Id", "Name", "Code", "IsActive", "Created", "CreatedBy", "LastModified", "LastModifiedBy")
    VALUES (
        netherlands_id,
        'Netherlands',
        'NL',
        true,
        NOW() AT TIME ZONE 'UTC',
        'System',
        NOW() AT TIME ZONE 'UTC',
        'System'
    );

    -- Insert Dutch Cities (Major cities and municipalities)
    INSERT INTO "Cities" ("Id", "Name", "CountryId", "IsActive", "Created", "CreatedBy", "LastModified", "LastModifiedBy")
    VALUES
        -- Major Cities (Top 50 by population)
        (gen_random_uuid(), 'Amsterdam', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Rotterdam', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'The Hague', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Utrecht', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Eindhoven', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Groningen', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Tilburg', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Almere', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Breda', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Nijmegen', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Apeldoorn', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Haarlem', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Arnhem', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Enschede', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Amersfoort', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Zaanstad', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), '''s-Hertogenbosch', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Zwolle', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Maastricht', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Leiden', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Dordrecht', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Haarlemmermeer', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Zoetermeer', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Emmen', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Ede', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Delft', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Venlo', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Deventer', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Leeuwarden', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Alkmaar', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Helmond', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Hilversum', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Heerlen', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Oss', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Sittard-Geleen', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Amstelveen', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Purmerend', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Schiedam', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Spijkenisse', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Vlaardingen', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Hoorn', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Gouda', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Lelystad', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Alphen aan den Rijn', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Roosendaal', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Assen', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Nieuwegein', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Veenendaal', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Zeist', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Hardenberg', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),

        -- Additional Cities by Province
        -- Noord-Holland
        (gen_random_uuid(), 'Hengelo', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Hoofddorp', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Den Helder', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Heerhugowaard', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Beverwijk', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Velsen', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'IJmuiden', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Enkhuizen', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Medemblik', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),

        -- Zuid-Holland
        (gen_random_uuid(), 'Capelle aan den IJssel', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Ridderkerk', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Barendrecht', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Papendrecht', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Sliedrecht', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Gorinchem', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Leerdam', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Katwijk', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Noordwijk', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Voorburg', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Rijswijk', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Wassenaar', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Leidschendam', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),

        -- Gelderland
        (gen_random_uuid(), 'Barneveld', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Doetinchem', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Tiel', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Culemborg', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Wageningen', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Rhenen', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Wijchen', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Harderwijk', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Ermelo', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Nunspeet', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Elburg', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Putten', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Epe', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Zutphen', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Lochem', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Winterswijk', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Groenlo', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Zevenaar', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Duiven', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Westervoort', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),

        -- Noord-Brabant
        (gen_random_uuid(), 'Waalwijk', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Uden', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Veghel', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Boxtel', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Best', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Veldhoven', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Bergen op Zoom', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Etten-Leur', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Oosterhout', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Dongen', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Geertruidenberg', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Raamsdonksveer', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Made', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Heusden', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Vught', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Sint-Michielsgestel', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Schijndel', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Sint-Oedenrode', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Son en Breugel', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Nuenen', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Geldrop', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Mierlo', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Heeze', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Valkenswaard', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Waalre', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Eersel', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Bladel', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Reusel', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Bergeijk', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),

        -- Limburg
        (gen_random_uuid(), 'Roermond', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Weert', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Kerkrade', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Landgraaf', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Brunssum', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Stein', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Beek', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Meerssen', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Valkenburg aan de Geul', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Vaals', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Gulpen-Wittem', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Eijsden-Margraten', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Venray', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Horst aan de Maas', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Peel en Maas', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Nederweert', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Leudal', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Echt-Susteren', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Maasgouw', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Beesel', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Bergen', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Gennep', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Mook en Middelaar', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),

        -- Overijssel
        (gen_random_uuid(), 'Kampen', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Almelo', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Oldenzaal', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Rijssen', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Holten', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Vriezenveen', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Wierden', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Borne', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Losser', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Dinkelland', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Tubbergen', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Hellendoorn', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Raalte', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Olst-Wijhe', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Dalfsen', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Ommen', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Staphorst', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Steenwijkerland', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),

        -- Zeeland
        (gen_random_uuid(), 'Middelburg', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Vlissingen', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Goes', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Terneuzen', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Hulst', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Sluis', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Veere', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Schouwen-Duiveland', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Noord-Beveland', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Borsele', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Kapelle', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Reimerswaal', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Tholen', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),

        -- Friesland
        (gen_random_uuid(), 'Sneek', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Drachten', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Heerenveen', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Harlingen', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Franeker', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Dokkum', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Bolsward', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Joure', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Wolvega', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Lemmer', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Balk', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Workum', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Makkum', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Stavoren', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Hindeloopen', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),

        -- Drenthe
        (gen_random_uuid(), 'Hoogeveen', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Meppel', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Coevorden', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Beilen', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Roden', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Borger', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Klazienaveen', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),

        -- Flevoland
        (gen_random_uuid(), 'Dronten', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Zeewolde', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Urk', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Noordoostpolder', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),

        -- Utrecht Province (additional)
        (gen_random_uuid(), 'Woerden', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'IJsselstein', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Houten', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Vianen', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Bunschoten', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Soest', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Baarn', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'De Bilt', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Bilthoven', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Maarssen', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Breukelen', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Loenen', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Wijk bij Duurstede', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Driebergen', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Doorn', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Leersum', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Amerongen', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System');

    -- Output the generated Netherlands ID
    RAISE NOTICE 'Netherlands inserted with ID: %', netherlands_id;
END $$;

-- =====================================================
-- Verification Queries
-- =====================================================
SELECT 'Netherlands Country:' as info, "Id", "Name", "Code" FROM "Countries" WHERE "Code" = 'NL';
SELECT 'Total Dutch Cities:' as info, COUNT(*) as count FROM "Cities" c
    INNER JOIN "Countries" co ON c."CountryId" = co."Id"
    WHERE co."Code" = 'NL';
