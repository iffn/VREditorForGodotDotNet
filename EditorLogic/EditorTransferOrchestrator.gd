@tool
extends Node

@export var object_spawner: ObjectSpawner

@export_group("Manual Triggers")
@export var bake_baseline_now: bool = false:
	set(value):
		if value and Engine.is_editor_hint():
			bake_scene_baseline()
			bake_baseline_now = false

@export var apply_layout_now: bool = false:
	set(value):
		if value and Engine.is_editor_hint():
			apply_layout()
			apply_layout_now = false
	get:
		return false


func _notification(what: int) -> void:
	if not object_spawner:
		return

	if Engine.is_editor_hint():
		if what == NOTIFICATION_READY:
			if object_spawner.is_baseline_empty():
				bake_scene_baseline()
		elif what == NOTIFICATION_APPLICATION_FOCUS_IN:
			call_deferred("apply_layout")
		elif what == NOTIFICATION_EDITOR_PRE_SAVE:
			bake_scene_baseline()
	else:
		if what == NOTIFICATION_WM_CLOSE_REQUEST:
			save_layout()


func bake_scene_baseline() -> void:
	print("baking scene baseline")
	if object_spawner:
		object_spawner.bake_scene_baseline()


func save_layout() -> void:
	print("saving layout")
	if object_spawner:
		object_spawner.save_ingame_layout()


func apply_layout() -> void:
	print("applying layout")
	if object_spawner:
		object_spawner.apply_pending_layout()
