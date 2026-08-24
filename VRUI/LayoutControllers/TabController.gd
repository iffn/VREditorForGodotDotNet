extends Node

class_name TabController

@export var buttons : Array[Button]
@export var elements : Array[UIModeController]

@export var default : Button

var current_index : int

var current_button : Button :
	get:
		return buttons[current_index]

var current_element : UIModeController :
	get:
		return elements[current_index]

func set_visibility():
	for i in buttons.size():
		elements[i].visible = false
	current_element.visible = true

func _ready() -> void:
	for i in buttons.size():
		buttons[i].pressed.connect(select.bind(i))
		if buttons[i] == default:
			current_index = i
	
	current_button.button_pressed = true
	current_element.visible = true

func select(new_index: int):
	current_button.button_pressed = false
	current_element.visible = false
	current_element.enabled(false)
	
	current_index = new_index
	
	current_button.button_pressed = true
	current_element.visible = true
	current_element.enabled(true)
