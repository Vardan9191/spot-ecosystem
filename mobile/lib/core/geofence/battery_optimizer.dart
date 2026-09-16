import 'dart:math';
import '../location/location_service.dart';

enum PowerMode {
  /// User is far from all known geofences (> 2.5km). GPS updates throttled to 100m+ filter.
  dormant,
  /// User is approaching geofence cluster (500m - 2.5km). Balanced 25m filter.
  proximity,
  /// User is near or inside boundary (< 500m). High accuracy with 10m filter.
  entryDetection,
}

/// Dynamic battery optimization manager that adjusts location polling intervals
/// based on the user's distance to the closest active geofence cluster.
class BatteryOptimizer {
  static const double dormantThresholdMeters = 2500.0;
  static const double proximityThresholdMeters = 500.0;

  PowerMode _currentMode = PowerMode.dormant;
  PowerMode get currentMode => _currentMode;

  /// Determines the optimal power profile given the user's distance to the closest geofence.
  PowerMode evaluatePowerMode(double closestDistanceMeters) {
    if (closestDistanceMeters > dormantThresholdMeters) {
      _currentMode = PowerMode.dormant;
    } else if (closestDistanceMeters > proximityThresholdMeters) {
      _currentMode = PowerMode.proximity;
    } else {
      _currentMode = PowerMode.entryDetection;
    }
    return _currentMode;
  }

  /// Returns recommended distance filter in meters for the current power mode.
  int get recommendedDistanceFilterMeters {
    switch (_currentMode) {
      case PowerMode.dormant:
        return 100; // Minimal CPU wakeups (saves up to 85% battery)
      case PowerMode.proximity:
        return 25;  // Balanced navigation mode
      case PowerMode.entryDetection:
        return 10;  // High-precision boundary crossing
    }
  }

  /// Minimum dwell time (seconds) required inside a boundary to prevent highway drive-by false triggers.
  int get minimumDwellSeconds => 15;
}
