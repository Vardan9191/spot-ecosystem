import 'package:flutter/material.dart';
import '../../core/geofence/geofence_manager.dart';
import '../../core/location/location_service.dart';

class MapView extends StatefulWidget {
  final GeofenceManager geofenceManager;
  final LocationService locationService;

  const MapView({
    super.key,
    required this.geofenceManager,
    required this.locationService,
  });

  @override
  State<MapView> createState() => _MapViewState();
}

class _MapViewState extends State<MapView> {
  UserCoordinates? _currentPosition;
  GeofenceRegion? _selectedRegion;

  @override
  void initState() {
    super.initState();
    _loadLocation();
    widget.locationService.coordinatesStream.listen((coords) {
      if (mounted) {
        setState(() {
          _currentPosition = coords;
        });
      }
    });
  }

  Future<void> _loadLocation() async {
    final pos = await widget.locationService.getCurrentLocation();
    if (mounted && pos != null) {
      setState(() {
        _currentPosition = pos;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final regions = widget.geofenceManager.activeRegions;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Hyper-Local Map & Geofences'),
        actions: [
          IconButton(
            icon: const Icon(Icons.my_location),
            tooltip: 'Center on user',
            onPressed: _loadLocation,
          ),
        ],
      ),
      body: Column(
        children: [
          // Geofence Visualizer Canvas / Map Mockup
          Expanded(
            flex: 3,
            child: Container(
              color: const Color(0xFF1E293B), // Dark sleek modern map styling
              child: Stack(
                children: [
                  // Grid background lines
                  CustomPaint(
                    size: Size.infinite,
                    painter: _MapGridPainter(
                      currentPos: _currentPosition,
                      regions: regions,
                      selectedRegion: _selectedRegion,
                    ),
                  ),

                  // Pin Picker / Legend
                  Positioned(
                    top: 16,
                    left: 16,
                    child: Container(
                      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                      decoration: BoxDecoration(
                        color: Colors.black.withOpacity(0.75),
                        borderRadius: BorderRadius.circular(8),
                        border: Border.all(color: Colors.white24),
                      ),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Row(
                            children: const [
                              Icon(Icons.circle, color: Colors.blueAccent, size: 10),
                              SizedBox(width: 6),
                              Text('Your Location', style: TextStyle(color: Colors.white, fontSize: 12)),
                            ],
                          ),
                          const SizedBox(height: 4),
                          Row(
                            children: const [
                              Icon(Icons.circle, color: Colors.greenAccent, size: 10),
                              SizedBox(width: 6),
                              Text('Active Geofence (<150m)', style: TextStyle(color: Colors.white, fontSize: 12)),
                            ],
                          ),
                          const SizedBox(height: 4),
                          Row(
                            children: const [
                              Icon(Icons.circle, color: Colors.amberAccent, size: 10),
                              SizedBox(width: 6),
                              Text('Nearby Stores (<1.5km)', style: TextStyle(color: Colors.white, fontSize: 12)),
                            ],
                          ),
                        ],
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ),

          // Active Geofence & Store List
          Expanded(
            flex: 2,
            child: Container(
              padding: const EdgeInsets.all(16),
              decoration: const BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
                boxShadow: [BoxShadow(color: Colors.black12, blurRadius: 10, offset: Offset(0, -4))],
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    'Discovered Geofences (${regions.length})',
                    style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16),
                  ),
                  const SizedBox(height: 8),
                  Expanded(
                    child: ListView.builder(
                      itemCount: regions.length,
                      itemBuilder: (context, idx) {
                        final r = regions[idx];
                        final state = widget.geofenceManager.getRegionState(r.id);
                        final isInside = state == GeofenceState.inside;

                        return ListTile(
                          contentPadding: EdgeInsets.zero,
                          leading: CircleAvatar(
                            backgroundColor: isInside ? Colors.green.shade100 : Colors.indigo.shade50,
                            child: Icon(
                              isInside ? Icons.store : Icons.storefront,
                              color: isInside ? Colors.green.shade800 : Colors.indigo.shade600,
                            ),
                          ),
                          title: Text(r.name, style: const TextStyle(fontWeight: FontWeight.w600)),
                          subtitle: Text('Radius: ${r.radiusMeters.toInt()}m • ${r.category}'),
                          trailing: Chip(
                            backgroundColor: isInside ? Colors.green : Colors.grey.shade200,
                            label: Text(
                              isInside ? 'INSIDE' : 'OUTSIDE',
                              style: TextStyle(
                                fontSize: 11,
                                fontWeight: FontWeight.bold,
                                color: isInside ? Colors.white : Colors.black87,
                              ),
                            ),
                          ),
                          onTap: () {
                            setState(() {
                              _selectedRegion = r;
                            });
                          },
                        );
                      },
                    ),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _MapGridPainter extends CustomPainter {
  final UserCoordinates? currentPos;
  final List<GeofenceRegion> regions;
  final GeofenceRegion? selectedRegion;

  _MapGridPainter({
    required this.currentPos,
    required this.regions,
    required this.selectedRegion,
  });

  @override
  void paint(Canvas canvas, Size size) {
    final center = Offset(size.width / 2, size.height / 2);

    // Draw coordinate grid
    final gridPaint = Paint()
      ..color = Colors.white.withOpacity(0.05)
      ..strokeWidth = 1.0;

    for (double i = 0; i < size.width; i += 40) {
      canvas.drawLine(Offset(i, 0), Offset(i, size.height), gridPaint);
    }
    for (double j = 0; j < size.height; j += 40) {
      canvas.drawLine(Offset(0, j), Offset(size.width, j), gridPaint);
    }

    // 1.5km Boundary Circle
    final boundaryPaint = Paint()
      ..color = Colors.cyanAccent.withOpacity(0.2)
      ..style = PaintingStyle.stroke
      ..strokeWidth = 1.5;
    canvas.drawCircle(center, 120, boundaryPaint);

    // User marker
    final userPaint = Paint()..color = Colors.blueAccent;
    canvas.drawCircle(center, 8, userPaint);
    canvas.drawCircle(center, 16, Paint()..color = Colors.blueAccent.withOpacity(0.3));

    // Draw geofence regions relative to center
    for (int i = 0; i < regions.length; i++) {
      final r = regions[i];
      // Offset visualization based on index
      final angle = (i * 2 * 3.14159) / (regions.isEmpty ? 1 : regions.length);
      final distRadius = 40.0 + (i * 30.0);
      final offset = Offset(center.dx + distRadius * 0.8 * (i % 2 == 0 ? 1 : -1),
                            center.dy + distRadius * 0.6 * (i % 3 == 0 ? 1 : -1));

      // Draw geofence radius
      final geofenceFill = Paint()
        ..color = (r == selectedRegion ? Colors.amberAccent : Colors.greenAccent).withOpacity(0.25)
        ..style = PaintingStyle.fill;
      canvas.drawCircle(offset, 25, geofenceFill);

      final geofenceBorder = Paint()
        ..color = (r == selectedRegion ? Colors.amberAccent : Colors.greenAccent)
        ..style = PaintingStyle.stroke
        ..strokeWidth = 2.0;
      canvas.drawCircle(offset, 25, geofenceBorder);

      // Pin center
      final pinPaint = Paint()..color = Colors.white;
      canvas.drawCircle(offset, 4, pinPaint);
    }
  }

  @override
  bool shouldRepaint(covariant _MapGridPainter oldDelegate) => true;
}
