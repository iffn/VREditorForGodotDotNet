extends UIModeController

class_name EditObjectController

var object_edit_controller : ObjectEditController

func assign(_object_edit_controller : ObjectEditController):
	object_edit_controller = _object_edit_controller

func enabled(state : bool):
	if object_edit_controller:
		object_edit_controller.active = state
