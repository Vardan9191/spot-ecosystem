using Spot.Api.DTOs;

namespace Spot.Api.Hubs;

public interface ILocationClient
{
    Task ReceiveNearbyStores(List<StoreDto> stores);
    Task ReceiveGeofenceTrigger(GeofenceAlertDto alert);
    Task ReceiveBroadcastNotification(string title, string message);
    Task LocationUpdatedAcknowledged(double latitude, double longitude, int storesFound);
}
