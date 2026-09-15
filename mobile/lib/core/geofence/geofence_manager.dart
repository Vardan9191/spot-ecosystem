import 'dart:async';
import '../cooldown/cooldown_manager.dart';
import '../location/location_service.dart';
import '../notifications/notification_service.dart';

enum GeofenceState { unknown, inside, outside }
enum GeofenceTransitionType { enter, exit }

class GeofenceRegion {
  final String id;
  final String name;
  final double latitude;
  final double longitude;
  final double radiusMeters;
  final String message;
  final String category;
  final Map<String, dynamic>? metadata;

  GeofenceRegion({
    required this.id,
    required this.name,
    required this.latitude,
    required this.longitude,
    required this.radiusMeters,
    required this.message,
    this.category = 'general',
    this.metadata,
  });
}

class GeofenceEvent {
  final GeofenceRegion region;
  final GeofenceTransitionType transition;
  final double distanceMeters;
  final DateTime timestamp;

  GeofenceEvent({
    required this.region,
    required this.transition,
    required this.distanceMeters,
    required this.timestamp,
  });
}

/// GeofenceManager handles native circular boundary detection with ENTER / EXIT transitions.
/// Coordinates with CooldownManager to strictly enforce the 4-hour anti-spam window per entity.
class GeofenceManager {
  final LocationService _locationService;
  final CooldownManager _cooldownManager;
  final NotificationService _notificationService;

  final Map<String, GeofenceRegion> _activeRegions = {};
  final Map<String, GeofenceState> _regionStates = {};
  StreamSubscription<UserCoordinates>? _locationSub;

  final _eventController = StreamController<GeofenceEvent>.broadcast();
  Stream<GeofenceEvent> get transitionEvents => _eventController.stream;

  GeofenceManager({
    required LocationService locationService,
    required CooldownManager cooldownManager,
    required NotificationService notificationService,
  })  : _locationService = locationService,
        _cooldownManager = cooldownManager,
        _notificationService = notificationService;

  List<GeofenceRegion> get activeRegions => _activeRegions.values.toList();

  void registerRegion(GeofenceRegion region) {
    _activeRegions[region.id] = region;
    _regionStates[region.id] ??= GeofenceState.unknown;
  }

  void registerRegions(List<GeofenceRegion> regions) {
    for (final r in regions) {
      registerRegion(r);
    }
  }

  void removeRegion(String id) {
    _activeRegions.remove(id);
    _regionStates.remove(id);
  }

  void clearAllRegions() {
    _activeRegions.clear();
    _regionStates.clear();
  }

  /// Begins listening to location updates and evaluating geofence boundaries.
  void startMonitoring() {
    _locationSub?.cancel();
    _locationSub = _locationService.coordinatesStream.listen((coords) {
      _evaluateGeofences(coords);
    });
  }

  void stopMonitoring() {
    _locationSub?.cancel();
    _locationSub = null;
  }

  /// Pure evaluation logic: can be directly tested with synthetic coordinates.
  void _evaluateGeofences(UserCoordinates coords) {
    for (final entry in _activeRegions.entries) {
      final region = entry.value;
      final currentState = _regionStates[region.id] ?? GeofenceState.unknown;

      final distance = LocationService.distanceBetweenMeters(
        coords.latitude,
        coords.longitude,
        region.latitude,
        region.longitude,
      );

      final isInside = distance <= region.radiusMeters;

      if (isInside) {
        if (currentState != GeofenceState.inside) {
          // ENTER transition
          _regionStates[region.id] = GeofenceState.inside;
          final event = GeofenceEvent(
            region: region,
            transition: GeofenceTransitionType.enter,
            distanceMeters: distance,
            timestamp: coords.timestamp,
          );
          _eventController.add(event);

          _handleGeofenceEnter(region, distance);
        }
      } else {
        if (currentState == GeofenceState.inside) {
          // EXIT transition
          _regionStates[region.id] = GeofenceState.outside;
          final event = GeofenceEvent(
            region: region,
            transition: GeofenceTransitionType.exit,
            distanceMeters: distance,
            timestamp: coords.timestamp,
          );
          _eventController.add(event);
        } else if (currentState == GeofenceState.unknown) {
          _regionStates[region.id] = GeofenceState.outside;
        }
      }
    }
  }

  Future<void> _handleGeofenceEnter(GeofenceRegion region, double distance) async {
    // Check 4-hour anti-spam cooldown
    if (!_cooldownManager.canTrigger(region.id)) {
      // Cooldown active, suppress push notification to save user attention
      return;
    }

    // Trigger local push notification
    final notificationId = region.id.hashCode.abs() % 100000;
    await _notificationService.showGeofenceAlert(
      id: notificationId,
      title: '📍 Spot Nearby: ${region.name}',
      body: region.message.isNotEmpty 
          ? region.message 
          : 'You are within ${region.radiusMeters.toInt()}m of ${region.name}. Check your task list!',
      payload: region.id,
    );

    // Record trigger timestamp to activate 4-hour cooldown
    await _cooldownManager.recordTrigger(region.id);
  }

  GeofenceState getRegionState(String regionId) {
    return _regionStates[regionId] ?? GeofenceState.unknown;
  }

  void dispose() {
    stopMonitoring();
    _eventController.close();
  }
}
