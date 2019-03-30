using System.Collections.Generic;
using UnityEngine;
// ReSharper disable InconsistentNaming
// ReSharper disable StringLiteralTypo
// ReSharper disable IdentifierTypo

namespace Scenes
{
    public class RayTracingMaster : MonoBehaviour
    {
        public ComputeShader RayTracingShader;
        public Texture skyboxTexture ;
        public Light DirectionalLight;
        public Vector2 SphereRadius = new Vector2(3.0f,8.0f ); 
        public uint SpheresMax = 100 ; 
        public float SpherePlacementRadius = 100.0f;
        public int SphereSeed ;
        
        private ComputeBuffer _sphereBuffer;
        private RenderTexture _converged;
        private RenderTexture _target;
        private Camera _camera;
        private uint _currentSample ;
        private Material _addMaterial;
        private static readonly int Sample = Shader.PropertyToID("_Sample");

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }
        private void OnEnable()
        {
            _currentSample = 0;
            SetUpScene();
        }
        private void OnDisable()
        {
            _sphereBuffer?.Release();
        }
        
        private void SetShaderParameters()
        {
            RayTracingShader.SetFloat("_Seed", Random.value);
            RayTracingShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
            RayTracingShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
            RayTracingShader.SetTexture(0, "_SkyboxTexture", skyboxTexture);
            RayTracingShader.SetVector("_PixelOffset", new Vector2(Random.value, Random.value));
            
            // light dir ;
            var l = DirectionalLight.transform.forward;
            RayTracingShader.SetVector("_DirectionalLight", new Vector4(l.x, l.y, l.z, DirectionalLight.intensity ));
            
            RayTracingShader.SetBuffer(0, "_Spheres", _sphereBuffer);
        }
        
        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            SetShaderParameters();
            Render(destination);
        }
        private void Render(RenderTexture destination)
        {
            // Make sure we have a current render target
            InitRenderTexture();
            
            // Set the target and dispatch the compute shader
            RayTracingShader.SetTexture(0, "Result", _target);
            var threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
            var threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
            RayTracingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);
            // Blit the result texture to the screen
            if (_addMaterial == null)
                _addMaterial = new Material(Shader.Find("Hidden/AddShader"));
            _addMaterial.SetFloat(Sample, _currentSample);
            
            Graphics.Blit(_target, _converged, _addMaterial);
            Graphics.Blit(_converged, destination);
            _currentSample++;
        }
        private void InitRenderTexture()
        {
            if (_target != null && _target.width == Screen.width && _target.height == Screen.height) return;
            
            // Release render texture if we already have one
            if (_target != null)
            {
                _target.Release();
                _converged.Release();
            }
                
            // Get a render target for Ray Tracing
            _target = new RenderTexture(Screen.width, Screen.height, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear) {enableRandomWrite = true};
            _target.Create();
            
            _converged = new RenderTexture(Screen.width, Screen.height, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear) {enableRandomWrite = true};
            _converged.Create();
            
            // Reset sampling
            _currentSample = 0;
        }
        
        private void Update()
        {
            if (!transform.hasChanged && !DirectionalLight.transform.hasChanged) return;
            _currentSample = 0;
            transform.hasChanged = false;
            DirectionalLight.transform.hasChanged = false;
        }

        private struct Sphere
        {
            public Vector3 position; // 12 
            public float radius;     // 4 
            public Vector3 albedo;    // 12 
            public Vector3 specular;  // 12 
            public float smoothness;  // 4 
            public Vector3 emission;  // 12 
            // total = 48 + 8 = 56 
        }
        

        private void SetUpScene()
        {
            Random.InitState(SphereSeed);
            var spheres = new List<Sphere>();
            // Add a number of random spheres
            
            for (var i = 0; i < SpheresMax; i++)
            {
                var sphere = new Sphere
                {
                    radius = SphereRadius.x + Random.value * (SphereRadius.y - SphereRadius.x)
                };
                // Radius and radius
                var randomPos = Random.insideUnitCircle * SpherePlacementRadius;
                sphere.position = new Vector3(randomPos.x, sphere.radius, randomPos.y);
                // Reject spheres that are intersecting others
                foreach (var other in spheres)
                {
                    var minDist = sphere.radius + other.radius;
                    if (Vector3.SqrMagnitude(sphere.position - other.position) < minDist * minDist)
                        goto SkipSphere;
                }
                var chance = Random.value;
                if (chance < 0.8f)
                {
                    // Albedo and specular color
                    var color = Random.ColorHSV(0.2f, 1, 0.4f, 1, 0.2f, 1.0f);
                    var metal = chance < 0.4f;
                    sphere.albedo = metal ? Vector3.zero : new Vector3(color.r, color.g, color.b);
                    sphere.specular = metal ? new Vector3(color.r, color.g, color.b) : new Vector3(0.1f, 0.1f, 0.1f);
                    sphere.smoothness = Random.value;
                }
                else
                {
                    var emission = Random.ColorHSV(0, 1, 0, 1, 3.0f, 6.0f);
                    //var emission = new Color(1.0f,1.0f,1.0f);
                    sphere.emission = new Vector3(emission.r, emission.g, emission.b);
                }
                // Add the sphere to the list
                spheres.Add(sphere);
                SkipSphere: ;
            }
            // Assign to compute buffer
            // Assign to compute buffer
            _sphereBuffer?.Release();
            if (spheres.Count <= 0) return;
            _sphereBuffer = new ComputeBuffer(spheres.Count, 56);
            _sphereBuffer.SetData(spheres);
        }
    }
}
