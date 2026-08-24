extends UIModeController

class_name MovementModeController

@export var edit_mode_elements : Array[Control]
@export var rpg_mode_elements : Array[Control]
@export var start_select_toggle : Button

var player_side_coordinator: PlayerSideCoordinator

var _rpg_mode:
	set(active):
		for element in edit_mode_elements:
			element.visible = !active
		for element in rpg_mode_elements:
			element.visible = active

func disable_start_toggle():
	start_select_toggle.set_pressed_no_signal(false)

func assign(_player_side_coordinator: PlayerSideCoordinator):
	player_side_coordinator = _player_side_coordinator
	print("assign on movement was called")

func enabled(state : bool):
	if state:
		player_side_coordinator.interaction_state = PlayerSideCoordinator.interaction_states.none

func toggle_select_start_mode(active: bool):
	if start_select_toggle.pressed:
		pass

func enter_rpg_here():
	disable_start_toggle()
	_rpg_mode = true

func enter_edit_mode():
	_rpg_mode = false
