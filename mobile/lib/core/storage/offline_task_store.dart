import 'dart:convert';
import 'package:shared_preferences/shared_preferences.dart';

class LocalTaskItem {
  final String id;
  final String listId;
  String title;
  bool isCompleted;
  String quantity;

  LocalTaskItem({
    required this.id,
    required this.listId,
    required this.title,
    this.isCompleted = false,
    this.quantity = '1',
  });

  Map<String, dynamic> toJson() => {
    'id': id,
    'listId': listId,
    'title': title,
    'isCompleted': isCompleted,
    'quantity': quantity,
  };

  factory LocalTaskItem.fromJson(Map<String, dynamic> json) => LocalTaskItem(
    id: json['id'] as String,
    listId: json['listId'] as String,
    title: json['title'] as String,
    isCompleted: json['isCompleted'] as bool? ?? false,
    quantity: json['quantity'] as String? ?? '1',
  );
}

class LocalTaskList {
  final String id;
  final String userId;
  String title;
  String? categorySlug;
  double? latitude;
  double? longitude;
  int radiusMeters;
  bool isActive;
  bool isSynced;
  List<LocalTaskItem> items;

  LocalTaskList({
    required this.id,
    required this.userId,
    required this.title,
    this.categorySlug,
    this.latitude,
    this.longitude,
    this.radiusMeters = 150,
    this.isActive = true,
    this.isSynced = true,
    List<LocalTaskItem>? items,
  }) : items = items ?? [];

  Map<String, dynamic> toJson() => {
    'id': id,
    'userId': userId,
    'title': title,
    'categorySlug': categorySlug,
    'latitude': latitude,
    'longitude': longitude,
    'radiusMeters': radiusMeters,
    'isActive': isActive,
    'isSynced': isSynced,
    'items': items.map((i) => i.toJson()).toList(),
  };

  factory LocalTaskList.fromJson(Map<String, dynamic> json) => LocalTaskList(
    id: json['id'] as String,
    userId: json['userId'] as String,
    title: json['title'] as String,
    categorySlug: json['categorySlug'] as String?,
    latitude: (json['latitude'] as num?)?.toDouble(),
    longitude: (json['longitude'] as num?)?.toDouble(),
    radiusMeters: json['radiusMeters'] as int? ?? 150,
    isActive: json['isActive'] as bool? ?? true,
    isSynced: json['isSynced'] as bool? ?? true,
    items: (json['items'] as List<dynamic>?)
            ?.map((e) => LocalTaskItem.fromJson(e as Map<String, dynamic>))
            .toList() ??
        [],
  );
}

/// Offline-First Task Store: caches task lists locally for instant responsiveness
/// even without internet connectivity.
class OfflineTaskStore {
  static const String _storageKey = 'spot_offline_task_lists';
  final Map<String, LocalTaskList> _memoryCache = {};
  SharedPreferences? _prefs;

  OfflineTaskStore({SharedPreferences? prefs}) : _prefs = prefs;

  Future<void> init() async {
    _prefs ??= await SharedPreferences.getInstance();
    _loadFromDisk();
  }

  void _loadFromDisk() {
    final raw = _prefs?.getString(_storageKey);
    if (raw == null || raw.isEmpty) return;

    try {
      final List<dynamic> list = jsonDecode(raw);
      for (final item in list) {
        final taskList = LocalTaskList.fromJson(item as Map<String, dynamic>);
        _memoryCache[taskList.id] = taskList;
      }
    } catch (_) {
      // In case of error, start clean
    }
  }

  Future<void> _persistToDisk() async {
    if (_prefs == null) return;
    final list = _memoryCache.values.map((l) => l.toJson()).toList();
    await _prefs!.setString(_storageKey, jsonEncode(list));
  }

  List<LocalTaskList> getAllLists() => _memoryCache.values.toList();

  LocalTaskList? getListById(String id) => _memoryCache[id];

  Future<void> saveList(LocalTaskList list) async {
    _memoryCache[list.id] = list;
    await _persistToDisk();
  }

  Future<void> deleteList(String id) async {
    _memoryCache.remove(id);
    await _persistToDisk();
  }

  Future<void> toggleItem(String listId, String itemId) async {
    final list = _memoryCache[listId];
    if (list != null) {
      final item = list.items.firstWhere((i) => i.id == itemId, orElse: () => throw Exception('Item not found'));
      item.isCompleted = !item.isCompleted;
      list.isSynced = false;
      await _persistToDisk();
    }
  }

  Future<void> addItem(String listId, LocalTaskItem item) async {
    final list = _memoryCache[listId];
    if (list != null) {
      list.items.add(item);
      list.isSynced = false;
      await _persistToDisk();
    }
  }

  Future<void> removeItem(String listId, String itemId) async {
    final list = _memoryCache[listId];
    if (list != null) {
      list.items.removeWhere((i) => i.id == itemId);
      list.isSynced = false;
      await _persistToDisk();
    }
  }
}
