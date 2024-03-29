void GenerateBoxQuadIndices(__global uint *indices24, uint vertexOffset)
{
  /* Bottom */
  indices24[0] = vertexOffset + 0;
  indices24[1] = vertexOffset + 1;
  indices24[2] = vertexOffset + 2;
  indices24[3] = vertexOffset + 3;
  
  /* Front */
  indices24 += 4;
  indices24[0] = vertexOffset + 0;
  indices24[1] = vertexOffset + 1;
  indices24[2] = vertexOffset + 5;
  indices24[3] = vertexOffset + 4;
  
  /* Right */
  indices24 += 4;
  indices24[0] = vertexOffset + 1;
  indices24[1] = vertexOffset + 2;
  indices24[2] = vertexOffset + 6;
  indices24[3] = vertexOffset + 5;
  
  /* Back */
  indices24 += 4;
  indices24[0] = vertexOffset + 2;
  indices24[1] = vertexOffset + 3;
  indices24[2] = vertexOffset + 7;
  indices24[3] = vertexOffset + 6;
  
  /* Left */
  indices24 += 4;
  indices24[0] = vertexOffset + 3;
  indices24[1] = vertexOffset + 0;
  indices24[2] = vertexOffset + 4;
  indices24[3] = vertexOffset + 7;
  
  /* Top */
  indices24 += 4;
  indices24[0] = vertexOffset + 4;
  indices24[1] = vertexOffset + 5;
  indices24[2] = vertexOffset + 6;
  indices24[3] = vertexOffset + 7;
}