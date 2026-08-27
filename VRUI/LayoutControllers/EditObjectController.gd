extends UIModeController

class_name EditObjectController

var object_edit_controller : ObjectEditController

func assign(_object_edit_controller : ObjectEditController):
	object_edit_controller = _object_edit_controller

func enabled(state : bool):
	if object_edit_controller:
		object_edit_controller.active = state

func spawn_player_sized_capsule():
	object_edit_controller.spawn_element(ObjectSpawner.SpawnableElement.PLAYER_SIZED_CAPSULE)

func spawn_cube():
	object_edit_controller.spawn_element(ObjectSpawner.SpawnableElement.CUBE)

func spawn_sphere():
	object_edit_controller.spawn_element(ObjectSpawner.SpawnableElement.SPHERE)

func spawn_cylinder():
	object_edit_controller.spawn_element(ObjectSpawner.SpawnableElement.CYLINDER)
