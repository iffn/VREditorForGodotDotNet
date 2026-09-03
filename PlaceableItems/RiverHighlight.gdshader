shader_type spatial;
render_mode unshaded, cull_disabled;

uniform float outline_distance = 0.1;
uniform vec3 albedo : source_color;

void vertex() {
	VERTEX += sign(VERTEX) * outline_distance;
}

void fragment() {
	ALBEDO = albedo;
}
