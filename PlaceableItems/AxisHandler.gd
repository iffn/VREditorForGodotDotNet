extends Node

class_name AxisHandler

@export var x_axis : Node3D
@export var y_axis : Node3D
@export var z_axis : Node3D

func update_axis(axis_selection : ObjectEditController.AxisOptions):
	x_axis.visible = false
	y_axis.visible = false
	z_axis.visible = false
	
	match axis_selection:
		ObjectEditController.AxisOptions.X:
			x_axis.visible = true
		ObjectEditController.AxisOptions.Y:
			y_axis.visible = true
		ObjectEditController.AxisOptions.Z:
			z_axis.visible = true
		_:
			pass
