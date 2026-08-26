@tool
class_name XRToolsMovementGhost
extends XRToolsMovementProvider

## XR Tools Movement Provider for Ghost Mode / Noclip Flight

@export_group("Provider Setup")
## Execution order priority
@export var order: int = 10

@export_group("XR Nodes")
## Target XROrigin3D node (auto-resolved from player_body if unassigned)
@export var xr_origin: XROrigin3D

@export_group("Flight Settings")
## Movement speed (meters per second)
@export var max_speed: float = 3.0

## If true, flies in full 3D toward where the head is looking.
## If false, stays locked to the horizontal plane.
@export var fly_in_head_direction: bool = false

## Allow strafing (left/right joystick movement)
@export var strafe: bool = true

## Action name for translation joystick
@export var move_input_action: String = "primary"

## Deadzone threshold to filter out joystick drift
@export var deadzone: float = 0.1

# Controller node reference
var _controller: XRController3D


func is_xr_class(xr_name: String) -> bool:
	return xr_name == "XRToolsMovementGhost" or super(xr_name)


func _enter_tree() -> void:
	_controller = XRHelpers.get_xr_controller(self)


func _exit_tree() -> void:
	_controller = null


func physics_movement(delta: float, player_body: XRToolsPlayerBody, disabled: bool) -> bool:
	if disabled or not enabled or not _controller or not _controller.get_is_active():
		return false

	# 1. Continually zero out body physics so you hover cleanly in place
	if player_body:
		player_body.velocity = Vector3.ZERO

	# 2. Process translation input if the joystick is pushed
	var input: Vector2 = XRToolsUserSettings.get_adjusted_vector2(_controller, move_input_action)
	if input.length_squared() >= (deadzone * deadzone):
		_apply_ghost_translation(delta, player_body, input)

	# 3. ALWAYS return true while Ghost Mode is active to prevent gravity from pulling you down
	return true


func _apply_ghost_translation(delta: float, player_body: XRToolsPlayerBody, input: Vector2) -> void:
	if not player_body:
		return

	var camera: XRCamera3D = player_body.camera_node
	if not camera:
		return

	# Resolve origin safely from export or player_body
	var origin: XROrigin3D = xr_origin
	if not origin:
		origin = player_body.get_node_or_null(player_body.origin) as XROrigin3D

	if not origin:
		return

	# Determine forward and right vectors relative to camera head orientation
	var forward: Vector3 = -camera.global_transform.basis.z
	var right: Vector3 = camera.global_transform.basis.x

	# If horizontal flight only, project vectors onto the ground plane
	if not fly_in_head_direction:
		var up: Vector3 = player_body.up_player
		forward = forward.slide(up).normalized()
		right = right.slide(up).normalized()

	# Calculate movement vector
	var move_dir: Vector3 = forward * input.y
	if strafe:
		move_dir += right * input.x

	# Apply direct world translation
	origin.global_position += move_dir * max_speed * origin.world_scale * delta


func _get_configuration_warnings() -> PackedStringArray:
	var warnings := super()

	if not XRHelpers.get_xr_controller(self):
		warnings.append("This node must be placed as a child of an XRController3D node.")

	return warnings
