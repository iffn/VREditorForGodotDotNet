@tool
class_name ObjectSpawner
extends Node3D

enum SpawnableElement {
	PLAYER_SIZED_CAPSULE,
	CUBE,
	SPHERE,
	CYLINDER
}

## Assign your target JSON file from the project FileSystem (res://)
@export var layout_file: JSON

@export var spawnable_scenes: Dictionary[SpawnableElement, PackedScene] = {}

## Pre-baked baseline captured in the editor before game start. Saved directly into the scene (.tscn).
@export var scene_baseline_data: Array[Dictionary] = []

## Inspector toggle to manually bake the scene baseline
@export var bake_baseline_now: bool = false:
	set(value):
		if value and Engine.is_editor_hint():
			bake_scene_baseline()
			bake_baseline_now = false

func _notification(what: int) -> void:
	if Engine.is_editor_hint():
		if what == NOTIFICATION_APPLICATION_FOCUS_IN:
			call_deferred("_try_import_vr_json")
		elif what == NOTIFICATION_EDITOR_PRE_SAVE:
			bake_scene_baseline()
	else:
		if what == NOTIFICATION_WM_CLOSE_REQUEST:
			_save_vr_layout()

## Bakes the current editor scene state into the exportable baseline array
func bake_scene_baseline() -> void:
	scene_baseline_data.clear()
	var pickables = find_children("*", "VREditorPickableSerializable", true, false)
	
	for i in range(pickables.size()):
		var pickable = pickables[i] as VREditorPickableSerializable
		if pickable:
			# Ensure every pre-placed editor node has a static deterministic ID
			if pickable.instance_id.is_empty():
				pickable.instance_id = "editor_node_%d" % i
			
			scene_baseline_data.append(pickable.serialize_data())

## Spawns new runtime objects and assigns dynamic IDs
## Spawns new runtime objects and assigns dynamic IDs (preserves native scale)
func spawn_element(element: SpawnableElement, spawn_transform: Transform3D) -> VREditorPickableSerializable:
	var scene: PackedScene = spawnable_scenes.get(element)
	if not scene:
		push_warning("No PackedScene assigned for enum: %s" % element)
		return null

	var instance = scene.instantiate()
	if instance is VREditorPickableSerializable:
		add_child(instance)
		
		# Retain native object scale while applying target rotation and origin position
		var target_basis := spawn_transform.basis.orthonormalized().scaled(instance.scale)
		instance.global_transform = Transform3D(target_basis, spawn_transform.origin)

		instance.instance_id = "runtime_node_%s" % str(instance.get_instance_id())
		return instance
	else:
		push_warning("Instantiated scene is not a VREditorPickableSerializable")
		instance.queue_free()
		return null

## Compares live scene state against pre-baked baseline and writes diff to JSON
func _save_vr_layout() -> void:
	if not layout_file:
		push_warning("ObjectSpawner: No layout_file assigned in Inspector.")
		return

	var file_path: String = layout_file.resource_path
	if file_path.is_empty():
		push_warning("ObjectSpawner: Assigned layout_file does not have a valid path.")
		return

	# Build lookup map of live objects currently in scene
	var current_live_map: Dictionary = {}
	var pickables = find_children("*", "VREditorPickableSerializable", true, false)
	for pickable in pickables:
		if pickable is VREditorPickableSerializable and not pickable.instance_id.is_empty():
			current_live_map[pickable.instance_id] = pickable

	# Build lookup map of baseline editor objects
	var baseline_map: Dictionary = {}
	for base_item in scene_baseline_data:
		var base_id: String = base_item.get("id", "")
		if not base_id.is_empty():
			baseline_map[base_id] = base_item

	var diff_added: Array = []
	var diff_modified: Array = []
	var diff_deleted: Array = []

	# 1. Identify Additions & Modifications
	for id in current_live_map.keys():
		var live_node: VREditorPickableSerializable = current_live_map[id]
		var live_data: Dictionary = live_node.serialize_data()

		if not baseline_map.has(id):
			# Object was created at runtime
			diff_added.append(live_data)
		else:
			# Object existed in baseline: check if modified
			var base_data: Dictionary = baseline_map[id]
			if JSON.stringify(live_data) != JSON.stringify(base_data):
				diff_modified.append(live_data)

	# 2. Identify Deletions
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

	var file = FileAccess.open(file_path, FileAccess.WRITE)
	if file:
		file.store_string(JSON.stringify(diff_payload, "\t"))
		file.close()
		print("VR layout diff saved to: ", file_path)

func _try_import_vr_json() -> void:
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
	if json.parse(json_text) == OK and json.data is Dictionary:
		var diff_data = json.data.get("diff", {})
		_apply_layout_to_editor(diff_data)

## Reconciles editor scene state against the JSON diff payload
func _apply_layout_to_editor(diff_data: Dictionary) -> void:
	if not Engine.is_editor_hint():
		return

	var scene_root = EditorInterface.get_edited_scene_root()
	if not scene_root:
		return

	# Quick check if there are actual diff items to process
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

	# 1. Apply Deletions
	for del_id in deleted_ids:
		if existing_nodes.has(del_id):
			var node_to_free = existing_nodes[del_id]
			existing_nodes.erase(del_id)
			node_to_free.queue_free()
			has_changes = true

	# 2. Apply Modifications
	for item in modified_items:
		var item_id: String = item.get("id", "")
		if existing_nodes.has(item_id):
			var node: VREditorPickableSerializable = existing_nodes[item_id]
			node.deserialize_data(item)
			has_changes = true

	# 3. Instantiate Additions
	var restored_nodes: Dictionary = existing_nodes.duplicate()

	for item in added_items:
		var item_id: String = item.get("id", "")
		var node: VREditorPickableSerializable = existing_nodes.get(item_id)

		if not node:
			var scene_path: String = item.get("scene_path", "")
			if FileAccess.file_exists(scene_path):
				var scene = load(scene_path) as PackedScene
				if scene:
					node = scene.instantiate() as VREditorPickableSerializable
			
			if not node:
				continue

			add_child(node)
			node.owner = scene_root

		node.instance_id = item_id
		node.deserialize_data(item)
		restored_nodes[item_id] = node
		has_changes = true

	# 4. Re-parenting Hierarchy for Added Objects
	for item in added_items:
		var item_id: String = item.get("id", "")
		var parent_id: String = item.get("parent_id", "")
		var node: VREditorPickableSerializable = restored_nodes.get(item_id)

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
		# Wipe the JSON file so modifications are not re-applied on subsequent focus events
		_clear_vr_json()

		# Trigger Godot scene dirty flag via standard EditorUndoRedoManager context
		var dummy_plugin := EditorPlugin.new()
		var undo_redo := dummy_plugin.get_undo_redo()
		dummy_plugin.free()

		if undo_redo:
			undo_redo.create_action("Import VR Layout", UndoRedo.MERGE_DISABLE, scene_root)
			undo_redo.add_do_property(scene_root, "position", scene_root.position)
			undo_redo.add_undo_property(scene_root, "position", scene_root.position)
			undo_redo.commit_action()

## Clears the content of the target layout JSON file after successful application
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
		file.close()
