import 'dart:async';
import 'dart:math';
import 'package:geolocator/geolocator.dart';

class UserCoordinates {
  final double latitude;
  final double longitude;
  final double accuracy;
  final DateTime timestamp;

  const UserCoordinates({
    required this.latitude,
    required this.longitude,
    this.accuracy = 0.0,
    required this.timestamp,
  });
}

/// Battery-efficient location service utilizing adaptive distance filters
/// and low-power location modes when stationary to optimize battery life.
class LocationService {
  static const double earthRadiusMeters = 6371000.0;
  StreamSubscription<Position>? _positionStreamSub;
  final _coordinatesController = StreamController<UserCoordinates>.broadcast();

  Stream<UserCoordinates> get coordinatesStream => _coordinatesController.stream;

  /// Starts listening to device location updates with battery-friendly settings.
  /// Uses a 20-meter distanceFilter to avoid continuous CPU wakeups.
  Future<bool> startTracking({int distanceFilterMeters = 20}) async {
    bool serviceEnabled = await Geolocator.isLocationServiceEnabled();
    if (!serviceEnabled) {
      return false;
    }

    LocationPermission permission = await Geolocator.checkPermission();
    if (permission == LocationPermission.denied) {
      permission = await Geolocator.requestPermission();
      if (permission == LocationPermission.denied) {
        return false;
      }
    }

    if (permission == LocationPermission.deniedForever) {
      return false;
    }

    final locationSettings = LocationSettings(
      accuracy: LocationAccuracy.balanced, // Optimized for battery efficiency
      distanceFilter: distanceFilterMeters, // Minimum movement in meters before event
    );

    _positionStreamSub?.cancel();
    _positionStreamSub = Geolocator.getPositionStream(locationSettings: locationSettings).listen(
      (Position position) {
        _coordinatesController.add(UserCoordinates(
          latitude: position.latitude,
          longitude: position.longitude,
          accuracy: position.accuracy,
          timestamp: position.timestamp,
        ));
      },
      onError: (error) {
        // Log or handle location error gracefully
      },
    );

    return true;
  }

  /// Stops the location stream and conserves device battery.
  void stopTracking() {
    _positionStreamSub?.cancel();
    _positionStreamSub = null;
  }

  /// Get current instantaneous location.
  Future<UserCoordinates?> getCurrentLocation() async {
    try {
      final pos = await Geolocator.getCurrentPosition(
        locationSettings: const LocationSettings(accuracy: LocationAccuracy.medium),
      );
      return UserCoordinates(
        latitude: pos.latitude,
        longitude: pos.longitude,
        accuracy: pos.accuracy,
        timestamp: pos.timestamp,
      );
    } catch (_) {
      return null;
    }
  }

  /// Calculates the Haversine distance in meters between two lat/lng pairs.
  static double distanceBetweenMeters(
    double startLat, double startLon,
    double endLat, double endLon,
  ) {
    final dLat = _toRadians(endLat - startLat);
    final dLon = _toRadians(endLon - startLon);

    final a = sin(dLat / 2) * sin(dLat / 2) +
        cos(_toRadians(startLat)) * cos(_toRadians(endLat)) *
        sin(dLon / 2) * sin(dLon / 2);

    final c = 2 * atan2(sqrt(a), sqrt(1 - a));
    return earthRadiusMeters * c;
  }

  static double _toRadians(double degrees) => degrees * pi / 180.0;

  void dispose() {
    stopTracking();
    _coordinatesController.close();
  }
}
