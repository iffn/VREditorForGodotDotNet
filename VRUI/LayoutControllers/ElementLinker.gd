extends Node

class_name ElementLinker

@export var movement_mode_controller : MovementModeController # Weird caching bug: Breaks if called without 2
@export var terrain_edit_controller : TerrainEditModeController
@export var edit_object_controller : EditObjectController
@export var save_and_load_controller : SaveAndLoadController
@export var settings_controller : SettingsController
@export var tab_controller : TabController

func assign(player_side_coordinator: PlayerSideCoordinator):
	print("Assignment trying")
	
	while edit_object_controller == null or save_and_load_controller == null:
		await get_tree().process_frame
	movement_mode_controller.assign(player_side_coordinator)
	terrain_edit_controller.assign(player_side_coordinator)
	edit_object_controller.assign(player_side_coordinator)
	save_and_load_controller.assign(player_side_coordinator)
	settings_controller.assign(player_side_coordinator)
	
	tab_controller.set_visibility()
	print("Assignmnent complete")
