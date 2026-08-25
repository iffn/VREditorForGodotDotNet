extends Node

class_name ElementLinker

# Usin Onready, since XRToolsViewport2DIn3D breaks @export assigned in prefab scene
# https://github.com/GodotVR/godot-xr-tools/issues/889
@onready var movement_mode_controller : MovementUI = $"Vertical arrangement/Movement"
@onready var terrain_edit_controller : VoxelUI = $"Vertical arrangement/Terrain editing"
@onready var edit_object_controller : EditObjectController = $"Vertical arrangement/Edit objects"
@onready var save_and_load_controller : SaveAndLoadUI = $"Vertical arrangement/Save and load"
@onready var settings_controller : SettingsUI = $"Vertical arrangement/Settings"
@onready var tab_controller : TabUI = $"Vertical arrangement/Tabs"

@export var player_movement_controller : PlayerMovementController
@export var voxel_editor : VoxelEditor
@export var object_edit_controller : ObjectEditController

func _ready() -> void:
	movement_mode_controller.assign(player_movement_controller)
	terrain_edit_controller.assign(voxel_editor)
	edit_object_controller.assign(object_edit_controller)
	print("UI setup complete")
	
	return
	print(movement_mode_controller != null)
	print(terrain_edit_controller != null)
	print(edit_object_controller != null)
	print(save_and_load_controller != null)
	print(settings_controller != null)
	print(tab_controller != null)
