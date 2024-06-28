#version 150

in vec3 v_lightDir;
in vec2 texCoord;
in vec4 v_color;

out vec4 o_FragData;

void main()
{
  if (v_color.w == 0.0)
    discard;

  vec3 N;

  N.xy = texCoord.xy*vec2(2.0,2.0) - vec2(1.0,1.0);

  float mag  = dot(N.xy, N.xy);
  if (mag > 1.0) discard;   // kill pixels outside circle
  N.z = sqrt(1.0-mag);

  // calculate lighting
  float diffuse = max(0.0, dot(v_lightDir, N));
  diffuse = 0.2 + diffuse*0.8;

  float alpha = v_color.w;
  // float alpha = v_color.w * N.z; // Scale alpha from sphere depth at current framgment
  o_FragData = v_color * diffuse;
  o_FragData.w = alpha;
}
