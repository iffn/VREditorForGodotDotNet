extends Node
class_name ObjectEditController

@export var spawner: ObjectSpawner
@export var interaction_point: Node3D
@export var interaction_visualizer: Node3D

@export var controller: XRController3D
@export var function_pickup: XRToolsFunctionPickup

@export var pickup_left: XRToolsFunctionPickup
@export var pickup_right: XRToolsFunctionPickup

@export var scale_speed: float = 1.0

enum AxisOptions {
	NONE,
	X,
	Y,
	Z
}

static var axis_selection : AxisOptions
static var current_highlight : HighlightWithAxis

var active: bool = true:
	set(value):
		active = value
		
		if pickup_left:
			#pickup_left.enabled = value
			if not value and is_instance_valid(pickup_left.picked_up_object):
				pickup_left.drop_object()
				
		if pickup_right:
			pickup_right.enabled = value
			if not value and is_instance_valid(pickup_right.picked_up_object):
				pickup_right.drop_object()

		if interaction_visualizer:
			interaction_visualizer.visible = value

func _enter_tree() -> void:
	if not controller:
		controller = XRHelpers.get_xr_controller(self)

	if controller:
		if not controller.button_pressed.is_connected(_on_button_pressed):
			controller.button_pressed.connect(_on_button_pressed)

func _exit_tree() -> void:
	if controller:
		if controller.button_pressed.is_connected(_on_button_pressed):
			controller.button_pressed.disconnect(_on_button_pressed)

func _ready() -> void:
	if not function_pickup:
		function_pickup = XRToolsFunctionPickup.find_right(self)

var trigger_was_active := false

func _process(delta: float) -> void:
	if not active:
		return

	var target_controller: XRController3D = controller
	if not target_controller and function_pickup:
		target_controller = function_pickup.get_controller()

	if not target_controller or not function_pickup:
		return

	# Scale direction
	var trigger_is_active := controller.is_button_pressed("trigger_click")
	if !trigger_was_active && trigger_is_active:
		axis_selection = ((axis_selection + 1) % AxisOptions.size()) as AxisOptions
		if current_highlight != null:
			current_highlight.update_axis()
	trigger_was_active = trigger_is_active

	# Scaling
	var input_y: float = target_controller.get_vector2("primary").y
	var highlighted_object = function_pickup.closest_object
	if is_instance_valid(highlighted_object):
		if abs(input_y) > 0.1:
			var scale_factor: float = 1.0 + (input_y * scale_speed * delta)
			
			if highlighted_object is VREditorPickableSerializable:
				highlighted_object.apply_scale_delta(scale_factor)

func _on_button_pressed(p_name: String) -> void:
	if not active:
		return

	match p_name:
		"by_button":
			delete_highlighted_element()
		"ax_button":
			duplicate_highlighted_element()
		

func delete_highlighted_element() -> void:
	if not function_pickup:
		push_warning("FunctionPickup reference is missing.")
		return

	var target = function_pickup.closest_object

	if is_instance_valid(target):
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
		var duplicate_instance = target.duplicate(DUPLICATE_USE_INSTANTIATION | DUPLICATE_SIGNALS | DUPLICATE_GROUPS) as Node3D
		
		var parent_node = spawner if spawner else get_tree().current_scene
		parent_node.add_child(duplicate_instance)

		duplicate_instance.global_transform = target.global_transform
		duplicate_instance.global_position = interaction_point.global_position

		if duplicate_instance is VREditorPickableSerializable:
			duplicate_instance.instance_id = str(duplicate_instance.get_instance_id())

		if duplicate_instance.has_method("request_highlight"):
			duplicate_instance.request_highlight(function_pickup, false)

		if duplicate_instance is XRToolsPickable:
			duplicate_instance.enabled = true
			if duplicate_instance.is_picked_up():
				duplicate_instance.let_go(null, Vector3.ZERO, Vector3.ZERO)
		elif duplicate_instance is RigidBody3D:
			duplicate_instance.freeze = false
			duplicate_instance.linear_velocity = Vector3.ZERO
			duplicate_instance.angular_velocity = Vector3.ZERO

func spawn_element(element: int) -> void:
	if not spawner:
		push_warning("Spawner is not assigned.")
		return
		
	if not interaction_point:
		push_warning("Spawn point is not assigned.")
		return

	spawner.spawn_element(element as ObjectSpawner.SpawnableElement, interaction_point.global_transform)
