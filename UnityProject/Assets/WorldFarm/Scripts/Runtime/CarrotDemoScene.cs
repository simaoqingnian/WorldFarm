using UnityEngine;
using UnityEngine.Rendering;

namespace WorldFarm.Runtime
{
    [DisallowMultipleComponent]
    public sealed class CarrotDemoScene : MonoBehaviour
    {
        private const string GeneratedRootName = "GeneratedCarrotDemo";
        private const float CarrotTopY = 0.98f;
        private const float CarrotHeight = 2.72f;

        [SerializeField] private float autoRotateDegreesPerSecond = 16f;
        [SerializeField] private float dragDegreesPerPixel = 0.18f;

        private Transform carrotPivot;
        private float yaw = -24f;
        private float pitch = 4f;
        private bool dragging;
        private int activeTouchId = -1;
        private Vector2 previousPointerPosition;

        private void Awake()
        {
            ConfigureScene();
            BuildDemo();
            ApplyModelRotation();
        }

        private void Update()
        {
            HandlePointerInput();

            if (!dragging)
            {
                yaw += autoRotateDegreesPerSecond * Time.deltaTime;
            }

            ApplyModelRotation();
        }

        private void ConfigureScene()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.78f, 0.88f, 0.92f);
            RenderSettings.ambientEquatorColor = new Color(0.48f, 0.56f, 0.50f);
            RenderSettings.ambientGroundColor = new Color(0.28f, 0.22f, 0.18f);
            RenderSettings.ambientIntensity = 1.1f;

            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowDistance = 35f;

            var camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();

            }

            camera.orthographic = false;
            camera.fieldOfView = 38f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 80f;
            camera.backgroundColor = new Color(0.62f, 0.77f, 0.82f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.transform.position = new Vector3(0f, 0.35f, -8.5f);
            camera.transform.rotation = Quaternion.LookRotation(new Vector3(0f, -0.1f, 0f) - camera.transform.position, Vector3.up);

            var keyLight = GameObject.Find("Demo Key Light");
            if (keyLight == null)
            {
                keyLight = GameObject.Find("Directional Light") ?? new GameObject("Demo Key Light");
                keyLight.name = "Demo Key Light";
            }

            var light = keyLight.GetComponent<Light>() ?? keyLight.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.94f, 0.82f);
            light.intensity = 1.35f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.55f;
            keyLight.transform.rotation = Quaternion.Euler(48f, -34f, 18f);

            var fillLight = GameObject.Find("Demo Fill Light");
            if (fillLight == null)
            {
                fillLight = new GameObject("Demo Fill Light");
                fillLight.AddComponent<Light>();
            }

            var fill = fillLight.GetComponent<Light>();
            fill.type = LightType.Point;
            fill.color = new Color(0.58f, 0.82f, 1f);
            fill.intensity = 1.6f;
            fill.range = 8f;
            fill.shadows = LightShadows.None;
            fillLight.transform.position = new Vector3(-2.5f, 2.8f, -3.2f);
        }

        private void BuildDemo()
        {
            DestroyGeneratedRoot();

            var generatedRoot = new GameObject(GeneratedRootName);
            generatedRoot.transform.SetParent(transform, false);

            carrotPivot = new GameObject("CarrotPivot").transform;
            carrotPivot.SetParent(generatedRoot.transform, false);
            carrotPivot.localPosition = Vector3.zero;

            var carrotMaterial = CreateMaterial("Demo Carrot Body", new Color(0.93f, 0.37f, 0.09f), 0.32f);
            var carrotShadowMaterial = CreateMaterial("Demo Carrot Soft Ridges", new Color(0.70f, 0.25f, 0.06f), 0.24f);
            var leafMaterial = CreateMaterial("Demo Leaf Primary", new Color(0.16f, 0.56f, 0.24f), 0.42f, true);
            var leafDarkMaterial = CreateMaterial("Demo Leaf Deep Green", new Color(0.06f, 0.34f, 0.16f), 0.35f, true);
            var soilMaterial = CreateMaterial("Demo Warm Soil", new Color(0.42f, 0.29f, 0.18f), 0.18f);

            AddMeshObject("CarrotBody", CreateCarrotBodyMesh(72, 40), carrotMaterial, carrotPivot, Vector3.zero, Quaternion.identity);
            AddGrowthRings(carrotPivot, carrotShadowMaterial);
            AddLeafCluster(carrotPivot, leafMaterial, leafDarkMaterial);
            AddGroundPlane(generatedRoot.transform, soilMaterial);
        }

        private void DestroyGeneratedRoot()
        {
            var existing = transform.Find(GeneratedRootName);
            if (existing == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(existing.gameObject);
            }
            else
            {
                DestroyImmediate(existing.gameObject);
            }
        }

        private static Material CreateMaterial(string name, Color color, float smoothness, bool doubleSided = false)
        {
            var shader = Shader.Find("Standard") ??
                         Shader.Find("Universal Render Pipeline/Lit") ??
                         Shader.Find("Diffuse");

            var material = new Material(shader)
            {
                name = name,
                color = color
            };

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", smoothness);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }

            if (doubleSided && material.HasProperty("_Cull"))
            {
                material.SetInt("_Cull", (int)CullMode.Off);
            }

            return material;
        }

        private static GameObject AddMeshObject(string name, Mesh mesh, Material material, Transform parent, Vector3 localPosition, Quaternion localRotation)
        {
            var meshObject = new GameObject(name);
            meshObject.transform.SetParent(parent, false);
            meshObject.transform.localPosition = localPosition;
            meshObject.transform.localRotation = localRotation;

            var filter = meshObject.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            var renderer = meshObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;

            return meshObject;
        }

        private static void AddGrowthRings(Transform parent, Material material)
        {
            for (var index = 0; index < 6; index++)
            {
                var t = 0.24f + index * 0.095f;
                var radius = CarrotRadius(t) * 1.008f;
                var tubeRadius = Mathf.Lerp(0.007f, 0.003f, t);
                var ring = AddMeshObject(
                    $"GrowthRing_{index:00}",
                    CreateTorusMesh(radius, tubeRadius, 72, 8),
                    material,
                    parent,
                    CarrotCenter(t),
                    Quaternion.identity);

                ring.transform.localScale = new Vector3(0.92f, 1f, 1.08f);
            }
        }

        private static void AddLeafCluster(Transform parent, Material primaryMaterial, Material darkMaterial)
        {
            var crownPosition = CarrotCenter(0f) + new Vector3(0f, 0.17f, 0f);
            var crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crown.name = "LeafCrown";
            crown.transform.SetParent(parent, false);
            crown.transform.localPosition = crownPosition;
            crown.transform.localScale = new Vector3(0.34f, 0.15f, 0.34f);
            crown.GetComponent<MeshRenderer>().sharedMaterial = darkMaterial;
            crown.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.On;
            crown.GetComponent<MeshRenderer>().receiveShadows = true;

            var angles = new[] { -8f, 44f, 96f, 148f, 204f, 260f, 316f };
            for (var index = 0; index < angles.Length; index++)
            {
                var length = Mathf.Lerp(0.68f, 0.96f, index / (float)(angles.Length - 1));
                var height = index % 2 == 0 ? 0.82f : 0.66f;
                var width = index % 3 == 0 ? 0.14f : 0.11f;
                var curve = index % 2 == 0 ? 0.16f : -0.13f;
                var material = index % 2 == 0 ? primaryMaterial : darkMaterial;

                var leaf = AddMeshObject(
                    $"Leaf_{index:00}",
                    CreateLeafMesh(length, height, width, curve, 8),
                    material,
                    parent,
                    crownPosition + new Vector3(0f, 0.03f, 0f),
                    Quaternion.Euler(4f, angles[index], 0f));

                leaf.GetComponent<MeshRenderer>().receiveShadows = false;
            }
        }

        private static void AddGroundPlane(Transform parent, Material material)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "DemoGround";
            ground.transform.SetParent(parent, false);
            ground.transform.localPosition = new Vector3(0f, -2.72f, 0.12f);
            ground.transform.localRotation = Quaternion.identity;
            ground.transform.localScale = new Vector3(0.82f, 1f, 0.82f);

            var renderer = ground.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
        }

        private static Mesh CreateCarrotBodyMesh(int radialSegments, int heightSegments)
        {
            var ringCount = heightSegments + 1;
            var vertices = new Vector3[ringCount * radialSegments + 2];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[(ringCount - 1) * radialSegments * 6 + radialSegments * 6];

            for (var y = 0; y < ringCount; y++)
            {
                var t = y / (float)heightSegments;
                var center = CarrotCenter(t);
                var radius = CarrotRadius(t);
                var squashX = Mathf.Lerp(1.02f, 0.94f, t);
                var squashZ = Mathf.Lerp(1.06f, 0.98f, t);
                var ringStartIndex = y * radialSegments;

                for (var r = 0; r < radialSegments; r++)
                {
                    var angle = Mathf.PI * 2f * r / radialSegments;
                    var ringVariation = 1f + 0.003f * Mathf.Sin(angle * 2f + t * 7f);
                    var x = Mathf.Cos(angle) * radius * squashX * ringVariation;
                    var z = Mathf.Sin(angle) * radius * squashZ * ringVariation;
                    var vertexIndex = ringStartIndex + r;
                    vertices[vertexIndex] = center + new Vector3(x, 0f, z);
                    uvs[vertexIndex] = new Vector2(r / (float)radialSegments, t);
                }
            }

            var topCapIndex = vertices.Length - 2;
            vertices[topCapIndex] = CarrotCenter(0f) + new Vector3(0f, -0.035f, 0f);
            uvs[topCapIndex] = new Vector2(0.5f, 0f);

            var bottomCapIndex = vertices.Length - 1;
            vertices[bottomCapIndex] = CarrotCenter(1f) + new Vector3(0f, 0.018f, 0f);
            uvs[bottomCapIndex] = new Vector2(0.5f, 1f);

            var triangleIndex = 0;
            for (var ring = 0; ring < ringCount - 1; ring++)
            {
                for (var r = 0; r < radialSegments; r++)
                {
                    var nextR = (r + 1) % radialSegments;
                    var a = ring * radialSegments + r;
                    var b = ring * radialSegments + nextR;
                    var c = (ring + 1) * radialSegments + r;
                    var d = (ring + 1) * radialSegments + nextR;

                    triangles[triangleIndex++] = a;
                    triangles[triangleIndex++] = b;
                    triangles[triangleIndex++] = c;

                    triangles[triangleIndex++] = b;
                    triangles[triangleIndex++] = d;
                    triangles[triangleIndex++] = c;
                }
            }

            for (var r = 0; r < radialSegments; r++)
            {
                var nextR = (r + 1) % radialSegments;
                triangles[triangleIndex++] = topCapIndex;
                triangles[triangleIndex++] = nextR;
                triangles[triangleIndex++] = r;

                var bottomRingStart = (ringCount - 1) * radialSegments;
                triangles[triangleIndex++] = bottomCapIndex;
                triangles[triangleIndex++] = bottomRingStart + r;
                triangles[triangleIndex++] = bottomRingStart + nextR;
            }

            var mesh = new Mesh
            {
                name = "Procedural Carrot Body",
                vertices = vertices,
                uv = uvs,
                triangles = triangles
            };

            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateTorusMesh(float majorRadius, float tubeRadius, int majorSegments, int tubeSegments)
        {
            var vertices = new Vector3[majorSegments * tubeSegments];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[majorSegments * tubeSegments * 6];

            var vertexIndex = 0;
            for (var major = 0; major < majorSegments; major++)
            {
                var theta = Mathf.PI * 2f * major / majorSegments;
                var radial = new Vector3(Mathf.Cos(theta), 0f, Mathf.Sin(theta));
                var center = radial * majorRadius;

                for (var tube = 0; tube < tubeSegments; tube++)
                {
                    var phi = Mathf.PI * 2f * tube / tubeSegments;
                    vertices[vertexIndex] = center + radial * (Mathf.Cos(phi) * tubeRadius) + Vector3.up * (Mathf.Sin(phi) * tubeRadius);
                    uvs[vertexIndex] = new Vector2(major / (float)majorSegments, tube / (float)tubeSegments);
                    vertexIndex++;
                }
            }

            var triangleIndex = 0;
            for (var major = 0; major < majorSegments; major++)
            {
                var nextMajor = (major + 1) % majorSegments;
                for (var tube = 0; tube < tubeSegments; tube++)
                {
                    var nextTube = (tube + 1) % tubeSegments;
                    var a = major * tubeSegments + tube;
                    var b = nextMajor * tubeSegments + tube;
                    var c = major * tubeSegments + nextTube;
                    var d = nextMajor * tubeSegments + nextTube;

                    triangles[triangleIndex++] = a;
                    triangles[triangleIndex++] = b;
                    triangles[triangleIndex++] = c;

                    triangles[triangleIndex++] = c;
                    triangles[triangleIndex++] = b;
                    triangles[triangleIndex++] = d;
                }
            }

            var mesh = new Mesh
            {
                name = "Procedural Growth Ring",
                vertices = vertices,
                uv = uvs,
                triangles = triangles
            };

            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateLeafMesh(float length, float height, float width, float sideCurve, int segments)
        {
            var vertices = new Vector3[(segments + 1) * 2];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[segments * 12];

            for (var segment = 0; segment <= segments; segment++)
            {
                var t = segment / (float)segments;
                var leafWidth = width * Mathf.Sin(t * Mathf.PI);
                var center = new Vector3(
                    sideCurve * Mathf.Sin(t * Mathf.PI),
                    height * Mathf.Sin(t * Mathf.PI * 0.62f) - 0.18f * t * t,
                    length * t);

                var leftIndex = segment * 2;
                vertices[leftIndex] = center + Vector3.left * leafWidth;
                vertices[leftIndex + 1] = center + Vector3.right * leafWidth;
                uvs[leftIndex] = new Vector2(0f, t);
                uvs[leftIndex + 1] = new Vector2(1f, t);
            }

            var triangleIndex = 0;
            for (var segment = 0; segment < segments; segment++)
            {
                var left0 = segment * 2;
                var right0 = left0 + 1;
                var left1 = (segment + 1) * 2;
                var right1 = left1 + 1;

                triangles[triangleIndex++] = left0;
                triangles[triangleIndex++] = left1;
                triangles[triangleIndex++] = right0;

                triangles[triangleIndex++] = right0;
                triangles[triangleIndex++] = left1;
                triangles[triangleIndex++] = right1;

                triangles[triangleIndex++] = left0;
                triangles[triangleIndex++] = right0;
                triangles[triangleIndex++] = left1;

                triangles[triangleIndex++] = right0;
                triangles[triangleIndex++] = right1;
                triangles[triangleIndex++] = left1;
            }

            var mesh = new Mesh
            {
                name = "Procedural Carrot Leaf",
                vertices = vertices,
                uv = uvs,
                triangles = triangles
            };

            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Vector3 CarrotCenter(float t)
        {
            var bend = Mathf.Sin(t * Mathf.PI) * 0.10f + t * t * 0.05f;
            var topDomeLift = 0.05f * (1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.18f)));
            var bottomDomeDrop = 0.06f * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.86f) / 0.14f));
            return new Vector3(bend, CarrotTopY + topDomeLift - t * CarrotHeight - bottomDomeDrop, 0f);
        }

        private static float CarrotRadius(float t)
        {
            float radius;
            if (t < 0.18f)
            {
                radius = Mathf.Lerp(0.34f, 0.82f, Mathf.SmoothStep(0f, 1f, t / 0.18f));
            }
            else if (t < 0.78f)
            {
                radius = Mathf.Lerp(0.82f, 0.50f, Mathf.SmoothStep(0f, 1f, (t - 0.18f) / 0.60f));
            }
            else
            {
                radius = Mathf.Lerp(0.50f, 0.18f, Mathf.SmoothStep(0f, 1f, (t - 0.78f) / 0.22f));
            }

            var ridge = 1f + 0.003f * Mathf.Sin(t * 30f);
            return radius * ridge;
        }

        private void HandlePointerInput()
        {
            if (Input.touchCount > 0)
            {
                HandleTouchInput();
                return;
            }

            activeTouchId = -1;

            if (Input.GetMouseButtonDown(0))
            {
                dragging = true;
                previousPointerPosition = Input.mousePosition;
            }
            else if (Input.GetMouseButton(0) && dragging)
            {
                var currentPosition = (Vector2)Input.mousePosition;
                RotateFromDelta(currentPosition - previousPointerPosition);
                previousPointerPosition = currentPosition;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                dragging = false;
            }
        }

        private void HandleTouchInput()
        {
            if (activeTouchId < 0)
            {
                var touch = Input.GetTouch(0);
                activeTouchId = touch.fingerId;
                previousPointerPosition = touch.position;
                dragging = true;
                return;
            }

            for (var index = 0; index < Input.touchCount; index++)
            {
                var touch = Input.GetTouch(index);
                if (touch.fingerId != activeTouchId)
                {
                    continue;
                }

                if (touch.phase == TouchPhase.Canceled || touch.phase == TouchPhase.Ended)
                {
                    activeTouchId = -1;
                    dragging = false;
                    return;
                }

                RotateFromDelta(touch.position - previousPointerPosition);
                previousPointerPosition = touch.position;
                return;
            }

            activeTouchId = -1;
            dragging = false;
        }

        private void RotateFromDelta(Vector2 delta)
        {
            yaw -= delta.x * dragDegreesPerPixel;
            pitch += delta.y * dragDegreesPerPixel;
            pitch = Mathf.Clamp(pitch, -28f, 32f);
        }

        private void ApplyModelRotation()
        {
            if (carrotPivot == null)
            {
                return;
            }

            carrotPivot.localRotation = Quaternion.Euler(pitch, yaw, -8f);
        }
    }
}
