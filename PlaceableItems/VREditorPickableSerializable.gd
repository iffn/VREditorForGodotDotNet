@tool
class_name VREditorPickableSerializable
extends XRToolsPickable

@export_group("VR Editor Persistence (Auto-Managed)")

## Automatically generated unique identifier for editor sync.
@export var instance_id: String = ""

## Auto-captured scene path. Used as a fallback when nodes are duplicated at runtime.
@export_file("*.tscn") var scene_file_path_override: String = ""

## Uniform or non-uniform scale factor applied to all direct Node3D children.
@export var scale_factor: Vector3 = Vector3.ONE

func _ready() -> void:
	super._ready()
	
	# Auto-capture native scene path if override is unassigned
	if scene_file_path_override.is_empty() and not scene_file_path.is_empty():
		scene_file_path_override = scene_file_path

	# Auto-generate unique ID in editor if missing
	if Engine.is_editor_hint() and instance_id.is_empty():
		instance_id = str(get_instance_id())

	_apply_current_scale()

## Applies incremental uniform scale multiplier from controller input
func apply_scale_delta(factor: float) -> void:
	scale_factor *= factor
	_apply_current_scale()

## Recomputes transform scale for ALL direct Node3D children
func _apply_current_scale() -> void:
	for child in get_children():
		if child is Node3D:
			child.scale = scale_factor

## Encapsulates item data into a Dictionary for JSON output
func serialize_data() -> Dictionary:
	return {
		"id": instance_id,
		"scene_path": scene_file_path_override if not scene_file_path_override.is_empty() else scene_file_path,
		"parent_id": _get_parent_instance_id(),
		"transform": {
			"pos": [transform.origin.x, transform.origin.y, transform.origin.z],
			"rot": [transform.basis.get_euler().x, transform.basis.get_euler().y, transform.basis.get_euler().z],
			"scale": [scale_factor.x, scale_factor.y, scale_factor.z]
		}
	}

## Restores item data from a JSON Dictionary
func deserialize_data(data: Dictionary) -> void:
	if data.has("transform"):
		var t: Dictionary = data["transform"]
		
		if t.has("pos"):
			transform.origin = Vector3(t["pos"][0], t["pos"][1], t["pos"][2])
			
		if t.has("rot"):
			rotation = Vector3(t["rot"][0], t["rot"][1], t["rot"][2])

		if t.has("scale"):
			scale_factor = Vector3(t["scale"][0], t["scale"][1], t["scale"][2])
			_apply_current_scale()

func _get_parent_instance_id() -> String:
	var p = get_parent()
	if p is VREditorPickableSerializable:
		return p.instance_id
	return ""
