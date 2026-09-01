extends XRToolsHighlightVisible

class_name HighlightWithAxis

@export var x_axis : Node3D
@export var y_axis : Node3D
@export var z_axis : Node3D

func update_axis():
	x_axis.visible = false
	y_axis.visible = false
	z_axis.visible = false
	
	match ObjectEditController.axis_selection:
		ObjectEditController.AxisOptions.X:
			x_axis.visible = true
		ObjectEditController.AxisOptions.Y:
			y_axis.visible = true
		ObjectEditController.AxisOptions.Z:
			z_axis.visible = true
		_:
			pass

func _on_highlight_updated(_pickable, enable: bool) -> void:
	super._on_highlight_updated(_pickable, enable)
	
	ObjectEditController.current_highlight = self
	
	update_axis()
