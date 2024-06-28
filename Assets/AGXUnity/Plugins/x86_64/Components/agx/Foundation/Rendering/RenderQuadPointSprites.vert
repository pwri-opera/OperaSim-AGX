#version 150

in vec4 a_position;
in vec4 a_color;
in float a_radius;
in float a_enableRendering;

uniform float radiusScaling;
uniform mat4 u_modelViewMatrix;
uniform vec4 u_light0_pos;

out float pointSize;
out vec3 v_lightDirVs;
out vec4 v_colorVs;

void main()
{
  vec4 vertex = vec4(a_position.xyz, 1.0);
  vec3 posLight = vec3(u_modelViewMatrix * vec4(u_light0_pos.xyz, 1.0));
  vec3 posEye = vec3(u_modelViewMatrix * vertex);
  v_lightDirVs = normalize(posLight - posEye);
  pointSize = a_radius * radiusScaling;
  gl_Position = u_modelViewMatrix * vertex;
  v_colorVs = a_color;
  v_colorVs[3] = a_enableRendering > 0.0 ? v_colorVs[3] : 0.0;
}
