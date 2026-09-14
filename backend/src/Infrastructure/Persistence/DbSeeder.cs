using Microsoft.EntityFrameworkCore;
using Spot.Domain.Common;
using Spot.Domain.Entities;

namespace Spot.Infrastructure.Persistence;

public static class DbSeeder
{
    public static readonly Guid CatGroceryId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid CatCoffeeId  = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid CatTechId    = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid CatFitnessId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    public static readonly Guid StoreGroceryId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid StoreCoffeeId  = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public static readonly Guid StoreTechId    = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    public static readonly Guid StoreFarId     = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    public static readonly Guid TestUserId = Guid.Parse("99999999-9999-9999-9999-999999999999");
    public static readonly Guid TestListId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

    public static async Task SeedAsync(SpotDbContext context)
    {
        if (await context.Categories.AnyAsync())
        {
            return; // Already seeded
        }

        var catGrocery = new Category { Id = CatGroceryId, Name = "Groceries & Fresh Food", Slug = "groceries", Icon = "basket-fill" };
        var catCoffee  = new Category { Id = CatCoffeeId,  Name = "Artisan Coffee & Bakery", Slug = "coffee",    Icon = "cup-hot-fill" };
        var catTech    = new Category { Id = CatTechId,    Name = "Electronics & Gadgets",   Slug = "tech",      Icon = "laptop-fill" };
        var catFitness = new Category { Id = CatFitnessId, Name = "Fitness & Wellness",      Slug = "fitness",   Icon = "heart-pulse-fill" };

        await context.Categories.AddRangeAsync(catGrocery, catCoffee, catTech, catFitness);

        // Store 1: ~250m from center (INSIDE 1.5km)
        var storeGrocery = new Store
        {
            Id = StoreGroceryId,
            Name = "Green Grocer Organic Market",
            CategoryId = CatGroceryId,
            Location = GeoUtils.CreatePoint(40.7145, -74.0080),
            RadiusMeters = 150,
            Address = "124 Chambers St, New York, NY",
            IsPartner = true
        };

        // Store 2: ~650m from center (INSIDE 1.5km)
        var storeCoffee = new Store
        {
            Id = StoreCoffeeId,
            Name = "Cortado Artisan Coffee Roasters",
            CategoryId = CatCoffeeId,
            Location = GeoUtils.CreatePoint(40.7180, -74.0020),
            RadiusMeters = 100,
            Address = "45 Franklin St, New York, NY",
            IsPartner = true
        };

        // Store 3: ~1250m from center (INSIDE 1.5km)
        var storeTech = new Store
        {
            Id = StoreTechId,
            Name = "Pixel & Wire Electronics",
            CategoryId = CatTechId,
            Location = GeoUtils.CreatePoint(40.7220, -73.9980),
            RadiusMeters = 200,
            Address = "580 Broadway, New York, NY",
            IsPartner = false
        };

        // Store 4: ~2600m from center (STRICTLY OUTSIDE 1.5km boundary)
        var storeFar = new Store
        {
            Id = StoreFarId,
            Name = "Hudson River Fitness & Spa",
            CategoryId = CatFitnessId,
            Location = GeoUtils.CreatePoint(40.7350, -74.0120),
            RadiusMeters = 250,
            Address = "354 West St, New York, NY",
            IsPartner = true
        };

        await context.Stores.AddRangeAsync(storeGrocery, storeCoffee, storeTech, storeFar);

        // Reviews
        var review1 = new StoreReview
        {
            StoreId = StoreGroceryId,
            UserId = TestUserId,
            RatingService = 5,
            RatingQuality = 5,
            RatingOverall = 5,
            Comment = "Super fresh organic avocados and stellar curbside pickup service!",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-2)
        };
        var review2 = new StoreReview
        {
            StoreId = StoreGroceryId,
            UserId = TestUserId,
            RatingService = 4,
            RatingQuality = 5,
            RatingOverall = 5,
            Comment = "Great selection of sourdough bread and local cheeses.",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        await context.StoreReviews.AddRangeAsync(review1, review2);

        // Task List & Items
        var taskList = new TaskList
        {
            Id = TestListId,
            UserId = TestUserId,
            Title = "Weekend Farmers Haul",
            CategoryId = CatGroceryId,
            CustomLocation = GeoUtils.CreatePoint(40.7145, -74.0080),
            RadiusMeters = 150,
            IsActive = true
        };

        taskList.Items.Add(new TaskItem { Title = "Organic Hass Avocados", IsCompleted = false, Quantity = "4 pcs" });
        taskList.Items.Add(new TaskItem { Title = "Almond Milk (Unsweetened)", IsCompleted = true, Quantity = "2 cartons" });
        taskList.Items.Add(new TaskItem { Title = "Artisan Sourdough Loaf", IsCompleted = false, Quantity = "1 loaf" });

        await context.TaskLists.AddAsync(taskList);
        await context.SaveChangesAsync();
    }
}
