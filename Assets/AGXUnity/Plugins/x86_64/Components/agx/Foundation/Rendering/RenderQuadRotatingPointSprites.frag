#version 150

in vec3 v_lightDir;
in vec2 texCoord;
in vec4 v_color;
in vec4 v_rotation;
in float v_projectedPointSize;

uniform mat4 u_modelViewMatrix;

out vec4 o_FragData;

vec3 quat_rotate_vector( vec4 quat, vec3 vec )
{
  return vec + 2.0 * cross( cross( vec, quat.xyz ) + quat.w * vec, quat.xyz );
}

struct SphereCoordinate
{
  vec3 coord;
  bool outside;
};

// Convert 2d sprite coordinate [0, 1] to 3d normalized sprite coordinate [-1, 1]
SphereCoordinate calculate_half_sphere_coordinates( vec2 coord, bool backSide )
{
  SphereCoordinate result;
  result.outside = false;
  vec2 xy = coord * vec2(2.0, 2.0) - vec2(1.0, 1.0);
  result.coord.xy = xy;

  float mag = dot(xy, xy);

  if (mag > 1.0)
  {
    result.outside = true;
    return result;
  }

  result.coord.z = sqrt(1.0 - mag);
  
  if (backSide)
    result.coord.z = -result.coord.z;

  return result;
}

vec3 clamp_half_sphere_coordinate( vec3 coord )
{
  vec3 clamped;
  
  float len = sqrt(dot(coord.xy, coord.xy));
  clamped.xy = coord.xy / len;
  clamped.z = 0.0;
  
  return clamped;
}

vec3 half_sphere_to_local( vec3 coord )
{
  vec3 result = vec3(vec4(coord, 1.0) * u_modelViewMatrix);
  result = quat_rotate_vector(v_rotation, result);
  
  return result;
}

SphereCoordinate calculate_local_sphere_coordinates( vec2 coord, bool backSide )
{
  SphereCoordinate N = calculate_half_sphere_coordinates( coord, backSide );
  
  if (N.outside)
    N.coord = clamp_half_sphere_coordinate(N.coord);
  
  N.coord = half_sphere_to_local(N.coord);
  
  return N;
}


struct WeightedColor
{
  vec4 color;
  float weight;
};

WeightedColor calculate_color( vec2 coord )
{
  vec4 darkColor = vec4(v_color.xyz * 0.5, v_color.w);
  
#if ALPHA_SAMPLING
  SphereCoordinate frontCoord = calculate_local_sphere_coordinates( coord, false );
  SphereCoordinate backCoord = calculate_local_sphere_coordinates( coord, true );
  
  vec4 colorFront = frontCoord.coord.x > 0.0 ? v_color : darkColor;
  vec4 colorBack = backCoord.coord.x > 0.0 ? v_color : darkColor;

  float frontDistance = abs(frontCoord.coord.x);
  float backDistance = abs(backCoord.coord.x);
  
  // Ratio of front segment compared to full depth of sphere at current pixel
  float d = frontDistance / (frontDistance + backDistance);
  
  // Alpha scaling, eg alpha 0.99 should take more color from front segment than back segment
  // even if the depths are equal (eg center pixel). The scaling is rather arbitrary.s
  d = (0.5 + (d * colorFront.w * 0.5));
  
  vec4 color = colorFront * d + colorBack * (1.0 - d);
  
  if (frontCoord.outside)
    color.w = 0.0;
    // color = vec4(1.0, 0.0, 0.0, 1.0);
  
  
  return WeightedColor(color, abs(frontCoord.coord.x));
  // return WeightedColor(color, 0.25);
#else
  SphereCoordinate frontCoord = calculate_local_sphere_coordinates( coord, false );
  vec4 colorFront = frontCoord.coord.x > 0.0 ? v_color : darkColor;
  
  if (frontCoord.outside)
    colorFront.w = 0.0;
  
  return WeightedColor(colorFront, abs(frontCoord.coord.x));
#endif
}


vec4 calculate_multisample_color(WeightedColor corners[4])
{
  float totWeight = corners[0].weight + corners[1].weight + corners[2].weight + corners[3].weight;
  
  vec4 color =
  (
    corners[0].color * corners[0].weight / totWeight +
    corners[1].color * corners[1].weight / totWeight +
    corners[2].color * corners[2].weight / totWeight +
    corners[3].color * corners[3].weight / totWeight
  );

  return color;
}

vec4 calculate_permimeter_multisample_color(WeightedColor corners[4])
{
  vec4 color = (corners[0].color + corners[1].color + corners[2].color + corners[3].color) / 4.0;
  
#if !ALPHA_SAMPLING
  color.w = 1.0;
#endif
  
  return color;
}


void main()
{
  vec2 coord = texCoord;

  if (v_color.w == 0.0)
    discard;
  
  SphereCoordinate sphereCoord = calculate_half_sphere_coordinates( coord, false );

  if (sphereCoord.outside)
    discard; // kill pixels outside circle
  
  vec3 N = sphereCoord.coord;
  vec3 localN = half_sphere_to_local(N);
  
  vec4 color;
  
  float cornerDistanceMultiplier = 2.0;
  float cornerOffset = 1.0 / v_projectedPointSize * 0.5 * cornerDistanceMultiplier;

  bool colorEdgeSampling = abs(localN.x) <= cornerOffset * 3.0;
  bool perimeterSampling = sqrt(dot(N.xy, N.xy)) + cornerOffset * 4.0 > 1.0;

  // Sampling for antialiasing
  if (colorEdgeSampling || perimeterSampling)
  {
    vec2 corners[4];
    corners[0] = coord + vec2(-cornerOffset,  cornerOffset);
    corners[1] = coord + vec2(-cornerOffset, -cornerOffset);
    corners[2] = coord + vec2( cornerOffset, -cornerOffset);
    corners[3] = coord + vec2( cornerOffset,  cornerOffset);

    WeightedColor cornerColors[4];

    cornerColors[0] = calculate_color( corners[0] );
    cornerColors[1] = calculate_color( corners[1] );
    cornerColors[2] = calculate_color( corners[2] );
    cornerColors[3] = calculate_color( corners[3] );

  
    if (perimeterSampling)
      color = calculate_permimeter_multisample_color(cornerColors);
    else
      color = calculate_multisample_color(cornerColors);
      
    // color = vec4(1.0, 0.0, 0.0, 1.0);
  }
  else // Not near perimeter or half-sphere edge, no need for multisampling
  {
    color = calculate_color(coord).color;
  }  

  /**
  Special handling of small particles. Otherwise they dissapear as camera zoom out
  because all corners samples are outside sphere and thus total alpha becomes zero.
  */
  if (v_projectedPointSize < 3.0)
  {
    // Sample center of sphere
    vec4 color2 = calculate_color(vec2(0.5, 0.5)).color;
    
    // Blend with default sampling to get smooth transition
    float d = v_projectedPointSize / 3.0;
    color = color * d + color2 * (1.0 - d);
  }
  
#if ALPHA_SAMPLING
  // Scale alpha from sphere depth at current framgment
  float maxAlpha = color.w;
  float minAlpha = maxAlpha > 0.95 ? maxAlpha : maxAlpha * 0.5; // This is arbitrary scaling
  float alpha = minAlpha + (maxAlpha - minAlpha) * N.z;
#else
  float alpha = color.w;
#endif
  
  // calculate lighting
  float diffuse = max(0.0, dot(v_lightDir, N));
  diffuse = 0.2 + diffuse * 0.8;
  // float alpha = v_color.w * N.z; // Scale alpha from sphere depth at current framgment
  o_FragData = color * diffuse;
  o_FragData.w = alpha;
}
