using System.IO;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.UI;
using VRSim.Accessibility;
using VRSim.Core;
using VRSim.Interaction;
using VRSim.Scenario;
using VRSim.UI;

namespace VRSim.EditorTools
{
    /// <summary>
    /// Builds the milestone 1 graybox for the fire evacuation scenario: a building interior with
    /// a hallway and two rooms, the player rig, teleportation, and every interactive object the
    /// MVP in section 3 calls for.
    ///
    /// Everything is primitives and flat colours on purpose. Section 12 asks for the layout to be
    /// blocked out first and for Blender assets to replace the placeholders only once the
    /// scenario works from beginning to end.
    ///
    /// Re-running rebuilds the scene from scratch, so edit the numbers here rather than the scene.
    /// </summary>
    public static class GrayboxBuilder
    {
        const string ScenePath = "Assets/Scenes/FireEvacuation.unity";
        const string RigPrefabPath =
            "Assets/Samples/XR Interaction Toolkit/3.0.11/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
        const string SimulatorPrefabPath =
            "Assets/Samples/XR Interaction Toolkit/3.0.11/XR Device Simulator/XR Device Simulator.prefab";
        const string MaterialsFolder = "Assets/Art/Materials";

        // Building dimensions in metres. The hallway runs north to south along Z.
        const float WallHeight = 3f;
        const float WallThickness = 0.2f;
        const float HallHalfWidth = 2f;
        const float HallMinZ = -12f;
        const float HallMaxZ = 12f;
        const float RoomDepthMinZ = -6f;
        const float RoomDepthMaxZ = 2f;
        const float RoomOuterX = 12f;
        const float DoorWidth = 1.2f;

        [MenuItem("Tools/VR Sim/Build Fire Evacuation Graybox")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateLighting();
            var manager = CreateScenarioManager();
            CreateFloorAndCeiling();
            CreateWalls();
            CreateRooms();
            CreateInteractables();
            CreateHazards();
            CreateExits();
            CreateHallwayProps();
            CreatePlayerRig();
            CreateUserInterface();

            EditorSceneManager.MarkSceneDirty(scene);
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath) ?? "Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);

            Debug.Log($"[Graybox] Rebuilt {ScenePath} with scenario manager '{manager.name}'.");
        }

        // ------------------------------------------------------------------ scene parts

        static void CreateLighting()
        {
            var sun = new GameObject("Directional Light");
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.75f;
            light.color = new Color(1f, 0.96f, 0.9f);
            light.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Cool, slightly dim ambient reads as an interior lit by its own ceiling fittings.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.52f, 0.54f, 0.60f);
            RenderSettings.ambientEquatorColor = new Color(0.44f, 0.45f, 0.50f);
            RenderSettings.ambientGroundColor = new Color(0.28f, 0.28f, 0.32f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.24f, 0.25f, 0.29f);
            RenderSettings.fogStartDistance = 22f;
            RenderSettings.fogEndDistance = 60f;

            var fittings = new GameObject("Ceiling Lights").transform;
            for (var z = HallMinZ + 2f; z <= HallMaxZ - 1f; z += 4f)
                CreateCeilingLight(fittings, new Vector3(0f, WallHeight - 0.06f, z));

            CreateCeilingLight(fittings, new Vector3(-7f, WallHeight - 0.06f, -2f));
            CreateCeilingLight(fittings, new Vector3(7f, WallHeight - 0.06f, -2f));
        }

        /// <summary>A recessed panel fitting: an emissive slab with a soft point light beneath it.</summary>
        static void CreateCeilingLight(Transform parent, Vector3 position)
        {
            CreateBox("Light Panel", parent, position, new Vector3(1.2f, 0.06f, 0.35f),
                new Color(1f, 0.98f, 0.92f), emission: new Color(1.6f, 1.55f, 1.4f));

            var lightObject = new GameObject("Point Light");
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.position = position - new Vector3(0f, 0.3f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 14f;
            light.intensity = 3.2f;
            light.color = new Color(1f, 0.97f, 0.9f);
            light.shadows = LightShadows.None;
        }

        static ScenarioManager CreateScenarioManager()
        {
            var go = new GameObject("Scenario Manager");
            return go.AddComponent<ScenarioManager>();
        }

        static void CreateFloorAndCeiling()
        {
            var floorParent = new GameObject("Floor").transform;

            // One slab covering the hallway and both rooms, so teleportation is continuous.
            var floor = CreateBox("Floor Slab", floorParent,
                new Vector3(0f, -0.05f, (HallMinZ + HallMaxZ) / 2f),
                new Vector3(RoomOuterX * 2f, 0.1f, HallMaxZ - HallMinZ),
                new Color(0.28f, 0.30f, 0.33f), smoothness: 0.35f);

            // Teleportation is the primary locomotion required by sections 3 and 13.
            var area = floor.AddComponent<TeleportationArea>();
            area.matchOrientation = MatchOrientation.WorldSpaceUp;

            CreateBox("Ceiling", floorParent,
                new Vector3(0f, WallHeight, (HallMinZ + HallMaxZ) / 2f),
                new Vector3(RoomOuterX * 2f, 0.1f, HallMaxZ - HallMinZ),
                new Color(0.62f, 0.63f, 0.66f));

            // A painted guide line down the hallway, the sort a real building uses to mark a route.
            CreateBox("Hallway Guide Line", floorParent,
                new Vector3(0f, 0.005f, (HallMinZ + HallMaxZ) / 2f),
                new Vector3(0.12f, 0.01f, HallMaxZ - HallMinZ),
                new Color(0.85f, 0.82f, 0.35f), noCollider: true);

            // Green chevrons pointing north, towards the safe exit.
            var chevrons = new GameObject("Exit Chevrons").transform;
            chevrons.SetParent(floorParent, false);
            for (var z = -6f; z <= 10f; z += 2.5f)
            {
                // Two bars meeting on the centre line, so the arrow points north up the corridor.
                var left = CreateBox($"Chevron {z} L", chevrons,
                    new Vector3(-0.26f, 0.006f, z), new Vector3(0.62f, 0.01f, 0.13f),
                    new Color(0.20f, 0.78f, 0.36f), emission: new Color(0.08f, 0.32f, 0.13f),
                    noCollider: true);
                left.transform.rotation = Quaternion.Euler(0f, -42f, 0f);

                var right = CreateBox($"Chevron {z} R", chevrons,
                    new Vector3(0.26f, 0.006f, z), new Vector3(0.62f, 0.01f, 0.13f),
                    new Color(0.20f, 0.78f, 0.36f), emission: new Color(0.08f, 0.32f, 0.13f),
                    noCollider: true);
                right.transform.rotation = Quaternion.Euler(0f, 42f, 0f);
            }
        }

        static void CreateWalls()
        {
            var parent = new GameObject("Hallway").transform;
            var colour = new Color(0.74f, 0.75f, 0.72f);

            // Skirting and a dado rail down both sides break up the flat walls.
            foreach (var x in new[] { -HallHalfWidth + 0.12f, HallHalfWidth - 0.12f })
            {
                CreateBox("Skirting", parent,
                    new Vector3(x, 0.08f, (HallMinZ + HallMaxZ) / 2f),
                    new Vector3(0.06f, 0.16f, HallMaxZ - HallMinZ),
                    new Color(0.22f, 0.23f, 0.25f), noCollider: true);

                CreateBox("Dado Rail", parent,
                    new Vector3(x, 1.05f, (HallMinZ + HallMaxZ) / 2f),
                    new Vector3(0.05f, 0.07f, HallMaxZ - HallMinZ),
                    new Color(0.38f, 0.42f, 0.48f), noCollider: true);

                CreateBox("Lower Wall", parent,
                    new Vector3(x, 0.6f, (HallMinZ + HallMaxZ) / 2f),
                    new Vector3(0.04f, 0.9f, HallMaxZ - HallMinZ),
                    new Color(0.34f, 0.40f, 0.45f), noCollider: true);
            }

            // Hallway side walls, split so a doorway opening is left into each room.
            CreateWallWithDoorway(parent, "Hall Wall West", -HallHalfWidth, colour);
            CreateWallWithDoorway(parent, "Hall Wall East", HallHalfWidth, colour);

            // North and south end caps, each with an exit opening left in the middle.
            CreateEndWall(parent, "North Wall", HallMaxZ, colour);
            CreateEndWall(parent, "South Wall", HallMinZ, colour);
        }

        static void CreateWallWithDoorway(Transform parent, string label, float x, Color colour)
        {
            const float doorCentreZ = (RoomDepthMinZ + RoomDepthMaxZ) / 2f;
            var southLength = doorCentreZ - DoorWidth / 2f - HallMinZ;
            var northLength = HallMaxZ - (doorCentreZ + DoorWidth / 2f);

            CreateBox($"{label} South", parent,
                new Vector3(x, WallHeight / 2f, HallMinZ + southLength / 2f),
                new Vector3(WallThickness, WallHeight, southLength), colour);

            CreateBox($"{label} North", parent,
                new Vector3(x, WallHeight / 2f, HallMaxZ - northLength / 2f),
                new Vector3(WallThickness, WallHeight, northLength), colour);
        }

        static void CreateEndWall(Transform parent, string label, float z, Color colour)
        {
            var sideWidth = HallHalfWidth - DoorWidth / 2f;
            var offset = DoorWidth / 2f + sideWidth / 2f;

            CreateBox($"{label} Left", parent,
                new Vector3(-offset, WallHeight / 2f, z),
                new Vector3(sideWidth, WallHeight, WallThickness), colour);

            CreateBox($"{label} Right", parent,
                new Vector3(offset, WallHeight / 2f, z),
                new Vector3(sideWidth, WallHeight, WallThickness), colour);
        }

        static void CreateRooms()
        {
            CreateRoom("Room West", -1f, new Color(0.78f, 0.8f, 0.85f));
            CreateRoom("Room East", 1f, new Color(0.85f, 0.8f, 0.78f));
        }

        /// <param name="side">-1 builds the room west of the hallway, +1 builds it east.</param>
        static void CreateRoom(string label, float side, Color colour)
        {
            var parent = new GameObject(label).transform;
            var innerX = side * HallHalfWidth;
            var outerX = side * RoomOuterX;
            var centreX = (innerX + outerX) / 2f;
            var width = Mathf.Abs(outerX - innerX);

            CreateBox($"{label} Outer Wall", parent,
                new Vector3(outerX, WallHeight / 2f, (RoomDepthMinZ + RoomDepthMaxZ) / 2f),
                new Vector3(WallThickness, WallHeight, RoomDepthMaxZ - RoomDepthMinZ), colour);

            CreateBox($"{label} South Wall", parent,
                new Vector3(centreX, WallHeight / 2f, RoomDepthMinZ),
                new Vector3(width, WallHeight, WallThickness), colour);

            CreateBox($"{label} North Wall", parent,
                new Vector3(centreX, WallHeight / 2f, RoomDepthMaxZ),
                new Vector3(width, WallHeight, WallThickness), colour);

            CreateBox($"{label} Skirting South", parent,
                new Vector3(centreX, 0.08f, RoomDepthMinZ + 0.12f),
                new Vector3(width, 0.16f, 0.06f), new Color(0.22f, 0.23f, 0.25f), noCollider: true);

            // Rows of desks and chairs so the space reads as a classroom and gives a sense of scale.
            var desk = new Color(0.62f, 0.48f, 0.33f);
            var frame = new Color(0.24f, 0.25f, 0.28f);
            var chair = new Color(0.20f, 0.34f, 0.48f);

            for (var row = 0; row < 2; row++)
            {
                for (var column = 0; column < 3; column++)
                {
                    var x = centreX + (column - 1) * 2.3f;
                    var z = RoomDepthMinZ + 2f + row * 2.4f;

                    CreateBox($"{label} Desk Top", parent, new Vector3(x, 0.74f, z),
                        new Vector3(1.5f, 0.05f, 0.7f), desk, smoothness: 0.4f);
                    CreateBox($"{label} Desk Leg A", parent, new Vector3(x - 0.65f, 0.37f, z),
                        new Vector3(0.06f, 0.74f, 0.6f), frame, noCollider: true);
                    CreateBox($"{label} Desk Leg B", parent, new Vector3(x + 0.65f, 0.37f, z),
                        new Vector3(0.06f, 0.74f, 0.6f), frame, noCollider: true);

                    CreateBox($"{label} Chair Seat", parent, new Vector3(x, 0.45f, z - 0.85f),
                        new Vector3(0.45f, 0.06f, 0.45f), chair, noCollider: true);
                    CreateBox($"{label} Chair Back", parent, new Vector3(x, 0.72f, z - 1.05f),
                        new Vector3(0.45f, 0.5f, 0.06f), chair, noCollider: true);
                    CreateCylinder($"{label} Chair Post", parent, new Vector3(x, 0.22f, z - 0.85f),
                        new Vector3(0.06f, 0.22f, 0.06f), frame);
                }
            }

            // A whiteboard on the far wall and a teaching desk complete the room.
            CreateBox($"{label} Whiteboard", parent,
                new Vector3(outerX - side * 0.12f, 1.7f, (RoomDepthMinZ + RoomDepthMaxZ) / 2f),
                new Vector3(0.06f, 1.2f, 3.2f), new Color(0.94f, 0.95f, 0.94f), smoothness: 0.6f);
            CreateBox($"{label} Whiteboard Tray", parent,
                new Vector3(outerX - side * 0.2f, 1.05f, (RoomDepthMinZ + RoomDepthMaxZ) / 2f),
                new Vector3(0.12f, 0.05f, 3.2f), new Color(0.55f, 0.57f, 0.6f), noCollider: true);

            CreateBox($"{label} Bin", parent,
                new Vector3(centreX + 3.6f, 0.22f, RoomDepthMaxZ - 0.8f),
                new Vector3(0.38f, 0.44f, 0.38f), new Color(0.25f, 0.35f, 0.28f));
        }

        static void CreateInteractables()
        {
            var parent = new GameObject("Interactables").transform;

            // Fire alarm on the hallway wall beside the west room doorway, with a backing plate
            // and a strobe above it that the scenario switches on.
            CreateBox("Alarm Backing Plate", parent,
                new Vector3(-HallHalfWidth + 0.13f, 1.4f, -1f),
                new Vector3(0.03f, 0.42f, 0.34f), new Color(0.85f, 0.83f, 0.78f), noCollider: true);

            var alarm = CreateBox("FireAlarm_A", parent,
                new Vector3(-HallHalfWidth + 0.2f, 1.4f, -1f),
                new Vector3(0.12f, 0.26f, 0.2f), new Color(0.80f, 0.11f, 0.11f),
                emission: new Color(0.22f, 0.02f, 0.02f), smoothness: 0.5f);
            AddInteractable<FireAlarm>(alarm, "FireAlarm_A");

            CreateBox("Alarm Call Point", parent,
                new Vector3(-HallHalfWidth + 0.27f, 1.4f, -1f),
                new Vector3(0.02f, 0.13f, 0.11f), new Color(0.95f, 0.95f, 0.9f), noCollider: true);

            var strobe = CreateBox("Alarm Strobe", parent,
                new Vector3(-HallHalfWidth + 0.2f, 2.3f, -1f),
                new Vector3(0.16f, 0.14f, 0.22f), new Color(0.95f, 0.85f, 0.3f), noCollider: true);
            var strobeLight = new GameObject("Strobe Light");
            strobeLight.transform.SetParent(strobe.transform, false);
            var light = strobeLight.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 12f;
            light.intensity = 3f;
            light.color = new Color(1f, 0.85f, 0.4f);
            var flasher = strobe.AddComponent<AlarmStrobe>();
            flasher.Light = light;
            flasher.Renderer = strobe.GetComponent<MeshRenderer>();

            // Evacuation map opposite the alarm, in a frame.
            CreateBox("Map Frame", parent,
                new Vector3(HallHalfWidth - 0.12f, 1.5f, -1f),
                new Vector3(0.05f, 0.84f, 1.14f), new Color(0.20f, 0.22f, 0.26f), noCollider: true);

            var map = CreateBox("EvacuationMap_A", parent,
                new Vector3(HallHalfWidth - 0.16f, 1.5f, -1f),
                new Vector3(0.05f, 0.74f, 1.02f), new Color(0.96f, 0.96f, 0.92f),
                emission: new Color(0.1f, 0.1f, 0.09f), smoothness: 0.5f);
            AddInteractable<EvacuationMap>(map, "EvacuationMap_A");

            // A simple plan drawn on the map: a corridor line and a green dot at the safe exit.
            CreateBox("Map Corridor", parent,
                new Vector3(HallHalfWidth - 0.19f, 1.5f, -1f),
                new Vector3(0.01f, 0.5f, 0.1f), new Color(0.25f, 0.27f, 0.3f), noCollider: true);
            CreateBox("Map Exit Marker", parent,
                new Vector3(HallHalfWidth - 0.19f, 1.78f, -1f),
                new Vector3(0.01f, 0.1f, 0.1f), new Color(0.15f, 0.7f, 0.3f),
                emission: new Color(0.1f, 0.6f, 0.2f), noCollider: true);
            CreateBox("Map Title", parent,
                new Vector3(HallHalfWidth - 0.19f, 1.82f, -1.35f),
                new Vector3(0.01f, 0.06f, 0.3f), new Color(0.2f, 0.22f, 0.25f), noCollider: true);

            // Door between the hallway and the west room, where the user starts.
            var door = CreateBox("Door_WestRoom", parent,
                new Vector3(-HallHalfWidth, 1.05f, -2f),
                new Vector3(0.1f, 2.05f, DoorWidth), new Color(0.52f, 0.40f, 0.28f),
                smoothness: 0.35f);
            AddInteractable<InteractiveDoor>(door, "Door_WestRoom");

            CreateBox("Door Handle", parent,
                new Vector3(-HallHalfWidth - 0.09f, 1.05f, -2.4f),
                new Vector3(0.09f, 0.05f, 0.16f), new Color(0.72f, 0.70f, 0.55f),
                smoothness: 0.85f, noCollider: true);
        }

        static void CreateHazards()
        {
            var parent = new GameObject("Hazards").transform;

            // Layered translucent slabs read as smoke banking up, and are far more legible than a
            // single flat box. Only the parent carries the trigger.
            var random = new System.Random(20260909);
            for (var i = 0; i < 14; i++)
            {
                // Thin slabs at jittered heights and depths. Individually faint, they build up into
                // a bank of haze without any one of them reading as a pane of glass.
                var height = 0.25f + (float)random.NextDouble() * (WallHeight - 0.4f);
                var depth = -9.4f + (float)random.NextDouble() * 2.6f;
                var thickness = 0.5f + (float)random.NextDouble() * 0.9f;
                var length = 1.6f + (float)random.NextDouble() * 2.4f;

                // Denser towards the ceiling, the way smoke actually banks up.
                var alpha = Mathf.Lerp(0.045f, 0.13f, height / WallHeight);

                var layer = CreateBox($"Smoke Layer {i + 1}", parent,
                    new Vector3((float)(random.NextDouble() - 0.5) * 0.6f, height, depth),
                    new Vector3(HallHalfWidth * 2f - 0.05f, thickness, length),
                    new Color(0.46f, 0.46f, 0.49f, alpha), transparent: true, noCollider: true);
                layer.transform.rotation = Quaternion.Euler(0f, (float)(random.NextDouble() - 0.5) * 12f, 0f);
            }

            // Hazard warning sign on the wall, so the danger is not signalled by haze alone.
            CreateBox("Smoke Warning Sign", parent,
                new Vector3(HallHalfWidth - 0.13f, 1.9f, -6.4f),
                new Vector3(0.04f, 0.4f, 0.4f), new Color(0.92f, 0.75f, 0.10f),
                emission: new Color(0.35f, 0.26f, 0.02f), noCollider: true);

            // Also logic only: the smoke layers above are what the user actually sees.
            var hazard = CreateBox("HazardZone_A", parent,
                new Vector3(0f, WallHeight / 2f, -8.5f),
                new Vector3(HallHalfWidth * 2f, WallHeight, 4f), Color.clear, transparent: true);
            Object.DestroyImmediate(hazard.GetComponent<MeshRenderer>());
            var hazardCollider = hazard.GetComponent<BoxCollider>();
            hazardCollider.isTrigger = true;
            var zone = hazard.AddComponent<HazardZone>();
            zone.ZoneId = "HazardZone_A";

            // Barrier making the south route impassable. Forcing it is the critical unsafe action.
            var barrier = CreateBox("BlockedRoute_A", parent,
                new Vector3(0f, 0.55f, -11f),
                new Vector3(HallHalfWidth * 2f, 0.14f, 0.12f), new Color(0.92f, 0.72f, 0.08f),
                emission: new Color(0.3f, 0.22f, 0.02f));
            AddInteractable<BlockedRoute>(barrier, "BlockedRoute_A");

            CreateBox("Barrier Lower Rail", parent, new Vector3(0f, 0.28f, -11f),
                new Vector3(HallHalfWidth * 2f, 0.1f, 0.1f), new Color(0.92f, 0.72f, 0.08f),
                noCollider: true);
            foreach (var x in new[] { -HallHalfWidth + 0.3f, HallHalfWidth - 0.3f })
                CreateCylinder("Barrier Post", parent, new Vector3(x, 0.4f, -11f),
                    new Vector3(0.08f, 0.4f, 0.08f), new Color(0.28f, 0.29f, 0.32f));
        }

        static void CreateExits()
        {
            var parent = new GameObject("Exits").transform;

            // A lit stairwell beyond the north exit: the doorway has to lead somewhere, or it
            // reads as a hole in the building.
            var vestibuleWall = new Color(0.66f, 0.68f, 0.70f);
            CreateBox("North Vestibule Floor", parent, new Vector3(0f, -0.05f, HallMaxZ + 2.6f),
                new Vector3(5f, 0.1f, 5.4f), new Color(0.32f, 0.34f, 0.37f));
            CreateBox("North Vestibule Ceiling", parent, new Vector3(0f, WallHeight, HallMaxZ + 2.6f),
                new Vector3(5f, 0.1f, 5.4f), new Color(0.62f, 0.63f, 0.66f));
            CreateBox("North Vestibule Back", parent, new Vector3(0f, WallHeight / 2f, HallMaxZ + 5.2f),
                new Vector3(5f, WallHeight, WallThickness), vestibuleWall);
            foreach (var side in new[] { -1f, 1f })
                CreateBox("North Vestibule Side", parent,
                    new Vector3(side * 2.5f, WallHeight / 2f, HallMaxZ + 2.6f),
                    new Vector3(WallThickness, WallHeight, 5.4f), vestibuleWall);

            CreateCeilingLight(parent, new Vector3(0f, WallHeight - 0.06f, HallMaxZ + 2.6f));

            // Steps down and out, so the safe exit visibly leads away from the building.
            for (var step = 0; step < 4; step++)
                CreateBox($"Step {step + 1}", parent,
                    new Vector3(0f, 0.06f + step * 0.001f, HallMaxZ + 3.4f + step * 0.42f),
                    new Vector3(3.2f, 0.12f + step * 0.12f, 0.42f), new Color(0.42f, 0.44f, 0.46f));

            // Behind the south door: a dead end, immediately visible as no way out.
            CreateBox("South Dead End", parent, new Vector3(0f, WallHeight / 2f, HallMinZ - 2.2f),
                new Vector3(5f, WallHeight, WallThickness), new Color(0.30f, 0.29f, 0.30f));
            CreateBox("South Dead End Floor", parent, new Vector3(0f, -0.05f, HallMinZ - 1.2f),
                new Vector3(5f, 0.1f, 2.4f), new Color(0.26f, 0.26f, 0.28f));
            CreateBox("South Dead End Ceiling", parent, new Vector3(0f, WallHeight, HallMinZ - 1.2f),
                new Vector3(5f, 0.1f, 2.4f), new Color(0.5f, 0.5f, 0.52f));
            foreach (var side in new[] { -1f, 1f })
                CreateBox("South Dead End Side", parent,
                    new Vector3(side * 2.5f, WallHeight / 2f, HallMinZ - 1.2f),
                    new Vector3(WallThickness, WallHeight, 2.4f), new Color(0.30f, 0.29f, 0.30f));

            // North exit is the safe route. The sign is emissive so it reads from down the hallway,
            // and it is backed up by the floor chevrons rather than by colour alone.
            CreateBox("North Exit Sign", parent,
                new Vector3(0f, 2.42f, HallMaxZ - 0.3f),
                new Vector3(1.1f, 0.4f, 0.06f), new Color(0.09f, 0.55f, 0.22f),
                emission: new Color(0.15f, 1.5f, 0.45f), noCollider: true);
            CreateBox("North Exit Sign Arrow", parent,
                new Vector3(0.36f, 2.42f, HallMaxZ - 0.34f),
                new Vector3(0.22f, 0.22f, 0.02f), Color.white,
                emission: new Color(1.6f, 1.6f, 1.6f), noCollider: true);

            // Repeater signs further down the hallway so the route is findable from the classroom.
            foreach (var z in new[] { 2f, 7f })
                CreateBox($"Exit Repeater {z}", parent,
                    new Vector3(HallHalfWidth - 0.12f, 2.35f, z),
                    new Vector3(0.05f, 0.28f, 0.75f), new Color(0.09f, 0.55f, 0.22f),
                    emission: new Color(0.12f, 1.2f, 0.35f), noCollider: true);

            // The trigger volume is logic, not scenery: rendering it puts a coloured slab across
            // the doorway. The exit is communicated by the sign, the chevrons and the lit stairwell.
            var north = CreateBox("north_exit", parent,
                new Vector3(0f, 1.2f, HallMaxZ + 1f),
                new Vector3(DoorWidth, 2.4f, 1.5f), Color.clear, transparent: true);
            north.GetComponent<BoxCollider>().isTrigger = true;
            Object.DestroyImmediate(north.GetComponent<MeshRenderer>());
            var northExit = north.AddComponent<ExitZone>();
            northExit.ExitId = "north_exit";
            northExit.IsCorrectExit = true;

            // South exit looks like a way out but is behind the smoke and the barrier. Its sign is
            // unlit and marked closed, so the difference is legible without relying on colour.
            CreateBox("South Exit Sign", parent,
                new Vector3(0f, 2.42f, HallMinZ + 0.3f),
                new Vector3(1.1f, 0.4f, 0.06f), new Color(0.32f, 0.33f, 0.35f), noCollider: true);
            CreateBox("South Exit Cross", parent,
                new Vector3(0f, 2.42f, HallMinZ + 0.26f),
                new Vector3(0.7f, 0.06f, 0.02f), new Color(0.75f, 0.2f, 0.15f), noCollider: true)
                .transform.rotation = Quaternion.Euler(0f, 0f, 28f);

            var south = CreateBox("south_exit", parent,
                new Vector3(0f, 1.2f, HallMinZ - 1f),
                new Vector3(DoorWidth, 2.4f, 1.5f), Color.clear, transparent: true);
            south.GetComponent<BoxCollider>().isTrigger = true;
            Object.DestroyImmediate(south.GetComponent<MeshRenderer>());
            var southExit = south.AddComponent<ExitZone>();
            southExit.ExitId = "south_exit";
            southExit.IsCorrectExit = false;
        }

        static void CreatePlayerRig()
        {
            // The user starts in the west room, per section 4.2.
            var spawn = new Vector3(-7f, 0f, -2f);

            var rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RigPrefabPath);
            if (rigPrefab == null)
            {
                Debug.LogWarning($"[Graybox] XR rig prefab not found at {RigPrefabPath}. " +
                                 "Run Tools > VR Sim > Set Up Headless Development first.");
                return;
            }

            var rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab);
            rig.name = "XR Origin (XR Rig)";
            rig.transform.position = spawn;

            // Trigger volumes identify the user by tag, so the rig has to carry it along with a
            // body collider that the hazard and exit zones can detect.
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

            var follower = playerBody.AddComponent<PlayerBodyFollower>();
            follower.Target = origin != null ? origin.Camera.transform : null;

            // The desktop simulator stands in for the headset while no hardware is attached. It is
            // spawned at runtime rather than saved into the scene, because its on-screen UI throws
            // when it starts in a headless run with no input devices registered.
            var simulatorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SimulatorPrefabPath);
            if (simulatorPrefab != null)
            {
                var development = new GameObject("Development");
                var bootstrap = development.AddComponent<DesktopSimulatorBootstrap>();
                var serialized = new SerializedObject(bootstrap);
                serialized.FindProperty("m_SimulatorPrefab").objectReferenceValue = simulatorPrefab;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            // One key per hand for desk testing: no modifier to hold, no controller to select
            // first. Falls back to where the user is looking when a hand is not aimed at anything.
            var quickObject = new GameObject("Desktop Quick Interact");
            var quick = quickObject.AddComponent<DesktopQuickInteract>();
            quick.LeftHand = FindDescendant(rig.transform, "Left Controller");
            quick.RightHand = FindDescendant(rig.transform, "Right Controller");

            var resetPoint = new GameObject("Reset Point");
            resetPoint.transform.position = spawn;
        }

        /// <summary>
        /// Dressing for the hallway: lockers, a bench, a notice board, a fire extinguisher and a
        /// water fountain. None of it is interactive; it exists so the space reads as a real
        /// corridor rather than a corridor-shaped box.
        /// </summary>
        static void CreateHallwayProps()
        {
            var parent = new GameObject("Hallway Props").transform;

            var lockerBody = new Color(0.30f, 0.42f, 0.50f);
            var lockerDoor = new Color(0.35f, 0.48f, 0.57f);
            var metal = new Color(0.55f, 0.57f, 0.60f);

            // A bank of lockers along the west wall, north of the classroom door.
            for (var i = 0; i < 6; i++)
            {
                var z = 3f + i * 0.85f;
                CreateBox($"Locker {i + 1}", parent,
                    new Vector3(-HallHalfWidth + 0.28f, 0.95f, z),
                    new Vector3(0.45f, 1.9f, 0.8f), lockerBody, smoothness: 0.45f);
                CreateBox($"Locker Door {i + 1}", parent,
                    new Vector3(-HallHalfWidth + 0.51f, 0.95f, z),
                    new Vector3(0.03f, 1.8f, 0.72f), lockerDoor, smoothness: 0.5f, noCollider: true);
                CreateBox($"Locker Handle {i + 1}", parent,
                    new Vector3(-HallHalfWidth + 0.54f, 0.95f, z + 0.28f),
                    new Vector3(0.03f, 0.16f, 0.04f), metal, smoothness: 0.8f, noCollider: true);
            }

            // A bench opposite the lockers.
            CreateBox("Bench Seat", parent, new Vector3(HallHalfWidth - 0.45f, 0.45f, 5f),
                new Vector3(0.5f, 0.08f, 2.4f), new Color(0.55f, 0.43f, 0.30f), smoothness: 0.4f);
            foreach (var z in new[] { 4.1f, 5.9f })
                CreateBox($"Bench Leg {z}", parent, new Vector3(HallHalfWidth - 0.45f, 0.21f, z),
                    new Vector3(0.4f, 0.42f, 0.06f), metal, noCollider: true);

            // Notice board with paper on it, near the classroom door.
            CreateBox("Notice Board", parent, new Vector3(HallHalfWidth - 0.14f, 1.75f, 1.2f),
                new Vector3(0.06f, 1f, 1.6f), new Color(0.42f, 0.32f, 0.22f), noCollider: true);
            CreateBox("Notice Board Cork", parent, new Vector3(HallHalfWidth - 0.19f, 1.75f, 1.2f),
                new Vector3(0.02f, 0.88f, 1.46f), new Color(0.72f, 0.56f, 0.36f), noCollider: true);
            for (var i = 0; i < 4; i++)
                CreateBox($"Notice {i + 1}", parent,
                    new Vector3(HallHalfWidth - 0.21f, 1.55f + (i % 2) * 0.38f, 0.75f + (i / 2) * 0.85f),
                    new Vector3(0.01f, 0.3f, 0.22f), new Color(0.95f, 0.95f, 0.92f), noCollider: true);

            // Fire extinguisher below the alarm, where a real building would put it.
            CreateCylinder("Fire Extinguisher", parent,
                new Vector3(-HallHalfWidth + 0.25f, 0.42f, -0.2f),
                new Vector3(0.22f, 0.32f, 0.22f), new Color(0.75f, 0.10f, 0.10f), smoothness: 0.65f);
            CreateCylinder("Extinguisher Neck", parent,
                new Vector3(-HallHalfWidth + 0.25f, 0.76f, -0.2f),
                new Vector3(0.07f, 0.06f, 0.07f), metal);
            CreateBox("Extinguisher Sign", parent,
                new Vector3(-HallHalfWidth + 0.13f, 1.05f, -0.2f),
                new Vector3(0.02f, 0.28f, 0.2f), new Color(0.75f, 0.10f, 0.10f),
                emission: new Color(0.18f, 0.02f, 0.02f), noCollider: true);

            // Water fountain on the east wall.
            CreateBox("Water Fountain", parent, new Vector3(HallHalfWidth - 0.3f, 0.5f, -3.5f),
                new Vector3(0.36f, 1f, 0.5f), new Color(0.62f, 0.64f, 0.67f), smoothness: 0.55f);

            // Door frames where the rooms open off the hallway.
            foreach (var x in new[] { -HallHalfWidth, HallHalfWidth })
            {
                var doorCentre = (RoomDepthMinZ + RoomDepthMaxZ) / 2f;
                CreateBox("Door Frame Head", parent, new Vector3(x, 2.15f, doorCentre),
                    new Vector3(0.16f, 0.12f, DoorWidth + 0.2f),
                    new Color(0.42f, 0.44f, 0.47f), noCollider: true);
                foreach (var offset in new[] { -1f, 1f })
                    CreateBox("Door Frame Post", parent,
                        new Vector3(x, 1.05f, doorCentre + offset * (DoorWidth / 2f + 0.08f)),
                        new Vector3(0.16f, 2.2f, 0.1f), new Color(0.42f, 0.44f, 0.47f),
                        noCollider: true);
            }
        }

        // -------------------------------------------------------------- user interface

        static void CreateUserInterface()
        {
            var manager = Object.FindAnyObjectByType<ScenarioManager>();
            var origin = Object.FindAnyObjectByType<XROrigin>();
            var spawn = origin != null ? origin.transform.position : Vector3.zero;

            // A ray interactor needs an event system with the XR input module to click UI.
            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("EventSystem");
                events.AddComponent<EventSystem>();
                events.AddComponent<XRUIInputModule>();
            }

            var systems = new GameObject("Systems");
            var accessibility = systems.AddComponent<AccessibilityManager>();
            var audio = systems.AddComponent<AudioManager>();
            var alarmSource = systems.AddComponent<AudioSource>();
            alarmSource.playOnAwake = false;
            var effectsSource = systems.AddComponent<AudioSource>();
            effectsSource.playOnAwake = false;

            var audioSerialized = new SerializedObject(audio);
            audioSerialized.FindProperty("m_AlarmSource").objectReferenceValue = alarmSource;
            audioSerialized.FindProperty("m_EffectsSource").objectReferenceValue = effectsSource;
            audioSerialized.ApplyModifiedPropertiesWithoutUndo();

            var hudObject = new GameObject("Scenario HUD");
            var hud = hudObject.AddComponent<ScenarioHud>();

            // Briefing and results sit on a board in front of the start position rather than on the
            // user's face, so nothing is locked to the head.
            var boardPosition = spawn + new Vector3(0f, 1.55f, 2.2f);
            var briefing = CreatePanel("Briefing Panel", boardPosition, 2.1f, 2.2f, out var briefingText);
            var results = CreatePanel("Results Panel", boardPosition, 2.1f, 2.2f, out var resultsText);
            results.SetActive(false);

            var startButton = CreateButton(briefing.transform, "Start Button", "Start the drill",
                new Vector2(0f, -520f));
            UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(
                startButton.onClick, hud.StartScenario);

            var replayButton = CreateButton(results.transform, "Replay Button", "Run it again",
                new Vector2(0f, -520f));
            UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(
                replayButton.onClick, hud.Replay);

            // The running HUD is small, dim and parked below eye line so it never blocks a sign.
            var camera = origin != null && origin.Camera != null ? origin.Camera.transform : null;
            var hudPanel = new GameObject("HUD Panel");
            hudPanel.transform.SetParent(camera, false);
            hudPanel.transform.localPosition = new Vector3(0f, -0.35f, 1.2f);
            var hudCanvas = hudPanel.AddComponent<Canvas>();
            hudCanvas.renderMode = RenderMode.WorldSpace;
            var hudRect = hudPanel.GetComponent<RectTransform>();
            hudRect.sizeDelta = new Vector2(1800f, 360f);
            hudRect.localScale = Vector3.one * 0.0011f;

            var timerText = CreateText(hudPanel.transform, "Timer", "Time  0:00", 48f,
                new Vector2(0f, 110f), TextAlignmentOptions.Center);
            var scoreText = CreateText(hudPanel.transform, "Score", "Score  0", 48f,
                new Vector2(0f, 40f), TextAlignmentOptions.Center);
            var promptText = CreateText(hudPanel.transform, "Prompt", string.Empty, 44f,
                new Vector2(0f, -30f), TextAlignmentOptions.Center);
            var subtitleText = CreateText(hudPanel.transform, "Subtitle", string.Empty, 40f,
                new Vector2(0f, -110f), TextAlignmentOptions.Center);

            // The objective checklist sits to the user's left, out of the way of the exit signs.
            var objectivesPanel = new GameObject("Objectives Panel");
            objectivesPanel.transform.SetParent(hudPanel.transform, false);
            var objectivesRect = objectivesPanel.AddComponent<RectTransform>();
            objectivesRect.anchoredPosition = new Vector2(-620f, 0f);
            objectivesRect.sizeDelta = new Vector2(520f, 330f);

            var objectivesBackground = objectivesPanel.AddComponent<Image>();
            objectivesBackground.color = new Color(0.05f, 0.06f, 0.09f, 0.72f);

            var objectivesText = CreateText(objectivesPanel.transform, "Objectives", string.Empty, 32f,
                Vector2.zero, TextAlignmentOptions.TopLeft);
            objectivesText.GetComponent<RectTransform>().sizeDelta = new Vector2(480f, 300f);

            var hudSerialized = new SerializedObject(hud);
            hudSerialized.FindProperty("m_BriefingPanel").objectReferenceValue = briefing;
            hudSerialized.FindProperty("m_HudPanel").objectReferenceValue = hudPanel;
            hudSerialized.FindProperty("m_ResultsPanel").objectReferenceValue = results;
            hudSerialized.FindProperty("m_BriefingText").objectReferenceValue = briefingText;
            hudSerialized.FindProperty("m_ResultsText").objectReferenceValue = resultsText;
            hudSerialized.FindProperty("m_TimerText").objectReferenceValue = timerText;
            hudSerialized.FindProperty("m_ScoreText").objectReferenceValue = scoreText;
            hudSerialized.FindProperty("m_PromptText").objectReferenceValue = promptText;
            hudSerialized.FindProperty("m_SubtitleText").objectReferenceValue = subtitleText;
            hudSerialized.FindProperty("m_ObjectivesText").objectReferenceValue = objectivesText;
            hudSerialized.FindProperty("m_ScenarioManager").objectReferenceValue = manager;
            hudSerialized.FindProperty("m_AccessibilityManager").objectReferenceValue = accessibility;
            hudSerialized.FindProperty("m_AudioManager").objectReferenceValue = audio;
            hudSerialized.ApplyModifiedPropertiesWithoutUndo();

            CreatePauseAndSettings(hud);
        }

        /// <summary>
        /// The pause menu and the comfort settings panel required by section 13. Both are placed in
        /// front of the user when opened rather than being attached to the head.
        /// </summary>
        static void CreatePauseAndSettings(ScenarioHud hud)
        {
            var menuObject = new GameObject("Pause Menu");
            var menu = menuObject.AddComponent<PauseMenu>();

            var pausePanel = CreatePanel("Pause Panel", Vector3.zero, 1.5f, 1.55f, out var pauseText);
            pauseText.text = "<b>Paused</b>\n\n<size=85%>The drill is on hold. Nothing is scored " +
                             "while this menu is open.</size>";
            pauseText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 430f);
            pausePanel.SetActive(false);

            var resume = CreateButton(pausePanel.transform, "Resume Button", "Resume drill",
                new Vector2(0f, 40f));
            var restart = CreateButton(pausePanel.transform, "Restart Button", "Restart from briefing",
                new Vector2(0f, -80f));
            var settings = CreateButton(pausePanel.transform, "Settings Button", "Comfort settings",
                new Vector2(0f, -200f));
            var exit = CreateButton(pausePanel.transform, "Exit Button", "Exit the simulation",
                new Vector2(0f, -320f));
            exit.targetGraphic.color = new Color(0.45f, 0.18f, 0.16f, 1f);

            UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(resume.onClick, menu.Close);
            UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(restart.onClick, menu.RestartDrill);
            UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(settings.onClick, menu.ShowAccessibility);
            UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(exit.onClick, menu.ExitSimulation);

            var settingsPanel = CreatePanel("Accessibility Panel", Vector3.zero, 1.7f, 1.75f,
                out var settingsText);
            settingsText.text = "<b>Comfort and accessibility</b>\n\n<size=85%>Changes apply " +
                                "immediately and are remembered next time.</size>";
            settingsText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 470f);
            settingsPanel.SetActive(false);

            var panel = settingsPanel.AddComponent<AccessibilityPanel>();
            var seated = CreateToggle(settingsPanel.transform, "Seated Mode", "Seated mode", new Vector2(-260f, 170f));
            var subtitles = CreateToggle(settingsPanel.transform, "Subtitles", "Subtitles", new Vector2(-260f, 90f));
            var snapTurn = CreateToggle(settingsPanel.transform, "Snap Turn", "Snap turning", new Vector2(-260f, 10f));
            var contrast = CreateToggle(settingsPanel.transform, "High Contrast", "High contrast text", new Vector2(-260f, -70f));

            var textScale = CreateSlider(settingsPanel.transform, "Text Size", "Text size",
                new Vector2(-260f, -170f), out var textScaleValue);
            var moveSpeed = CreateSlider(settingsPanel.transform, "Movement Speed", "Movement speed",
                new Vector2(-260f, -260f), out var moveSpeedValue);

            var back = CreateButton(settingsPanel.transform, "Back Button", "Back", new Vector2(0f, -360f));
            UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(back.onClick, menu.BackToPause);

            var panelSerialized = new SerializedObject(panel);
            panelSerialized.FindProperty("m_SeatedToggle").objectReferenceValue = seated;
            panelSerialized.FindProperty("m_SubtitlesToggle").objectReferenceValue = subtitles;
            panelSerialized.FindProperty("m_SnapTurnToggle").objectReferenceValue = snapTurn;
            panelSerialized.FindProperty("m_HighContrastToggle").objectReferenceValue = contrast;
            panelSerialized.FindProperty("m_TextScaleSlider").objectReferenceValue = textScale;
            panelSerialized.FindProperty("m_MovementSpeedSlider").objectReferenceValue = moveSpeed;
            panelSerialized.FindProperty("m_TextScaleValue").objectReferenceValue = textScaleValue;
            panelSerialized.FindProperty("m_MovementSpeedValue").objectReferenceValue = moveSpeedValue;
            panelSerialized.ApplyModifiedPropertiesWithoutUndo();

            var menuSerialized = new SerializedObject(menu);
            menuSerialized.FindProperty("m_PausePanel").objectReferenceValue = pausePanel;
            menuSerialized.FindProperty("m_AccessibilityPanel").objectReferenceValue = settingsPanel;
            menuSerialized.FindProperty("m_Hud").objectReferenceValue = hud;
            menuSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static Toggle CreateToggle(Transform parent, string label, string caption, Vector2 position)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(70f, 70f);

            var background = go.AddComponent<Image>();
            background.color = new Color(0.2f, 0.22f, 0.26f, 1f);

            var toggle = go.AddComponent<Toggle>();
            toggle.targetGraphic = background;

            var tick = new GameObject("Tick");
            tick.transform.SetParent(go.transform, false);
            var tickImage = tick.AddComponent<Image>();
            tickImage.color = new Color(0.25f, 0.8f, 0.4f, 1f);
            var tickRect = tick.GetComponent<RectTransform>();
            tickRect.sizeDelta = new Vector2(46f, 46f);
            toggle.graphic = tickImage;

            var text = CreateText(go.transform, "Caption", caption, 34f, new Vector2(330f, 0f),
                TextAlignmentOptions.Left);
            text.GetComponent<RectTransform>().sizeDelta = new Vector2(560f, 60f);

            return toggle;
        }

        static Slider CreateSlider(Transform parent, string label, string caption, Vector2 position,
            out TMP_Text valueText)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = position + new Vector2(180f, -30f);
            rect.sizeDelta = new Vector2(500f, 34f);

            var background = go.AddComponent<Image>();
            background.color = new Color(0.18f, 0.19f, 0.23f, 1f);

            var slider = go.AddComponent<Slider>();

            var fillArea = new GameObject("Fill");
            fillArea.transform.SetParent(go.transform, false);
            var fill = fillArea.AddComponent<Image>();
            fill.color = new Color(0.25f, 0.6f, 0.85f, 1f);
            Stretch(fillArea.GetComponent<RectTransform>());
            slider.fillRect = fillArea.GetComponent<RectTransform>();

            var handle = new GameObject("Handle");
            handle.transform.SetParent(go.transform, false);
            var handleImage = handle.AddComponent<Image>();
            handleImage.color = new Color(0.92f, 0.93f, 0.96f, 1f);
            handle.GetComponent<RectTransform>().sizeDelta = new Vector2(34f, 52f);
            slider.handleRect = handle.GetComponent<RectTransform>();
            slider.targetGraphic = handleImage;

            var captionText = CreateText(parent, label + " Caption", caption, 34f,
                position + new Vector2(180f, 26f), TextAlignmentOptions.Left);
            captionText.GetComponent<RectTransform>().sizeDelta = new Vector2(560f, 50f);

            valueText = CreateText(parent, label + " Value", string.Empty, 32f,
                position + new Vector2(640f, -30f), TextAlignmentOptions.Right);
            valueText.GetComponent<RectTransform>().sizeDelta = new Vector2(180f, 50f);

            return slider;
        }

        static GameObject CreatePanel(string label, Vector3 position, float width, float height,
            out TMP_Text bodyText)
        {
            var go = new GameObject(label);
            go.transform.position = position;

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            go.AddComponent<GraphicRaycaster>();
            go.AddComponent<TrackedDeviceGraphicRaycaster>();

            // Tall enough for the full briefing plus a button row underneath it.
            const float pixelWidth = 1200f;
            const float pixelHeight = 1250f;

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(pixelWidth, pixelHeight);
            rect.localScale = new Vector3(width / pixelWidth, height / pixelHeight, 1f);

            var background = new GameObject("Background");
            background.transform.SetParent(go.transform, false);
            var image = background.AddComponent<Image>();
            image.color = new Color(0.06f, 0.07f, 0.1f, 0.94f);
            Stretch(background.GetComponent<RectTransform>());

            // Anchored high with a fixed height so the buttons below always have clear space.
            bodyText = CreateText(go.transform, "Body", string.Empty, 34f, new Vector2(0f, 105f),
                TextAlignmentOptions.TopLeft);
            var bodyRect = bodyText.GetComponent<RectTransform>();
            bodyRect.sizeDelta = new Vector2(1090f, 940f);

            return go;
        }

        static TMP_Text CreateText(Transform parent, string label, string content, float size,
            Vector2 position, TextAlignmentOptions alignment)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);

            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = new Color(0.92f, 0.92f, 0.95f);
            text.enableWordWrapping = true;

            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(880f, 120f);

            return text;
        }

        static Button CreateButton(Transform parent, string label, string caption, Vector2 position)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);

            var image = go.AddComponent<Image>();
            image.color = new Color(0.15f, 0.45f, 0.3f, 1f);

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;

            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(520f, 110f);

            var text = CreateText(go.transform, "Caption", caption, 40f, Vector2.zero,
                TextAlignmentOptions.Center);
            text.GetComponent<RectTransform>().sizeDelta = rect.sizeDelta;

            return button;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>Depth-first search for a named child anywhere under <paramref name="root"/>.</summary>
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

        // ---------------------------------------------------------------------- helpers

        static void AddInteractable<T>(GameObject target, string objectId) where T : InteractableObject
        {
            var interactable = target.AddComponent<T>();
            interactable.ObjectId = objectId;

            // InteractableObject subscribes to this itself, so ray interactors, direct grabs and
            // the desktop simulator all reach Interact() with no per-object event wiring.
            target.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        }

        static GameObject CreateBox(string label, Transform parent, Vector3 position, Vector3 size,
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

        static GameObject CreateCylinder(string label, Transform parent, Vector3 position,
            Vector3 size, Color colour, float smoothness = 0.3f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = label;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial =
                GetMaterial(colour, false, null, smoothness);
            return go;
        }

        static Material GetMaterial(Color colour, bool transparent, Color? emission = null,
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

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var material = new Material(shader) { color = colour };
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
