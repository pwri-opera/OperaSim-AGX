#version 150

uniform mat4 u_modelViewMatrix;
uniform mat4 u_projection;
uniform float projectionScaling;

in vec3 v_lightDirVs[];
in vec4 v_rotationVs[];
in vec4 v_colorVs[];
in float v_pointSizeVs[];

const vec2 corners[4] = vec2[]( vec2(0.0, 1.0), vec2(0.0, 0.0), vec2(1.0, 1.0), vec2(1.0, 0.0) );

layout(points) in;
layout(triangle_strip, max_vertices = 4) out;

out vec2 texCoord;
out vec3 v_lightDir;
out vec4 v_color;
out vec4 v_rotation;
out float v_projectedPointSize;
out float v_radiusVs;

void main()
{  
    for(int i=0; i<4; ++i)
    {
        vec4 eyePos = gl_in[0].gl_Position;           //start with point position
        eyePos.xy += v_pointSizeVs[0] * (corners[i] - vec2(0.5)); //add corner position
        gl_Position = u_projection * eyePos;                          //complete transformation
        texCoord = corners[i];                         //use corner as texCoord
        v_lightDir = v_lightDirVs[0];
        v_color = v_colorVs[0];
        v_rotation = v_rotationVs[0];

        // We need the projected point size to determine sampling in the fragment shader
        v_projectedPointSize =  v_pointSizeVs[0] * ( projectionScaling / -eyePos.z);

        EmitVertex();
    }
}
