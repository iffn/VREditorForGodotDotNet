extends UIModeController

class_name MovementModeController

@export var edit_mode_elements : Array[Control]
@export var rpg_mode_elements : Array[Control]
@export var start_select_toggle : Button

var player_movement_controller : PlayerMovementController

var _rpg_mode:
	set(value):
		for element in edit_mode_elements:
			element.visible = !value
		for element in rpg_mode_elements:
			element.visible = value
		player_movement_controller.rpg_movement = value

func disable_start_toggle():
	start_select_toggle.set_pressed_no_signal(false)

func assign(_player_movement_controller : PlayerMovementController):
	player_movement_controller = _player_movement_controller

func enabled(state : bool):
	if !state:
		toggle_select_start_mode(false)

func toggle_select_start_mode(active: bool):
	start_select_toggle.set_pressed_no_signal(active)

func enter_rpg_here():
	disable_start_toggle()
	_rpg_mode = true

func enter_edit_mode():
	_rpg_mode = false
