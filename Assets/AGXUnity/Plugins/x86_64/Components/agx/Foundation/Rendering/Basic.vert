attribute vec4 a_vertex;
attribute vec4 a_color;

uniform mat4 u_modelViewProjectionMatrix;

varying vec4 v_color;

void main()
{	
  gl_Position = u_modelViewProjectionMatrix * vec4(a_vertex.xyz, 1.0);
  // v_color = vec4(1,0,0,1);
  v_color = a_color;
  gl_PointSize = 10.0;
}
