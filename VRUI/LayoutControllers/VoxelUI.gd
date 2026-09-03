extends UIModeController

class_name VoxelUI

@export var sphere_tool : Button
@export var box_tool : Button
@export var cylinder_tool : Button
@export var debug_dext : Label

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

func _process(delta: float) -> void:
	debug_dext.text = output_debug()

var min_fps : float = 10000.0
var max_paint_time : float = 0.0

func output_debug() -> String:
	var return_string := ""
	
	var fps : float = voxel_editor.debugFps
	var painting_time : float = voxel_editor.debugPaintingTime
	
	return_string += "FPS: " + str(fps) + "\n"
	return_string += "Painting time: " + str(painting_time) + "ms\n"
	return_string += "Shape scale: " + str(voxel_editor.DebugPaintScale) + "	\n"
	return_string += "\n"
	
	min_fps = min(min_fps, fps)
	max_paint_time = max(max_paint_time, painting_time)
	return_string += "Limits, resets every 10s\n"
	return_string += "Min FPS: " + str(min_fps) + "\n"
	return_string += "Max painting time: " + str(max_paint_time) + "ms\n"
	
	if Time.get_ticks_msec() % 10000 < 1000:
		min_fps = 1000.0
		max_paint_time = 0.0
	
	return return_string

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
