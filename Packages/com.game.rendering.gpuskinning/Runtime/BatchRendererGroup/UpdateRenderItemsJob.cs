using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;


// ... existing code ...

[BurstCompile]
public struct UpdateRenderItemsJob : IJobParallelFor
{
    [NativeDisableParallelForRestriction] public NativeArray<float4> sysmemBuffer;
    [ReadOnly] public NativeList<UpdateRendererItem> items;
    public int maxInstancePerWindow;
    public int windowSizeInFloat4;
    public bool updateAll;


    public void Execute(int index)
    {
        var item = items[index].item;
        var itemIndex = items[index].index;

        int windowId = Math.DivRem(itemIndex, maxInstancePerWindow, out int i);
        int windowOffsetInFloat4 = windowId * windowSizeInFloat4;

        float scale = item.scale;
        float3 position = item.position;

        float3x3 rotation = scale * new float3x3(item.rotation);
        int offset = windowOffsetInFloat4 + i * 3;

        sysmemBuffer[offset] = new float4(rotation.c0.x, rotation.c0.y, rotation.c0.z, rotation.c1.x);
        sysmemBuffer[offset + 1] = new float4(rotation.c1.y, rotation.c1.z, rotation.c2.x, rotation.c2.y);
        sysmemBuffer[offset + 2] = new float4(rotation.c2.z, position.x, position.y, position.z);

        if (updateAll)
        {
            offset = windowOffsetInFloat4 + maxInstancePerWindow * 3 + i * 3;
            sysmemBuffer[offset] = new float4(1, 0, 0, 0);
            sysmemBuffer[offset + 1] = new float4(1, 0, 0, 0);
            sysmemBuffer[offset + 2] = new float4(1, 0, 0, 0);

            sysmemBuffer[windowOffsetInFloat4 + maxInstancePerWindow * 6 + i] = item.color;
        }

        sysmemBuffer[windowOffsetInFloat4 + maxInstancePerWindow * 7 + i] = item.gpuskinParam1;
        sysmemBuffer[windowOffsetInFloat4 + maxInstancePerWindow * 8 + i] = item.gpuskinParam2;
    }


// // Add a new method to handle batch updates
//     public JobHandle UpdateRenderItemsBatch(int startIndex, int count, bool updateAll = false, JobHandle dependsOn = default)
//     {
//         var job = new UpdateRenderItemsJob
//         {
//             sysmemBuffer = m_sysmemBuffer,
//             items = m_RendererItems,
//             maxInstancePerWindow = m_maxInstancePerWindow,
//             windowSizeInFloat4 = _windowSizeInFloat4,
//             updateAll = updateAll
//         };
//
//         return job.Schedule(count, 64, dependsOn);
//     }
}


public struct UpdateRendererItem
{
    public int index;
    public BatchRendererGroupContainer.RendererItem item;
}