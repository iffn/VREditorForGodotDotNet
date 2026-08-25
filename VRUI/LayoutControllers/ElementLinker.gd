extends Node

class_name ElementLinker

# Usin Onready, since XRToolsViewport2DIn3D breaks @export assigned in prefab scene
# https://github.com/GodotVR/godot-xr-tools/issues/889
@onready var movement_mode_ui : MovementUI = $"Vertical arrangement/Movement"
@onready var terrain_edit_ui : VoxelUI = $"Vertical arrangement/Voxel editor"
@onready var edit_object_ui : EditObjectController = $"Vertical arrangement/Edit objects"
@onready var save_and_load_ui : SaveAndLoadUI = $"Vertical arrangement/Save and load"
@onready var settings_ui : SettingsUI = $"Vertical arrangement/Settings"
@onready var tab_ui : TabUI = $"Vertical arrangement/Tabs"

@export var player_movement_controller : PlayerMovementController
@export var voxel_editor : VoxelEditor
@export var object_edit_controller : ObjectEditController

func _ready() -> void:
	movement_mode_ui.assign(player_movement_controller)
	terrain_edit_ui.assign(voxel_editor)
	edit_object_ui.assign(object_edit_controller)
	
	movement_mode_ui.enabled(false)
	terrain_edit_ui.enabled(false)
	edit_object_ui.enabled(false)
	
	tab_ui.activate_current()
	
	print("UI setup complete")
	
	return
	
	print(movement_mode_ui != null)
	print(terrain_edit_ui != null)
	print(edit_object_ui != null)
	print(save_and_load_ui != null)
	print(settings_ui != null)
	print(tab_ui != null)
