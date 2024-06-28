#if 1

attribute vec4 a_vertex;
attribute vec4 a_normal;
attribute vec4 a_color;

uniform mat4 u_modelViewProjectionMatrix;
uniform mat4 u_modelViewMatrix;
uniform mat3 u_normalMatrix;

varying vec3 v_normal;
varying vec3 v_vertex;
varying vec4 v_color;
varying vec3 v_lightDir;

void main()
{	
  vec4 lightPos = vec4(5.0, 4.0, 7.0, 1.0);
  v_lightDir = vec3(u_modelViewMatrix * lightPos);

  v_vertex = vec3(u_modelViewMatrix * a_vertex);
  v_normal = u_normalMatrix * a_normal.xyz; // TODO Do we need normalization?
  v_color = a_color;
  gl_Position = u_modelViewProjectionMatrix * a_vertex;
}

#else
varying vec3 N;
varying vec3 v;
void main(void)  
{     
   v = vec3(gl_ModelViewMatrix * gl_Vertex);       
   N = normalize(gl_NormalMatrix * gl_Normal);
   gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;  
}
          
#endif