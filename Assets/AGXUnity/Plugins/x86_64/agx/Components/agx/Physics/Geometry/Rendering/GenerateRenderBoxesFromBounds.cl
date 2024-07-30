#include "GenerateBoxQuadIndices.cl"
#include <types.h>
#include <AffineMatrix4x4.h>


__kernel void GenerateRenderBoxesFromBounds(
uint numBoxes,
__global Bound4 *bounds,
__global Real4 *vertexBuffer,
__global Real4 *normalBuffer
)
{
  uint tid = get_global_id(0);
  if (tid >= numBoxes)
    return;

  Bound4 bound = bounds[tid];
  const Real4 min = bound.min;
  const Real4 max = bound.max;

  __global Real4 *vertices = &vertexBuffer[tid * 24];
  __global Real4 *normals = &normalBuffer[tid * 24];

  const Real4 V0 = (Real4)(min.x, min.y, min.z, 0);
  const Real4 V1 = (Real4)(max.x, min.y, min.z, 0);
  const Real4 V2 = (Real4)(max.x, max.y, min.z, 0);
  const Real4 V3 = (Real4)(min.x, max.y, min.z, 0);

  const Real4 V4 = (Real4)(min.x, min.y, max.z, 0);
  const Real4 V5 = (Real4)(max.x, min.y, max.z, 0);
  const Real4 V6 = (Real4)(max.x, max.y, max.z, 0);
  const Real4 V7 = (Real4)(min.x, max.y, max.z, 0);

  // Bottom
  vertices[0] = V0;
  vertices[1] = V1;
  vertices[2] = V2;
  vertices[3] = V3;


  for (int i = 0; i < 4; ++i)
    normals[i] = (Real4)(0, 0, -1, 0);


  // Front
  vertices[4] = V0;
  vertices[5] = V1;
  vertices[6] = V5;
  vertices[7] = V4;

  for (int i = 4; i < 8; ++i)
    normals[i] = (Real4)(0, -1, 0, 0);


  // Right
  vertices[8] = V1;
  vertices[9] = V2;
  vertices[10] = V6;
  vertices[11] = V5;

  for (int i = 8; i < 12; ++i)
    normals[i] = (Real4)(1, 0, 0, 0);


  // Back
  vertices[12] = V2;
  vertices[13] = V3;
  vertices[14] = V7;
  vertices[15] = V6;

  for (int i = 12; i < 16; ++i)
    normals[i] = (Real4)(0, 1, 0, 0);


  // Left
  vertices[16] = V3;
  vertices[17] = V0;
  vertices[18] = V4;
  vertices[19] = V7;

  for (int i = 16; i < 20; ++i)
    normals[i] = (Real4)(-1, 0, 0, 0);


  // Top
  vertices[20] = V4;
  vertices[21] = V5;
  vertices[22] = V6;
  vertices[23] = V7;

  for (int i = 20; i < 24; ++i)
    normals[i] = (Real4)(0, 0, 1, 0);
}
