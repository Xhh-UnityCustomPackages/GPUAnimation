using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

/*
    This class handle rendering of ground cells & debris using BRG.
    Both ground cells & debris could be rendered using the same GPU data layout:
        - obj2world matrix ( 3 * float4 )
        - world2obj matrix ( 3 * float4 )
        - color ( 1 * float4 )

    so 7 float4 per mesh.

    Do not forget data is stored in SoA
*/


public unsafe class BatchRendererGroupContainer
{
    // In GLES mode, BRG raw buffer is a constant buffer (UBO)
    private bool UseConstantBuffer => BatchRendererGroup.BufferTarget == BatchBufferTarget.ConstantBuffer;
    private ShadowCastingMode m_ShadowCastingMode;

    //  GPU item size ( 2 * 4x3 matrices plus 1 color per item ) 
    private const int kGpuItemSize = (3 * 2 + 1 + 2) * 16;

    // Some helper constants to make calculations more convenient.
    private const uint kSizeOfMatrix = sizeof(float) * 4 * 4;
    private const uint kSizeOfPackedMatrix = sizeof(float) * 4 * 3;
    private const int kSizeOfFloat4 = sizeof(float) * 4;
    private const uint kBytesPerInstance = (kSizeOfPackedMatrix * 2) + kSizeOfFloat4 * 3;
    private const uint kExtraBytes = kSizeOfMatrix * 2;


    private int m_maxInstances; // maximum item in this container
    private int m_instanceCount; // current item count
    private int m_alignedGPUWindowSize; // BRG raw window size
    private int m_maxInstancePerWindow; // max instance per window (
    private int _windowSizeInFloat4;
    private int m_windowCount; // amount of window (1 in SSBO mode, n in UBO mode)
    private int m_totalGpuBufferSize; // total size of the raw buffer
    private NativeArray<float4> m_sysmemBuffer; // system memory copy of the raw buffer
    private bool m_initialized;
    private BatchID[] m_batchIDs; // one batchID per window
    private BatchMaterialID m_materialID;
    private BatchMeshID m_meshID;
    private BatchRendererGroup m_BatchRendererGroup; // BRG object
    private GraphicsBuffer m_GPUPersistentInstanceData; // GPU raw buffer (could be SSBO or UBO)

    [StructLayout(LayoutKind.Sequential)]
    public struct RendererItem
    {
        public float3 position;
        public float scale;
        public quaternion rotation;
        public float4 color;
        public float4 gpuskinParam1;
        public float4 gpuskinParam2;
    };

    public NativeArray<RendererItem> m_RendererItems;

    private int m_RefCount;
    public int refCount => m_RefCount;

    public void AddRef()
    {
        m_RefCount++;
    }

    public void RemoveRef()
    {
        m_RefCount--;
    }


    // Create a BRG object and allocate buffers. 
    public bool Init(Mesh mesh, Material mat, int maxInstances, ShadowCastingMode shadowCastingMode)
    {
        // Create the BRG object, specifying our BRG callback
        m_BatchRendererGroup = new BatchRendererGroup(this.OnPerformCulling, IntPtr.Zero);

        m_instanceCount = 0;
        m_maxInstances = maxInstances;
        m_ShadowCastingMode = shadowCastingMode;

        // BRG uses a large GPU buffer. This is a RAW buffer on almost all platforms, and a constant buffer on GLES
        // In case of constant buffer, we split it into several "windows" of BatchRendererGroup.GetConstantBufferMaxWindowSize() bytes each
        if (UseConstantBuffer)
        {
            m_alignedGPUWindowSize = BatchRendererGroup.GetConstantBufferMaxWindowSize();
            m_maxInstancePerWindow = m_alignedGPUWindowSize / kGpuItemSize;
            m_windowCount = (m_maxInstances + m_maxInstancePerWindow - 1) / m_maxInstancePerWindow;
            m_totalGpuBufferSize = m_windowCount * m_alignedGPUWindowSize;
            m_GPUPersistentInstanceData = new GraphicsBuffer(GraphicsBuffer.Target.Constant, m_totalGpuBufferSize / 16, 16);
        }
        else
        {
            m_alignedGPUWindowSize = (m_maxInstances * kGpuItemSize + 15) & (-16);
            m_maxInstancePerWindow = maxInstances;
            m_windowCount = 1;
            m_totalGpuBufferSize = m_windowCount * m_alignedGPUWindowSize;
            m_GPUPersistentInstanceData = new GraphicsBuffer(GraphicsBuffer.Target.Raw, m_totalGpuBufferSize / 4, 4);
        }


        _windowSizeInFloat4 = m_alignedGPUWindowSize / 16;

        // In our sample game we're dealing with 3 instanced properties: obj2world, world2obj and baseColor
        var batchMetadata = new NativeArray<MetadataValue>(5, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

        // Batch metadata buffer
        int objectToWorldID = Shader.PropertyToID("unity_ObjectToWorld");
        int worldToObjectID = Shader.PropertyToID("unity_WorldToObject");
        int colorID = Shader.PropertyToID("_BaseColor");
        int PixelSegmentationID = Shader.PropertyToID("_GPUSkinning_FrameIndex_PixelSegmentation");
        int PixelSegmentation_Blend_CrossFadeID = Shader.PropertyToID("_GPUSkinning_FrameIndex_PixelSegmentation_Blend_CrossFade");

        //这是额外添加的

        // Create system memory copy of big GPU raw buffer
        // 看看需要分配多少个float4 大小
        m_sysmemBuffer = new NativeArray<float4>(m_totalGpuBufferSize / 16, Allocator.TempJob, NativeArrayOptions.ClearMemory);

        //数据是这样排列的
        // 0   * InstanceCount  unity_ObjectToWorld                     float3x4
        // 48  * InstanceCount  unity_WorldToObject                     float3x4
        // 96  * InstanceCount  _BaseColor                              float4  
        // 112 * InstanceCount  _PixelSegmentation                      float4
        // 128 * InstanceCount  _PixelSegmentation_Blend_CrossFade      float4


        //梳理一下内存排布
        uint byteAddressObjectToWorld = 0;
        uint byteAddressWorldToObject = (uint)(m_maxInstancePerWindow * 3 * 16);
        uint byteAddressColor = (uint)(m_maxInstancePerWindow * 3 * 2 * 16);
        uint byteAddressPixelSegmentation = (uint)(m_maxInstancePerWindow * (3 * 2 + 1) * 16);
        uint byteAddressPixelSegmentationBlend = (uint)(m_maxInstancePerWindow * (3 * 2 + 2) * 16);

        // register one kind of batch per "window" in the large BRG raw buffer
        m_batchIDs = new BatchID[m_windowCount];
        for (int b = 0; b < m_windowCount; b++)
        {
            batchMetadata[0] = CreateMetadataValue(objectToWorldID, 0); // matrices
            batchMetadata[1] = CreateMetadataValue(worldToObjectID, byteAddressWorldToObject); // inverse matrices
            batchMetadata[2] = CreateMetadataValue(colorID, byteAddressColor); // colors

            //GPUSkinning
            batchMetadata[3] = CreateMetadataValue(PixelSegmentationID, byteAddressPixelSegmentation); // colors 
            batchMetadata[4] = CreateMetadataValue(PixelSegmentation_Blend_CrossFadeID, byteAddressPixelSegmentationBlend); // colors

            int offset = b * m_alignedGPUWindowSize;
            m_batchIDs[b] = m_BatchRendererGroup.AddBatch(batchMetadata, m_GPUPersistentInstanceData.bufferHandle, (uint)offset, UseConstantBuffer ? (uint)m_alignedGPUWindowSize : 0);
        }

        // we don't need this metadata description array anymore
        batchMetadata.Dispose();

        //TODO 修复剔除系统
        // Setup very large bound to be sure BRG is never culled
        UnityEngine.Bounds bounds = new Bounds(new Vector3(0, 0, 0), new Vector3(1048576.0f, 1048576.0f, 1048576.0f));
        m_BatchRendererGroup.SetGlobalBounds(bounds);

        // Register mesh and material
        m_meshID = m_BatchRendererGroup.RegisterMesh(mesh);
        m_materialID = m_BatchRendererGroup.RegisterMaterial(mat);

        // setup positions & scale of each background elements
        m_RendererItems = new NativeArray<RendererItem>(maxInstances, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        m_initialized = true;
        return true;
    }

    // Raw buffers are allocated in ints. This is a utility method that calculates
    // the required number of ints for the data.
    int BufferCountForInstances(int bytesPerInstance, int numInstances, int extraBytes = 0)
    {
        // Round byte counts to int multiples
        bytesPerInstance = (bytesPerInstance + sizeof(int) - 1) / sizeof(int) * sizeof(int);
        extraBytes = (extraBytes + sizeof(int) - 1) / sizeof(int) * sizeof(int);
        int totalBytes = bytesPerInstance * numInstances + extraBytes;
        return totalBytes / sizeof(int);
    }

    private int m_TempInstanceCount;


    [BurstCompile]
    public int AddRenderItem(ref RendererItem item)
    {
        if ((uint)m_TempInstanceCount >= (uint)m_maxInstances)
            return -1;

        Profiler.BeginSample("BatchRendererGroupContainer.AddRenderItem");

        UpdateRenderItem(m_TempInstanceCount, ref item, true);

        Profiler.EndSample();

        return m_TempInstanceCount++;
    }

    private NativeList<UpdateRendererItem> _tempUpdateRendererItems;

    //数据需要并行写入
    [BurstCompile]
    public void UpdateRenderItemJob(int index, ref RendererItem item, bool updateAll = false)
    {
        Profiler.BeginSample("BatchRendererGroupContainer.UpdateRenderItemJob");
        if (!_tempUpdateRendererItems.IsCreated)
            _tempUpdateRendererItems = new(m_maxInstances, Allocator.TempJob);

        _tempUpdateRendererItems.Add(new UpdateRendererItem() { index = index, item = item });
        Profiler.EndSample();
    }

    public void DoUpdateRenderItemJob()
    {
        if (!_tempUpdateRendererItems.IsCreated || _tempUpdateRendererItems.Length <= 0)
            return;

        var job = new UpdateRenderItemsJob
        {
            sysmemBuffer = m_sysmemBuffer,
            items = _tempUpdateRendererItems,
            maxInstancePerWindow = m_maxInstancePerWindow,
            windowSizeInFloat4 = _windowSizeInFloat4,
            updateAll = false,
        };

        job.Schedule(_tempUpdateRendererItems.Length, 64).Complete();
        _tempUpdateRendererItems.Clear();
    }


    [BurstCompile]
    //数据需要并行写入
    public void UpdateRenderItem(int index, ref RendererItem item, bool updateAll = false)
    {
        Profiler.BeginSample("BatchRendererGroupContainer.UpdateRenderItem");
        m_RendererItems[index] = item;
        //求余
        int windowId = Math.DivRem(index, m_maxInstancePerWindow, out int i);
        int windowOffsetInFloat4 = windowId * _windowSizeInFloat4;


        float scale = item.scale;
        float3 position = item.position;

        // compute the new current frame matrix
        // m_sysmemBuffer[(windowOffsetInFloat4 + i * 3 + 0)] = new float4(scale, 0, 0, 0);
        // m_sysmemBuffer[(windowOffsetInFloat4 + i * 3 + 1)] = new float4(scale, 0, 0, 0);
        // m_sysmemBuffer[(windowOffsetInFloat4 + i * 3 + 2)] = new float4(scale, position.x, position.y, position.z);

        // 使用矩阵直接操控BbjectToWorld
        // float4x4 trs = float4x4.TRS(item.position, item.rotation, item.scale);
        float3x3 rotation = scale * new float3x3(item.rotation); //scale左乘可以提升效率
        int offset = windowOffsetInFloat4 + i * 3 + 0;
        m_sysmemBuffer[offset] = new float4(rotation.c0.x, rotation.c0.y, rotation.c0.z, rotation.c1.x);
        m_sysmemBuffer[offset + 1] = new float4(rotation.c1.y, rotation.c1.z, rotation.c2.x, rotation.c2.y);
        m_sysmemBuffer[offset + 2] = new float4(rotation.c2.z, position.x, position.y, position.z);

        // compute the new inverse matrix (note: shortcut use identity because aligned cubes normals aren't affected by any non uniform scale
        //这部分不用一直更新
        if (updateAll)
        {
            offset = windowOffsetInFloat4 + m_maxInstancePerWindow * 3 * 1 + i * 3 + 0;
            m_sysmemBuffer[offset] = new float4(1, 0, 0, 0);
            m_sysmemBuffer[offset + 1] = new float4(1, 0, 0, 0);
            m_sysmemBuffer[offset + 2] = new float4(1, 0, 0, 0);
        }

        // update colors
        m_sysmemBuffer[windowOffsetInFloat4 + m_maxInstancePerWindow * 3 * 2 + i] = item.color;

        //GPUSkinning
        m_sysmemBuffer[windowOffsetInFloat4 + m_maxInstancePerWindow * (3 * 2 + 1) + i] = item.gpuskinParam1;
        m_sysmemBuffer[windowOffsetInFloat4 + m_maxInstancePerWindow * (3 * 2 + 2) + i] = item.gpuskinParam2;
        Profiler.EndSample();
    }


    public void LogData(int id)
    {
        int _maxInstancePerWindow = m_alignedGPUWindowSize / kGpuItemSize;
        int _windowSizeInFloat4 = kGpuItemSize / kSizeOfFloat4;

        int windowId = System.Math.DivRem(m_TempInstanceCount, _maxInstancePerWindow, out int i);
        int windowOffsetInFloat4 = windowId * _windowSizeInFloat4;
        i = id;
        Debug.LogError($"------LogStart:{id} total:{m_sysmemBuffer.Length}");
        // compute the new current frame matrix
        Debug.LogError($"{(windowOffsetInFloat4 + i * 3 + 0)}-" + m_sysmemBuffer[(windowOffsetInFloat4 + i * 3 + 0)]);
        Debug.LogError($"{(windowOffsetInFloat4 + i * 3 + 1)}-" + m_sysmemBuffer[(windowOffsetInFloat4 + i * 3 + 1)]);
        Debug.LogError($"{(windowOffsetInFloat4 + i * 3 + 2)}-" + m_sysmemBuffer[(windowOffsetInFloat4 + i * 3 + 2)]);

        // compute the new inverse matrix (note: shortcut use identity because aligned cubes normals aren't affected by any non uniform scale
        Debug.LogError($"{(windowOffsetInFloat4 + _maxInstancePerWindow * 3 * 1 + i * 3 + 0)}-" + m_sysmemBuffer[(windowOffsetInFloat4 + _maxInstancePerWindow * 3 * 1 + i * 3 + 0)]);
        Debug.LogError($"{(windowOffsetInFloat4 + _maxInstancePerWindow * 3 * 1 + i * 3 + 1)}-" + m_sysmemBuffer[(windowOffsetInFloat4 + _maxInstancePerWindow * 3 * 1 + i * 3 + 1)]);
        Debug.LogError($"{(windowOffsetInFloat4 + _maxInstancePerWindow * 3 * 1 + i * 3 + 2)}-" + m_sysmemBuffer[(windowOffsetInFloat4 + _maxInstancePerWindow * 3 * 1 + i * 3 + 2)]);

        // update colors
        Debug.LogError($"{(windowOffsetInFloat4 + _maxInstancePerWindow * 3 * 2 + i)}-" + m_sysmemBuffer[windowOffsetInFloat4 + _maxInstancePerWindow * 3 * 2 + i]);
        Debug.LogError($"------LogEnd:{id}");
    }


    //  Upload minimal GPU data according to "instanceCount"
    //  Because of SoA and this class is managing 3 BRG properties ( 2 matrices & 1 color ), the last window could use up to 3 SetData
    [BurstCompile]
    public bool UploadGpuData()
    {
        if ((uint)m_TempInstanceCount > (uint)m_maxInstances)
            return false;

        m_instanceCount = m_TempInstanceCount;
        int completeWindows = m_instanceCount / m_maxInstancePerWindow;

        // update all complete windows in one go
        if (completeWindows > 0)
        {
            int sizeInFloat4 = (completeWindows * m_alignedGPUWindowSize) / 16;
            m_GPUPersistentInstanceData.SetData(m_sysmemBuffer, 0, 0, sizeInFloat4);
        }

        // then upload data for the last (incomplete) window
        int lastBatchId = completeWindows;
        int itemInLastBatch = m_instanceCount - m_maxInstancePerWindow * completeWindows;


        if (itemInLastBatch > 0)
        {
            //这里在移动端发生错误 需要调试
            int windowOffsetInFloat4 = (lastBatchId * m_alignedGPUWindowSize) / 16;
            int offsetMat1 = windowOffsetInFloat4 + m_maxInstancePerWindow * 0;
            int offsetMat2 = windowOffsetInFloat4 + m_maxInstancePerWindow * 3;
            int offsetColor = windowOffsetInFloat4 + m_maxInstancePerWindow * 3 * 2;
            m_GPUPersistentInstanceData.SetData(m_sysmemBuffer, offsetMat1, offsetMat1, itemInLastBatch * 3); // 3 float4 for obj2world
            m_GPUPersistentInstanceData.SetData(m_sysmemBuffer, offsetMat2, offsetMat2, itemInLastBatch * 3); // 3 float4 for world2obj
            m_GPUPersistentInstanceData.SetData(m_sysmemBuffer, offsetColor, offsetColor, itemInLastBatch * 1); // 1 float4 for color

            int offsetColor2 = windowOffsetInFloat4 + m_maxInstancePerWindow * (3 * 2 + 1);
            int offsetColor3 = windowOffsetInFloat4 + m_maxInstancePerWindow * (3 * 2 + 2);
            m_GPUPersistentInstanceData.SetData(m_sysmemBuffer, offsetColor2, offsetColor2, itemInLastBatch * 1);
            m_GPUPersistentInstanceData.SetData(m_sysmemBuffer, offsetColor3, offsetColor3, itemInLastBatch * 1);
        }

        return true;
    }

    public void ClearInstnce()
    {
        m_TempInstanceCount = 0;
    }

    // Release all allocated buffers
    public void Shutdown()
    {
        if (m_initialized)
        {
            for (uint b = 0; b < m_windowCount; b++)
                m_BatchRendererGroup.RemoveBatch(m_batchIDs[b]);

            m_BatchRendererGroup.UnregisterMaterial(m_materialID);
            m_BatchRendererGroup.UnregisterMesh(m_meshID);
            m_BatchRendererGroup.Dispose();
            m_GPUPersistentInstanceData.Dispose();
            m_sysmemBuffer.Dispose();
            m_RendererItems.Dispose();
        }
    }

    // return the system memory buffer and the window size, so BRG_Background and BRG_Debris can fill the buffer with new content
    public NativeArray<float4> GetSysmemBuffer(out int totalSize, out int alignedWindowSize)
    {
        totalSize = m_totalGpuBufferSize;
        alignedWindowSize = m_alignedGPUWindowSize;
        return m_sysmemBuffer;
    }

    // helper function to create the 32bits metadata value. Bit 31 means property have different value per instance
    static MetadataValue CreateMetadataValue(int nameID, uint gpuOffset)
    {
        const uint kIsPerInstanceBit = 0x80000000;
        return new MetadataValue
        {
            NameID = nameID,
            Value = (uint)gpuOffset | kIsPerInstanceBit,
        };
    }

    // Helper function to allocate BRG buffers during the BRG callback function
    private static T* Malloc<T>(uint count) where T : unmanaged
    {
        return (T*)UnsafeUtility.Malloc(
            UnsafeUtility.SizeOf<T>() * count,
            UnsafeUtility.AlignOf<T>(),
            Allocator.TempJob);
    }

    // Main BRG entry point per frame. In this sample we won't use BatchCullingContext as we don't need culling
    // This callback is responsible to fill cullingOutput with all draw commands we need to render all the items
    [BurstCompile]
    public JobHandle OnPerformCulling(BatchRendererGroup rendererGroup, BatchCullingContext cullingContext, BatchCullingOutput cullingOutput, IntPtr userContext)
    {
        if (m_initialized)
        {
            BatchCullingOutputDrawCommands drawCommands = new BatchCullingOutputDrawCommands();

            // calculate the amount of draw commands we need in case of UBO mode (one draw command per window)
            int drawCommandCount = (m_instanceCount + m_maxInstancePerWindow - 1) / m_maxInstancePerWindow;
            int maxInstancePerDrawCommand = m_maxInstancePerWindow;
            drawCommands.drawCommandCount = drawCommandCount;

            // Allocate a single BatchDrawRange. ( all our draw commands will refer to this BatchDrawRange)
            drawCommands.drawRangeCount = 1;
            drawCommands.drawRanges = Malloc<BatchDrawRange>(1);
            drawCommands.drawRanges[0] = new BatchDrawRange
            {
                drawCommandsBegin = 0,
                drawCommandsCount = (uint)drawCommandCount,
                filterSettings = new BatchFilterSettings
                {
                    renderingLayerMask = 1,
                    layer = 0,
                    motionMode = MotionVectorGenerationMode.Camera,
                    shadowCastingMode = m_ShadowCastingMode,
                    receiveShadows = true,
                    staticShadowCaster = false,
                    allDepthSorted = false
                }
            };

            if (drawCommands.drawCommandCount > 0)
            {
                // as we don't need culling, the visibility int array buffer will always be {0,1,2,3,...} for each draw command
                // so we just allocate maxInstancePerDrawCommand and fill it
                int visibilityArraySize = maxInstancePerDrawCommand;
                if (m_instanceCount < visibilityArraySize)
                    visibilityArraySize = m_instanceCount;

                drawCommands.visibleInstances = Malloc<int>((uint)visibilityArraySize);

                // As we don't need any frustum culling in our context, we fill the visibility array with {0,1,2,3,...}
                for (int i = 0; i < visibilityArraySize; i++)
                    drawCommands.visibleInstances[i] = i;

                // Allocate the BatchDrawCommand array (drawCommandCount entries)
                // In SSBO mode, drawCommandCount will be just 1
                drawCommands.drawCommands = Malloc<BatchDrawCommand>((uint)drawCommandCount);
                int left = m_instanceCount;
                for (int b = 0; b < drawCommandCount; b++)
                {
                    int inBatchCount = left > maxInstancePerDrawCommand ? maxInstancePerDrawCommand : left;
                    drawCommands.drawCommands[b] = new BatchDrawCommand
                    {
                        visibleOffset = (uint)0, // all draw command is using the same {0,1,2,3...} visibility int array
                        visibleCount = (uint)inBatchCount,
                        batchID = m_batchIDs[b],
                        materialID = m_materialID,
                        meshID = m_meshID,
                        submeshIndex = 0,
                        splitVisibilityMask = 0xff,
                        flags = BatchDrawCommandFlags.None,
                        sortingPosition = 0
                    };
                    left -= inBatchCount;
                }
            }

            cullingOutput.drawCommands[0] = drawCommands;
            drawCommands.instanceSortingPositions = null;
            drawCommands.instanceSortingPositionFloatCount = 0;
        }

        return new JobHandle();
    }
}