extends Node
class_name ObjectEditController

@export var spawn_point: Node3D
@export var interaction_visualizer: Node3D

enum SpawnableElement {
	PLAYER_SIZED_CAPSULE,
	CUBE,
	SPHERE,
	CYLINDER
}

# Maps each enum entry directly to a PackedScene slot in the Inspector
@export var spawnable_scenes: Dictionary[SpawnableElement, PackedScene] = {}

var active: bool:
	set(value):
		if interaction_visualizer:
			interaction_visualizer.visible = value

func spawn_element(element: SpawnableElement) -> void:
	if not spawn_point:
		push_warning("Spawn point is not assigned.")
		return
		
	var scene_to_spawn: PackedScene = spawnable_scenes.get(element)
	if not scene_to_spawn:
		push_warning("No PackedScene assigned for enum index: %s" % element)
		return

	# Instantiate and attach
	var instance = scene_to_spawn.instantiate() as Node3D
	
	# Adding to get_tree().current_scene prevents scaling issues 
	# that happen if spawn_point itself is scaled.
	get_tree().current_scene.add_child(instance)
	instance.global_position = spawn_point.global_position
	instance.global_rotation = spawn_point.global_rotation
