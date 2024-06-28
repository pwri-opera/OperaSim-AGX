#ifdef GL_ES
// define default precision for float, vec, mat.
precision mediump float;
#endif

varying vec4 v_color;

void main()
{
  gl_FragColor = v_color;
}
