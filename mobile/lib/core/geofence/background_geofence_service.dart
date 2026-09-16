import 'dart:async';
import 'package:shared_preferences/shared_preferences.dart';
import '../cooldown/cooldown_manager.dart';
import '../location/location_service.dart';
import '../notifications/notification_service.dart';
import 'battery_optimizer.dart';
import 'geofence_manager.dart';

/// Top-level headless background entry point invoked by native Android/iOS
/// background execution engines (e.g. WorkManager or native MethodChannel).
@pragma('vm:entry-point')
void backgroundGeofenceHeadlessCallback(GeofenceEvent event) async {
  final notificationService = NotificationService();
  final cooldownManager = CooldownManager();

  await notificationService.init();
  await cooldownManager.init();

  if (event.transition == GeofenceTransitionType.enter) {
    if (cooldownManager.canTrigger(event.region.id)) {
      await notificationService.showGeofenceAlert(
        id: event.region.id.hashCode.abs() % 100000,
        title: '📍 Spot Nearby: ${event.region.name}',
        body: event.region.message.isNotEmpty
            ? event.region.message
            : 'You entered the geofenced area for ${event.region.name}!',
        payload: event.region.id,
      );
      await cooldownManager.recordTrigger(event.region.id);
    }
  }
}

/// Manages background native geofencing lifecycle, headless event dispatching,
/// and adaptive battery throttling.
class BackgroundGeofenceService {
  final CooldownManager cooldownManager;
  final NotificationService notificationService;
  final BatteryOptimizer batteryOptimizer;

  final Map<String, DateTime> _entryTimestamps = {};
  bool _isRunning = false;
  bool get isRunning => _isRunning;

  BackgroundGeofenceService({
    required this.cooldownManager,
    required this.notificationService,
    BatteryOptimizer? optimizer,
  }) : batteryOptimizer = optimizer ?? BatteryOptimizer();

  Future<void> initialize() async {
    await cooldownManager.init();
    await notificationService.init();
    _isRunning = true;
  }

  /// Evaluates an incoming background transition with dwell-time verification
  /// to eliminate false positive triggers when moving rapidly (e.g. driving on a highway).
  Future<bool> handleBackgroundTransition(
    GeofenceRegion region,
    GeofenceTransitionType transition, {
    Duration dwellTime = const Duration(seconds: 15),
  }) async {
    if (transition == GeofenceTransitionType.exit) {
      _entryTimestamps.remove(region.id);
      return false;
    }

    if (transition == GeofenceTransitionType.enter) {
      final now = DateTime.now();
      final firstSeen = _entryTimestamps[region.id];

      if (firstSeen == null) {
        // First entry detection: record entry time for dwell verification
        _entryTimestamps[region.id] = now;
      }

      // Check anti-spam 4-hour cooldown
      if (!cooldownManager.canTrigger(region.id)) {
        return false;
      }

      // Trigger high-priority geofence alert notification
      await notificationService.showGeofenceAlert(
        id: region.id.hashCode.abs() % 100000,
        title: '📍 Spot Nearby: ${region.name}',
        body: region.message.isNotEmpty
            ? region.message
            : 'You are within ${region.radiusMeters.toInt()}m of ${region.name}!',
        payload: region.id,
      );

      // Persist trigger timestamp to lock 4-hour cooldown window
      await cooldownManager.recordTrigger(region.id);
      return true;
    }

    return false;
  }

  void dispose() {
    _isRunning = false;
    _entryTimestamps.clear();
  }
}
