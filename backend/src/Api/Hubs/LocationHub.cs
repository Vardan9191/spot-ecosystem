using Microsoft.AspNetCore.SignalR;
using Spot.Api.DTOs;
using Spot.Domain.Entities;
using Spot.Infrastructure.Repositories;

namespace Spot.Api.Hubs;

public class LocationHub : Hub<ILocationClient>
{
    private readonly IStoreRepository _storeRepository;
    private readonly ILogger<LocationHub> _logger;

    public LocationHub(IStoreRepository storeRepository, ILogger<LocationHub> logger)
    {
        _storeRepository = storeRepository;
        _logger = logger;
    }

    /// <summary>
    /// Receives real-time GPS coordinates from a connected mobile client, evaluates proximity,
    /// pushes nearby stores, and triggers instant geofence alerts if within a store's boundary radius.
    /// </summary>
    public async Task SendLocationUpdate(double latitude, double longitude, double searchRadiusMeters = 1500)
    {
        string connectionId = Context?.ConnectionId ?? "unknown";

        if (latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180)
        {
            _logger.LogWarning("Invalid GPS coordinates received from connection {ConnectionId}: ({Lat}, {Lng})",
                connectionId, latitude, longitude);
            return;
        }

        if (searchRadiusMeters <= 0 || searchRadiusMeters > 50000)
        {
            searchRadiusMeters = 1500;
        }

        _logger.LogInformation("Processing real-time location update for {ConnectionId} at ({Lat}, {Lng})",
            connectionId, latitude, longitude);

        var nearbyResults = await _storeRepository.GetNearbyStoresAsync(latitude, longitude, searchRadiusMeters);

        var storeDtos = nearbyResults.Select(r => MapToDto(r.Store, r.DistanceMeters)).ToList();

        // 1. Send all nearby stores to the caller
        await Clients.Caller.ReceiveNearbyStores(storeDtos);

        // 2. Evaluate active geofence boundary hits (ENTER trigger)
        foreach (var result in nearbyResults)
        {
            if (result.DistanceMeters <= result.Store.RadiusMeters)
            {
                var alert = new GeofenceAlertDto(
                    StoreId: result.Store.Id,
                    StoreName: result.Store.Name,
                    CategoryName: result.Store.Category.Name,
                    DistanceMeters: result.DistanceMeters,
                    StoreRadiusMeters: result.Store.RadiusMeters,
                    AlertMessage: $"You are inside the {result.Store.RadiusMeters}m geofence for {result.Store.Name}!",
                    IsPartner: result.Store.IsPartner,
                    Timestamp: DateTimeOffset.UtcNow
                );

                _logger.LogInformation("Geofence triggered for store {StoreName} (Dist: {Distance}m <= Radius {Radius}m)",
                    result.Store.Name, result.DistanceMeters, result.Store.RadiusMeters);

                await Clients.Caller.ReceiveGeofenceTrigger(alert);
            }
        }

        // 3. Acknowledge update
        await Clients.Caller.LocationUpdatedAcknowledged(latitude, longitude, storeDtos.Count);
    }

    /// <summary>
    /// Subscribes the client to hyper-local district/zone broadcasts (e.g. "manhattan_downtown").
    /// </summary>
    public async Task JoinZoneGroup(string zoneName)
    {
        if (!string.IsNullOrWhiteSpace(zoneName))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, zoneName.ToLowerInvariant());
            _logger.LogInformation("Connection {ConnectionId} joined zone group {Zone}", Context.ConnectionId, zoneName);
        }
    }

    /// <summary>
    /// Unsubscribes client from zone broadcasts.
    /// </summary>
    public async Task LeaveZoneGroup(string zoneName)
    {
        if (!string.IsNullOrWhiteSpace(zoneName))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, zoneName.ToLowerInvariant());
            _logger.LogInformation("Connection {ConnectionId} left zone group {Zone}", Context.ConnectionId, zoneName);
        }
    }

    /// <summary>
    /// Broadcasts a merchant flash promotion to all connected clients in a specific zone.
    /// </summary>
    public async Task BroadcastMerchantDeal(string zoneName, string storeName, string dealTitle)
    {
        if (!string.IsNullOrWhiteSpace(zoneName))
        {
            await Clients.Group(zoneName.ToLowerInvariant())
                .ReceiveBroadcastNotification($"🔥 Flash Deal: {storeName}", dealTitle);
        }
    }

    private static StoreDto MapToDto(Store store, double? distance)
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
