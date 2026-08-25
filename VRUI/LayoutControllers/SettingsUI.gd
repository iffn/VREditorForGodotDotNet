extends UIModeController

class_name SettingsUI

var player_side_coordinator: PlayerSideCoordinator

func assign(_player_side_coordinator: PlayerSideCoordinator):
	player_side_coordinator = _player_side_coordinator

func enabled(state : bool):
	if state:
		player_side_coordinator.interaction_state = PlayerSideCoordinator.interaction_states.none
