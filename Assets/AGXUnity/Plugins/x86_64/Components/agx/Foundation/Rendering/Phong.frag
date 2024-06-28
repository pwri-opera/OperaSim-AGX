#if 1
#ifdef GL_ES
precision mediump float;
#endif

varying vec3 v_vertex;
varying vec3 v_normal;
varying vec4 v_color;
varying vec3 v_lightDir;

void main (void)  
{
  #define MATERIAL_ALPHA 1.0
  vec4 materialAmbient = vec4(0.2, 0.2, 0.2, MATERIAL_ALPHA);
  vec4 materialDiffuse = v_color; //vec4(0.8, 0.8, 0.8, MATERIAL_ALPHA);
  vec4 materialSpecular = vec4(0.2, 0.2, 0.2, MATERIAL_ALPHA);
  float materialShininess = 40.0;

  
  vec4 lightAmbient = vec4(0.2, 0.2, 0.2, 1.0);
  vec4 lightDiffuse = vec4(0.8, 0.8, 0.8, 1.0);
  vec4 lightSpecular = vec4(0.7, 0.7, 0.4, 1.0);
  
  vec4 lightModelAmbient = vec4(0.2, 0.2, 0.2, 1.0);

  vec4 ambient = materialAmbient * (lightAmbient + lightModelAmbient);
  vec4 diffuse = materialDiffuse * lightDiffuse;
  vec4 specular = materialSpecular * lightSpecular;
  
  // Handle back facing triangles (needed for transparent triangles). NOTE: Artifacts in edge-cases, fix math
  // vec3 N = dot(v_normal, v_vertex) < 0.0 ? v_normal : -v_normal;
  vec3 N = v_normal;
  
  vec3 L = normalize(v_lightDir - v_vertex);
  vec3 E = normalize(-v_vertex); // we are in Eye Coordinates, so EyePos is (0,0,0)  
  vec3 R = normalize(-reflect(L,N));

  //calculate Ambient Term:  
  vec4 Iamb = ambient;    

  //calculate Diffuse Term:  
  vec4 Idiff = diffuse * max(dot(N,L), 0.0);
  Idiff = clamp(Idiff, 0.0, 1.0);     

  // calculate Specular Term:
  vec4 Ispec = specular * pow(max(dot(R,E),0.0), materialShininess);
  Ispec = clamp(Ispec, 0.0, 1.0); 

  // write Total Color:  
  gl_FragColor = Iamb + Idiff + Ispec;
}

#else
varying vec3 N;
varying vec3 v;    
void main (void)  
{  
   vec3 L = normalize(gl_LightSource[0].position.xyz - v);   
   vec3 E = normalize(-v); // we are in Eye Coordinates, so EyePos is (0,0,0)  
   vec3 R = normalize(-reflect(L,N));
   float shininess = 2.0;
 
   //calculate Ambient Term:  
   vec4 Iamb = gl_FrontLightProduct[0].ambient;    

   //calculate Diffuse Term:  
   vec4 Idiff = gl_FrontLightProduct[0].diffuse * max(dot(N,L), 0.0);
   Idiff = clamp(Idiff, 0.0, 1.0);     
   
   // calculate Specular Term:
   vec4 Ispec = gl_FrontLightProduct[0].specular * pow(max(dot(R,E),0.0), shininess);
    Ispec = vec4(0.0); //clamp(Ispec, 0.0, 1.0);
   
    
   // write Total Color:  
   gl_FragColor = gl_FrontLightModelProduct.sceneColor + Iamb + Idiff + Ispec;     
}
#endif