@tool
class_name ObjectSpawner
extends Node3D

enum SpawnableElement {
	PLAYER_SIZED_CAPSULE,
	CUBE,
	SPHERE,
	CYLINDER
}

@export var layout_file: JSON

@export var spawnable_scenes: Dictionary[SpawnableElement, PackedScene] = {}

@export var scene_baseline_data: Array[Dictionary] = []

@export_group("Debug")
## If true, keeps the JSON file contents intact after successfully applying changes for inspection.
@export var keep_json: bool = false

@export var bake_baseline_now: bool = false:
	set(value):
		if value and Engine.is_editor_hint():
			bake_scene_baseline()
			bake_baseline_now = false

@export var load_layout_now: bool = false:
	set(value):
		if value and Engine.is_editor_hint():
			_try_import_vr_json()
			load_layout_now = false
	get:
		return false

func _notification(what: int) -> void:
	if Engine.is_editor_hint():
		if what == NOTIFICATION_READY:
			if scene_baseline_data.is_empty():
				bake_scene_baseline()
		elif what == NOTIFICATION_APPLICATION_FOCUS_IN:
			call_deferred("_try_import_vr_json")
		elif what == NOTIFICATION_EDITOR_PRE_SAVE:
			bake_scene_baseline()
	else:
		if what == NOTIFICATION_WM_CLOSE_REQUEST:
			_save_vr_layout()


func bake_scene_baseline() -> void:
	scene_baseline_data.clear()
	var pickables = find_children("*", "VREditorPickableSerializable", true, false)

	for i in range(pickables.size()):
		var pickable = pickables[i] as VREditorPickableSerializable
		if pickable:
			if pickable.instance_id.is_empty():
				pickable.instance_id = "editor_node_%d" % i

			scene_baseline_data.append(pickable.serialize_data())
	
	if Engine.is_editor_hint():
		print("ObjectSpawner (Editor): Baseline automatically baked with %d items." % scene_baseline_data.size())


func spawn_element(element: SpawnableElement, spawn_transform: Transform3D) -> VREditorPickableSerializable:
	var scene: PackedScene = spawnable_scenes.get(element)
	if not scene:
		push_warning("No PackedScene assigned for enum: %s" % element)
		return null

	var instance = scene.instantiate()
	if instance is VREditorPickableSerializable:
		add_child(instance)

		var target_basis := spawn_transform.basis.orthonormalized().scaled(instance.scale)
		instance.global_transform = Transform3D(target_basis, spawn_transform.origin)

		instance.instance_id = "runtime_node_%s" % str(instance.get_instance_id())
		return instance
	else:
		push_warning("Instantiated scene is not a VREditorPickableSerializable")
		instance.queue_free()
		return null


func _save_vr_layout() -> void:
	if not layout_file:
		push_warning("ObjectSpawner: No layout_file assigned.")
		return

	var file_path: String = layout_file.resource_path
	if file_path.is_empty():
		return

	if scene_baseline_data.is_empty():
		push_warning("ObjectSpawner: Baseline data is empty! Cannot compute deletions.")
		return

	var current_live_map: Dictionary = {}
	var pickables = find_children("*", "VREditorPickableSerializable", true, false)
	for pickable in pickables:
		if pickable is VREditorPickableSerializable and not pickable.instance_id.is_empty():
			current_live_map[pickable.instance_id] = pickable

	var baseline_map: Dictionary = {}
	for base_item in scene_baseline_data:
		var base_id: String = base_item.get("id", "")
		if not base_id.is_empty():
			baseline_map[base_id] = base_item

	var diff_added: Array = []
	var diff_modified: Array = []
	var diff_deleted: Array = []

	for id in current_live_map.keys():
		var live_node: VREditorPickableSerializable = current_live_map[id]
		var live_data: Dictionary = live_node.serialize_data()

		if not baseline_map.has(id):
			diff_added.append(live_data)
		else:
			var base_data: Dictionary = baseline_map[id]
			if JSON.stringify(live_data) != JSON.stringify(base_data):
				diff_modified.append(live_data)

	for base_id in baseline_map.keys():
		if not current_live_map.has(base_id):
			diff_deleted.append(base_id)

	var diff_payload = {
		"diff": {
			"added": diff_added,
			"modified": diff_modified,
			"deleted": diff_deleted
		}
	}

	var payload_string = JSON.stringify(diff_payload, "\t")

	var file = FileAccess.open(file_path, FileAccess.WRITE)
	if file:
		file.store_string(payload_string)
		file.flush()
		file.close()
		print("ObjectSpawner (Runtime): Layout saved before exiting. Deletions tracked: %s" % str(diff_deleted))


func _try_import_vr_json() -> void:
	if not Engine.is_editor_hint():
		return

	if not layout_file:
		return

	var file_path: String = layout_file.resource_path
	if file_path.is_empty() or not FileAccess.file_exists(file_path):
		return

	var file = FileAccess.open(file_path, FileAccess.READ)
	if not file:
		return

	var json_text = file.get_as_text()
	file.close()

	if json_text.strip_edges().is_empty():
		return

	var json = JSON.new()
	var parse_result = json.parse(json_text)
	if parse_result == OK and json.data is Dictionary:
		var diff_data = json.data.get("diff", {})
		_apply_layout_to_editor(diff_data)


func _apply_layout_to_editor(diff_data: Dictionary) -> void:
	if not Engine.is_editor_hint():
		return

	var scene_root = EditorInterface.get_edited_scene_root()
	if not scene_root:
		return

	var deleted_ids: Array = diff_data.get("deleted", [])
	var modified_items: Array = diff_data.get("modified", [])
	var added_items: Array = diff_data.get("added", [])

	if deleted_ids.is_empty() and modified_items.is_empty() and added_items.is_empty():
		return

	var existing_nodes: Dictionary = {}
	var all_pickables = find_children("*", "VREditorPickableSerializable", true, false)

	for node in all_pickables:
		if node is VREditorPickableSerializable and not node.instance_id.is_empty():
			existing_nodes[node.instance_id] = node

	var has_changes: bool = false

	for del_id in deleted_ids:
		if existing_nodes.has(del_id):
			var node_to_free = existing_nodes[del_id]
			existing_nodes.erase(del_id)
			node_to_free.queue_free()
			has_changes = true

	for item in modified_items:
		var item_id: String = item.get("id", "")
		if existing_nodes.has(item_id):
			var node: VREditorPickableSerializable = existing_nodes[item_id]
			node.deserialize_data(item)
			has_changes = true

	var restored_nodes: Dictionary = existing_nodes.duplicate()

	for item in added_items:
		var item_id: String = item.get("id", "")
		var node: Node = existing_nodes.get(item_id)

		if not node:
			var scene_path: String = item.get("scene_path", "")
			var packed_scene: PackedScene = null

			if not scene_path.is_empty():
				if ResourceLoader.exists(scene_path):
					packed_scene = ResourceLoader.load(scene_path, "PackedScene", ResourceLoader.CACHE_MODE_REUSE) as PackedScene

			if not packed_scene:
				var element_type: int = item.get("element_type", -1)
				if element_type != -1 and spawnable_scenes.has(element_type):
					packed_scene = spawnable_scenes[element_type]

			if packed_scene:
				node = packed_scene.instantiate()

			if not node:
				continue

			if not node.has_method("deserialize_data"):
				node.queue_free()
				continue

			add_child(node)
			node.owner = scene_root

		if "instance_id" in node:
			node.instance_id = item_id

		node.call("deserialize_data", item)
		restored_nodes[item_id] = node
		has_changes = true

	for item in added_items:
		var item_id: String = item.get("id", "")
		var parent_id: String = item.get("parent_id", "")
		var node: Node = restored_nodes.get(item_id)

		if node:
			if not parent_id.is_empty() and restored_nodes.has(parent_id):
				var parent_node = restored_nodes[parent_id]
				if node.get_parent() != parent_node:
					node.reparent(parent_node)
					node.owner = scene_root
			elif node.get_parent() != self:
				node.reparent(self)
				node.owner = scene_root

	if has_changes:
		if not keep_json:
			_clear_vr_json()
			print("ObjectSpawner (Editor): Successfully applied changes and cleared layout JSON.")
		else:
			print("ObjectSpawner (Editor): Successfully applied changes. JSON file kept intact (Debug mode).")
		_mark_scene_dirty()


func _mark_scene_dirty() -> void:
	if not Engine.is_editor_hint():
		return

	var undo_redo = EditorInterface.get_editor_undo_redo()
	if undo_redo:
		var scene_root = EditorInterface.get_edited_scene_root()
		undo_redo.create_action("Import VR Layout")
		undo_redo.add_do_property(scene_root, "position", scene_root.position)
		undo_redo.add_undo_property(scene_root, "position", scene_root.position)
		undo_redo.commit_action()


func _clear_vr_json() -> void:
	if not layout_file:
		return

	var file_path: String = layout_file.resource_path
	if file_path.is_empty():
		return

	var empty_payload = {
		"diff": {
			"added": [],
			"modified": [],
			"deleted": []
		}
	}

	var file = FileAccess.open(file_path, FileAccess.WRITE)
	if file:
		file.store_string(JSON.stringify(empty_payload, "\t"))
		file.flush()
		file.close()
