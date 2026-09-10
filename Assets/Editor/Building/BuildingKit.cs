using System.IO;
using UnityEditor;
using UnityEngine;

namespace VRSim.EditorTools.Building
{
    /// <summary>
    /// Shared construction helpers for building scenario environments out of primitives.
    ///
    /// Every level is generated from code rather than authored by hand, so the layout lives in
    /// version control as something reviewable and diffable instead of a binary scene file. That
    /// only stays practical if the levels share one kit of parts; otherwise each new scenario
    /// becomes a copy of the last one.
    ///
    /// Materials are cached as assets keyed by their settings, so a level with two hundred boxes
    /// still only creates a handful of materials.
    /// </summary>
    public static class BuildingKit
    {
        public const string MaterialsFolder = "Assets/Art/Materials";

        /// <summary>Standard interior wall height in metres.</summary>
        public const float WallHeight = 3f;

        /// <summary>Standard partition thickness in metres.</summary>
        public const float WallThickness = 0.2f;

        // A consistent palette keeps separate levels looking like one building.
        public static readonly Color WallPaint = new Color(0.74f, 0.75f, 0.72f);
        public static readonly Color WallDado = new Color(0.34f, 0.40f, 0.45f);
        public static readonly Color Skirting = new Color(0.22f, 0.23f, 0.25f);
        public static readonly Color FloorVinyl = new Color(0.28f, 0.30f, 0.33f);
        public static readonly Color CeilingTile = new Color(0.62f, 0.63f, 0.66f);
        public static readonly Color Metalwork = new Color(0.55f, 0.57f, 0.60f);
        public static readonly Color Timber = new Color(0.62f, 0.48f, 0.33f);
        public static readonly Color SafetyGreen = new Color(0.09f, 0.55f, 0.22f);
        public static readonly Color SafetyRed = new Color(0.75f, 0.10f, 0.10f);
        public static readonly Color SafetyYellow = new Color(0.92f, 0.72f, 0.08f);

        // ------------------------------------------------------------------ primitives

        /// <param name="noCollider">
        /// Trim, signage and floor markings are decoration. Leaving colliders on them would make
        /// the corridor feel like an obstacle course and would confuse interaction raycasts.
        /// </param>
        public static GameObject Box(string label, Transform parent, Vector3 position, Vector3 size,
            Color colour, bool transparent = false, Color? emission = null, float smoothness = 0.15f,
            bool noCollider = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = label;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial =
                GetMaterial(colour, transparent, emission, smoothness);

            if (noCollider)
                Object.DestroyImmediate(go.GetComponent<Collider>());

            return go;
        }

        public static GameObject Cylinder(string label, Transform parent, Vector3 position,
            Vector3 size, Color colour, float smoothness = 0.3f, bool noCollider = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = label;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial =
                GetMaterial(colour, false, null, smoothness);

            if (noCollider)
                Object.DestroyImmediate(go.GetComponent<Collider>());

            return go;
        }

        /// <summary>
        /// A trigger volume that drives scenario logic. Its renderer is removed: a tinted box was
        /// useful when the level was untextured, but over real scenery it just occludes the thing
        /// the user is supposed to be looking at.
        /// </summary>
        public static GameObject Trigger(string label, Transform parent, Vector3 position, Vector3 size)
        {
            var go = Box(label, parent, position, size, Color.clear, transparent: true);
            go.GetComponent<BoxCollider>().isTrigger = true;
            Object.DestroyImmediate(go.GetComponent<MeshRenderer>());
            return go;
        }

        // ---------------------------------------------------------------- architecture

        /// <summary>A wall with skirting and a dado rail, which reads far better than a flat slab.</summary>
        public static void Wall(string label, Transform parent, Vector3 centre, Vector3 size,
            bool addTrim = true)
        {
            Box(label, parent, centre, size, WallPaint);

            if (!addTrim)
                return;

            var runsAlongZ = size.z > size.x;
            var trimSize = runsAlongZ
                ? new Vector3(size.x + 0.02f, 0.16f, size.z)
                : new Vector3(size.x, 0.16f, size.z + 0.02f);

            Box($"{label} Skirting", parent, new Vector3(centre.x, 0.08f, centre.z),
                trimSize, Skirting, noCollider: true);

            var railSize = runsAlongZ
                ? new Vector3(size.x + 0.03f, 0.07f, size.z)
                : new Vector3(size.x, 0.07f, size.z + 0.03f);

            Box($"{label} Dado", parent, new Vector3(centre.x, 1.05f, centre.z),
                railSize, Metalwork, noCollider: true);
        }

        /// <summary>Floor slab with an optional teleportation area added by the caller.</summary>
        public static GameObject Floor(string label, Transform parent, Vector3 centre, Vector2 size)
        {
            return Box(label, parent, new Vector3(centre.x, -0.05f, centre.z),
                new Vector3(size.x, 0.1f, size.y), FloorVinyl, smoothness: 0.35f);
        }

        public static GameObject Ceiling(string label, Transform parent, Vector3 centre, Vector2 size)
        {
            return Box(label, parent, new Vector3(centre.x, WallHeight, centre.z),
                new Vector3(size.x, 0.1f, size.y), CeilingTile, noCollider: true);
        }

        /// <summary>A recessed panel fitting: an emissive slab with a soft point light beneath it.</summary>
        public static void CeilingLight(Transform parent, Vector3 position, float intensity = 3.2f)
        {
            Box("Light Panel", parent, position, new Vector3(1.2f, 0.06f, 0.35f),
                new Color(1f, 0.98f, 0.92f), emission: new Color(1.6f, 1.55f, 1.4f), noCollider: true);

            var lightObject = new GameObject("Point Light");
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.position = position - new Vector3(0f, 0.3f, 0f);

            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 14f;
            light.intensity = intensity;
            light.color = new Color(1f, 0.97f, 0.9f);
            light.shadows = LightShadows.None;
        }

        /// <summary>Door leaf plus frame. Returns the leaf so the caller can make it interactive.</summary>
        public static GameObject Door(string label, Transform parent, Vector3 position, float width,
            bool alongZ = true)
        {
            var leafSize = alongZ
                ? new Vector3(0.1f, 2.05f, width)
                : new Vector3(width, 2.05f, 0.1f);

            var leaf = Box(label, parent, position, leafSize, new Color(0.52f, 0.40f, 0.28f),
                smoothness: 0.35f);

            var headSize = alongZ
                ? new Vector3(0.16f, 0.12f, width + 0.2f)
                : new Vector3(width + 0.2f, 0.12f, 0.16f);

            Box($"{label} Frame Head", parent,
                new Vector3(position.x, 2.15f, position.z), headSize,
                new Color(0.42f, 0.44f, 0.47f), noCollider: true);

            foreach (var offset in new[] { -1f, 1f })
            {
                var postPosition = alongZ
                    ? new Vector3(position.x, 1.05f, position.z + offset * (width / 2f + 0.08f))
                    : new Vector3(position.x + offset * (width / 2f + 0.08f), 1.05f, position.z);

                var postSize = alongZ
                    ? new Vector3(0.16f, 2.2f, 0.1f)
                    : new Vector3(0.1f, 2.2f, 0.16f);

                Box($"{label} Frame Post", parent, postPosition, postSize,
                    new Color(0.42f, 0.44f, 0.47f), noCollider: true);
            }

            return leaf;
        }

        /// <summary>Emissive running-man style exit sign. Lit signs mean "this way out".</summary>
        public static void ExitSign(Transform parent, Vector3 position, bool lit, bool alongZ = true)
        {
            var size = alongZ
                ? new Vector3(1.1f, 0.4f, 0.06f)
                : new Vector3(0.06f, 0.4f, 1.1f);

            if (lit)
            {
                Box("Exit Sign", parent, position, size, SafetyGreen,
                    emission: new Color(0.15f, 1.5f, 0.45f), noCollider: true);
                Box("Exit Sign Arrow", parent, position + new Vector3(0.36f, 0f, -0.04f),
                    new Vector3(0.22f, 0.22f, 0.02f), Color.white,
                    emission: new Color(1.6f, 1.6f, 1.6f), noCollider: true);
                return;
            }

            // An unlit sign with a cross reads as "closed" without depending on colour, which
            // section 13 requires.
            Box("Exit Sign", parent, position, size, new Color(0.32f, 0.33f, 0.35f), noCollider: true);
            var cross = Box("Exit Sign Cross", parent, position - new Vector3(0f, 0f, 0.04f),
                new Vector3(0.7f, 0.06f, 0.02f), new Color(0.75f, 0.2f, 0.15f), noCollider: true);
            cross.transform.rotation = Quaternion.Euler(0f, 0f, 28f);
        }

        /// <summary>Directional floor chevrons. The apex points the way out.</summary>
        public static void Chevrons(Transform parent, float x, float fromZ, float toZ, float spacing,
            bool pointingNorth = true)
        {
            var sign = pointingNorth ? 1f : -1f;

            for (var z = fromZ; z <= toZ; z += spacing)
            {
                var left = Box($"Chevron {z:0.0} L", parent, new Vector3(x - 0.26f, 0.006f, z),
                    new Vector3(0.62f, 0.01f, 0.13f), new Color(0.20f, 0.78f, 0.36f),
                    emission: new Color(0.08f, 0.32f, 0.13f), noCollider: true);
                left.transform.rotation = Quaternion.Euler(0f, -42f * sign, 0f);

                var right = Box($"Chevron {z:0.0} R", parent, new Vector3(x + 0.26f, 0.006f, z),
                    new Vector3(0.62f, 0.01f, 0.13f), new Color(0.20f, 0.78f, 0.36f),
                    emission: new Color(0.08f, 0.32f, 0.13f), noCollider: true);
                right.transform.rotation = Quaternion.Euler(0f, 42f * sign, 0f);
            }
        }

        // ---------------------------------------------------------------------- props

        /// <summary>Desk with legs, and optionally a chair tucked in behind it.</summary>
        public static void Desk(Transform parent, Vector3 position, bool withChair = true)
        {
            Box("Desk Top", parent, position + new Vector3(0f, 0.74f, 0f),
                new Vector3(1.5f, 0.05f, 0.7f), Timber, smoothness: 0.4f);

            foreach (var offset in new[] { -0.65f, 0.65f })
                Box("Desk Leg", parent, position + new Vector3(offset, 0.37f, 0f),
                    new Vector3(0.06f, 0.74f, 0.6f), new Color(0.24f, 0.25f, 0.28f), noCollider: true);

            if (!withChair)
                return;

            var chair = new Color(0.20f, 0.34f, 0.48f);
            Box("Chair Seat", parent, position + new Vector3(0f, 0.45f, -0.85f),
                new Vector3(0.45f, 0.06f, 0.45f), chair, noCollider: true);
            Box("Chair Back", parent, position + new Vector3(0f, 0.72f, -1.05f),
                new Vector3(0.45f, 0.5f, 0.06f), chair, noCollider: true);
            Cylinder("Chair Post", parent, position + new Vector3(0f, 0.22f, -0.85f),
                new Vector3(0.06f, 0.22f, 0.06f), new Color(0.24f, 0.25f, 0.28f), noCollider: true);
        }

        /// <summary>Fire extinguisher with its wall sign, the pairing a real building uses.</summary>
        public static void FireExtinguisher(Transform parent, Vector3 floorPosition, float wallX)
        {
            Cylinder("Fire Extinguisher", parent, floorPosition + new Vector3(0f, 0.42f, 0f),
                new Vector3(0.22f, 0.32f, 0.22f), SafetyRed, smoothness: 0.65f);
            Cylinder("Extinguisher Neck", parent, floorPosition + new Vector3(0f, 0.76f, 0f),
                new Vector3(0.07f, 0.06f, 0.07f), Metalwork, noCollider: true);
            Box("Extinguisher Sign", parent, new Vector3(wallX, 1.05f, floorPosition.z),
                new Vector3(0.02f, 0.28f, 0.2f), SafetyRed,
                emission: new Color(0.18f, 0.02f, 0.02f), noCollider: true);
        }

        /// <summary>A bank of lockers along a wall running north to south.</summary>
        public static void Lockers(Transform parent, float x, float fromZ, int count, float facing)
        {
            var body = new Color(0.30f, 0.42f, 0.50f);
            var door = new Color(0.35f, 0.48f, 0.57f);

            for (var i = 0; i < count; i++)
            {
                var z = fromZ + i * 0.85f;
                Box($"Locker {i + 1}", parent, new Vector3(x, 0.95f, z),
                    new Vector3(0.45f, 1.9f, 0.8f), body, smoothness: 0.45f);
                Box($"Locker Door {i + 1}", parent, new Vector3(x + 0.23f * facing, 0.95f, z),
                    new Vector3(0.03f, 1.8f, 0.72f), door, smoothness: 0.5f, noCollider: true);
                Box($"Locker Handle {i + 1}", parent,
                    new Vector3(x + 0.26f * facing, 0.95f, z + 0.28f),
                    new Vector3(0.03f, 0.16f, 0.04f), Metalwork, smoothness: 0.8f, noCollider: true);
            }
        }

        /// <summary>Layered haze that reads as smoke. Seeded so a rebuild produces the same level.</summary>
        public static void SmokeBank(Transform parent, Vector3 centre, float width, float length,
            int seed, int layers = 14)
        {
            var random = new System.Random(seed);

            for (var i = 0; i < layers; i++)
            {
                var height = 0.25f + (float)random.NextDouble() * (WallHeight - 0.4f);
                var thickness = 0.5f + (float)random.NextDouble() * 0.9f;
                var slabLength = length * 0.4f + (float)random.NextDouble() * length * 0.6f;

                // Denser towards the ceiling, the way smoke actually banks up.
                var alpha = Mathf.Lerp(0.045f, 0.13f, height / WallHeight);

                var layer = Box($"Smoke Layer {i + 1}", parent,
                    new Vector3(centre.x + (float)(random.NextDouble() - 0.5) * 0.6f, height,
                        centre.z + (float)(random.NextDouble() - 0.5) * length * 0.6f),
                    new Vector3(width, thickness, slabLength),
                    new Color(0.46f, 0.46f, 0.49f, alpha), transparent: true, noCollider: true);

                layer.transform.rotation =
                    Quaternion.Euler(0f, (float)(random.NextDouble() - 0.5) * 12f, 0f);
            }
        }

        // ------------------------------------------------------------------- lighting

        /// <summary>Interior lighting defaults shared by every level.</summary>
        public static void InteriorLighting(float fogStart = 22f, float fogEnd = 60f)
        {
            var sun = new GameObject("Directional Light");
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.75f;
            light.color = new Color(1f, 0.96f, 0.9f);
            light.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.52f, 0.54f, 0.60f);
            RenderSettings.ambientEquatorColor = new Color(0.44f, 0.45f, 0.50f);
            RenderSettings.ambientGroundColor = new Color(0.28f, 0.28f, 0.32f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.24f, 0.25f, 0.29f);
            RenderSettings.fogStartDistance = fogStart;
            RenderSettings.fogEndDistance = fogEnd;
        }

        // ------------------------------------------------------------------ materials

        public static Material GetMaterial(Color colour, bool transparent, Color? emission = null,
            float smoothness = 0.15f)
        {
            Directory.CreateDirectory(MaterialsFolder);

            var key = transparent ? "T" : "O";
            var emissionKey = emission.HasValue ? ColorUtility.ToHtmlStringRGB(emission.Value) : "none";
            var name = $"Sim_{key}_{ColorUtility.ToHtmlStringRGBA(colour)}_{emissionKey}_{smoothness:0.00}.mat";
            var path = $"{MaterialsFolder}/{name}";

            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
                return existing;

            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = colour };
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", 0f);

            if (emission.HasValue)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission.Value);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            if (transparent)
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetFloat("_ZWrite", 0f);
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                material.SetOverrideTag("RenderType", "Transparent");
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }

            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
