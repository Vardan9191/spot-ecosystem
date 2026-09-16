-- ==============================================================================
-- Project Spot: PostgreSQL + PostGIS Core Schema & Spatial Indexes
-- SRID: 4326 (WGS 84)
-- ==============================================================================

-- 1. Categories Table
CREATE TABLE IF NOT EXISTS categories (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name TEXT NOT NULL,
    slug TEXT NOT NULL UNIQUE,
    icon TEXT NOT NULL
);

-- 2. Stores Table (Spatial point location with SRID 4326)
CREATE TABLE IF NOT EXISTS stores (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name TEXT NOT NULL,
    category_id UUID NOT NULL REFERENCES categories(id) ON DELETE RESTRICT,
    location GEOMETRY(Point, 4326) NOT NULL,
    radius_meters INT NOT NULL DEFAULT 100,
    address TEXT NOT NULL,
    is_partner BOOLEAN NOT NULL DEFAULT FALSE
);

-- 3. Store Reviews Table
CREATE TABLE IF NOT EXISTS store_reviews (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    store_id UUID NOT NULL REFERENCES stores(id) ON DELETE CASCADE,
    user_id UUID NOT NULL,
    rating_service INT NOT NULL CHECK (rating_service BETWEEN 1 AND 5),
    rating_quality INT NOT NULL CHECK (rating_quality BETWEEN 1 AND 5),
    rating_overall INT NOT NULL CHECK (rating_overall BETWEEN 1 AND 5),
    comment TEXT NOT NULL DEFAULT '',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 4. Task Lists Table (Geofenced user shopping / to-do lists)
CREATE TABLE IF NOT EXISTS task_lists (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL,
    title TEXT NOT NULL,
    category_id UUID REFERENCES categories(id) ON DELETE SET NULL,
    custom_location GEOMETRY(Point, 4326),
    radius_meters INT NOT NULL DEFAULT 150,
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);

-- 5. Task Items Table
CREATE TABLE IF NOT EXISTS task_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    list_id UUID NOT NULL REFERENCES task_lists(id) ON DELETE CASCADE,
    title TEXT NOT NULL,
    is_completed BOOLEAN NOT NULL DEFAULT FALSE,
    quantity TEXT NOT NULL DEFAULT '1'
);

-- 6. Store Stories Table (24h Merchant Media & Shorts)
CREATE TABLE IF NOT EXISTS store_stories (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    store_id UUID NOT NULL REFERENCES stores(id) ON DELETE CASCADE,
    title TEXT NOT NULL,
    description TEXT NOT NULL DEFAULT '',
    media_url TEXT NOT NULL,
    thumbnail_url TEXT,
    promo_badge TEXT NOT NULL DEFAULT '',
    view_count INT NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at TIMESTAMPTZ NOT NULL
);

-- ==============================================================================
-- SPATIAL (GIST) & RELATIONAL INDEXES
-- ==============================================================================

-- Primary GIST spatial index on store locations
CREATE INDEX IF NOT EXISTS idx_stores_location ON stores USING GIST(location);

-- GIST spatial index on custom task list locations
CREATE INDEX IF NOT EXISTS idx_task_lists_custom_location ON task_lists USING GIST(custom_location);

-- Relational lookups
CREATE INDEX IF NOT EXISTS idx_stores_category_id ON stores(category_id);
CREATE INDEX IF NOT EXISTS idx_store_reviews_store_id ON store_reviews(store_id);
CREATE INDEX IF NOT EXISTS idx_task_lists_user_id ON task_lists(user_id);
CREATE INDEX IF NOT EXISTS idx_task_items_list_id ON task_items(list_id);
CREATE INDEX IF NOT EXISTS idx_store_stories_store_expires ON store_stories(store_id, expires_at);

-- ==============================================================================
-- SEED FIXTURES (Standard test anchors for hyper-local spatial validation)
-- Reference Anchor: NYC Downtown Civic Center (Lat: 40.7128, Lng: -74.0060)
-- Point convention: ST_SetSRID(ST_MakePoint(longitude, latitude), 4326)
-- ==============================================================================

DO $$
DECLARE
    cat_grocery UUID := '11111111-1111-1111-1111-111111111111';
    cat_coffee  UUID := '22222222-2222-2222-2222-222222222222';
    cat_tech    UUID := '33333333-3333-3333-3333-333333333333';
    cat_fitness UUID := '44444444-4444-4444-4444-444444444444';

    store_grocery_id UUID := 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
    store_coffee_id  UUID := 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';
    store_tech_id    UUID := 'cccccccc-cccc-cccc-cccc-cccccccccccc';
    store_far_id     UUID := 'dddddddd-dddd-dddd-dddd-dddddddddddd';

    list_grocery_id  UUID := 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee';
    test_user_id     UUID := '99999999-9999-9999-9999-999999999999';
BEGIN
    -- Insert Categories
    INSERT INTO categories (id, name, slug, icon) VALUES
        (cat_grocery, 'Groceries & Fresh Food', 'groceries', 'basket-fill'),
        (cat_coffee,  'Artisan Coffee & Bakery', 'coffee', 'cup-hot-fill'),
        (cat_tech,    'Electronics & Gadgets',   'tech', 'laptop-fill'),
        (cat_fitness, 'Fitness & Wellness',      'fitness', 'heart-pulse-fill')
    ON CONFLICT (slug) DO NOTHING;

    -- Store 1: ~250m from center (INSIDE 1.5km)
    INSERT INTO stores (id, name, category_id, location, radius_meters, address, is_partner) VALUES
        (store_grocery_id, 'Green Grocer Organic Market', cat_grocery, 
         ST_SetSRID(ST_MakePoint(-74.0080, 40.7145), 4326), 150, '124 Chambers St, New York, NY', TRUE)
    ON CONFLICT (id) DO NOTHING;

    -- Store 2: ~650m from center (INSIDE 1.5km)
    INSERT INTO stores (id, name, category_id, location, radius_meters, address, is_partner) VALUES
        (store_coffee_id, 'Cortado Artisan Coffee Roasters', cat_coffee, 
         ST_SetSRID(ST_MakePoint(-74.0020, 40.7180), 4326), 100, '45 Franklin St, New York, NY', TRUE)
    ON CONFLICT (id) DO NOTHING;

    -- Store 3: ~1250m from center (INSIDE 1.5km)
    INSERT INTO stores (id, name, category_id, location, radius_meters, address, is_partner) VALUES
        (store_tech_id, 'Pixel & Wire Electronics', cat_tech, 
         ST_SetSRID(ST_MakePoint(-73.9980, 40.7220), 4326), 200, '580 Broadway, New York, NY', FALSE)
    ON CONFLICT (id) DO NOTHING;

    -- Store 4: ~2600m from center (STRICTLY OUTSIDE 1.5km boundary)
    INSERT INTO stores (id, name, category_id, location, radius_meters, address, is_partner) VALUES
        (store_far_id, 'Hudson River Fitness & Spa', cat_fitness, 
         ST_SetSRID(ST_MakePoint(-74.0120, 40.7350), 4326), 250, '354 West St, New York, NY', TRUE)
    ON CONFLICT (id) DO NOTHING;

    -- Seed Reviews for Store 1
    INSERT INTO store_reviews (store_id, user_id, rating_service, rating_quality, rating_overall, comment) VALUES
        (store_grocery_id, test_user_id, 5, 5, 5, 'Super fresh organic avocados and stellar curbside pickup service!'),
        (store_grocery_id, test_user_id, 4, 5, 5, 'Great selection of sourdough bread and local cheeses.')
    ON CONFLICT DO NOTHING;

    -- Seed Task List
    INSERT INTO task_lists (id, user_id, title, category_id, custom_location, radius_meters, is_active) VALUES
        (list_grocery_id, test_user_id, 'Weekend Farmers Haul', cat_grocery,
         ST_SetSRID(ST_MakePoint(-74.0080, 40.7145), 4326), 150, TRUE)
    ON CONFLICT (id) DO NOTHING;

    -- Seed Task Items
    INSERT INTO task_items (list_id, title, is_completed, quantity) VALUES
        (list_grocery_id, 'Organic Hass Avocados', FALSE, '4 pcs'),
        (list_grocery_id, 'Almond Milk (Unsweetened)', TRUE, '2 cartons'),
        (list_grocery_id, 'Artisan Sourdough Loaf', FALSE, '1 loaf')
    ON CONFLICT DO NOTHING;

    -- Seed Merchant Stories
    INSERT INTO store_stories (id, store_id, title, description, media_url, thumbnail_url, promo_badge, expires_at) VALUES
        ('12121212-1212-1212-1212-121212121212', store_coffee_id, 
         'Fresh Single-Origin Ethiopian Roast Just Dropped! ☕',
         'Flash Happy Hour: Enjoy 20% off all pour-overs when you walk in before 11 AM.',
         'https://assets.spot.dev/media/cortado_ethiopian.mp4', 'https://assets.spot.dev/media/cortado_thumb.jpg',
         '20% OFF IN-STORE', NOW() + INTERVAL '12 hours'),
        ('34343434-3434-3434-3434-343434343434', store_grocery_id,
         'Local Farm-to-Table Hass Avocados & Sourdough 🥑',
         'Geofenced exclusive: Scan your Spot app at checkout for a complimentary cold-pressed juice with orders over $25.',
         'https://assets.spot.dev/media/grocer_avocado.mp4', 'https://assets.spot.dev/media/grocer_thumb.jpg',
         'FREE GIFT WITH PURCHASE', NOW() + INTERVAL '8 hours')
    ON CONFLICT (id) DO NOTHING;

END $$;
