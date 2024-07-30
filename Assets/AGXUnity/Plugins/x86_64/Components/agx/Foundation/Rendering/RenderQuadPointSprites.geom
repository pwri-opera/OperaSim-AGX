#version 150

uniform mat4 u_modelViewMatrix;
uniform mat4 u_projection;

in vec3 v_lightDirVs[];
in vec4 v_colorVs[];

const vec2 corners[4] = vec2[]( vec2(0.0, 1.0), vec2(0.0, 0.0), vec2(1.0, 1.0), vec2(1.0, 0.0) );

layout(points) in;
layout(triangle_strip, max_vertices = 4) out;

out vec2 texCoord;
out vec3 v_lightDir;
out vec4 v_color;
in float pointSize[];

void main()
{  
    for(int i=0; i<4; ++i)
    {
        vec4 eyePos = gl_in[0].gl_Position;           //start with point position
        eyePos.xy += pointSize[0] * (corners[i] - vec2(0.5)); //add corner position
        gl_Position = u_projection * eyePos;                          //complete transformation
        texCoord = corners[i];                         //use corner as texCoord
        v_lightDir = v_lightDirVs[0];
        v_color = v_colorVs[0];
        EmitVertex();
    }
}
