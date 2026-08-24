extends Node

class_name PlayerSideCoordinator

@export var player : XROrigin3D
@export var body : XRToolsPlayerBody
@export var scaler : XRToolsMovementScalingGhost
@export var hand_menu : XRToolsViewport2DIn3D
@export var rpg_movement_provider : XRToolsMovementProvider
@export var ghost_movement_provider : XRToolsMovementProvider
@export var voxel_editor : VoxelEditor
@export var interaction_point : Node3D
@export var sphere_edit_shape : Node3D
@export var interaction_visualizer : Node3D

@export_flags_3d_physics var rgp_collisions
@export_flags_3d_physics var ghost_collisions

var ui : ElementLinker

var right_is_primary := true

func _ready():
	await get_tree().process_frame
	ui = hand_menu.get_scene_instance() as ElementLinker
	
	if not ui.is_node_ready():
		await ui.ready
	
	ui.assign(self)

enum interaction_states {
	painting,
	interacting,
	none
}

var interaction_state: interaction_states:
	set(new_state):
		voxel_editor.paintingActive = new_state == interaction_states.painting
		sphere_edit_shape.visible = new_state == interaction_states.painting
		interaction_visualizer.visible = new_state == interaction_states.interacting

var rpg_movement : bool:
	set(rpg_movement_active):
		var gost_movement_active := !rpg_movement_active
		
		rpg_movement_provider.enabled = rpg_movement_active
		scaler.enabled = gost_movement_active
		ghost_movement_provider.enabled = gost_movement_active
		
		if rpg_movement_active:
			body.collision_mask = rgp_collisions
		else:
			body.collision_mask = ghost_collisions
