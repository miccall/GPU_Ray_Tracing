using System.Collections.Generic;
using System.Linq;
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
            _meshObjectBuffer?.Release();
            _vertexBuffer?.Release();
            _indexBuffer?.Release();
        }
        
        private void SetShaderParameters()
        {
            RayTracingShader.SetFloat("_Seed", Random.value);
            RayTracingShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
            RayTracingShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
            RayTracingShader.SetTexture(0, "_SkyboxTexture", skyboxTexture);
            RayTracingShader.SetVector("_PixelOffset", new Vector2(Random.value, Random.value));
            
            SetComputeBuffer("_Spheres", _sphereBuffer);
            SetComputeBuffer("_MeshObjects", _meshObjectBuffer);
            SetComputeBuffer("_Vertices", _vertexBuffer);
            SetComputeBuffer("_Indices", _indexBuffer);
            
            // light dir ;
            var l = DirectionalLight.transform.forward;
            RayTracingShader.SetVector("_DirectionalLight", new Vector4(l.x, l.y, l.z, DirectionalLight.intensity ));
            
            RayTracingShader.SetBuffer(0, "_Spheres", _sphereBuffer);
        }
        
        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            RebuildMeshObjectBuffers();
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

        private struct MeshObject
        {
            public Matrix4x4 localToWorldMatrix;
            public int indices_offset;
            public int indices_count;
        }
        
        private static bool _meshObjectsNeedRebuilding = false;
        private static List<RayTracingObject> _rayTracingObjects = new List<RayTracingObject>();
        private static List<MeshObject> _meshObjects = new List<MeshObject>();
        private static List<Vector3> _vertices = new List<Vector3>();
        private static List<int> _indices = new List<int>();
        private ComputeBuffer _meshObjectBuffer;
        private ComputeBuffer _vertexBuffer;
        private ComputeBuffer _indexBuffer;
        public static void RegisterObject(RayTracingObject obj)
        {
            _rayTracingObjects.Add(obj);
            _meshObjectsNeedRebuilding = true;
        }

        public static void UnregisterObject(RayTracingObject obj)
        {
            _rayTracingObjects.Remove(obj);
            _meshObjectsNeedRebuilding = true;
        }
        
        
        private void RebuildMeshObjectBuffers()
        {
            if (!_meshObjectsNeedRebuilding)
            {
                return;
            }
            _meshObjectsNeedRebuilding = false;
            _currentSample = 0;
            // Clear all lists
            _meshObjects.Clear();
            _vertices.Clear();
            _indices.Clear();
            // Loop over all objects and gather their data
            foreach (RayTracingObject obj in _rayTracingObjects)
            {
                Mesh mesh = obj.GetComponent<MeshFilter>().sharedMesh;
                // Add vertex data
                int firstVertex = _vertices.Count;
                _vertices.AddRange(mesh.vertices);
                // Add index data - if the vertex buffer wasn't empty before, the
                // indices need to be offset
                int firstIndex = _indices.Count;
                var indices = mesh.GetIndices(0);
                _indices.AddRange(indices.Select(index => index + firstVertex));
                // Add the object itself
                _meshObjects.Add(new MeshObject()
                {
                    localToWorldMatrix = obj.transform.localToWorldMatrix,
                    indices_offset = firstIndex,
                    indices_count = indices.Length
                });
            }
            CreateComputeBuffer(ref _meshObjectBuffer, _meshObjects, 72);
            CreateComputeBuffer(ref _vertexBuffer, _vertices, 12);
            CreateComputeBuffer(ref _indexBuffer, _indices, 4);
        }
        private static void CreateComputeBuffer<T>(ref ComputeBuffer buffer, List<T> data, int stride)
            where T : struct
        {
            // Do we already have a compute buffer?
            if (buffer != null)
            {
                // If no data or buffer doesn't match the given criteria, release it
                if (data.Count == 0 || buffer.count != data.Count || buffer.stride != stride)
                {
                    buffer.Release();
                    buffer = null;
                }
            }
            if (data.Count != 0)
            {
                // If the buffer has been released or wasn't there to
                // begin with, create it
                if (buffer == null)
                {
                    buffer = new ComputeBuffer(data.Count, stride);
                }
                // Set data on the buffer
                buffer.SetData(data);
            }
        }
        private void SetComputeBuffer(string name, ComputeBuffer buffer)
        {
            if (buffer != null)
            {
                RayTracingShader.SetBuffer(0, name, buffer);
            }
        }
    }
}
