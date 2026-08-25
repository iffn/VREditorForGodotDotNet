extends Node

class_name ElementLinker

# Usin Onready, since XRToolsViewport2DIn3D breaks @export assigned in prefab scene
# https://github.com/GodotVR/godot-xr-tools/issues/889
@onready var movement_mode_controller : MovementModeController = $"Vertical arrangement/Movement"
@onready var terrain_edit_controller : TerrainEditModeController = $"Vertical arrangement/Terrain editing"
@onready var edit_object_controller : EditObjectController = $"Vertical arrangement/Edit objects"
@onready var save_and_load_controller : SaveAndLoadController = $"Vertical arrangement/Save and load"
@onready var settings_controller : SettingsController = $"Vertical arrangement/Settings"
@onready var tab_controller : TabController = $"Vertical arrangement/Tabs"

func _ready() -> void:
	return
	print(movement_mode_controller != null)
	print(terrain_edit_controller != null)
	print(edit_object_controller != null)
	print(save_and_load_controller != null)
	print(settings_controller != null)
	print(tab_controller != null)

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
