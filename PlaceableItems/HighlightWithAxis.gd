extends XRToolsHighlightVisible

class_name HighlightWithAxis

@export var x_axis : Node3D
@export var y_axis : Node3D
@export var z_axis : Node3D

func _on_highlight_updated(_pickable, enable: bool) -> void:
	super._on_highlight_updated(_pickable, enable)
	
	ObjectEditController.current_highlight = self
	
	x_axis.visible = false
	y_axis.visible = false
	z_axis.visible = false
	
	match ObjectEditController.axis_selection:
		ObjectEditController.axis_options.X:
			x_axis.visible = true
		ObjectEditController.axis_options.Y:
			y_axis.visible = true
		ObjectEditController.axis_options.Z:
			z_axis.visible = true
		_:
			pass
