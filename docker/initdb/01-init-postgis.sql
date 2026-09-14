-- ==============================================================================
-- Project Spot: Database Extension Setup
-- Enable PostGIS spatial engine & UUID generators
-- ==============================================================================

CREATE EXTENSION IF NOT EXISTS postgis;
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Verify PostGIS Version
DO $$
BEGIN
    RAISE NOTICE 'PostGIS Version: %', postgis_full_version();
END $$;
