extends Node

class_name MovementModeController

@export var edit_mode_elements : Array[Control]
@export var rpg_mode_elements : Array[Control]
@export var start_select_toggle : Button

var _rpg_mode:
	set(active):
		for element in edit_mode_elements:
			element.visible = !active
		for element in rpg_mode_elements:
			element.visible = active

func disable_start_toggle():
	start_select_toggle.set_pressed_no_signal(false)

func _ready() -> void:
	_rpg_mode = false

func _process(delta: float) -> void:
	if(start_select_toggle.pressed):
		pass

func enter_rpg_here():
	disable_start_toggle()
	_rpg_mode = true

func enter_edit_mode():
	_rpg_mode = false
