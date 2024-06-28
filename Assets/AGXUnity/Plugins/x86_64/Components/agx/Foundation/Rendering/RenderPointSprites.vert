uniform float projectionScaling;   // scale to calculate size in pixels
uniform float radiusScaling;

attribute vec4 a_position;
attribute vec4 a_color;
attribute float a_radius;
attribute float a_enableRendering;

uniform mat4 u_modelViewProjectionMatrix;
uniform mat4 u_modelViewMatrix;
uniform vec4 u_light0_pos;

// varying vec3 v_vertex;
varying vec3 v_lightDir;
varying vec4 v_color;
varying float v_radius;
varying float v_near;
varying float v_far;
varying float v_z;


void main()
{
  vec4 vertex = vec4(a_position.xyz, 1.0);

  // Find lightsource TODO Send as uniform/attribute
  vec3 posLight = vec3(u_modelViewMatrix * vec4(u_light0_pos.xyz, 1.0));
  // vec3 posLight = vec3(3.0, -30.0, 40.0);

  // calculate window-space point size
  vec3 posEye = vec3(u_modelViewMatrix * vertex);
  //float dist = length(posEye);
  v_lightDir = normalize(posLight - posEye);
  gl_PointSize = a_radius * (projectionScaling / (-posEye.z)) * radiusScaling;

#ifndef GL_ES
  gl_TexCoord[0] = gl_MultiTexCoord0;
  v_near = gl_ProjectionMatrix[3][2] / (gl_ProjectionMatrix[2][2] - 1.0);
  v_far = gl_ProjectionMatrix[3][2] / (1.0 + gl_ProjectionMatrix[2][2]);
  v_z = posEye.z;
#endif

  gl_Position = u_modelViewProjectionMatrix * vertex;

  v_radius = a_radius;
  v_color = a_color;
  v_color[3] = a_enableRendering > 0.0 ? v_color[3] : 0.0;
}
