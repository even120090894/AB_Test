using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System.IO;
using UnityEngine.UI;

enum KernelFunction
{
    GenerateKernelCore,
    KernelNormalize,
    WorldUpdate
}

public class LeniaManager : MonoBehaviour
{
    public ComputeShader leniaShader;
    // 用来看lenia的图像节点
    public RawImage leniaImage;
    // 用来显示lenia的图像
    public Material leniaMaterial;
    // 用来看kernelCore的图像节点
    public RawImage kernelCoreImage;
    // 用来显示kernelCore的材质
    public Material kernelCoreMaterial;

    public Texture testOldWorldTexture;

    // 展示卷积核图像的texture
    private RenderTexture kernelCoreTexture;
    // 实际用来存储卷积核信息的struct
    private ComputeBuffer kernelCoreBuffer;
    // 用来展示的世界texture
    private RenderTexture oldWorldT;
    // 用来计算的世界texture
    private RenderTexture newWorldT;

    // 存储核函数的index
    private Dictionary<KernelFunction, int> kernelIndexDict = new Dictionary<KernelFunction, int>();

    private bool ableUpdate = false;

    public float targetFrameRate = 10;

    public int conv_r = 20;

    const int max_conv_r = 30;

    public int leniaWidth = 256;

    public float grow_mu = 0.15f;
    public float grow_sigma = 0.016f;

    private void Awake() {
        Time.fixedDeltaTime = 1f / targetFrameRate;


        kernelIndexDict.Add(KernelFunction.GenerateKernelCore, leniaShader.FindKernel("GenerateKernelCore"));
        kernelIndexDict.Add(KernelFunction.KernelNormalize, leniaShader.FindKernel("KernelNormalize"));
        kernelIndexDict.Add(KernelFunction.WorldUpdate, leniaShader.FindKernel("WorldUpdate"));
    }

    private void WorldInit()
    {
        leniaShader.SetFloat("grow_mu", grow_mu);
        leniaShader.SetFloat("grow_sigma", grow_sigma);
        leniaShader.SetInt("world_size", leniaWidth);
        leniaShader.SetInt("max_conv_r", max_conv_r);

        if (testOldWorldTexture == null)
        {
            // 创建旧世界texture
            RenderTexture oldWorldTexture = new RenderTexture(leniaWidth, leniaWidth, 0);
            oldWorldTexture.enableRandomWrite = true;
            oldWorldTexture.Create();

            oldWorldT = oldWorldTexture;
            
        } else {
            // 将旧的Texture转换为RenderTexture
            RenderTexture oldWorldTexture = new RenderTexture(testOldWorldTexture.width, testOldWorldTexture.height, 0);
            oldWorldTexture.enableRandomWrite = true;
            oldWorldTexture.Create();

            Graphics.Blit(testOldWorldTexture, oldWorldTexture);

            oldWorldT = oldWorldTexture;
        }

        // 创建新世界texture
        RenderTexture newWorldTexture = new RenderTexture(leniaWidth, leniaWidth, 0);
        newWorldTexture.enableRandomWrite = true;
        newWorldTexture.Create();

        newWorldT = newWorldTexture;

        // 设置texture
        leniaMaterial.mainTexture = oldWorldT;
        int kernelIndex = kernelIndexDict[KernelFunction.WorldUpdate];
        leniaShader.SetTexture(kernelIndex, "oldWorldTexture", oldWorldT);
        leniaShader.SetTexture(kernelIndex, "newWorldTexture", newWorldT);
        leniaShader.SetBuffer(kernelIndex, "KernelCoreBuffer", kernelCoreBuffer);

        leniaImage.material = leniaMaterial;
        leniaImage.texture = oldWorldT;

        ableUpdate = true;
    }

    private void GrowParamSet(){
        leniaShader.SetFloat("grow_mu", grow_mu);
        leniaShader.SetFloat("grow_sigma", grow_sigma);
    }

    private void WorldUpdate()
    {
        ableUpdate = false;

        int kernelIndex = kernelIndexDict[KernelFunction.WorldUpdate];
        leniaShader.Dispatch(kernelIndex, leniaWidth / 8, leniaWidth / 8, 1);

        // 使用AsyncGPUReadback确保GPU计算完成
        AsyncGPUReadback.Request(newWorldT, 0, TextureFormat.RGBA32, OnWorldUpdateReadback);
    }

    private void OnWorldUpdateReadback(AsyncGPUReadbackRequest request)
    {
        Debug.Log("World Update GPU computation completed.");
        if (request.hasError)
        {
            Debug.LogError("GPU readback error detected.");
            return;
        }
        // 读取RenderTexture的数据
        // var data = request.GetData<Color32>();

        // 将所有R通道值加起来
        // float totalR = 0;
        // for (int i = 0; i < data.Length; i++)
        // {
        //     totalR += data[i].r;
        // }
        // Debug.Log("Total R value: " + totalR / 256 / 256);

        Graphics.Blit(newWorldT, oldWorldT);
        ableUpdate = true;
    }

    private void InitKernel()
    {
        leniaShader.SetInt("max_conv_r", max_conv_r);
        leniaShader.SetInt("conv_r", conv_r);

        int kernelLength = Mathf.CeilToInt(2.0f * max_conv_r / 8.0f);
        RenderTexture kernalTexture = new RenderTexture(kernelLength * 8, kernelLength * 8, 1);
        kernalTexture.enableRandomWrite = true;
        kernalTexture.Create();

        // 保存RenderTexture
        kernelCoreTexture = kernalTexture;

        kernelCoreMaterial.mainTexture = kernalTexture;

        ComputeBuffer buffer = new ComputeBuffer(kernelLength * kernelLength * 8 * 8, sizeof(float));
        kernelCoreBuffer = buffer;

        int kernelIndex = kernelIndexDict[KernelFunction.GenerateKernelCore];

        leniaShader.SetTexture(kernelIndex, "KernelCoreResult", kernalTexture);
        leniaShader.SetBuffer(kernelIndex, "KernelCoreBuffer", buffer);
        leniaShader.Dispatch(kernelIndex, kernelLength, kernelLength, 1);


        // 使用AsyncGPUReadback确保GPU计算完成
        AsyncGPUReadback.Request(kernalTexture, 0, TextureFormat.RGBA32, OnKernelInitReadback);

        Debug.Log("Init Kernel " + kernelLength);
        kernelCoreImage.material = kernelCoreMaterial;
        kernelCoreImage.texture = kernalTexture;
    }

    private void OnKernelInitReadback(AsyncGPUReadbackRequest request)
    {
        if (request.hasError)
        {
            Debug.LogError("GPU readback error detected.");
            return;
        }
        Debug.Log("Kernel Core GPU computation completed.");

        // 注意：想看卷积核的图像，可以注释掉下面的代码

        // 读取RenderTexture的数据
        var data = request.GetData<Color32>();

        // 将所有R通道值加起来
        // float totalR = 0;
        // for (int i = 0; i < data.Length; i++)
        // {
        //     totalR += data[i].r;
        // }
        // Debug.Log("Total R value: " + totalR);
        
        // 读取compute buffer中的数据
        float totalC = ProcessBufferData();

        KernelNormalize(totalC);
    }
    private float ProcessBufferData()
    {
        // 同步获取ComputeBuffer的数据
        int kernelLength = Mathf.CeilToInt(2.0f * max_conv_r / 8.0f);
        float[] bufferData = new float[kernelLength * kernelLength * 8 * 8]; // 和你的buffer大小一致
        kernelCoreBuffer.GetData(bufferData);  // 获取数据
        // 处理你获取到的buffer数据
        float totalBufferValue = 0;
        for (int i = 0; i < bufferData.Length; i++)
        {
            totalBufferValue += bufferData[i];
        }
        Debug.Log("Total Buffer value: " + totalBufferValue); // 此处数值应该是Total R数值的 1/255，因为texture数值会做一次rgb值的转化
        return totalBufferValue;
    }

    private void KernelNormalize(float totalC)
    {
        int kernelLengthNum = Mathf.CeilToInt(2.0f * max_conv_r / 8.0f);
        int kernelIndex = kernelIndexDict[KernelFunction.KernelNormalize];

        leniaShader.SetFloat("accumulatedResult", totalC);

        leniaShader.SetTexture(kernelIndex, "KernelCoreResult", kernelCoreTexture);
        leniaShader.SetBuffer(kernelIndex, "KernelCoreBuffer", kernelCoreBuffer);
        leniaShader.Dispatch(kernelIndex, kernelLengthNum, kernelLengthNum, 1);

        AsyncGPUReadback.Request(kernelCoreTexture, 0, TextureFormat.RGBA32, OnKernelNormalizeReadback);
    }

    private void OnKernelNormalizeReadback(AsyncGPUReadbackRequest request)
    {
        if (request.hasError)
        {
            Debug.LogError("GPU readback error detected.");
            return;
        }
        Debug.Log("Kernel Normalize GPU computation completed.");
        // 读取RenderTexture的数据
        var data = request.GetData<Color32>();
        // 将所有R通道值加起来
        // float totalR = 0;
        // for (int i = 0; i < data.Length; i++)
        // {
        //     totalR += data[i].r;
        // }
        // Debug.Log("Normalize R value: " + totalR);

        ProcessBufferData();

        WorldInit();
    }


    private void Start() {
        InitKernel();
    }

    private void FixedUpdate() {
        if (ableUpdate){
            GrowParamSet();
            WorldUpdate();
        }
    }
}
