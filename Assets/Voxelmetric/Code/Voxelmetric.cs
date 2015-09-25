﻿using UnityEngine;
using System.Threading;
using System.Collections.Generic;

public static class Voxelmetric
{
    //Used as a manager class with references to classes treated like singletons
    public static VoxelmetricResources resources = new VoxelmetricResources();

    public static GameObject CreateGameObjectBlock(Block original, Vector3 position, Quaternion rotation)
    {
        BlockPos blockPos = new BlockPos();

        if (original == Block.Air)
            return null;

        EmptyChunk chunk = World.instance.GetComponent<EmptyChunk>();
        if (chunk == null)
        {
            chunk = (EmptyChunk)World.instance.gameObject.AddComponent(typeof(EmptyChunk));
            chunk.world = World.instance;
        }

        original.controller.OnCreate(chunk, blockPos - blockPos.ContainingChunkCoordinates(), original);

        return GOFromBlock(original, blockPos, position, rotation, chunk);
    }

    public static GameObject CreateGameObjectBlock(BlockPos blockPos, Vector3 position, Quaternion rotation)
    {
        Block original = GetBlock(blockPos);
        if (original == Block.Air)
            return null;

        EmptyChunk chunk = World.instance.GetComponent<EmptyChunk>();
        if (chunk == null)
        {
            chunk = (EmptyChunk)World.instance.gameObject.AddComponent(typeof(EmptyChunk));
            chunk.world = World.instance;
        }

        original.controller.OnCreate(chunk, blockPos - blockPos.ContainingChunkCoordinates(), original);

        return GOFromBlock(original, blockPos, position, rotation, chunk);
    }

    static GameObject GOFromBlock(Block original, BlockPos blockPos, Vector3 position, Quaternion rotation, Chunk chunk)
    {
        GameObject go = (GameObject)GameObject.Instantiate(Resources.Load<GameObject>(Config.Directories.PrefabsFolder + "/Block"), position, rotation);
        go.transform.localScale = new Vector3(Config.Env.BlockSize, Config.Env.BlockSize, Config.Env.BlockSize);

        MeshData meshData = new MeshData();

        original.controller.AddBlockData(chunk, blockPos, blockPos, meshData, original);
        for (int i = 0; i < meshData.vertices.Count; i++)
        {
            meshData.vertices[i] -= (Vector3)blockPos;
        }

        Mesh mesh = new Mesh();
        mesh.vertices = meshData.vertices.ToArray();
        mesh.triangles = meshData.triangles.ToArray();

        mesh.colors = meshData.colors.ToArray();

        mesh.uv = meshData.uv.ToArray();
        mesh.RecalculateNormals();

        go.GetComponent<Renderer>().material.mainTexture = Voxelmetric.resources.textureIndex.atlas;
        go.GetComponent<MeshFilter>().mesh = mesh;

        return go;
    }

    public static BlockPos GetBlockPos(RaycastHit hit, bool adjacent = false)
    {
        Vector3 pos = new Vector3(
            MoveWithinBlock(hit.point.x, hit.normal.x, adjacent),
            MoveWithinBlock(hit.point.y, hit.normal.y, adjacent),
            MoveWithinBlock(hit.point.z, hit.normal.z, adjacent)
            );

        return pos;
    }

    static float MoveWithinBlock(float pos, float norm, bool adjacent = false)
    {
        float minHalfBlock = Config.Env.BlockSize / 2 - 0.01f;
        float maxHalfBlock = Config.Env.BlockSize / 2 + 0.01f;
        //Because of float imprecision we can't guarantee a hit on the side of a

        //Get the distance of this position from the nearest block center
        //accounting for the size of the block
        float offset = pos - ((int)(pos/Config.Env.BlockSize) * Config.Env.BlockSize);
        if ((offset > minHalfBlock && offset < maxHalfBlock) || (offset < -minHalfBlock && offset > -maxHalfBlock))
        {
            if (adjacent)
            {
                pos += (norm / 2 * Config.Env.BlockSize);
            }
            else
            {
                pos -= (norm / 2 * Config.Env.BlockSize);
            }
        }

        return pos;
    }

    public static bool SetBlock(RaycastHit hit, Block block, bool adjacent = false)
    {
        Chunk chunk = hit.collider.GetComponent<Chunk>();

        if (chunk == null)
            return false;

        BlockPos pos = GetBlockPos(hit, adjacent);
        chunk.world.SetBlock(pos, block, !Config.Toggle.BlockLighting);

        if (Config.Toggle.BlockLighting)
        {
            BlockLight.LightArea(chunk.world, pos);
        }

        return true;
    }

    public static bool SetBlock(BlockPos pos, Block block, World world = null)
    {
        if (!world)
            world = World.instance;

        Chunk chunk = world.GetChunk(pos);
        if (chunk == null)
            return false;

        chunk.world.SetBlock(pos, block, !Config.Toggle.BlockLighting);

        if (Config.Toggle.BlockLighting)
        {
            BlockLight.LightArea(world, pos);
        }

        return true;
    }

    public static Block GetBlock(RaycastHit hit)
    {
        Chunk chunk = hit.collider.GetComponent<Chunk>();
        if (chunk == null)
            return Block.Air;

        BlockPos pos = GetBlockPos(hit, false);

        return GetBlock(pos, chunk.world);
    }

    public static Block GetBlock(BlockPos pos, World world = null)
    {
        if (!world)
            world = World.instance;

        Block block = world.GetBlock(pos);

        return block;
    }

    /// <summary>
    /// Saves all chunks currently loaded, if UseMultiThreading is enabled it saves the chunks
    ///  asynchronously and the SaveProgress object returned will show the progress
    /// </summary>
    /// <param name="world">Optional parameter for the world to save chunks for, if left
    /// empty it will use the world Singleton instead</param>
    /// <returns>A SaveProgress object to monitor the save.</returns>
    public static SaveProgress SaveAll(World world = null)
    {
        if (!world)
            world = World.instance;

        //Create a saveprogress object with positions of all the chunks in the world
        //Then save each chunk and update the saveprogress's percentage for each save
        SaveProgress saveProgress = new SaveProgress(world.chunks.Keys);
        List<Chunk> chunksToSave = new List<Chunk>();
        chunksToSave.AddRange(world.chunks.Values);

        if (Config.Toggle.UseMultiThreading)
        {
            Thread thread = new Thread(() =>
           {

               foreach (var chunk in chunksToSave)
               {

                   while (!chunk.GetFlag(Chunk.Flag.terrainGenerated) || chunk.GetFlag(Chunk.Flag.busy))
                   {
                       Thread.Sleep(0);
                   }

                   Serialization.SaveChunk(chunk);

                   saveProgress.SaveCompleteForChunk(chunk.pos);
               }
           });
            thread.Start();
        }
        else
        {
            foreach (var chunk in chunksToSave)
            {
                Serialization.SaveChunk(chunk);
                saveProgress.SaveCompleteForChunk(chunk.pos);
            }
        }

        return saveProgress;
    }
}