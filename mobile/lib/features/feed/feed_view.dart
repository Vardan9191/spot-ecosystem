import 'package:flutter/material.dart';

class MerchantMediaItem {
  final String id;
  final String merchantName;
  final String category;
  final String title;
  final String description;
  final String discountTag;
  final double distanceMeters;
  final bool isPartner;

  const MerchantMediaItem({
    required this.id,
    required this.merchantName,
    required this.category,
    required this.title,
    required this.description,
    required this.discountTag,
    required this.distanceMeters,
    required this.isPartner,
  });
}

class FeedView extends StatelessWidget {
  const FeedView({super.key});

  static const List<MerchantMediaItem> demoFeed = [
    MerchantMediaItem(
      id: 'feed-1',
      merchantName: 'Cortado Artisan Coffee Roasters',
      category: 'Artisan Coffee & Bakery',
      title: 'Fresh Single-Origin Ethiopian Roast Just Dropped! ☕',
      description: 'Flash Happy Hour: Enjoy 20% off all pour-overs when you walk in before 11 AM.',
      discountTag: '20% OFF IN-STORE',
      distanceMeters: 650,
      isPartner: true,
    ),
    MerchantMediaItem(
      id: 'feed-2',
      merchantName: 'Green Grocer Organic Market',
      category: 'Groceries & Fresh Food',
      title: 'Local Farm-to-Table Hass Avocados & Sourdough 🥑',
      description: 'Geofenced exclusive: Scan your Spot app at checkout for a complimentary cold-pressed juice with orders over \$25.',
      discountTag: 'FREE GIFT WITH PURCHASE',
      distanceMeters: 250,
      isPartner: true,
    ),
    MerchantMediaItem(
      id: 'feed-3',
      merchantName: 'Pixel & Wire Electronics',
      category: 'Electronics & Gadgets',
      title: 'Mechanical Keyboards & Custom Keycaps Showcase ⌨️',
      description: 'Demo stations now open. Hands-on testing for hot-swappable switches and ergonomic wrists.',
      discountTag: 'HANDS-ON DEMO',
      distanceMeters: 1250,
      isPartner: false,
    ),
  ];

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Merchant Stories & Shorts'),
      ),
      body: PageView.builder(
        scrollDirection: Axis.vertical,
        itemCount: demoFeed.length,
        itemBuilder: (context, index) {
          final item = demoFeed[index];
          return Container(
            margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
            decoration: BoxDecoration(
              gradient: LinearGradient(
                colors: [
                  const Color(0xFF0F172A),
                  index % 2 == 0 ? const Color(0xFF1E1B4B) : const Color(0xFF064E3B),
                ],
                begin: Alignment.topCenter,
                end: Alignment.bottomCenter,
              ),
              borderRadius: BorderRadius.circular(20),
              boxShadow: const [
                BoxShadow(color: Colors.black26, blurRadius: 10, offset: Offset(0, 4)),
              ],
            ),
            child: Padding(
              padding: const EdgeInsets.all(20),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // Merchant Header
                  Row(
                    children: [
                      CircleAvatar(
                        backgroundColor: Colors.white24,
                        child: Text(
                          item.merchantName[0],
                          style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold),
                        ),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              children: [
                                Flexible(
                                  child: Text(
                                    item.merchantName,
                                    style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold, fontSize: 16),
                                    overflow: TextOverflow.ellipsis,
                                  ),
                                ),
                                if (item.isPartner) ...[
                                  const SizedBox(width: 6),
                                  const Icon(Icons.verified, color: Colors.cyanAccent, size: 16),
                                ],
                              ],
                            ),
                            Text(
                              '${item.category} • ${item.distanceMeters.toInt()}m away',
                              style: const TextStyle(color: Colors.white70, fontSize: 12),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                  const Spacer(),

                  // Discount / Promo Badge
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
                    decoration: BoxDecoration(
                      color: Colors.amberAccent.shade700,
                      borderRadius: BorderRadius.circular(8),
                    ),
                    child: Text(
                      item.discountTag,
                      style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold, fontSize: 12),
                    ),
                  ),
                  const SizedBox(height: 12),

                  // Title & Description
                  Text(
                    item.title,
                    style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold, fontSize: 20),
                  ),
                  const SizedBox(height: 8),
                  Text(
                    item.description,
                    style: const TextStyle(color: Colors.white70, fontSize: 14, height: 1.4),
                  ),
                  const SizedBox(height: 20),

                  // Interactive Action Bar
                  Row(
                    children: [
                      Expanded(
                        child: ElevatedButton.icon(
                          style: ElevatedButton.styleFrom(
                            backgroundColor: Colors.tealAccent.shade700,
                            foregroundColor: Colors.white,
                            padding: const EdgeInsets.symmetric(vertical: 12),
                            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                          ),
                          onPressed: () {
                            ScaffoldMessenger.of(context).showSnackBar(
                              SnackBar(content: Text('Offer claimed for ${item.merchantName}!')),
                            );
                          },
                          icon: const Icon(Icons.local_offer),
                          label: const Text('Claim Geofence Deal', style: TextStyle(fontWeight: FontWeight.bold)),
                        ),
                      ),
                      const SizedBox(width: 12),
                      IconButton(
                        style: IconButton.styleFrom(
                          backgroundColor: Colors.white12,
                          foregroundColor: Colors.white,
                        ),
                        icon: const Icon(Icons.bookmark_border),
                        onPressed: () {},
                      ),
                    ],
                  ),
                ],
              ),
            ),
          );
        },
      ),
    );
  }
}
