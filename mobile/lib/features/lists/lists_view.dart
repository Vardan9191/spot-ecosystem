import 'package:flutter/material.dart';
import '../../core/storage/offline_task_store.dart';

class ListsView extends StatefulWidget {
  final OfflineTaskStore taskStore;

  const ListsView({super.key, required this.taskStore});

  @override
  State<ListsView> createState() => _ListsViewState();
}

class _ListsViewState extends State<ListsView> {
  late List<LocalTaskList> _lists;

  @override
  void initState() {
    super.initState();
    _refreshLists();
  }

  void _refreshLists() {
    setState(() {
      _lists = widget.taskStore.getAllLists();
    });
  }

  void _showAddListDialog() {
    final titleController = TextEditingController();
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Create New Task List'),
        content: TextField(
          controller: titleController,
          autofocus: true,
          decoration: const InputDecoration(
            hintText: 'e.g. Organic Produce Shopping',
            border: OutlineInputBorder(),
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx),
            child: const Text('Cancel'),
          ),
          ElevatedButton(
            onPressed: () async {
              final title = titleController.text.trim();
              if (title.isNotEmpty) {
                final newList = LocalTaskList(
                  id: DateTime.now().millisecondsSinceEpoch.toString(),
                  userId: 'user-default-1',
                  title: title,
                  radiusMeters: 150,
                  isActive: true,
                );
                await widget.taskStore.saveList(newList);
                _refreshLists();
                if (mounted) Navigator.pop(ctx);
              }
            },
            child: const Text('Create'),
          ),
        ],
      ),
    );
  }

  void _showAddItemDialog(String listId) {
    final titleController = TextEditingController();
    final qtyController = TextEditingController(text: '1');

    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Add Shopping Item'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            TextField(
              controller: titleController,
              autofocus: true,
              decoration: const InputDecoration(
                labelText: 'Item name',
                hintText: 'e.g. Sourdough Loaf',
                border: OutlineInputBorder(),
              ),
            ),
            const SizedBox(height: 12),
            TextField(
              controller: qtyController,
              decoration: const InputDecoration(
                labelText: 'Quantity / Note',
                hintText: 'e.g. 2 pcs or 500g',
                border: OutlineInputBorder(),
              ),
            ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx),
            child: const Text('Cancel'),
          ),
          ElevatedButton(
            onPressed: () async {
              final title = titleController.text.trim();
              if (title.isNotEmpty) {
                final item = LocalTaskItem(
                  id: DateTime.now().millisecondsSinceEpoch.toString(),
                  listId: listId,
                  title: title,
                  quantity: qtyController.text.trim(),
                );
                await widget.taskStore.addItem(listId, item);
                _refreshLists();
                if (mounted) Navigator.pop(ctx);
              }
            },
            child: const Text('Add Item'),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Shopping & Task Lists'),
        actions: [
          IconButton(
            icon: const Icon(Icons.add),
            tooltip: 'Add List',
            onPressed: _showAddListDialog,
          ),
        ],
      ),
      body: _lists.isEmpty
          ? Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Icon(Icons.checklist_rounded, size: 64, color: Colors.grey.shade400),
                  const SizedBox(height: 16),
                  const Text('No active task lists yet', style: TextStyle(fontSize: 18, color: Colors.grey)),
                  const SizedBox(height: 8),
                  ElevatedButton.icon(
                    onPressed: _showAddListDialog,
                    icon: const Icon(Icons.add),
                    label: const Text('Create First List'),
                  ),
                ],
              ),
            )
          : ListView.builder(
              padding: const EdgeInsets.all(12),
              itemCount: _lists.length,
              itemBuilder: (context, index) {
                final list = _lists[index];
                final completedCount = list.items.where((i) => i.isCompleted).length;

                return Card(
                  margin: const EdgeInsets.only(bottom: 12),
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                  elevation: 2,
                  child: ExpansionTile(
                    initiallyExpanded: true,
                    leading: CircleAvatar(
                      backgroundColor: Colors.indigo.shade50,
                      child: Icon(Icons.shopping_bag_outlined, color: Colors.indigo.shade700),
                    ),
                    title: Text(
                      list.title,
                      style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16),
                    ),
                    subtitle: Row(
                      children: [
                        Text('$completedCount/${list.items.length} completed'),
                        const SizedBox(width: 8),
                        if (list.latitude != null)
                          Chip(
                            avatar: const Icon(Icons.location_on, size: 14, color: Colors.white),
                            label: Text('${list.radiusMeters}m geofence', style: const TextStyle(fontSize: 10, color: Colors.white)),
                            backgroundColor: Colors.teal,
                            visualDensity: VisualDensity.compact,
                          ),
                      ],
                    ),
                    children: [
                      ...list.items.map((item) => CheckboxListTile(
                            value: item.isCompleted,
                            title: Text(
                              item.title,
                              style: TextStyle(
                                decoration: item.isCompleted ? TextDecoration.lineThrough : null,
                                color: item.isCompleted ? Colors.grey : Colors.black87,
                              ),
                            ),
                            subtitle: Text('Qty: ${item.quantity}'),
                            secondary: IconButton(
                              icon: const Icon(Icons.delete_outline, size: 20, color: Colors.redAccent),
                              onPressed: () async {
                                await widget.taskStore.removeItem(list.id, item.id);
                                _refreshLists();
                              },
                            ),
                            onChanged: (val) async {
                              await widget.taskStore.toggleItem(list.id, item.id);
                              _refreshLists();
                            },
                          )),
                      Padding(
                        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                        child: Row(
                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                          children: [
                            TextButton.icon(
                              onPressed: () => _showAddItemDialog(list.id),
                              icon: const Icon(Icons.add, size: 18),
                              label: const Text('Add Item'),
                            ),
                            IconButton(
                              icon: const Icon(Icons.delete_forever, color: Colors.grey),
                              tooltip: 'Delete List',
                              onPressed: () async {
                                await widget.taskStore.deleteList(list.id);
                                _refreshLists();
                              },
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                );
              },
            ),
    );
  }
}
