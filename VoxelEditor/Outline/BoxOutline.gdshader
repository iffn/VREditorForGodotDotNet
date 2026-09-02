shader_type spatial;

render_mode cull_disabled, unshaded;

uniform float line_size = 0.1;

void vertex() {
	// Called for every vertex the material is visible on.
}

void fragment() {
	float other_line_size = 1.0 - line_size;
	
	float center = step(line_size, UV.x)
		* step(UV.x, other_line_size) 
		* step(line_size, UV.y) 
		* step(UV.y, other_line_size);
	
	if(center > 0.0)
		discard;
	
	ALBEDO = vec3(1.0, 0.0, 0.0);
}
