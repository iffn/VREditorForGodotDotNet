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

func _notification(what: int) -> void:
	if Engine.is_editor_hint():
		if what == NOTIFICATION_APPLICATION_FOCUS_IN:
			call_deferred("_try_import_vr_json")
	else:
		if what == NOTIFICATION_WM_CLOSE_REQUEST:
			_save_vr_layout()

func spawn_element(element: SpawnableElement, spawn_transform: Transform3D) -> VREditorPickableSerializable:
	var scene: PackedScene = spawnable_scenes.get(element)
	if not scene:
		push_warning("No PackedScene assigned for enum: %s" % element)
		return null

	var instance = scene.instantiate()
	if instance is VREditorPickableSerializable:
		add_child(instance)
		instance.global_transform = spawn_transform
		instance.instance_id = str(instance.get_instance_id())
		return instance
	else:
		push_warning("Instantiated scene is not a VREditorPickableSerializable")
		instance.queue_free()
		return null

func _save_vr_layout() -> void:
	if not layout_file:
		push_warning("ObjectSpawner: No layout_file assigned in Inspector.")
		return

	var file_path: String = layout_file.resource_path
	if file_path.is_empty():
		push_warning("ObjectSpawner: Assigned layout_file does not have a valid path.")
		return

	var items: Array = []
	var pickables = find_children("*", "VREditorPickableSerializable", true, false)
	
	for pickable in pickables:
		if pickable is VREditorPickableSerializable:
			items.append(pickable.serialize_data())

	var data = {"objects": items}
	var file = FileAccess.open(file_path, FileAccess.WRITE)
	if file:
		file.store_string(JSON.stringify(data, "\t"))
		file.close()
		print("VR layout saved to: ", file_path)

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
		_apply_layout_to_editor(json.data.get("objects", []))

func _apply_layout_to_editor(objects_data: Array) -> void:
	var scene_root = EditorInterface.get_edited_scene_root()
	if not scene_root:
		return

	var existing_nodes: Dictionary = {}
	var all_pickables = find_children("*", "VREditorPickableSerializable", true, false)
	
	for node in all_pickables:
		if node is VREditorPickableSerializable and not node.instance_id.is_empty():
			existing_nodes[node.instance_id] = node

	var restored_nodes: Dictionary = {}

	# Pass 1: Find existing nodes or instantiate missing ones
	for item in objects_data:
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

	# Pass 2: Re-parenting Hierarchy
	for item in objects_data:
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
