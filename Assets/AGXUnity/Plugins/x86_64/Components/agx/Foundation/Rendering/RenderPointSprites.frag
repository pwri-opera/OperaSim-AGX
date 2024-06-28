#ifdef GL_ES
precision mediump float;
#endif

varying vec3 v_lightDir;
varying vec4 v_color;
varying float v_radius;
varying float v_near;
varying float v_far;
varying float v_z;

void main()
{
  if (v_color.w == 0.0)
    discard;

  vec3 N;

#ifdef GL_ES
  N.xy = gl_PointCoord*vec2(2.0, 2.0) - vec2(1.0, 1.0);
#else
  N.xy = gl_TexCoord[0].xy*vec2(2.0, -2.0) + vec2(-1.0, 1.0);
#endif


  float mag = dot(N.xy, N.xy);
  if (mag > 1.0) discard;   // kill pixels outside circle
  N.z = sqrt(1.0-mag);

  // calculate lighting
  float diffuse = max(0.0, dot(v_lightDir, N));
  diffuse = 0.2 + diffuse*0.8;

  float alpha = v_color.w;
  // float alpha = v_color.w * N.z; // Scale alpha from sphere depth at current framgment
  gl_FragColor = v_color * diffuse;
  gl_FragColor.w = alpha;

#ifndef GL_ES
  // For OpenGL ES we need something like http://www.sunsetlakesoftware.com/2011/05/08/enhancing-molecules-using-opengl-es-20

  // Write z-values as if the sprite was an actual sphere
  // http://olivers.posterous.com/linear-depth-in-glsl-for-real
  float a = -(v_far +v_near) / (v_far -v_near);
  float b = -2.0*v_far*v_near / (v_far -v_near);
  float z = v_z + N.z * v_radius;
  float z_n = -(a*z + b) / z; // z_n in [-1, 1]
  gl_FragDepth = 0.5*z_n + 0.5; // z_b in [0, 1]
#endif
}
