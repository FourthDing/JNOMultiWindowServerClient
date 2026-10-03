using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.MemoryMappedFiles;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UI.Xml;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;
using WindowServer.IO;
using static UnityEngine.Networking.UnityWebRequest;

public class ExternalWindow : MonoBehaviour
{
    static string appId = "JunoNewOrigins";
    NamedPipeServerStream recv;
    NamedPipeClientStream send = new("UnityMultiWindowServicePipe");
    byte[] HeaderBuffer = new byte[UnsafeUtility.SizeOf<PipedCmdHeader>()];
    PipedCmdHeader Header => MemoryMarshal.Read<PipedCmdHeader>(HeaderBuffer);
    byte[] ContentBuffer = new byte[256];
    public string windowId;
    public WindowGeometry geometry;
    public Camera PipedCamera
    {
        get => _cam;
        set {
            if (_cam != null) _cam.targetTexture = null;
            _cam = value;
            if (_cam != null) _cam.targetTexture = tempTexture;
        } 
    }
    private Camera _cam;

    public Camera PipedSubCamera
    {
        get => _subCam;
        set
        {
            if (_subCam != null) _subCam.targetTexture = null;
            _subCam = value;
            if (_subCam != null) _subCam.targetTexture = tempTexture;
        }
    }
    private Camera _subCam;

    RenderTexture tempTexture = new(720, 720, 32);
    RenderTexture afterTexture = new(720, 720, 32);
    volatile byte[] tempBuffer;
    MemoryMappedFile sharedFB;
    MemoryMappedViewAccessor sharedFBAccessor;
    Thread client;
    Thread pipeHandler;
    volatile bool closeRequested = false;
    //private readonly ManualResetEventSlim firstFrameReady = new(false);
    private volatile bool newFrameReady = false;
    private volatile bool newFramePending = false;
    AsyncGPUReadbackRequest readFrameRequest;

    public void Close()
    {
        closeRequested = true;
    }
    void Resize(int Width,int Height)
    {
        if ((Width * Height) > (tempTexture.width * tempTexture.height))
        {
            sharedFBAccessor.Dispose();
            sharedFB.Dispose();
            sharedFB = MemoryMappedFile.CreateNew(appId +"-"+ windowId + ".fb", 
                1/*这可是信号值*/+ 4*Width*Height
                );
            sharedFBAccessor = sharedFB.CreateViewAccessor();
            PipeSendStr(PipedCmdType.SharedMem, appId + "-" + windowId + ".fb");
        }
        tempTexture.Release();
        tempTexture.width = Width;
        tempTexture.height = Height;
        tempTexture.Create();
    }

    // Start is called before the first frame update
    void Start()
    {

        GameObject cameraObject = new("MWM-Camera-" + windowId);
        GameObject subCamObject = new("MWM-SubCamera-" + windowId);
        cameraObject.transform.SetParent(GameObject.Find("NearCamera").transform,false);
        subCamObject.transform.SetParent(cameraObject.transform,false);
        Camera objectCamera = cameraObject.AddComponent<Camera>();
        Camera subCamera = subCamObject.AddComponent<Camera>();
        PipedCamera = objectCamera;
        PipedSubCamera = subCamera;
        PipedCamera.depth = 1;
        PipedSubCamera.depth = 0;
        PipedCamera.clearFlags = CameraClearFlags.Depth;
        PipedSubCamera.clearFlags = CameraClearFlags.Skybox;
        PipedCamera.farClipPlane = 10000;
        PipedCamera.nearClipPlane = 0.1f;
        PipedSubCamera.farClipPlane = 100000000;
        PipedSubCamera.nearClipPlane = 9999;

        Debug.Log("创建了相机！");
        //启动服务器（好吧这边是客户端）
        pipeHandler = new(new ThreadStart(ThreadServePipe));
        Debug.Log("创建了线程！");
        PipedSubCamera.Render();
        PipedCamera.Render();
        AsyncGPUReadback.Request(tempTexture, 0, TextureFormat.RGBA32, (request) =>
        {
            if (request.hasError)
            {
                Debug.LogError("GPU 读取失败");
                return;
            }
            tempBuffer = request.GetData<byte>().ToArray();
            newFrameReady = true;
        });
        Debug.Log("相机初次渲染完成");
        //pipeHandler建立好连接，再由它client.Start();
        pipeHandler.Start();
        Debug.Log("线程，启动！");

    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log("相机要渲染了");
        /*我真服了为啥老是在这种我“以为应该这样”的地方出的问题*/
        //PipedCamera.Render();
        //PipedSubCamera.Render();
        //Debug.Log("相机渲染完了，要拿RenderTexture了");
        //Graphics.Blit(tempTexture,afterTexture);
        if (newFramePending) return;
        newFramePending = true;
        readFrameRequest = AsyncGPUReadback.Request(tempTexture, 0, TextureFormat.RGBA32, (request) =>
        {
            newFramePending = false;
            if (request.hasError)
            {
                Debug.LogError("GPU 读取失败");
                return;
            }
            tempBuffer = request.GetData<byte>().ToArray();
            newFrameReady = true;
            //Debug.Log("设置了新帧就绪！");
        });

            //if (tempTexture.height != geometry.Height
            //    && tempTexture.width != geometry.Width)
            //{
            //    tempTexture = new(geometry.Width, geometry.Height, 32);
            //}
            //windowTexture = AsyncGPUReadback.Request(tempTexture).GetData<byte>().ToArray();

        }
    void ThreadServePipe()
    {
        recv = new(appId +"-"+ windowId);
        //tempTexture.Release();
        //tempTexture.width = 720;
        //tempTexture.height = 720;
        //tempTexture.Create();
        sharedFB = MemoryMappedFile.CreateNew(appId + "-" + windowId + ".fb",
                1/*这可是信号值*/+ 4 * 720 * 720
                );
        sharedFBAccessor = sharedFB.CreateViewAccessor();
        Debug.Log("创建了连接实例。等待服务端……");
        send.Connect();
        send.Flush();
        Debug.Log("服务端连接建立");
        Debug.Log("发送初通信……");
        PipeSendStr(PipedCmdType.S2CPipe, appId + "-" + windowId);
        Debug.Log("发送了初通信,等待服务器回连recv……");
        recv.WaitForConnection();
        Debug.Log("服务器回连了recv!");
        PipeSendStr(PipedCmdType.NewWindow, appId + "-" + windowId);
        Debug.Log("发送了NewWindow");

        WindowGeometry initialGeometry = new WindowGeometry(114, 514, 720, 720);
        PipeSend(PipedCmdType.SetGeometry,ref initialGeometry);

        Debug.Log("发送了SetGeometry");
        byte writing = 1;
        byte okay = 0;
        sharedFBAccessor.Write(0, ref writing);
        sharedFBAccessor.Write(0, ref okay);
        PipeSendStr(PipedCmdType.SharedMem, appId + "-" + windowId + ".fb");
        Debug.Log("发送了第一个共享内存路径");
        //firstFrameReady.Wait();
        while (!closeRequested)
        {
            if (!newFrameReady) {
                Thread.Sleep(0);//此次放弃调度时间片
                continue; };
            newFrameReady = false;
            sharedFBAccessor.Write(0, ref writing);
            sharedFBAccessor.WriteArray(1, tempBuffer, 0, tempBuffer.Length);
            sharedFBAccessor.Write(0, ref okay);
            //Thread.Sleep(1000 / 60);

        }
        Debug.Log("我滴任务完成了！正在收拾……");

        PipeSend<byte>(PipedCmdType.ClientEnd,ref okay);
        recv.Disconnect();
        send.Dispose();
        recv.Dispose();
        sharedFBAccessor.Dispose();
        sharedFB.Dispose();
        Destroy(PipedSubCamera);
        Destroy(PipedCamera);
    }
    public void PipeSend<Type>(PipedCmdType headerType, ref Type content) where Type : unmanaged
    {
        MemoryMarshal.Write<Type>(ContentBuffer, ref content);
        PipedCmdHeader head = new PipedCmdHeader(headerType, Convert.ToByte(UnsafeUtility.SizeOf<Type>()));
        MemoryMarshal.Write(HeaderBuffer, ref head);
        send.Write(HeaderBuffer);
        send.Write(ContentBuffer.AsSpan(0, UnsafeUtility.SizeOf<Type>()));
    }
    public void PipeSendStr(PipedCmdType headerType, string content)
    {
        int length = Encoding.UTF8.GetBytes(content, ContentBuffer.AsSpan<byte>());
        //照顾老dotnet
        PipedCmdHeader head = new PipedCmdHeader(headerType, Convert.ToByte(length));
        MemoryMarshal.Write(HeaderBuffer,ref head);
        send.Write(HeaderBuffer);
        send.Write(ContentBuffer.AsSpan(0, length));
    }
    public async Task PipeRecv()
    {
        await Task.WhenAll();
    }
}
