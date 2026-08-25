extends UIModeController

class_name VoxelUI

@export var sphere_tool : Button
@export var box_tool : Button
@export var cylinder_tool : Button

@export var default_tool : Button

var _tools : Array[Button]

var voxel_editor : VoxelEditor

func _ready() -> void:
	_tools = [
		sphere_tool, 
		box_tool, 
		cylinder_tool
		]
	for button in _tools:
		button.pressed.connect(select.bind(button))

func assign(_voxel_editor : VoxelEditor):
	voxel_editor = _voxel_editor

func enabled(state : bool):
	if voxel_editor:
		voxel_editor.PaintingActive = state


func setup():
	select(default_tool)

func select(button: Button):
	for i in _tools.size():
		var is_selected := _tools[i] == button
		_tools[i].button_pressed = is_selected
