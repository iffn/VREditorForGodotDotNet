extends Node

class_name PlayerMovementController

@export var body : XRToolsPlayerBody
@export var rpg_movement_provider : XRToolsMovementProvider
@export var ghost_movement_provider : XRToolsMovementProvider
@export var scaler : XRToolsMovementScalingGhost

@export_flags_3d_physics var rgp_collisions = 1023
@export_flags_3d_physics var ghost_collisions = 0

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
