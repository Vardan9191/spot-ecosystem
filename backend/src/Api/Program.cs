using Microsoft.EntityFrameworkCore;
using Spot.Infrastructure.Persistence;
using Spot.Infrastructure.Repositories;
using Spot.Infrastructure.Storage;
using Spot.Infrastructure.Cache;
using Spot.Api.Hubs;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// Configure EF Core with Npgsql & NetTopologySuite
var connectionString = builder.Configuration.GetConnectionString("SpotDb");
builder.Services.AddDbContext<SpotDbContext>(options =>
{
    if (!string.IsNullOrEmpty(connectionString) && !builder.Environment.IsEnvironment("Testing"))
    {
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.UseNetTopologySuite();
            npgsqlOptions.MigrationsAssembly(typeof(SpotDbContext).Assembly.FullName);
        });
    }
    else
    {
        // Fallback for isolated unit tests / environments
        options.UseInMemoryDatabase("SpotTestingDb");
    }
});

// Dependency Injection for Repositories & Services
builder.Services.AddSingleton<ISpatialCacheService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<RedisSpatialCacheService>>();
    var redisConn = builder.Configuration.GetConnectionString("Redis");
    IConnectionMultiplexer? redis = null;
    if (!string.IsNullOrWhiteSpace(redisConn))
    {
        try
        {
            var config = ConfigurationOptions.Parse(redisConn);
            config.AbortOnConnectFail = false;
            config.ConnectTimeout = 1500;
            redis = ConnectionMultiplexer.Connect(config);
        }
        catch
        {
            redis = null;
        }
    }
    return new RedisSpatialCacheService(redis, logger);
});
builder.Services.AddScoped<IStoreRepository, StoreRepository>();
builder.Services.AddScoped<ITaskListRepository, TaskListRepository>();
builder.Services.AddScoped<IStoreReviewRepository, StoreReviewRepository>();
builder.Services.AddScoped<IStoreStoryRepository, StoreStoryRepository>();
builder.Services.AddSingleton<IMediaStorageService, LocalStorageService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() 
    { 
        Title = "Spot Geofenced Ecosystem API", 
        Version = "v1",
        Description = "Hyper-local geofenced shopping and merchant media ecosystem API using PostGIS and NetTopologySuite."
    });
});

// Add SignalR for real-time spatial streaming
builder.Services.AddSignalR();

// Enable CORS for mobile & web clients
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Spot API v1"));
}

app.UseCors();
app.UseStaticFiles();
app.UseAuthorization();
app.MapControllers();
app.MapHub<LocationHub>("/hubs/location");

// Health Check Endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow }));

app.Run();

// Required for WebApplicationFactory in integration tests
public partial class Program { }
