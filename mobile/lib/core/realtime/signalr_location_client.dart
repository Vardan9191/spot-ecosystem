import 'dart:async';
import 'dart:convert';
import 'package:http/http.dart' as http;
import '../geofence/geofence_manager.dart';
import '../location/location_service.dart';

class RealtimeGeofenceAlert {
  final String storeId;
  final String storeName;
  final String categoryName;
  final double distanceMeters;
  final int storeRadiusMeters;
  final String alertMessage;
  final bool isPartner;
  final DateTime timestamp;

  RealtimeGeofenceAlert({
    required this.storeId,
    required this.storeName,
    required this.categoryName,
    required this.distanceMeters,
    required this.storeRadiusMeters,
    required this.alertMessage,
    required this.isPartner,
    required this.timestamp,
  });

  factory RealtimeGeofenceAlert.fromJson(Map<String, dynamic> json) => RealtimeGeofenceAlert(
    storeId: json['storeId'] as String? ?? '',
    storeName: json['storeName'] as String? ?? '',
    categoryName: json['categoryName'] as String? ?? '',
    distanceMeters: (json['distanceMeters'] as num?)?.toDouble() ?? 0.0,
    storeRadiusMeters: json['storeRadiusMeters'] as int? ?? 100,
    alertMessage: json['alertMessage'] as String? ?? '',
    isPartner: json['isPartner'] as bool? ?? false,
    timestamp: json['timestamp'] != null 
        ? DateTime.tryParse(json['timestamp'].toString()) ?? DateTime.now()
        : DateTime.now(),
  );
}

/// SignalR / Realtime Client for live location sync and instant geofence push alerts.
class SignalRLocationClient {
  final String hubBaseUrl;
  final LocationService locationService;
  final GeofenceManager geofenceManager;

  StreamSubscription<UserCoordinates>? _locationSub;
  final _alertsController = StreamController<RealtimeGeofenceAlert>.broadcast();

  Stream<RealtimeGeofenceAlert> get onGeofenceTrigger => _alertsController.stream;

  bool _isConnected = false;
  bool get isConnected => _isConnected;

  SignalRLocationClient({
    this.hubBaseUrl = 'http://localhost:5000',
    required this.locationService,
    required this.geofenceManager,
  });

  Future<void> connect() async {
    _isConnected = true;
    _locationSub?.cancel();
    _locationSub = locationService.coordinatesStream.listen((coords) {
      sendLocationUpdate(coords.latitude, coords.longitude);
    });
  }

  /// Sends instantaneous GPS coordinates over HTTP/SignalR endpoint
  Future<void> sendLocationUpdate(double latitude, double longitude, {double radiusMeters = 1500}) async {
    if (!_isConnected) return;

    try {
      final uri = Uri.parse('$hubBaseUrl/api/v1/stores/nearby?latitude=$latitude&longitude=$longitude&radiusMeters=$radiusMeters');
      final response = await http.get(uri).timeout(const Duration(seconds: 4));

      if (response.statusCode == 200) {
        final List<dynamic> data = jsonDecode(response.body);
        for (final item in data) {
          final dist = (item['distanceMeters'] as num?)?.toDouble() ?? 9999.0;
          final radius = item['radiusMeters'] as int? ?? 100;

          if (dist <= radius) {
            final alert = RealtimeGeofenceAlert(
              storeId: item['id'] as String,
              storeName: item['name'] as String,
              categoryName: (item['category']?['name'] as String?) ?? 'Store',
              distanceMeters: dist,
              storeRadiusMeters: radius,
              alertMessage: 'You are within the $radius m geofence of ${item['name']}!',
              isPartner: item['isPartner'] as bool? ?? false,
              timestamp: DateTime.now(),
            );
            _alertsController.add(alert);
          }
        }
      }
    } catch (_) {
      // Gracefully handle network glitches
    }
  }

  void disconnect() {
    _locationSub?.cancel();
    _locationSub = null;
    _isConnected = false;
  }

  void dispose() {
    disconnect();
    _alertsController.close();
  }
}
