using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using AGXUnity.Utils;
using GUI = AGXUnity.Utils.GUI;

public class TerrainScript : MonoBehaviour
{
    public float Terrain_size_x;
    public float Terrain_size_y;
    private float Terrain_size_z;
    public float Terrain_origin_x;
    public float Terrain_origin_y;
    public float Terrain_origin_z;

    // public ContactForce CF;
    // private Vector3 Wheel_pos;

    // Start is called before the first frame update
    void Start()
    {
        Terrain_size_z = Terrain_size_x;
        transform.position = new Vector3(Terrain_origin_x, Terrain_origin_y, Terrain_origin_z);

        // 1-1. オブジェクトを取得
        // Terrain terrain = GameObject.Find("Terrain").GetComponent<Terrain>();
        Terrain terrain = GetComponent<Terrain>();

        // 1-2. サイズを設定する
        terrain.terrainData.size = new Vector3(Terrain_size_x, Terrain_size_y, Terrain_size_z);

        // 2-2. ヘイトマップを取得する
        float[,] heightmap = terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapResolution, terrain.terrainData.heightmapResolution);


        // while文でTerrainの座標を設定
        // int j = 0;
        // while (j <= terrain.terrainData.heightmapResolution - 1)
        // {
        //     int i = 0;
        //     while (i <= terrain.terrainData.heightmapResolution - 1)
        //     {
        //         heightmap[j, i] = 0.0F;
        //         i += 1;
        //     }
        //     j += 1;
        // }
    }
    // Update is called once per frame
    void Update()
    {
        // Wheel_pos = CF.rb.transform.position;

        // Terrain_size_z = Terrain_size_x;
        // float Terrain_origin_x = -5;
        // float Terrain_origin_z = -Terrain_size_z / 2;

        // // 1-1. オブジェクトを取得
        // Terrain terrain = GameObject.Find("Terrain").GetComponent<Terrain>();

        // // 2-2. ヘイトマップを取得する
        // float[,] heightmap = terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapResolution, terrain.terrainData.heightmapResolution);

        // if ((Wheel_pos.x - Terrain_origin_x) <= Terrain_size_x && (Wheel_pos.z - Terrain_origin_z) <= Terrain_size_z && Wheel_pos.x >= Terrain_origin_x && Wheel_pos.z  >= Terrain_origin_z)
        // {

        //     // while文でTerrainの座標を設定
        //     int j = 0;
        //     while (j <= terrain.terrainData.heightmapResolution - 1)
        //     {
        //         int i = 0;
        //         while (i <= terrain.terrainData.heightmapResolution - 1)
        //         {
        //             heightmap[j, i] = 0.0F;
        //             i += 1;
        //         }
        //         j += 1;
        //     }

        //     //ある特定の位置の座標を取得する
        //     int x1 = (int)((Wheel_pos.x - Terrain_origin_x) * (terrain.terrainData.heightmapResolution - 1) / Terrain_size_x);
        //     int z1 = (int)((Wheel_pos.z - Terrain_origin_z) * (terrain.terrainData.heightmapResolution - 1) / Terrain_size_z);
        //     heightmap[z1, x1] = 0.2F;
        //     //heightmap[z1, x1] = 0.0F;

        //     // 2-4. ヘイトマップを設定する
        //     terrain.terrainData.SetHeightsDelayLOD(0, 0, heightmap);

        //     heightmap[z1, x1] = 0.0F;
        // }

    }
}