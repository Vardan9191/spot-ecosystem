import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../lib/core/cooldown/cooldown_manager.dart';
import '../lib/core/geofence/battery_optimizer.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  group('BatteryOptimizer Tests', () {
    late BatteryOptimizer optimizer;

    setUp(() {
      optimizer = BatteryOptimizer();
    });

    test('Distance > 2500m selects Dormant Mode with 100m distance filter', () {
      final mode = optimizer.evaluatePowerMode(3500.0);
      expect(mode, equals(PowerMode.dormant));
      expect(optimizer.recommendedDistanceFilterMeters, equals(100));
    });

    test('Distance between 500m and 2500m selects Proximity Mode with 25m filter', () {
      final mode = optimizer.evaluatePowerMode(1200.0);
      expect(mode, equals(PowerMode.proximity));
      expect(optimizer.recommendedDistanceFilterMeters, equals(25));
    });

    test('Distance <= 500m selects EntryDetection Mode with 10m high-accuracy filter', () {
      final mode = optimizer.evaluatePowerMode(180.0);
      expect(mode, equals(PowerMode.entryDetection));
      expect(optimizer.recommendedDistanceFilterMeters, equals(10));
    });
  });

  group('CooldownManager Anti-Spam 4-Hour Persistence Tests', () {
    late CooldownManager cooldownManager;

    setUp(() async {
      SharedPreferences.setMockInitialValues({});
      cooldownManager = CooldownManager();
      await cooldownManager.init();
    });

    test('Initial geofence entity can trigger notification immediately', () {
      expect(cooldownManager.canTrigger('store-grocery-1'), isTrue);
      expect(cooldownManager.remainingCooldown('store-grocery-1'), equals(Duration.zero));
    });

    test('After recording trigger, entity is blocked by 4-hour cooldown', () async {
      await cooldownManager.recordTrigger('store-grocery-1');

      expect(cooldownManager.canTrigger('store-grocery-1'), isFalse);
      final remaining = cooldownManager.remainingCooldown('store-grocery-1');
      expect(remaining.inMinutes, greaterThan(230)); // Close to 240 mins (4 hours)
      expect(remaining.inMinutes, lessThanOrEqualTo(240));
    });

    test('Different entities maintain independent cooldown states', () async {
      await cooldownManager.recordTrigger('store-grocery-1');

      // Grocery store is locked
      expect(cooldownManager.canTrigger('store-grocery-1'), isFalse);

      // Coffee store is fresh and can trigger
      expect(cooldownManager.canTrigger('store-coffee-2'), isTrue);
    });
  });
}
