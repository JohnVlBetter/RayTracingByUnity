using UnityEngine;

public class RayTracingMgr : MonoBehaviour {
    public ComputeShader rayTracingShader;
    public Texture SkyboxTexture;
    private RenderTexture target;
    private Camera camera;

    private void Awake() {
        camera = GetComponent<Camera>();
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination) {
        Render(destination);
    }

    private void Render(RenderTexture destination) {
        InitRT();
        SetShaderParameters();
        int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
        rayTracingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);
        Graphics.Blit(target, destination);
    }

    private void InitRT() {
        if (target == null || target.width != Screen.width || target.height != Screen.height) {
            if (target != null) target.Release();
            target = new RenderTexture(Screen.width, Screen.height, 0, 
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            target.enableRandomWrite = true;
            target.Create();
        }
    }

    private void SetShaderParameters() {
        rayTracingShader.SetMatrix("_CameraToWorld", camera.cameraToWorldMatrix);
        rayTracingShader.SetMatrix("_CameraInverseProjection", camera.projectionMatrix.inverse);
        rayTracingShader.SetTexture(0, "Result", target);
        rayTracingShader.SetTexture(0, "_SkyboxTexture", SkyboxTexture);
    }
}