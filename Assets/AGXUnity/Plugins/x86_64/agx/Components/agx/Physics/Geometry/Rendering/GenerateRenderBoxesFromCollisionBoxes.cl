#include "GenerateBoxQuadIndices.cl"
#include <types.h>
#include <AffineMatrix4x4.h>


__kernel void GenerateRenderBoxesFromCollisionBoxes(
uint numBoxes,
__global AffineMatrix4x4 *transforms,
__global Real4 *halfExtentsBuffer,
__global Real4 *vertexBuffer,
__global Real4 *normalBuffer
)
{
  uint tid = get_global_id(0);
  if (tid >= numBoxes)
    return;

  AffineMatrix4x4 transform = transforms[tid];
  Real4 halfExtents = halfExtentsBuffer[tid];
  Real4 min = -halfExtents;
  Real4 max = halfExtents;

  __global Real4 *vertices = &vertexBuffer[tid * 36];
  __global Real4 *normals = &normalBuffer[tid * 36];

  const Real4 V0 = (Real4)(min.x, min.y, min.z, 0);
  const Real4 V1 = (Real4)(max.x, min.y, min.z, 0);
  const Real4 V2 = (Real4)(max.x, max.y, min.z, 0);
  const Real4 V3 = (Real4)(min.x, max.y, min.z, 0);

  const Real4 V4 = (Real4)(min.x, min.y, max.z, 0);
  const Real4 V5 = (Real4)(max.x, min.y, max.z, 0);
  const Real4 V6 = (Real4)(max.x, max.y, max.z, 0);
  const Real4 V7 = (Real4)(min.x, max.y, max.z, 0);

  // Bottom
  vertices[0] = AffineMatrix4x4_preMult3(&transform, &V0);
  vertices[1] = AffineMatrix4x4_preMult3(&transform, &V1);
  vertices[2] = AffineMatrix4x4_preMult3(&transform, &V2);
  
  vertices[3] = AffineMatrix4x4_preMult3(&transform, &V2);
  vertices[4] = AffineMatrix4x4_preMult3(&transform, &V3);
  vertices[5] = AffineMatrix4x4_preMult3(&transform, &V0);


  Real4 normal;

  normal = (Real4)(0, 0, -1, 0);
  for (int i = 0; i < 6; ++i)
    normals[i] = AffineMatrix4x4_transform3x3(&transform, &normal);


  // Front
  vertices[6] = AffineMatrix4x4_preMult3(&transform, &V0);
  vertices[7] = AffineMatrix4x4_preMult3(&transform, &V1);
  vertices[8] = AffineMatrix4x4_preMult3(&transform, &V5);

  vertices[9] = AffineMatrix4x4_preMult3(&transform, &V5);
  vertices[10] = AffineMatrix4x4_preMult3(&transform, &V4);
  vertices[11] = AffineMatrix4x4_preMult3(&transform, &V0);

  normal = (Real4)(0, -1, 0, 0);
  for (int i = 6; i < 12; ++i)
    normals[i] = AffineMatrix4x4_transform3x3(&transform, &normal);


  // Right
  vertices[12] = AffineMatrix4x4_preMult3(&transform, &V1);
  vertices[13] = AffineMatrix4x4_preMult3(&transform, &V2);
  vertices[14] = AffineMatrix4x4_preMult3(&transform, &V6);

  vertices[15] = AffineMatrix4x4_preMult3(&transform, &V6);
  vertices[16] = AffineMatrix4x4_preMult3(&transform, &V5);
  vertices[17] = AffineMatrix4x4_preMult3(&transform, &V1);

  normal = (Real4)(1, 0, 0, 0);
  for (int i = 12; i < 18; ++i)
    normals[i] = AffineMatrix4x4_transform3x3(&transform, &normal);


  // Back
  vertices[18] = AffineMatrix4x4_preMult3(&transform, &V2);
  vertices[19] = AffineMatrix4x4_preMult3(&transform, &V3);
  vertices[20] = AffineMatrix4x4_preMult3(&transform, &V7);
  
  vertices[21] = AffineMatrix4x4_preMult3(&transform, &V7);
  vertices[22] = AffineMatrix4x4_preMult3(&transform, &V6);
  vertices[23] = AffineMatrix4x4_preMult3(&transform, &V2);

  normal = (Real4)(0, 1, 0, 0);
  for (int i = 18; i < 24; ++i)
    normals[i] = AffineMatrix4x4_transform3x3(&transform, &normal);


  // Left
  vertices[24] = AffineMatrix4x4_preMult3(&transform, &V3);
  vertices[25] = AffineMatrix4x4_preMult3(&transform, &V0);
  vertices[26] = AffineMatrix4x4_preMult3(&transform, &V4);

  vertices[27] = AffineMatrix4x4_preMult3(&transform, &V4);
  vertices[28] = AffineMatrix4x4_preMult3(&transform, &V7);
  vertices[29] = AffineMatrix4x4_preMult3(&transform, &V3);

  normal = (Real4)(-1, 0, 0, 0);
  for (int i = 24; i < 30; ++i)
    normals[i] = AffineMatrix4x4_transform3x3(&transform, &normal);


  // Top
  vertices[30] = AffineMatrix4x4_preMult3(&transform, &V4);
  vertices[31] = AffineMatrix4x4_preMult3(&transform, &V5);
  vertices[32] = AffineMatrix4x4_preMult3(&transform, &V6);

  vertices[33] = AffineMatrix4x4_preMult3(&transform, &V6);
  vertices[34] = AffineMatrix4x4_preMult3(&transform, &V7);
  vertices[35] = AffineMatrix4x4_preMult3(&transform, &V4);

  normal = (Real4)(0, 0, 1, 0);
  for (int i = 30; i < 36; ++i)
    normals[i] = AffineMatrix4x4_transform3x3(&transform, &normal);
}
