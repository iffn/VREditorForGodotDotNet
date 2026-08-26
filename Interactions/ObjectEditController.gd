extends Node
class_name ObjectEditController

# Target points & visual indicators
@export var interaction_point: Node3D
@export var interaction_visualizer: Node3D

# References (assign in Inspector OR leave empty for auto-detection)
@export var controller: XRController3D
@export var function_pickup: XRToolsFunctionPickup

enum SpawnableElement {
	PLAYER_SIZED_CAPSULE,
	CUBE,
	SPHERE,
	CYLINDER
}

@export var spawnable_scenes: Dictionary[SpawnableElement, PackedScene] = {}

var active: bool:
	set(value):
		active = value
		if interaction_visualizer:
			interaction_visualizer.visible = value

func _enter_tree() -> void:
	# Fallback: If controller isn't assigned in Inspector, search parent hierarchy
	if not controller:
		controller = XRHelpers.get_xr_controller(self)

	# Connect button signals safely
	if controller:
		if not controller.button_pressed.is_connected(_on_button_pressed):
			controller.button_pressed.connect(_on_button_pressed)

func _exit_tree() -> void:
	# Clean up signal connections on removal
	if controller:
		if controller.button_pressed.is_connected(_on_button_pressed):
			controller.button_pressed.disconnect(_on_button_pressed)

func _ready() -> void:
	# Fallback: Auto-find right hand pickup if not manually assigned
	if not function_pickup:
		function_pickup = XRToolsFunctionPickup.find_right(self)

func _on_button_pressed(p_name: String) -> void:
	if !active:
		return
	if p_name == "by_button":
		delete_highlighted_element()

func delete_highlighted_element() -> void:
	if not function_pickup:
		push_warning("FunctionPickup reference is missing.")
		return

	# Grab the currently targeted object
	var target = function_pickup.closest_object

	if is_instance_valid(target):
		# If the object is held, force the hand to drop it before deleting
		if function_pickup.picked_up_object == target:
			function_pickup.drop_object()

		target.queue_free()

func spawn_element(element: SpawnableElement) -> void:
	if not interaction_point:
		push_warning("Spawn point is not assigned.")
		return
		
	var scene_to_spawn: PackedScene = spawnable_scenes.get(element)
	if not scene_to_spawn:
		push_warning("No PackedScene assigned for enum index: %s" % element)
		return

	var instance = scene_to_spawn.instantiate() as Node3D
	
	get_tree().current_scene.add_child(instance)
	
	instance.global_position = interaction_point.global_position
	instance.global_rotation = interaction_point.global_rotation
