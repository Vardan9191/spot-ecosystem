import 'dart:convert';
import 'package:shared_preferences/shared_preferences.dart';

/// Manages a strict 4-hour anti-spam cooldown per geofence entity to prevent
/// push notification fatigue when users linger near or repeatedly cross geofence boundaries.
class CooldownManager {
  static const Duration cooldownDuration = Duration(hours: 4);
  static const String _storageKey = 'spot_geofence_cooldowns';

  final Map<String, DateTime> _lastTriggered = {};
  SharedPreferences? _prefs;

  CooldownManager({SharedPreferences? prefs}) : _prefs = prefs;

  Future<void> init() async {
    _prefs ??= await SharedPreferences.getInstance();
    _loadFromStorage();
  }

  void _loadFromStorage() {
    final raw = _prefs?.getString(_storageKey);
    if (raw == null || raw.isEmpty) return;

    try {
      final Map<String, dynamic> decoded = jsonDecode(raw);
      final now = DateTime.now();

      decoded.forEach((key, value) {
        final dt = DateTime.tryParse(value.toString());
        if (dt != null) {
          // Discard stale entries older than 4 hours
          if (now.difference(dt) < cooldownDuration) {
            _lastTriggered[key] = dt;
          }
        }
      });
    } catch (_) {
      // In case of corrupt storage, gracefully continue
    }
  }

  Future<void> _persistToStorage() async {
    if (_prefs == null) return;
    final Map<String, String> mapped = {};
    _lastTriggered.forEach((key, dt) {
      mapped[key] = dt.toIso8601String();
    });
    await _prefs!.setString(_storageKey, jsonEncode(mapped));
  }

  /// Checks whether a notification can be triggered for the specified entity ID.
  /// Returns `true` if the entity has never been triggered or if at least 4 hours have elapsed.
  bool canTrigger(String entityId) {
    final lastTime = _lastTriggered[entityId];
    if (lastTime == null) return true;

    final elapsed = DateTime.now().difference(lastTime);
    return elapsed >= cooldownDuration;
  }

  /// Records that a notification was just triggered for the specified entity.
  Future<void> recordTrigger(String entityId) async {
    _lastTriggered[entityId] = DateTime.now();
    await _persistToStorage();
  }

  /// Returns the remaining duration of the cooldown window, or Duration.zero if ready.
  Duration remainingCooldown(String entityId) {
    final lastTime = _lastTriggered[entityId];
    if (lastTime == null) return Duration.zero;

    final elapsed = DateTime.now().difference(lastTime);
    if (elapsed >= cooldownDuration) return Duration.zero;

    return cooldownDuration - elapsed;
  }

  /// Clears cooldown for testing or manual user reset.
  Future<void> clear(String entityId) async {
    _lastTriggered.remove(entityId);
    await _persistToStorage();
  }
}
