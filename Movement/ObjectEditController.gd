extends Node
class_name ObjectEditController

@export var interaciton_point : Node3D:
	get:
		return interaciton_point

var active : bool:
	set(value):
		interaciton_point.visible = value
