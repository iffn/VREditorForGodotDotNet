extends Node

@export var buttons : Array[Button]
@export var elements : Array[Control]

@export var default : Button

func _ready() -> void:
	for button in buttons:
		button.pressed.connect(select.bind(button))
	select(default)

func select(button: Button):
	for i in buttons.size():
		var is_selected := buttons[i] == button
		buttons[i].button_pressed = is_selected
		elements[i].visible = is_selected
