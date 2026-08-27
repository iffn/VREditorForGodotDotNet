extends Node
class_name ObjectEditController

# Target points & visual indicators
@export var interaction_point: Node3D
@export var interaction_visualizer: Node3D

# References (assign in Inspector OR leave empty for auto-detection)
@export var controller: XRController3D
@export var function_pickup: XRToolsFunctionPickup

@export var pickup_left : XRToolsFunctionPickup
@export var pickup_right : XRToolsFunctionPickup

# Scaling settings
@export var scale_speed: float = 1.0

enum SpawnableElement {
	PLAYER_SIZED_CAPSULE,
	CUBE,
	SPHERE,
	CYLINDER
}

@export var spawnable_scenes: Dictionary[SpawnableElement, PackedScene] = {}

var active: bool = true:
	set(value):
		active = value
		
		if pickup_left:
			pickup_left.enabled = value
			if not value and is_instance_valid(pickup_left.picked_up_object):
				pickup_left.drop_object()
				
		if pickup_right:
			pickup_right.enabled = value
			if not value and is_instance_valid(pickup_right.picked_up_object):
				pickup_right.drop_object()

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

func _process(delta: float) -> void:
	if not active:
		return

	# Fallback to function_pickup controller if 'controller' is null
	var target_controller: XRController3D = controller
	if not target_controller and function_pickup:
		target_controller = function_pickup.get_controller()

	if not target_controller or not function_pickup:
		return

	var input_y: float = target_controller.get_vector2("primary").y

	# Scale the highlighted (closest) object instead of the held object
	var highlighted_object = function_pickup.closest_object
	if is_instance_valid(highlighted_object) and highlighted_object is Node3D:
		if abs(input_y) > 0.1:
			print("Scaling Highlighted Object Y: ", input_y)
			var scale_factor: float = 1.0 + (input_y * scale_speed * delta)
			highlighted_object.scale *= scale_factor

func _on_button_pressed(p_name: String) -> void:
	# Ignore input if the controller is inactive
	if not active:
		return

	match p_name:
		"by_button": # B Button
			delete_highlighted_element()
		"ax_button": # A Button
			duplicate_highlighted_element()

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

func duplicate_highlighted_element() -> void:
	if not function_pickup:
		push_warning("FunctionPickup reference is missing.")
		return

	if not interaction_point:
		push_warning("Interaction point is not assigned.")
		return

	var target = function_pickup.closest_object

	if is_instance_valid(target) and target is Node3D:
		# Create a full runtime duplicate of the node and sub-nodes
		var duplicate_instance = target.duplicate(DUPLICATE_USE_INSTANTIATION | DUPLICATE_SIGNALS | DUPLICATE_GROUPS) as Node3D
		
		# Add to current scene tree
		get_tree().current_scene.add_child(duplicate_instance)

		# Set transform
		duplicate_instance.global_transform = target.global_transform
		duplicate_instance.global_position = interaction_point.global_position

		# --- XR TOOLS PICKABLE CLEANUP ---
		# Clear any copied highlight state on the new object
		if duplicate_instance.has_method("request_highlight"):
			duplicate_instance.request_highlight(function_pickup, false)

		# Ensure the duplicate's physics state is active and unheld
		if duplicate_instance is XRToolsPickable:
			duplicate_instance.enabled = true
			if duplicate_instance.is_picked_up():
				duplicate_instance.let_go(null, Vector3.ZERO, Vector3.ZERO)
		elif duplicate_instance is RigidBody3D:
			duplicate_instance.freeze = false
			duplicate_instance.linear_velocity = Vector3.ZERO
			duplicate_instance.angular_velocity = Vector3.ZERO

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
