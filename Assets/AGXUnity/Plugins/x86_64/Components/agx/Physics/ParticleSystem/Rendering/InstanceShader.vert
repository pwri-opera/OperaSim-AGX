attribute vec4 a_vertex;
attribute vec4 a_normal;
attribute vec4 a_color;
attribute vec4 a_position;
attribute vec4 a_radius;

uniform mat4 u_modelViewProjectionMatrix;
uniform mat4 u_modelViewMatrix;
uniform mat3 u_normalMatrix;

varying vec3 v_normal;
varying vec3 v_vertex;
varying vec4 v_color;
varying vec3 v_lightDir;

void main()
{	
  vec4 lightPos = vec4(50.0, -40.0, 70.0, 1.0);
  v_lightDir = vec3(u_modelViewMatrix * lightPos);

  vec4 localVertex = a_position + a_vertex * a_radius;
  localVertex.w = 1.0;

  v_vertex = vec3(u_modelViewMatrix * localVertex);
  v_normal = u_normalMatrix * a_normal.xyz; // TODO Do we need normalization?
  v_color = a_color;
  gl_Position = u_modelViewProjectionMatrix * localVertex;
}
