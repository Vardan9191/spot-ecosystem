import 'package:flutter/material.dart';
import 'core/cooldown/cooldown_manager.dart';
import 'core/geofence/geofence_manager.dart';
import 'core/location/location_service.dart';
import 'core/notifications/notification_service.dart';
import 'core/storage/offline_task_store.dart';
import 'features/feed/feed_view.dart';
import 'features/lists/lists_view.dart';
import 'features/map/map_view.dart';
import 'features/reviews/reviews_view.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();

  // Core Services
  final locationService = LocationService();
  final cooldownManager = CooldownManager();
  final notificationService = NotificationService();
  final taskStore = OfflineTaskStore();

  await cooldownManager.init();
  await notificationService.init();
  await taskStore.init();

  final geofenceManager = GeofenceManager(
    locationService: locationService,
    cooldownManager: cooldownManager,
    notificationService: notificationService,
  );

  // Register Canonical Seed Geofences
  geofenceManager.registerRegions([
    GeofenceRegion(
      id: 'store-grocery',
      name: 'Green Grocer Organic Market',
      latitude: 40.7145,
      longitude: -74.0080,
      radiusMeters: 150,
      category: 'Groceries',
      message: 'You are near Green Grocer Organic Market. Check your shopping list for fresh produce!',
    ),
    GeofenceRegion(
      id: 'store-coffee',
      name: 'Cortado Artisan Coffee Roasters',
      latitude: 40.7180,
      longitude: -74.0020,
      radiusMeters: 100,
      category: 'Coffee',
      message: 'Flash Deal: 20% off all pour-overs at Cortado Roasters right now!',
    ),
    GeofenceRegion(
      id: 'store-tech',
      name: 'Pixel & Wire Electronics',
      latitude: 40.7220,
      longitude: -73.9980,
      radiusMeters: 200,
      category: 'Electronics',
      message: 'Check out new mechanical keyboards in-store at Pixel & Wire!',
    ),
  ]);

  // Seed default offline task list if empty
  if (taskStore.getAllLists().isEmpty) {
    final defaultList = LocalTaskList(
      id: 'default-weekend-list',
      userId: 'user-default-1',
      title: 'Weekend Grocery Haul',
      radiusMeters: 150,
      items: [
        LocalTaskItem(id: 'i-1', listId: 'default-weekend-list', title: 'Organic Hass Avocados', quantity: '4 pcs'),
        LocalTaskItem(id: 'i-2', listId: 'default-weekend-list', title: 'Unsweetened Almond Milk', quantity: '2 cartons', isCompleted: true),
        LocalTaskItem(id: 'i-3', listId: 'default-weekend-list', title: 'Artisan Sourdough Loaf', quantity: '1 loaf'),
      ],
    );
    await taskStore.saveList(defaultList);
  }

  // Start battery-efficient location tracking and geofence monitoring
  await locationService.startTracking();
  geofenceManager.startMonitoring();

  runApp(SpotApp(
    locationService: locationService,
    geofenceManager: geofenceManager,
    cooldownManager: cooldownManager,
    notificationService: notificationService,
    taskStore: taskStore,
  ));
}

class SpotApp extends StatelessWidget {
  final LocationService locationService;
  final GeofenceManager geofenceManager;
  final CooldownManager cooldownManager;
  final NotificationService notificationService;
  final OfflineTaskStore taskStore;

  const SpotApp({
    super.key,
    required this.locationService,
    required this.geofenceManager,
    required this.cooldownManager,
    required this.notificationService,
    required this.taskStore,
  });

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Spot - Hyper-Local Ecosystem',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(
          seedColor: const Color(0xFF4F46E5),
          brightness: Brightness.light,
        ),
        useMaterial3: true,
      ),
      home: SpotMainNavigation(
        locationService: locationService,
        geofenceManager: geofenceManager,
        taskStore: taskStore,
      ),
    );
  }
}

class SpotMainNavigation extends StatefulWidget {
  final LocationService locationService;
  final GeofenceManager geofenceManager;
  final OfflineTaskStore taskStore;

  const SpotMainNavigation({
    super.key,
    required this.locationService,
    required this.geofenceManager,
    required this.taskStore,
  });

  @override
  State<SpotMainNavigation> createState() => _SpotMainNavigationState();
}

class _SpotMainNavigationState extends State<SpotMainNavigation> {
  int _currentIndex = 0;

  late final List<Widget> _pages;

  @override
  void initState() {
    super.initState();
    _pages = [
      MapView(
        geofenceManager: widget.geofenceManager,
        locationService: widget.locationService,
      ),
      const FeedView(),
      ListsView(taskStore: widget.taskStore),
      const ReviewsView(),
    ];
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: IndexedStack(
        index: _currentIndex,
        children: _pages,
      ),
      bottomNavigationBar: NavigationBar(
        selectedIndex: _currentIndex,
        onDestinationSelected: (int index) {
          setState(() {
            _currentIndex = index;
          });
        },
        destinations: const [
          NavigationDestination(
            icon: Icon(Icons.explore_outlined),
            selectedIcon: Icon(Icons.explore),
            label: 'Map & Zones',
          ),
          NavigationDestination(
            icon: Icon(Icons.video_library_outlined),
            selectedIcon: Icon(Icons.video_library),
            label: 'Stories & Feed',
          ),
          NavigationDestination(
            icon: Icon(Icons.checklist_rtl_outlined),
            selectedIcon: Icon(Icons.checklist_rtl),
            label: 'Task Lists',
          ),
          NavigationDestination(
            icon: Icon(Icons.star_outline),
            selectedIcon: Icon(Icons.star),
            label: 'Reviews',
          ),
        ],
      ),
    );
  }
}
