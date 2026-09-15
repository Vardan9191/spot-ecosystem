using Microsoft.AspNetCore.SignalR;
using Spot.Api.DTOs;
using Spot.Domain.Entities;
using Spot.Infrastructure.Repositories;

namespace Spot.Api.Hubs;

public class LocationHub : Hub
{
    private readonly IStoreRepository _storeRepository;
    private readonly ILogger<LocationHub> _logger;

    public LocationHub(IStoreRepository storeRepository, ILogger<LocationHub> logger)
    {
        _storeRepository = storeRepository;
        _logger = logger;
    }

    /// <summary>
    /// Receives real-time coordinates from client, performs spatial evaluation,
    /// and streams nearby stores and active geofence alerts back to caller.
    /// </summary>
    public async Task SendLocation(double latitude, double longitude, double radiusMeters = 1500, string? category = null)
    {
        if (latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180)
        {
            await Clients.Caller.SendAsync("ReceiveError", "Invalid coordinates: latitude [-90, 90], longitude [-180, 180].");
            return;
        }

        if (radiusMeters <= 0 || radiusMeters > 100000)
        {
            await Clients.Caller.SendAsync("ReceiveError", "Invalid radius: must be between 1 and 100,000 meters.");
            return;
        }

        var results = await _storeRepository.GetNearbyStoresAsync(latitude, longitude, radiusMeters, category);
        var dtos = results.Select(r => MapToDto(r.Store, r.DistanceMeters)).ToList();

        // Stream nearby stores list sorted by distance
        await Clients.Caller.SendAsync("ReceiveNearbyStores", dtos);

        // Check if inside any store's immediate geofence boundary (e.g., within store.RadiusMeters)
        foreach (var result in results)
        {
            if (result.DistanceMeters <= result.Store.RadiusMeters)
            {
                var alert = new GeofenceAlertNotificationDto(
                    StoreId: result.Store.Id,
                    StoreName: result.Store.Name,
                    Category: result.Store.Category?.Name ?? "General",
                    DistanceMeters: result.DistanceMeters,
                    RadiusMeters: result.Store.RadiusMeters,
                    Message: $"Welcome! You are inside {result.Store.Name}'s {result.Store.RadiusMeters}m geofence."
                );

                await Clients.Caller.SendAsync("ReceiveGeofenceAlert", alert);
            }
        }
    }

    /// <summary>
    /// Subscribes the client connection to a specific store's real-time deal zone.
    /// </summary>
    public async Task JoinStoreZone(string storeId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"store_{storeId}");
        _logger.LogInformation("Connection {ConnectionId} joined zone {StoreId}", Context.ConnectionId, storeId);
    }

    /// <summary>
    /// Unsubscribes client connection from store's real-time deal zone.
    /// </summary>
    public async Task LeaveStoreZone(string storeId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"store_{storeId}");
        _logger.LogInformation("Connection {ConnectionId} left zone {StoreId}", Context.ConnectionId, storeId);
    }

    /// <summary>
    /// Merchant broadcast: sends real-time flash deal to all users connected to the store zone.
    /// </summary>
    public async Task BroadcastStoreDeal(string storeId, string dealTitle, string dealMessage)
    {
        if (Guid.TryParse(storeId, out var parsedId))
        {
            var deal = new StoreDealDto(parsedId, dealTitle, dealMessage, DateTimeOffset.UtcNow);
            await Clients.Group($"store_{storeId}").SendAsync("ReceiveStoreDeal", deal);
        }
    }

    private static StoreDto MapToDto(Store store, double distance)
    {
        double avgRating = store.Reviews.Count != 0 
            ? Math.Round(store.Reviews.Average(r => r.RatingOverall), 1) 
            : 0.0;

        return new StoreDto(
            Id: store.Id,
            Name: store.Name,
            Category: new CategoryDto(
                store.Category.Id,
                store.Category.Name,
                store.Category.Slug,
                store.Category.Icon
            ),
            Latitude: store.Location.Y,
            Longitude: store.Location.X,
            RadiusMeters: store.RadiusMeters,
            Address: store.Address,
            IsPartner: store.IsPartner,
            DistanceMeters: distance,
            AverageRating: avgRating,
            ReviewCount: store.Reviews.Count
        );
    }
}
