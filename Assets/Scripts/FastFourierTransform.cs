using UnityEngine;

public class FastFourierTransform
{
    const int LOCAL_WORK_GROUPS_X = 8;
    const int LOCAL_WORK_GROUPS_Y = 8;

    readonly int size;
    readonly ComputeShader fftShader;
    readonly RenderTexture precomputedData;

    public static RenderTexture CreateRenderTexture(int size, RenderTextureFormat format = RenderTextureFormat.RGFloat, bool useMips = false)
    {
        RenderTexture rt = new RenderTexture(size, size, 0,
            format, RenderTextureReadWrite.Linear);
        rt.useMipMap = useMips;
        rt.autoGenerateMips = false;
        rt.anisoLevel = 6;
        rt.filterMode = FilterMode.Trilinear;
        rt.wrapMode = TextureWrapMode.Repeat;
        rt.enableRandomWrite = true;
        rt.Create();
        return rt;
    }

    public FastFourierTransform(int size, ComputeShader fftShader)
    {
        this.size = size;
        this.fftShader = fftShader;
        precomputedData = PrecomputeTwiddleFactorsAndInputIndices();

        KERNEL_PRECOMPUTE = fftShader.FindKernel("PrecomputeTwiddleFactorsAndInputIndices");
        KERNEL_HORIZONTAL_STEP_FFT = fftShader.FindKernel("HorizontalStepFFT");
        KERNEL_VERTICAL_STEP_FFT = fftShader.FindKernel("VerticalStepFFT");
        KERNEL_HORIZONTAL_STEP_IFFT = fftShader.FindKernel("HorizontalStepInverseFFT");
        KERNEL_VERTICAL_STEP_IFFT = fftShader.FindKernel("VerticalStepInverseFFT");
        KERNEL_SCALE = fftShader.FindKernel("Scale");
        KERNEL_PERMUTE = fftShader.FindKernel("Permute");
    }

    public void FFT2D(RenderTexture input, RenderTexture buffer, bool outputToInput = false)
    {
        int logSize = (int)Mathf.Log(size, 2);
        bool pingPong = false;

        fftShader.SetTexture(KERNEL_HORIZONTAL_STEP_FFT, PROP_ID_PRECOMPUTED_DATA, precomputedData);
        fftShader.SetTexture(KERNEL_HORIZONTAL_STEP_FFT, PROP_ID_BUFFER0, input);
        //fftShader.SetTexture(KERNEL_HORIZONTAL_STEP_FFT, PROP_ID_BUFFER1, buffer);
        for (int i = 0; i < logSize; i++)
        {
            pingPong = !pingPong;
            fftShader.SetInt(PROP_ID_STEP, i);
            fftShader.SetBool(PROP_ID_PINGPONG, pingPong);
            fftShader.Dispatch(KERNEL_HORIZONTAL_STEP_FFT, size / LOCAL_WORK_GROUPS_X, size / LOCAL_WORK_GROUPS_Y, 1);
        }

        fftShader.SetTexture(KERNEL_VERTICAL_STEP_FFT, PROP_ID_PRECOMPUTED_DATA, precomputedData);
        fftShader.SetTexture(KERNEL_VERTICAL_STEP_FFT, PROP_ID_BUFFER0, input);
        //fftShader.SetTexture(KERNEL_VERTICAL_STEP_FFT, PROP_ID_BUFFER1, buffer);
        for (int i = 0; i < logSize; i++)
        {
            pingPong = !pingPong;
            fftShader.SetInt(PROP_ID_STEP, i);
            fftShader.SetBool(PROP_ID_PINGPONG, pingPong);
            fftShader.Dispatch(KERNEL_VERTICAL_STEP_FFT, size / LOCAL_WORK_GROUPS_X, size / LOCAL_WORK_GROUPS_Y, 1);
        }

        if (pingPong && outputToInput)
        {
            Graphics.Blit(buffer, input);
        }

        if (!pingPong && !outputToInput)
        {
            Graphics.Blit(input, buffer);
        }
    }

    public void IFFT2D(RenderTexture input, RenderTexture buffer, bool outputToInput = false, bool scale = true, bool permute = false)
    {
        int logSize = (int)Mathf.Log(size, 2);
        bool pingPong = false;

        fftShader.SetTexture(KERNEL_HORIZONTAL_STEP_IFFT, PROP_ID_PRECOMPUTED_DATA, precomputedData);
        fftShader.SetTexture(KERNEL_HORIZONTAL_STEP_IFFT, PROP_ID_BUFFER0, input);
        fftShader.SetTexture(KERNEL_HORIZONTAL_STEP_IFFT, PROP_ID_BUFFER_DXDZ, buffer);
        //for (int i = 0; i < logSize; i++)
        //{
        //    pingPong = !pingPong;
        //    fftShader.SetInt(PROP_ID_STEP, i);
        //    fftShader.SetBool(PROP_ID_PINGPONG, pingPong);
        //    fftShader.Dispatch(KERNEL_HORIZONTAL_STEP_IFFT, size / LOCAL_WORK_GROUPS_X, size / LOCAL_WORK_GROUPS_Y, 1);
        //}
        pingPong = !pingPong;
        //fftShader.Dispatch(KERNEL_HORIZONTAL_STEP_IFFT, 1, size/2+1, 1);
        fftShader.Dispatch(KERNEL_HORIZONTAL_STEP_IFFT, 1, size, 1);

        fftShader.SetTexture(KERNEL_VERTICAL_STEP_IFFT, PROP_ID_PRECOMPUTED_DATA, precomputedData);
        fftShader.SetTexture(KERNEL_VERTICAL_STEP_IFFT, PROP_ID_BUFFER0, input);
        fftShader.SetTexture(KERNEL_VERTICAL_STEP_IFFT, PROP_ID_BUFFER_DXDZ, buffer);
        //for (int i = 0; i < logSize; i++)
        //{
        //    pingPong = !pingPong;
        //    fftShader.SetInt(PROP_ID_STEP, i);
        //    fftShader.SetBool(PROP_ID_PINGPONG, pingPong);
        //    fftShader.Dispatch(KERNEL_VERTICAL_STEP_IFFT, size / LOCAL_WORK_GROUPS_X, size / LOCAL_WORK_GROUPS_Y, 1);
        //}
        pingPong = !pingPong;
        fftShader.Dispatch(KERNEL_VERTICAL_STEP_IFFT, 1, size, 1);

        //if (pingPong && outputToInput)
        //{
        //    Graphics.Blit(buffer, input);
        //}

        //if (!pingPong && !outputToInput)
        //{
        //    Graphics.Blit(input, buffer);
        //}

        //if (permute)
        //{
        //    fftShader.SetInt(PROP_ID_SIZE, size);
        //    fftShader.SetTexture(KERNEL_PERMUTE, PROP_ID_BUFFER0, outputToInput ? input : buffer);
        //    fftShader.Dispatch(KERNEL_PERMUTE, size / LOCAL_WORK_GROUPS_X, size / LOCAL_WORK_GROUPS_Y, 1);
        //}
        
        //if (scale)
        //{
        //    fftShader.SetInt(PROP_ID_SIZE, size);
        //    fftShader.SetTexture(KERNEL_SCALE, PROP_ID_BUFFER0, outputToInput ? input : buffer);
        //    fftShader.Dispatch(KERNEL_SCALE, size / LOCAL_WORK_GROUPS_X, size / LOCAL_WORK_GROUPS_Y, 1);
        //}
    }

    public void IFFT2D(RenderTexture H0, RenderTexture DxDz, RenderTexture DyDxz, RenderTexture DyxDyz, RenderTexture DxxDzz,
        RenderTexture bufferTmp0, RenderTexture bufferTmp1, RenderTexture WaveData, float time)
    {
        int logSize = (int)Mathf.Log(size, 2);
        //bool pingPong = false;

        fftShader.SetTexture(KERNEL_HORIZONTAL_STEP_IFFT, PROP_ID_PRECOMPUTED_DATA, precomputedData);
        fftShader.SetTexture(KERNEL_HORIZONTAL_STEP_IFFT, PROP_ID_BUFFER0, H0);
        fftShader.SetTexture(KERNEL_HORIZONTAL_STEP_IFFT, PROP_ID_BUFFER_TMP0, bufferTmp0);
        fftShader.SetTexture(KERNEL_HORIZONTAL_STEP_IFFT, PROP_ID_BUFFER_TMP1, bufferTmp1);
        fftShader.SetTexture(KERNEL_HORIZONTAL_STEP_IFFT, WAVE_DATA_PROP, WaveData);
        fftShader.SetFloat(TIME_PROP, time);
        //for (int i = 0; i < logSize; i++)
        //{
        //    pingPong = !pingPong;
        //    fftShader.SetInt(PROP_ID_STEP, i);
        //    fftShader.SetBool(PROP_ID_PINGPONG, pingPong);
        //    fftShader.Dispatch(KERNEL_HORIZONTAL_STEP_IFFT, size / LOCAL_WORK_GROUPS_X, size / LOCAL_WORK_GROUPS_Y, 1);
        //}
        //pingPong = !pingPong;
        //fftShader.Dispatch(KERNEL_HORIZONTAL_STEP_IFFT, 1, size/2+1, 1);
        fftShader.Dispatch(KERNEL_HORIZONTAL_STEP_IFFT, 1, size, 1);

        fftShader.SetTexture(KERNEL_VERTICAL_STEP_IFFT, PROP_ID_PRECOMPUTED_DATA, precomputedData);
        fftShader.SetTexture(KERNEL_VERTICAL_STEP_IFFT, PROP_ID_BUFFER0, H0);
        fftShader.SetTexture(KERNEL_VERTICAL_STEP_IFFT, PROP_ID_BUFFER_TMP0, bufferTmp0);
        fftShader.SetTexture(KERNEL_VERTICAL_STEP_IFFT, PROP_ID_BUFFER_TMP1, bufferTmp1);

        fftShader.SetTexture(KERNEL_VERTICAL_STEP_IFFT, PROP_ID_BUFFER_DXDZ, DxDz);
        fftShader.SetTexture(KERNEL_VERTICAL_STEP_IFFT, PROP_ID_BUFFER_DYDXZ, DyDxz);
        fftShader.SetTexture(KERNEL_VERTICAL_STEP_IFFT, PROP_ID_BUFFER_DYXDYZ, DyxDyz);
        fftShader.SetTexture(KERNEL_VERTICAL_STEP_IFFT, PROP_ID_BUFFER_DXXDZZ, DxxDzz);
        //for (int i = 0; i < logSize; i++)
        //{
        //    pingPong = !pingPong;
        //    fftShader.SetInt(PROP_ID_STEP, i);
        //    fftShader.SetBool(PROP_ID_PINGPONG, pingPong);
        //    fftShader.Dispatch(KERNEL_VERTICAL_STEP_IFFT, size / LOCAL_WORK_GROUPS_X, size / LOCAL_WORK_GROUPS_Y, 1);
        //}
        //pingPong = !pingPong;
        fftShader.Dispatch(KERNEL_VERTICAL_STEP_IFFT, 1, size, 1);        
    }

    RenderTexture PrecomputeTwiddleFactorsAndInputIndices()
    {
        int logSize = (int)Mathf.Log(size, 2);
        RenderTexture rt = new RenderTexture(logSize, size, 0,
            RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        rt.filterMode = FilterMode.Point;
        rt.wrapMode = TextureWrapMode.Repeat;
        rt.enableRandomWrite = true;
        rt.Create();

        fftShader.SetInt(PROP_ID_SIZE, size);
        fftShader.SetTexture(KERNEL_PRECOMPUTE, PROP_ID_PRECOMPUTE_BUFFER, rt);
        fftShader.Dispatch(KERNEL_PRECOMPUTE, logSize, size / 2 / LOCAL_WORK_GROUPS_Y, 1);
        return rt;
    }

    // Kernel IDs:
    readonly int KERNEL_PRECOMPUTE;
    readonly int KERNEL_HORIZONTAL_STEP_FFT;
    readonly int KERNEL_VERTICAL_STEP_FFT;
    readonly int KERNEL_HORIZONTAL_STEP_IFFT;
    readonly int KERNEL_VERTICAL_STEP_IFFT;
    readonly int KERNEL_SCALE;
    readonly int KERNEL_PERMUTE;

    // Property IDs:
    readonly int PROP_ID_PRECOMPUTE_BUFFER = Shader.PropertyToID("PrecomputeBuffer");
    readonly int PROP_ID_PRECOMPUTED_DATA = Shader.PropertyToID("PrecomputedData");
    readonly int WAVE_DATA_PROP = Shader.PropertyToID("WavesData");
    readonly int PROP_ID_BUFFER0 = Shader.PropertyToID("H0");
    readonly int PROP_ID_BUFFER_TMP0 = Shader.PropertyToID("BufferTmp0");
    readonly int PROP_ID_BUFFER_TMP1 = Shader.PropertyToID("BufferTmp1");
    readonly int PROP_ID_BUFFER_DXDZ = Shader.PropertyToID("DxDz");
    readonly int PROP_ID_BUFFER_DYDXZ = Shader.PropertyToID("DyDxz");
    readonly int PROP_ID_BUFFER_DYXDYZ = Shader.PropertyToID("DyxDyz");
    readonly int PROP_ID_BUFFER_DXXDZZ = Shader.PropertyToID("DxxDzz");
    readonly int PROP_ID_SIZE = Shader.PropertyToID("Size");
    readonly int PROP_ID_STEP = Shader.PropertyToID("Step");
    readonly int PROP_ID_PINGPONG = Shader.PropertyToID("PingPong");
    readonly int TIME_PROP = Shader.PropertyToID("Time");
}
