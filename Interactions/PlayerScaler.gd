@tool
class_name PlayerScaler
extends XRToolsMovementProvider

## XR Tools Movement Provider for Two-Handed Scaling
##
## Provides two-handed scaling and world manipulation via two-handed grip gesture.

@export_group("Provider Setup")
## Execution order priority (Must run before XRToolsFunctionPickup or default movement)
@export var order: int = 10

@export_group("XR Nodes")
## Target XROrigin3D node (auto-resolved from player_body if unassigned)
@export var xr_origin: XROrigin3D

## Left controller used for two-handed scaling
@export var left_controller: XRController3D

## Right controller used for two-handed scaling
@export var right_controller: XRController3D

@export_group("Pickup Integration")
## Left controller FunctionPickup
@export var left_pickup_func: XRToolsFunctionPickup

## Right controller FunctionPickup
@export var right_pickup_func: XRToolsFunctionPickup

@export_group("Scaling Settings")
## Button input action required on both controllers to trigger scaling
@export var scale_button_action: String = "grip_click"

## Maximum time window (in milliseconds) between hand grips where a gesture
## is treated as a scaling command instead of picking up objects.
@export var cancel_pickup_window_ms: int = 350

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

# Independent tracking per hand
var _left_picked_object: Node3D = null
var _left_pickup_transform: Transform3D
var _left_pickup_time_ms: int = 0

var _right_picked_object: Node3D = null
var _right_pickup_transform: Transform3D
var _right_pickup_time_ms: int = 0

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

	if scale_indicator:
		scale_indicator.visible = false
		scale_indicator.top_level = true

	if not Engine.is_editor_hint():
		call_deferred("_setup_pickup_listeners")


func _setup_pickup_listeners() -> void:
	if is_instance_valid(left_pickup_func):
		if not left_pickup_func.has_picked_up.is_connected(_on_left_picked_up):
			left_pickup_func.has_picked_up.connect(_on_left_picked_up)
			left_pickup_func.has_dropped.connect(_on_left_dropped)

	if is_instance_valid(right_pickup_func):
		if not right_pickup_func.has_picked_up.is_connected(_on_right_picked_up):
			right_pickup_func.has_picked_up.connect(_on_right_picked_up)
			right_pickup_func.has_dropped.connect(_on_right_dropped)


func _on_left_picked_up(what: Node3D) -> void:
	if _scaling_active:
		return
	_left_picked_object = what
	_left_pickup_transform = what.global_transform
	_left_pickup_time_ms = Time.get_ticks_msec()


func _on_left_dropped(_what: Node3D) -> void:
	_left_picked_object = null
	_left_pickup_time_ms = 0


func _on_right_picked_up(what: Node3D) -> void:
	if _scaling_active:
		return
	_right_picked_object = what
	_right_pickup_transform = what.global_transform
	_right_pickup_time_ms = Time.get_ticks_msec()


func _on_right_dropped(_what: Node3D) -> void:
	_right_picked_object = null
	_right_pickup_time_ms = 0


func _process(_delta: float) -> void:
	if Engine.is_editor_hint():
		return

	if not enabled:
		_reset_scaling_state()
		return

	_handle_hand_scale()


func is_xr_class(xr_name: String) -> bool:
	return xr_name == "PlayerScaler" or super(xr_name)


func physics_movement(_delta: float, player_body: XRToolsPlayerBody, disabled: bool) -> bool:
	if disabled or not enabled:
		_reset_scaling_state()
		return false

	if not xr_origin and player_body:
		xr_origin = player_body.get_node_or_null(player_body.origin) as XROrigin3D

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
		var initial_sample_center: Vector3 = controller_center_world

		if not _scaling_active:
			var current_time = Time.get_ticks_msec()

			var left_holding = is_instance_valid(left_pickup_func) and left_pickup_func.picked_up_object != null
			var right_holding = is_instance_valid(right_pickup_func) and right_pickup_func.picked_up_object != null

			# Evaluate individual timing for both hands
			var left_in_window = left_holding and (current_time - _left_pickup_time_ms) <= cancel_pickup_window_ms
			var right_in_window = right_holding and (current_time - _right_pickup_time_ms) <= cancel_pickup_window_ms

			# IF either hand holds an item outside the window -> BLOCK SCALING PERMANENTLY
			if (left_holding and not left_in_window) or (right_holding and not right_in_window):
				return

			# IF items were picked up within the grace window -> cancel them & revert position
			if left_holding and left_in_window:
				_cancel_hand_pickup(left_pickup_func, _left_picked_object, _left_pickup_transform)
				_left_picked_object = null
				_left_pickup_time_ms = 0

			if right_holding and right_in_window:
				_cancel_hand_pickup(right_pickup_func, _right_picked_object, _right_pickup_transform)
				_right_picked_object = null
				_right_pickup_time_ms = 0

			# Both hands are now free -> Activate world scaling
			_scaling_active = true
			_initial_scale_center_world = initial_sample_center
			_initial_scale = current_player_scale
			_initial_hand_distance_player_scale = current_dist / _initial_scale

			_set_pickups_enabled(false)

			if scale_indicator:
				scale_indicator.visible = true

		if current_dist <= 0.001:
			return

		var target_hand_distance_scale: float = current_dist / current_player_scale
		if target_hand_distance_scale > 0.0001:
			var new_scale: float = (_initial_hand_distance_player_scale * _initial_scale) / target_hand_distance_scale
			scale_player_and_objects(new_scale)

		if xr_origin:
			var current_center_world: Vector3 = controller_center_world
			var offset_world: Vector3 = _initial_scale_center_world - current_center_world
			xr_origin.global_position += offset_world

		if scale_indicator:
			var center_world: Vector3 = controller_center_world
			scale_indicator.global_position = center_world
			if left_controller.global_position.distance_squared_to(right_controller.global_position) > 0.0001:
				scale_indicator.look_at(right_controller.global_position, Vector3.UP)
			scale_indicator.scale = Vector3.ONE * (current_dist * 0.6)

		if scale_text:
			scale_text.text = "%.2f" % current_player_scale
	else:
		_reset_scaling_state()


func _cancel_hand_pickup(pickup: XRToolsFunctionPickup, obj: Node3D, saved_transform: Transform3D) -> void:
	if is_instance_valid(pickup) and pickup.picked_up_object != null:
		pickup.drop_object()

	if is_instance_valid(obj):
		obj.global_transform = saved_transform


func _set_pickups_enabled(enable_state: bool) -> void:
	if is_instance_valid(left_pickup_func):
		left_pickup_func.enabled = enable_state
	if is_instance_valid(right_pickup_func):
		right_pickup_func.enabled = enable_state


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
		_set_pickups_enabled(true)
		if scale_indicator:
			scale_indicator.visible = false


func _get_configuration_warnings() -> PackedStringArray:
	var warnings := super()

	if not left_controller or not right_controller:
		warnings.append("Both Left and Right Controllers are required for two-handed scaling.")

	return warnings
