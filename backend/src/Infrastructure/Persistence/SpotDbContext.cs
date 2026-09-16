using Microsoft.EntityFrameworkCore;
using Spot.Domain.Entities;

namespace Spot.Infrastructure.Persistence;

public class SpotDbContext : DbContext
{
    public SpotDbContext(DbContextOptions<SpotDbContext> options) : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<StoreReview> StoreReviews => Set<StoreReview>();
    public DbSet<TaskList> TaskLists => Set<TaskList>();
    public DbSet<TaskItem> TaskItems => Set<TaskItem>();
    public DbSet<StoreStory> StoreStories => Set<StoreStory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Enable PostGIS extension in PostgreSQL
        modelBuilder.HasPostgresExtension("postgis");
        modelBuilder.HasPostgresExtension("uuid-ossp");

        // Categories Configuration
        modelBuilder.Entity<Category>(b =>
        {
            b.ToTable("categories");
            b.HasKey(c => c.Id);
            b.Property(c => c.Id).HasColumnName("id");
            b.Property(c => c.Name).HasColumnName("name").IsRequired();
            b.Property(c => c.Slug).HasColumnName("slug").IsRequired();
            b.Property(c => c.Icon).HasColumnName("icon").IsRequired();
            b.HasIndex(c => c.Slug).IsUnique();
        });

        // Stores Configuration (Spatial Point geometry & GIST index)
        modelBuilder.Entity<Store>(b =>
        {
            b.ToTable("stores");
            b.HasKey(s => s.Id);
            b.Property(s => s.Id).HasColumnName("id");
            b.Property(s => s.Name).HasColumnName("name").IsRequired();
            b.Property(s => s.CategoryId).HasColumnName("category_id").IsRequired();
            b.Property(s => s.Location)
                .HasColumnName("location")
                .HasColumnType("geometry(Point, 4326)")
                .IsRequired();
            b.Property(s => s.RadiusMeters).HasColumnName("radius_meters").HasDefaultValue(100);
            b.Property(s => s.Address).HasColumnName("address").IsRequired();
            b.Property(s => s.IsPartner).HasColumnName("is_partner").HasDefaultValue(false);

            // Spatial GIST index on location
            b.HasIndex(s => s.Location)
                .HasDatabaseName("idx_stores_location")
                .HasMethod("GIST");

            b.HasOne(s => s.Category)
                .WithMany(c => c.Stores)
                .HasForeignKey(s => s.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Store Reviews Configuration
        modelBuilder.Entity<StoreReview>(b =>
        {
            b.ToTable("store_reviews");
            b.HasKey(r => r.Id);
            b.Property(r => r.Id).HasColumnName("id");
            b.Property(r => r.StoreId).HasColumnName("store_id").IsRequired();
            b.Property(r => r.UserId).HasColumnName("user_id").IsRequired();
            b.Property(r => r.RatingService).HasColumnName("rating_service").IsRequired();
            b.Property(r => r.RatingQuality).HasColumnName("rating_quality").IsRequired();
            b.Property(r => r.RatingAtmosphere).HasColumnName("rating_atmosphere").HasDefaultValue(5);
            b.Property(r => r.RatingOverall).HasColumnName("rating_overall").IsRequired();
            b.Property(r => r.Comment).HasColumnName("comment").HasDefaultValue("");
            b.Property(r => r.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            b.Property(r => r.IsFlaggedAsSpam).HasColumnName("is_flagged_as_spam").HasDefaultValue(false);
            b.Property(r => r.SpamConfidence).HasColumnName("spam_confidence").HasDefaultValue(0.0);
            b.Property(r => r.SpamReason).HasColumnName("spam_reason");
            b.Property(r => r.SentimentService).HasColumnName("sentiment_service").HasDefaultValue("Neutral");
            b.Property(r => r.SentimentQuality).HasColumnName("sentiment_quality").HasDefaultValue("Neutral");
            b.Property(r => r.SentimentAtmosphere).HasColumnName("sentiment_atmosphere").HasDefaultValue("Neutral");
            b.Property(r => r.AuthorIpAddress).HasColumnName("author_ip_address");

            b.HasIndex(r => r.IsFlaggedAsSpam);

            b.HasOne(r => r.Store)
                .WithMany(s => s.Reviews)
                .HasForeignKey(r => r.StoreId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Task Lists Configuration (Geofenced with optional custom location)
        modelBuilder.Entity<TaskList>(b =>
        {
            b.ToTable("task_lists");
            b.HasKey(t => t.Id);
            b.Property(t => t.Id).HasColumnName("id");
            b.Property(t => t.UserId).HasColumnName("user_id").IsRequired();
            b.Property(t => t.Title).HasColumnName("title").IsRequired();
            b.Property(t => t.CategoryId).HasColumnName("category_id");
            b.Property(t => t.CustomLocation)
                .HasColumnName("custom_location")
                .HasColumnType("geometry(Point, 4326)");
            b.Property(t => t.RadiusMeters).HasColumnName("radius_meters").HasDefaultValue(150);
            b.Property(t => t.IsActive).HasColumnName("is_active").HasDefaultValue(true);

            // Spatial GIST index on custom_location
            b.HasIndex(t => t.CustomLocation)
                .HasDatabaseName("idx_task_lists_custom_location")
                .HasMethod("GIST");

            b.HasOne(t => t.Category)
                .WithMany(c => c.TaskLists)
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Task Items Configuration
        modelBuilder.Entity<TaskItem>(b =>
        {
            b.ToTable("task_items");
            b.HasKey(i => i.Id);
            b.Property(i => i.Id).HasColumnName("id");
            b.Property(i => i.ListId).HasColumnName("list_id").IsRequired();
            b.Property(i => i.Title).HasColumnName("title").IsRequired();
            b.Property(i => i.IsCompleted).HasColumnName("is_completed").HasDefaultValue(false);
            b.Property(i => i.Quantity).HasColumnName("quantity").HasDefaultValue("1");

            b.HasOne(i => i.TaskList)
                .WithMany(t => t.Items)
                .HasForeignKey(i => i.ListId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Store Stories Configuration (24h Merchant Media & Shorts)
        modelBuilder.Entity<StoreStory>(b =>
        {
            b.ToTable("store_stories");
            b.HasKey(s => s.Id);
            b.Property(s => s.Id).HasColumnName("id");
            b.Property(s => s.StoreId).HasColumnName("store_id").IsRequired();
            b.Property(s => s.Title).HasColumnName("title").IsRequired();
            b.Property(s => s.Description).HasColumnName("description").HasDefaultValue("");
            b.Property(s => s.MediaUrl).HasColumnName("media_url").IsRequired();
            b.Property(s => s.ThumbnailUrl).HasColumnName("thumbnail_url");
            b.Property(s => s.PromoBadge).HasColumnName("promo_badge").HasDefaultValue("");
            b.Property(s => s.ViewCount).HasColumnName("view_count").HasDefaultValue(0);
            b.Property(s => s.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            b.Property(s => s.ExpiresAt).HasColumnName("expires_at").IsRequired();

            b.HasIndex(s => new { s.StoreId, s.ExpiresAt });

            b.HasOne(s => s.Store)
                .WithMany(st => st.Stories)
                .HasForeignKey(s => s.StoreId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
