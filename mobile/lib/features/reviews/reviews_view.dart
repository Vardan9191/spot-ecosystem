import 'package:flutter/material.dart';

class ReviewItem {
  final String author;
  final int ratingService;
  final int ratingQuality;
  final int ratingOverall;
  final String comment;
  final String date;

  const ReviewItem({
    required this.author,
    required this.ratingService,
    required this.ratingQuality,
    required this.ratingOverall,
    required this.comment,
    required this.date,
  });
}

class ReviewsView extends StatefulWidget {
  const ReviewsView({super.key});

  @override
  State<ReviewsView> createState() => _ReviewsViewState();
}

class _ReviewsViewState extends State<ReviewsView> {
  final List<ReviewItem> _reviews = [
    const ReviewItem(
      author: 'Alex M.',
      ratingService: 5,
      ratingQuality: 5,
      ratingOverall: 5,
      comment: 'Super fresh organic avocados and stellar curbside pickup service!',
      date: '2 days ago',
    ),
    const ReviewItem(
      author: 'Jordan K.',
      ratingService: 4,
      ratingQuality: 5,
      ratingOverall: 5,
      comment: 'Great selection of sourdough bread and local cheeses.',
      date: '1 day ago',
    ),
    const ReviewItem(
      author: 'Taylor R.',
      ratingService: 5,
      ratingQuality: 4,
      ratingOverall: 4,
      comment: 'Convenient geofenced alerts let me know when my favorite cold brew is in stock.',
      date: '3 hours ago',
    ),
  ];

  void _showAddReviewModal() {
    int service = 5;
    int quality = 5;
    int overall = 5;
    final commentController = TextEditingController();

    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      shape: const RoundedRectangleBorder(borderRadius: BorderRadius.vertical(top: Radius.circular(20))),
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setModalState) => Padding(
          padding: EdgeInsets.only(
            left: 20, right: 20, top: 20,
            bottom: MediaQuery.of(ctx).viewInsets.bottom + 20,
          ),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Text('Rate & Review Store', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
              const SizedBox(height: 16),

              _buildRatingRow('Overall Experience', overall, (val) => setModalState(() => overall = val)),
              _buildRatingRow('Customer Service', service, (val) => setModalState(() => service = val)),
              _buildRatingRow('Product Quality', quality, (val) => setModalState(() => quality = val)),

              const SizedBox(height: 12),
              TextField(
                controller: commentController,
                maxLines: 3,
                decoration: const InputDecoration(
                  labelText: 'Share details of your experience',
                  border: OutlineInputBorder(),
                ),
              ),
              const SizedBox(height: 16),
              SizedBox(
                width: double.infinity,
                child: ElevatedButton(
                  style: ElevatedButton.styleFrom(
                    backgroundColor: Colors.indigo,
                    foregroundColor: Colors.white,
                    padding: const EdgeInsets.symmetric(vertical: 14),
                  ),
                  onPressed: () {
                    if (commentController.text.trim().isNotEmpty) {
                      setState(() {
                        _reviews.insert(
                          0,
                          ReviewItem(
                            author: 'You',
                            ratingService: service,
                            ratingQuality: quality,
                            ratingOverall: overall,
                            comment: commentController.text.trim(),
                            date: 'Just now',
                          ),
                        );
                      });
                      Navigator.pop(ctx);
                    }
                  },
                  child: const Text('Submit Review', style: TextStyle(fontWeight: FontWeight.bold)),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildRatingRow(String label, int value, ValueChanged<int> onChanged) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: const TextStyle(fontWeight: FontWeight.w500)),
          Row(
            children: List.generate(5, (index) {
              final star = index + 1;
              return GestureDetector(
                onTap: () => onChanged(star),
                child: Icon(
                  star <= value ? Icons.star : Icons.star_border,
                  color: Colors.amber,
                  size: 26,
                ),
              );
            }),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Store Ratings & Reviews'),
        actions: [
          IconButton(
            icon: const Icon(Icons.rate_review),
            tooltip: 'Leave a Review',
            onPressed: _showAddReviewModal,
          ),
        ],
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Store Rating Summary Card
            Card(
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
              elevation: 2,
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Row(
                  children: [
                    Column(
                      children: const [
                        Text('4.8', style: TextStyle(fontSize: 40, fontWeight: FontWeight.bold, color: Colors.indigo)),
                        Row(
                          children: [
                            Icon(Icons.star, color: Colors.amber, size: 18),
                            Icon(Icons.star, color: Colors.amber, size: 18),
                            Icon(Icons.star, color: Colors.amber, size: 18),
                            Icon(Icons.star, color: Colors.amber, size: 18),
                            Icon(Icons.star_half, color: Colors.amber, size: 18),
                          ],
                        ),
                        SizedBox(height: 4),
                        Text('Based on 24 reviews', style: TextStyle(fontSize: 11, color: Colors.grey)),
                      ],
                    ),
                    const SizedBox(width: 24),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          _buildMetricBar('Overall', 0.95),
                          const SizedBox(height: 6),
                          _buildMetricBar('Service', 0.90),
                          const SizedBox(height: 6),
                          _buildMetricBar('Quality', 0.98),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 20),

            // Review List
            const Text('Customer Reviews', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
            const SizedBox(height: 12),
            ..._reviews.map((rev) => Card(
                  margin: const EdgeInsets.only(bottom: 12),
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                  child: Padding(
                    padding: const EdgeInsets.all(16),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                          children: [
                            Text(rev.author, style: const TextStyle(fontWeight: FontWeight.bold)),
                            Text(rev.date, style: const TextStyle(color: Colors.grey, fontSize: 12)),
                          ],
                        ),
                        const SizedBox(height: 6),
                        Row(
                          children: [
                            ...List.generate(
                              5,
                              (i) => Icon(
                                i < rev.ratingOverall ? Icons.star : Icons.star_border,
                                color: Colors.amber,
                                size: 16,
                              ),
                            ),
                            const SizedBox(width: 8),
                            Text(
                              'Service: ${rev.ratingService}/5 • Quality: ${rev.ratingQuality}/5',
                              style: const TextStyle(fontSize: 11, color: Colors.grey),
                            ),
                          ],
                        ),
                        const SizedBox(height: 8),
                        Text(rev.comment, style: const TextStyle(height: 1.3)),
                      ],
                    ),
                  ),
                )),
          ],
        ),
      ),
    );
  }

  Widget _buildMetricBar(String label, double value) {
    return Row(
      children: [
        SizedBox(width: 50, child: Text(label, style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w500))),
        Expanded(
          child: ClipRRect(
            borderRadius: BorderRadius.circular(4),
            child: LinearProgressIndicator(
              value: value,
              minHeight: 6,
              backgroundColor: Colors.grey.shade200,
              valueColor: const AlwaysStoppedAnimation<Color>(Colors.indigo),
            ),
          ),
        ),
      ],
    );
  }
}
