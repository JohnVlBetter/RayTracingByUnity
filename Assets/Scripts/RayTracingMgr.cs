using System.Collections.Generic;
using UnityEngine;

public class RayTracingMgr : MonoBehaviour {
    public ComputeShader rayTracingShader;
    public Texture SkyboxTexture;
    private RenderTexture target;
    private new Camera camera;
    public Light directionalLight;

    private uint currentSample = 0;
    private Material addMaterial;

    public Vector2 SphereRadius = new Vector2(3.0f, 8.0f);
    public uint SpheresMax = 100;
    public float SpherePlacementRadius = 100.0f;
    private ComputeBuffer sphereBuffer;

    struct Sphere
    {
        public Vector3 position;
        public float radius;
        public Vector3 albedo;
        public Vector3 specular;
    };

    private void Awake() {
        camera = GetComponent<Camera>();
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination) {
        Render(destination);
    }

    private void Update()
    {
        if (transform.hasChanged)
        {
            currentSample = 0;
            transform.hasChanged = false;
        }
    }

    private void OnEnable()
    {
        currentSample = 0;
        SetUpScene();
    }
    private void OnDisable()
    {
        if (sphereBuffer != null)
            sphereBuffer.Release();
    }
    private void SetUpScene()
    {
        List<Sphere> spheres = new List<Sphere>();
        for (int i = 0; i < SpheresMax; i++)
        {
            Sphere sphere = new Sphere();
            sphere.radius = SphereRadius.x + Random.value * (SphereRadius.y - SphereRadius.x);
            Vector2 randomPos = Random.insideUnitCircle * SpherePlacementRadius;
            sphere.position = new Vector3(randomPos.x, sphere.radius, randomPos.y);
            foreach (Sphere other in spheres)
            {
                float minDist = sphere.radius + other.radius;
                if (Vector3.SqrMagnitude(sphere.position - other.position) < minDist * minDist)
                    goto SkipSphere;
            }
            Color color = Random.ColorHSV();
            bool metal = Random.value < 0.5f;
            sphere.albedo = metal ? Vector3.zero : new Vector3(color.r, color.g, color.b);
            sphere.specular = metal ? new Vector3(color.r, color.g, color.b) : Vector3.one * 0.04f;
            spheres.Add(sphere);
        SkipSphere:
            continue;
        }
        sphereBuffer = new ComputeBuffer(spheres.Count, 40);
        sphereBuffer.SetData(spheres);
    }

    private void Render(RenderTexture destination) {
        InitRT();
        SetShaderParameters();
        int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
        rayTracingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

        if (addMaterial == null) addMaterial = new Material(Shader.Find("Hidden/AddShader"));
        addMaterial.SetFloat("_Sample", currentSample);
        Graphics.Blit(target, destination, addMaterial);
        currentSample++;
    }

    private void InitRT() {
        if (target == null || target.width != Screen.width || target.height != Screen.height) {
            if (target != null) target.Release();
            target = new RenderTexture(Screen.width, Screen.height, 0, 
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            target.enableRandomWrite = true;
            target.Create();
        }
        currentSample = 0;
    }

    private void SetShaderParameters() {
        rayTracingShader.SetMatrix("_CameraToWorld", camera.cameraToWorldMatrix);
        rayTracingShader.SetMatrix("_CameraInverseProjection", camera.projectionMatrix.inverse);
        rayTracingShader.SetTexture(0, "Result", target);
        rayTracingShader.SetTexture(0, "_SkyboxTexture", SkyboxTexture);
        rayTracingShader.SetVector("_PixelOffset", new Vector2(Random.value, Random.value));

        Vector3 l = directionalLight.transform.forward;
        rayTracingShader.SetVector("_DirectionalLight", new Vector4(l.x,l.y,l.z,directionalLight.intensity));

        rayTracingShader.SetBuffer(0, "_Spheres", sphereBuffer);
    }
}