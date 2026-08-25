@tool
class_name PlayerScaler
extends XRToolsMovementProvider

## XR Tools Movement Provider for Two-Handed Scaling
##
## Provides two-handed scaling and world manipulation via two-handed grip gesture.

@export_group("Provider Setup")
## Execution order priority
@export var order: int = 35

@export_group("XR Nodes")
## Target XROrigin3D node (auto-resolved from player_body if unassigned)
@export var xr_origin: XROrigin3D

## Left controller used for two-handed scaling
@export var left_controller: XRController3D

## Right controller used for two-handed scaling
@export var right_controller: XRController3D

@export_group("Scaling Settings")
## Button input action required on both controllers to trigger scaling
@export var scale_button_action: String = "grip_click"

## Optional visual indicator shown between hands during scaling
@export var scale_indicator: Node3D

## Optional label to reflect current scale value
@export var scale_text: Label3D

@export_group("Scalable Targets")
## Objects whose absolute scale directly matches world_scale
@export var direct_scaling_objects: Array[Node3D] = []

## Objects whose scale scales incrementally with relative change
@export var incremental_scaling_objects: Array[Node3D] = []

# Scaling state tracking
var _scaling_active: bool = false
var _initial_scale_center_world: Vector3
var _initial_hand_distance_player_scale: float
var _initial_scale: float

var current_player_scale: float:
	get:
		if xr_origin:
			return xr_origin.world_scale
		return 1.0

var controller_center_world: Vector3:
	get:
		if left_controller and right_controller:
			return 0.5 * (left_controller.global_position + right_controller.global_position)
		return Vector3.ZERO

var hand_distance_world: float:
	get:
		if left_controller and right_controller:
			return left_controller.global_position.distance_to(right_controller.global_position)
		return 0.0


func _ready() -> void:
	super._ready()

	# Ghost Mode: Disable World Gravity across physics server
	var world_space: RID = get_tree().root.get_world_3d().space
	PhysicsServer3D.area_set_param(world_space, PhysicsServer3D.AREA_PARAM_GRAVITY, 0.0)

	if scale_indicator:
		scale_indicator.visible = false
		scale_indicator.top_level = true


func is_xr_class(xr_name: String) -> bool:
	return xr_name == "XRToolsMovementScalingGhost" or super(xr_name)


func physics_movement(_delta: float, player_body: XRToolsPlayerBody, disabled: bool) -> bool:
	if disabled or not enabled:
		_reset_scaling_state()
		return false

	# Resolve XROrigin3D automatically if not set
	if not xr_origin and player_body:
		xr_origin = player_body.get_node_or_null(player_body.origin) as XROrigin3D

	# Permanent ghost state: eliminate accumulated gravity/momentum
	if player_body:
		player_body.velocity = Vector3.ZERO

	# Process 2-Handed Scale Logic
	_handle_hand_scale()

	# Return true ONLY while actively scaling to lock movement during scale gestures.
	# When idle, return false so XRToolsMovementGhost handles joystick movement.
	return _scaling_active


func _handle_hand_scale() -> void:
	if not is_instance_valid(left_controller) or not is_instance_valid(right_controller):
		_reset_scaling_state()
		return

	if not left_controller.get_is_active() or not right_controller.get_is_active():
		_reset_scaling_state()
		return

	var left_active: bool = left_controller.is_button_pressed(scale_button_action)
	var right_active: bool = right_controller.is_button_pressed(scale_button_action)

	if left_active and right_active:
		var current_dist: float = hand_distance_world
		var center_world: Vector3 = controller_center_world

		if not _scaling_active:
			_scaling_active = true
			_initial_scale_center_world = center_world
			_initial_scale = current_player_scale
			_initial_hand_distance_player_scale = current_dist / _initial_scale

			if scale_indicator:
				scale_indicator.visible = true

		if current_dist <= 0.001:
			return

		var target_hand_distance_scale: float = current_dist / current_player_scale
		if target_hand_distance_scale > 0.0001:
			var new_scale: float = (_initial_hand_distance_player_scale * _initial_scale) / target_hand_distance_scale
			scale_player_and_objects(new_scale)

		if xr_origin:
			var offset_world: Vector3 = _initial_scale_center_world - center_world
			xr_origin.global_position += offset_world

		if scale_indicator:
			scale_indicator.global_position = center_world
			if left_controller.global_position.distance_squared_to(right_controller.global_position) > 0.0001:
				scale_indicator.look_at(right_controller.global_position, Vector3.UP)
			scale_indicator.scale = Vector3.ONE * (current_dist * 0.6)

		if scale_text:
			scale_text.text = "%.2f" % current_player_scale
	else:
		_reset_scaling_state()


func scale_player_and_objects(scale: float) -> void:
	var old_scale: float = current_player_scale
	if old_scale <= 0.0:
		return

	var scale_factor: float = scale / old_scale
	var direct_scale_vector: Vector3 = Vector3.ONE * scale

	if xr_origin:
		xr_origin.world_scale = scale

	for direct_scaler in direct_scaling_objects:
		if is_instance_valid(direct_scaler):
			direct_scaler.scale = direct_scale_vector

	for incremental_scaler in incremental_scaling_objects:
		if is_instance_valid(incremental_scaler):
			incremental_scaler.scale *= scale_factor


func _reset_scaling_state() -> void:
	if _scaling_active:
		_scaling_active = false
		if scale_indicator:
			scale_indicator.visible = false


func _get_configuration_warnings() -> PackedStringArray:
	var warnings := super()

	if not left_controller or not right_controller:
		warnings.append("Both Left and Right Controllers are required for two-handed scaling.")

	return warnings
