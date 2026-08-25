extends UIModeController

class_name MovementUI

@export var edit_mode_elements : Array[Control]
@export var rpg_mode_elements : Array[Control]
@export var start_select_toggle : Button

@export var toggle_button : Button

var player_movement_controller : PlayerMovementController

var _rpg_mode:
	set(value):
		_rpg_mode = value
		for element in edit_mode_elements:
			element.visible = !value
			print(element.name," -> ", !value)
		for element in rpg_mode_elements:
			element.visible = value
			print(element.name," -> ", value)
		if player_movement_controller:
			player_movement_controller.rpg_movement = value
		if(value):
			toggle_button.text = "Enter\nEdit mode"
		else:
			toggle_button.text = "Enter\nRPG mode"

func disable_start_toggle():
	start_select_toggle.set_pressed_no_signal(false)

func assign(_player_movement_controller : PlayerMovementController):
	player_movement_controller = _player_movement_controller
	_rpg_mode = false

func enabled(state : bool):
	if !state:
		toggle_select_start_mode(false)

func toggle_select_start_mode(active: bool):
	start_select_toggle.set_pressed_no_signal(active)

func toggle_rpg_mode():
	_rpg_mode = !_rpg_mode
