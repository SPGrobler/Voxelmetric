﻿using Voxelmetric.Code.Configurable.Blocks.Utilities;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources.Textures;
using Voxelmetric.Code.Rendering;

public class CubeBlock : SolidBlock {

    public TextureCollection[] textures { get { return ((CubeBlockConfig)config).textures; } }

    public override void BuildFace(Chunk chunk, Vector3Int localPos, Direction direction)
    {
        VertexData[] vertexData = chunk.pools.PopVertexDataArray(4);
        VertexDataFixed[] vertexDataFixed = chunk.pools.PopVertexDataFixedArray(4);
        {
            for (int i = 0; i<4; i++)
                vertexData[i] = chunk.pools.PopVertexData();

            BlockUtils.PrepareVertices(localPos, vertexData, direction);
            BlockUtils.PrepareTexture(chunk, localPos, vertexData, direction, textures);
            BlockUtils.PrepareColors(chunk, localPos, vertexData, direction);

            for (int i = 0; i < 4; i++)
                vertexDataFixed[i] = VertexDataUtils.ClassToStruct(vertexData[i]);
            chunk.GeometryHandler.Batcher.AddFace(vertexDataFixed);

            for (int i = 0; i < 4; i++)
                chunk.pools.PushVertexData(vertexData[i]);
        }
        chunk.pools.PushVertexDataFixedArray(vertexDataFixed);
        chunk.pools.PushVertexDataArray(vertexData);
    }
}
