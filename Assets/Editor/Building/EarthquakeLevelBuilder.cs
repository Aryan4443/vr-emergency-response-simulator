using System.IO;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using VRSim.Core;
using VRSim.Interaction;
using VRSim.Scenario;
using VRSim.Scenario.Definitions;

namespace VRSim.EditorTools.Building
{
    /// <summary>
    /// Builds the earthquake drill: a classroom with a glazed wall and tall shelving, a stairwell
    /// at the north end and a lift beside it, opening onto an assembly point outside.
    ///
    /// The layout is the lesson. Desks are the only safe cover, the glazing and the bookcases are
    /// the things that come down, and the route out passes both of them so the user has to choose
    /// the inner wall deliberately rather than by accident. The lift sits next to the stairs for
    /// the same reason: the wrong option has to be genuinely tempting.
    /// </summary>
    public static class EarthquakeLevelBuilder
    {
        const string ScenePath = "Assets/Scenes/Earthquake.unity";
        const string RigPrefabPath =
            "Assets/Samples/XR Interaction Toolkit/3.0.11/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
        const string SimulatorPrefabPath =
            "Assets/Samples/XR Interaction Toolkit/3.0.11/XR Device Simulator/XR Device Simulator.prefab";

        // Classroom occupies negative Z; the corridor and stairwell run north from it.
        const float RoomMinX = -7f;
        const float RoomMaxX = 7f;
        const float RoomMinZ = -10f;
        const float RoomMaxZ = 2f;
        const float CorridorMaxZ = 14f;
        const float CorridorHalfWidth = 2.5f;
        const float DoorWidth = 1.4f;

        [MenuItem("Tools/VR Sim/Build Earthquake Level")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildingKit.InteriorLighting();

            var manager = CreateScenarioManager();
            var building = new GameObject("Building").transform;

            BuildClassroom(building);
            BuildCorridorAndStairs(building);
            var earthquake = CreateEarthquake(manager, building);
            BuildHazards(building, earthquake);
            BuildShelters(building, earthquake);
            BuildAssemblyPoint(building);
            CreatePlayerRig();

            EditorSceneManager.MarkSceneDirty(scene);
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath) ?? "Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);

            Debug.Log($"[Earthquake] Rebuilt {ScenePath}.");
        }

        static ScenarioManager CreateScenarioManager()
        {
            var go = new GameObject("Scenario Manager");
            var manager = go.AddComponent<ScenarioManager>();

            var definition = ScenarioCatalogue.Earthquake();
            var serialized = new SerializedObject(manager);
            serialized.FindProperty("m_ScenarioId").stringValue = definition.ScenarioId;
            serialized.FindProperty("m_TimeLimitSeconds").floatValue = definition.TimeLimitSeconds;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return manager;
        }

        static EarthquakeController CreateEarthquake(ScenarioManager manager, Transform building)
        {
            var go = new GameObject("Earthquake");
            var controller = go.AddComponent<EarthquakeController>();
            controller.Building = building;

            var serialized = new SerializedObject(controller);
            serialized.FindProperty("m_ScenarioManager").objectReferenceValue = manager;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return controller;
        }

        // ------------------------------------------------------------------ classroom

        static void BuildClassroom(Transform building)
        {
            var parent = new GameObject("Classroom").transform;
            parent.SetParent(building, false);

            var centre = new Vector3((RoomMinX + RoomMaxX) / 2f, 0f, (RoomMinZ + RoomMaxZ) / 2f);
            var size = new Vector2(RoomMaxX - RoomMinX, RoomMaxZ - RoomMinZ);

            var floor = BuildingKit.Floor("Classroom Floor", parent, centre, size);
            floor.AddComponent<TeleportationArea>().matchOrientation = MatchOrientation.WorldSpaceUp;
            BuildingKit.Ceiling("Classroom Ceiling", parent, centre, size);

            // South and west are solid; the east wall is the glazing that will fail.
            BuildingKit.Wall("Classroom South Wall", parent,
                new Vector3(centre.x, BuildingKit.WallHeight / 2f, RoomMinZ),
                new Vector3(size.x, BuildingKit.WallHeight, BuildingKit.WallThickness));
            BuildingKit.Wall("Classroom West Wall", parent,
                new Vector3(RoomMinX, BuildingKit.WallHeight / 2f, centre.z),
                new Vector3(BuildingKit.WallThickness, BuildingKit.WallHeight, size.y));

            BuildGlazing(parent);

            // North wall with the doorway through to the corridor.
            var sideWidth = (size.x - DoorWidth) / 2f;
            foreach (var side in new[] { -1f, 1f })
                BuildingKit.Wall("Classroom North Wall", parent,
                    new Vector3(centre.x + side * (DoorWidth / 2f + sideWidth / 2f),
                        BuildingKit.WallHeight / 2f, RoomMaxZ),
                    new Vector3(sideWidth, BuildingKit.WallHeight, BuildingKit.WallThickness));

            BuildingKit.Door("Door_Classroom", parent,
                new Vector3(centre.x, 1.02f, RoomMaxZ), DoorWidth, alongZ: false);

            BuildingKit.CeilingLight(parent, new Vector3(centre.x - 3f, BuildingKit.WallHeight - 0.06f, -6f));
            BuildingKit.CeilingLight(parent, new Vector3(centre.x + 3f, BuildingKit.WallHeight - 0.06f, -2f));

            // Teaching wall and desks. The desks double as the only safe cover in the level.
            BuildingKit.Box("Whiteboard", parent, new Vector3(centre.x, 1.7f, RoomMinZ + 0.12f),
                new Vector3(3.6f, 1.2f, 0.06f), new Color(0.94f, 0.95f, 0.94f), smoothness: 0.6f);

            for (var row = 0; row < 3; row++)
                for (var column = 0; column < 3; column++)
                    BuildingKit.Desk(parent,
                        new Vector3(centre.x + (column - 1) * 2.6f, 0f, RoomMinZ + 3f + row * 2.4f));

            // Tall shelving against the west wall: heavy, top-loaded, and directly on the tempting
            // straight-line route to the door.
            for (var i = 0; i < 3; i++)
                BuildBookcase(parent, new Vector3(RoomMinX + 0.55f, 0f, -7f + i * 2.6f));
        }

        static void BuildGlazing(Transform parent)
        {
            var glass = new Color(0.62f, 0.74f, 0.80f, 0.22f);

            for (var i = 0; i < 4; i++)
            {
                var z = RoomMinZ + 1.6f + i * 2.6f;

                BuildingKit.Box($"Window Mullion {i + 1}", parent,
                    new Vector3(RoomMaxX, BuildingKit.WallHeight / 2f, z - 1.3f),
                    new Vector3(0.16f, BuildingKit.WallHeight, 0.16f),
                    new Color(0.38f, 0.40f, 0.43f));

                BuildingKit.Box($"Window Pane {i + 1}", parent,
                    new Vector3(RoomMaxX, 1.7f, z),
                    new Vector3(0.06f, 2.1f, 2.4f), glass, transparent: true, smoothness: 0.9f,
                    noCollider: true);
            }

            BuildingKit.Box("Glazing Head", parent,
                new Vector3(RoomMaxX, BuildingKit.WallHeight - 0.15f, (RoomMinZ + RoomMaxZ) / 2f),
                new Vector3(0.2f, 0.3f, RoomMaxZ - RoomMinZ), new Color(0.38f, 0.40f, 0.43f));
            BuildingKit.Box("Glazing Cill", parent,
                new Vector3(RoomMaxX, 0.55f, (RoomMinZ + RoomMaxZ) / 2f),
                new Vector3(0.24f, 0.12f, RoomMaxZ - RoomMinZ), new Color(0.55f, 0.56f, 0.58f));
        }

        static void BuildBookcase(Transform parent, Vector3 position)
        {
            var carcass = new Color(0.46f, 0.34f, 0.24f);

            BuildingKit.Box("Bookcase", parent, position + new Vector3(0f, 1.1f, 0f),
                new Vector3(0.42f, 2.2f, 1.8f), carcass, smoothness: 0.3f);

            // Books packed high, which is exactly why these topple.
            for (var shelf = 0; shelf < 4; shelf++)
                BuildingKit.Box($"Books {shelf + 1}", parent,
                    position + new Vector3(0.05f, 0.45f + shelf * 0.5f, 0f),
                    new Vector3(0.3f, 0.32f, 1.6f),
                    new Color(0.35f + shelf * 0.08f, 0.28f, 0.30f + shelf * 0.05f), noCollider: true);
        }

        // ------------------------------------------------------- corridor and stairwell

        static void BuildCorridorAndStairs(Transform building)
        {
            var parent = new GameObject("Corridor").transform;
            parent.SetParent(building, false);

            var centre = new Vector3(0f, 0f, (RoomMaxZ + CorridorMaxZ) / 2f);
            var size = new Vector2(CorridorHalfWidth * 2f, CorridorMaxZ - RoomMaxZ);

            var floor = BuildingKit.Floor("Corridor Floor", parent, centre, size);
            floor.AddComponent<TeleportationArea>().matchOrientation = MatchOrientation.WorldSpaceUp;
            BuildingKit.Ceiling("Corridor Ceiling", parent, centre, size);

            foreach (var side in new[] { -1f, 1f })
                BuildingKit.Wall("Corridor Wall", parent,
                    new Vector3(side * CorridorHalfWidth, BuildingKit.WallHeight / 2f, centre.z),
                    new Vector3(BuildingKit.WallThickness, BuildingKit.WallHeight, size.y));

            for (var z = RoomMaxZ + 2f; z < CorridorMaxZ; z += 4f)
                BuildingKit.CeilingLight(parent, new Vector3(0f, BuildingKit.WallHeight - 0.06f, z));

            BuildingKit.Chevrons(parent, -1.1f, RoomMaxZ + 1.5f, CorridorMaxZ - 1.5f, 2.5f);
            BuildingKit.ExitSign(parent, new Vector3(-1.1f, 2.42f, CorridorMaxZ - 0.35f), lit: true);

            BuildStairwell(parent);
            BuildLift(parent);
        }

        static void BuildStairwell(Transform parent)
        {
            var stairs = new GameObject("Stairwell").transform;
            stairs.SetParent(parent, false);

            BuildingKit.Box("Stairwell Sign", stairs,
                new Vector3(-1.1f, 2.05f, CorridorMaxZ - 0.4f),
                new Vector3(0.8f, 0.28f, 0.04f), BuildingKit.SafetyGreen,
                emission: new Color(0.1f, 0.9f, 0.3f), noCollider: true);

            // Steps descending away from the corridor, so the safe route visibly leads downward
            // and out rather than into another room.
            for (var step = 0; step < 6; step++)
                BuildingKit.Box($"Step {step + 1}", stairs,
                    new Vector3(-1.1f, 0.06f - step * 0.02f, CorridorMaxZ + 0.6f + step * 0.45f),
                    new Vector3(2.2f, 0.14f + step * 0.1f, 0.45f), new Color(0.42f, 0.44f, 0.46f));

            BuildingKit.Box("Stair Handrail", stairs,
                new Vector3(-2.15f, 0.95f, CorridorMaxZ + 1.9f),
                new Vector3(0.06f, 0.06f, 3.2f), BuildingKit.Metalwork, smoothness: 0.8f,
                noCollider: true);
        }

        static void BuildLift(Transform parent)
        {
            var lift = new GameObject("Lift").transform;
            lift.SetParent(parent, false);

            // Deliberately inviting: brushed doors, a call panel, its own sign.
            BuildingKit.Box("Lift Surround", lift,
                new Vector3(1.6f, BuildingKit.WallHeight / 2f, CorridorMaxZ - 0.25f),
                new Vector3(2.1f, BuildingKit.WallHeight, 0.3f), new Color(0.48f, 0.50f, 0.53f));

            foreach (var side in new[] { -1f, 1f })
                BuildingKit.Box("Lift Door", lift,
                    new Vector3(1.6f + side * 0.45f, 1.1f, CorridorMaxZ - 0.42f),
                    new Vector3(0.88f, 2.2f, 0.06f), new Color(0.66f, 0.68f, 0.72f),
                    smoothness: 0.85f, noCollider: true);

            BuildingKit.Box("Lift Call Panel", lift,
                new Vector3(2.75f, 1.15f, CorridorMaxZ - 0.42f),
                new Vector3(0.16f, 0.28f, 0.04f), new Color(0.2f, 0.22f, 0.25f), noCollider: true);

            // The sign that tells the user not to use it. Wording plus a cross, never colour alone.
            BuildingKit.Box("Lift Notice", lift,
                new Vector3(1.6f, 2.5f, CorridorMaxZ - 0.42f),
                new Vector3(1.1f, 0.34f, 0.04f), new Color(0.85f, 0.78f, 0.2f),
                emission: new Color(0.3f, 0.26f, 0.04f), noCollider: true);

            var cross = BuildingKit.Box("Lift Notice Cross", lift,
                new Vector3(1.6f, 2.5f, CorridorMaxZ - 0.45f),
                new Vector3(0.8f, 0.05f, 0.02f), new Color(0.7f, 0.15f, 0.12f), noCollider: true);
            cross.transform.rotation = Quaternion.Euler(0f, 0f, 22f);
        }

        // -------------------------------------------------------------------- hazards

        static void BuildHazards(Transform building, EarthquakeController earthquake)
        {
            var parent = new GameObject("Hazards").transform;
            parent.SetParent(building, false);

            // The strip of floor in front of the glazing.
            var glazingZone = BuildingKit.Trigger("FallingHazard_Glazing", parent,
                new Vector3(RoomMaxX - 1.1f, 1.2f, (RoomMinZ + RoomMaxZ) / 2f),
                new Vector3(2.2f, 2.4f, RoomMaxZ - RoomMinZ - 0.6f));
            var glazingHazard = glazingZone.AddComponent<FallingHazardZone>();
            glazingHazard.ZoneId = "FallingHazard_Glazing";
            glazingHazard.Earthquake = earthquake;

            // The strip in front of the bookcases.
            var shelvingZone = BuildingKit.Trigger("FallingHazard_Shelving", parent,
                new Vector3(RoomMinX + 1.5f, 1.2f, -4.5f),
                new Vector3(2.2f, 2.4f, 7.5f));
            var shelvingHazard = shelvingZone.AddComponent<FallingHazardZone>();
            shelvingHazard.ZoneId = "FallingHazard_Shelving";
            shelvingHazard.Earthquake = earthquake;

            // Taking the lift is the critical unsafe action for this drill.
            var liftTrigger = BuildingKit.Trigger("BlockedRoute_Lift", parent,
                new Vector3(1.6f, 1.2f, CorridorMaxZ - 1.1f),
                new Vector3(2f, 2.4f, 1.2f));
            var blocked = liftTrigger.AddComponent<BlockedRoute>();
            blocked.ObjectId = "BlockedRoute_Lift";
        }

        static void BuildShelters(Transform building, EarthquakeController earthquake)
        {
            var parent = new GameObject("Shelters").transform;
            parent.SetParent(building, false);

            // Cover under the middle row of desks, away from both the glazing and the shelving.
            var centreX = (RoomMinX + RoomMaxX) / 2f;

            for (var i = 0; i < 2; i++)
            {
                var z = RoomMinZ + 5.4f + i * 2.4f;
                var zone = BuildingKit.Trigger($"Shelter_{i + 1}", parent,
                    new Vector3(centreX, 0.45f, z), new Vector3(1.5f, 0.9f, 1f));

                var shelter = zone.AddComponent<ShelterZone>();
                shelter.ShelterId = $"Shelter_{i + 1}";
                shelter.Earthquake = earthquake;
            }
        }

        static void BuildAssemblyPoint(Transform building)
        {
            var parent = new GameObject("Assembly Point").transform;
            parent.SetParent(building, false);

            var centre = new Vector3(-1.1f, 0f, CorridorMaxZ + 8f);

            BuildingKit.Box("Assembly Ground", parent, centre - new Vector3(0f, 1.2f, 0f),
                new Vector3(14f, 0.2f, 10f), new Color(0.30f, 0.34f, 0.28f));

            BuildingKit.Box("Assembly Sign Post", parent, centre + new Vector3(0f, -0.4f, 0f),
                new Vector3(0.1f, 1.6f, 0.1f), BuildingKit.Metalwork, noCollider: true);
            BuildingKit.Box("Assembly Sign", parent, centre + new Vector3(0f, 0.6f, 0f),
                new Vector3(1.3f, 0.9f, 0.06f), BuildingKit.SafetyGreen,
                emission: new Color(0.08f, 0.5f, 0.16f), noCollider: true);

            var zone = BuildingKit.Trigger("assembly_point", parent,
                centre + new Vector3(0f, 0.2f, 1.5f), new Vector3(6f, 2.6f, 4f));
            var exit = zone.AddComponent<ExitZone>();
            exit.ExitId = "assembly_point";
            exit.IsCorrectExit = true;
        }

        static void CreatePlayerRig()
        {
            var spawn = new Vector3(0f, 0f, -3.5f);

            var rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RigPrefabPath);
            if (rigPrefab == null)
            {
                Debug.LogWarning($"[Earthquake] XR rig prefab not found at {RigPrefabPath}.");
                return;
            }

            var rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab);
            rig.name = "XR Origin (XR Rig)";
            rig.transform.position = spawn;

            var origin = rig.GetComponent<XROrigin>();
            var body = origin != null && origin.CameraFloorOffsetObject != null
                ? origin.CameraFloorOffsetObject
                : rig;

            var playerBody = new GameObject("Player Body") { tag = "Player" };
            playerBody.transform.SetParent(body.transform, false);
            var capsule = playerBody.AddComponent<CapsuleCollider>();
            capsule.isTrigger = true;
            capsule.height = 1.7f;
            capsule.radius = 0.25f;
            capsule.center = new Vector3(0f, 0.85f, 0f);
            var rigidbody = playerBody.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;
            playerBody.AddComponent<PlayerBodyFollower>().Target =
                origin != null ? origin.Camera.transform : null;

            var quickObject = new GameObject("Desktop Quick Interact");
            var quick = quickObject.AddComponent<DesktopQuickInteract>();
            quick.LeftHand = FindDescendant(rig.transform, "Left Controller");
            quick.RightHand = FindDescendant(rig.transform, "Right Controller");

            var simulatorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SimulatorPrefabPath);
            if (simulatorPrefab == null)
                return;

            var development = new GameObject("Development");
            var bootstrap = development.AddComponent<DesktopSimulatorBootstrap>();
            var serialized = new SerializedObject(bootstrap);
            serialized.FindProperty("m_SimulatorPrefab").objectReferenceValue = simulatorPrefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static Transform FindDescendant(Transform root, string childName)
        {
            if (root.name == childName)
                return root;

            foreach (Transform child in root)
            {
                var found = FindDescendant(child, childName);
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}
