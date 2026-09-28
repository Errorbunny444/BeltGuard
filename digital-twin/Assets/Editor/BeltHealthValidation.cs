using System;
using System.Net.Sockets;
using System.Text;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Headless integration check: open the actual scene and exercise the live components.
public static class BeltHealthValidation
{
    private static double next;
    private static int step;
    private static JointHealthManager manager;
    private static BeltHealthUDPReceiver receiver;
    private static DemoScenarioController demo;
    private static int lastFrame = -1;
    private static float staleStart;
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        EditorApplication.playModeStateChanged += State;
        EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod]
    private static void ResumeAfterReload()
    {
        // Domain reload clears static callbacks. Command-line marker identifies this run.
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "BeltHealthValidation.Run") < 0) return;
        EditorApplication.playModeStateChanged -= State;
        EditorApplication.playModeStateChanged += State;
    }
    private static void State(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        next = EditorApplication.timeSinceStartup + 2;
        EditorApplication.update += Tick;
    }
    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        Debug.Log("[HEALTH TEST PASS] " + message);
    }
    private static BeltHealthData Data(float health, string joint = "J-03") => new BeltHealthData
        { joint_id = joint, health_score = health, belt_speed = 0.42f, temperature = 30,
          vibration = 1, motor_current = 1, damage_type = health < 80 ? "splice_crack" : "none",
          damage_detected = health < 80, damage_severity = Mathf.Clamp(100 - health, 0, 100), confidence = .91f };
    private static void Send(string json)
    {
        using (var socket = new UdpClient())
        {
            var bytes = Encoding.UTF8.GetBytes(json);
            socket.Send(bytes, bytes.Length, "127.0.0.1", receiver.port);
        }
    }
    private static void Tick()
    {
        if (EditorApplication.timeSinceStartup < next) return;
        if (Time.frameCount == lastFrame) return;
        if (step == 2 && Time.unscaledTime - staleStart < .1f) return;
        lastFrame = Time.frameCount;
        next = EditorApplication.timeSinceStartup + 0.5;
        try
        {
            if (step == 0)
            {
                manager = UnityEngine.Object.FindAnyObjectByType<JointHealthManager>();
                receiver = UnityEngine.Object.FindAnyObjectByType<BeltHealthUDPReceiver>();
                demo = UnityEngine.Object.FindAnyObjectByType<DemoScenarioController>();
                Assert(manager && receiver && demo, "Runtime scene wiring");
                demo.enabled = false;
                Assert(manager.joints.Length == 5, "Five joint passports");
                Assert(GameObject.Find("Robo_Assembly") == null, "Robotic arm removed");
                var loop = UnityEngine.Object.FindAnyObjectByType<BeltLoopMotion>();
                loop.Evaluate(0, out var first, out _, out _);
                loop.Evaluate(1, out var last, out _, out _);
                Assert(Vector3.Distance(first, last) < .001f, "Closed belt loop joins without teleporting");
                float distance = 0; var previous = first;
                for (int n = 1; n <= 1000; n++)
                {
                    loop.Evaluate(n / 1000f, out var point, out var normal, out var tangent);
                    distance += Vector3.Distance(previous, point); previous = point;
                    AssertFinite(point);
                }
                Assert(Mathf.Abs(distance - loop.Perimeter) < loop.Perimeter * .01f, "Encoder path length matches pulley wraps and straight runs");
                manager.ResetDemo(); manager.Apply(Data(100));
                manager.rupture.TriggerOperatorEmergency(); manager.Refresh();
                Assert(manager.rupture.emergencyStop && !manager.rupture.ruptureEvent && manager.rupture.beltNormal.activeSelf,
                    "Operator emergency stops motion without inventing a rupture");
                foreach (int health in new[] { 100, 80, 79, 60, 59, 40, 39, 10, 9, 0 })
                {
                    manager.ResetDemo(); manager.Apply(Data(health));
                    Assert(manager.rupture.emergencyStop == (health < 10), "Rupture boundary " + health);
                    Assert(manager.rupture.beltMinorCrack.activeSelf == (health >= 40 && health < 60), "Minor crack boundary " + health);
                    Assert(manager.rupture.beltCriticalCrack.activeSelf == (health >= 10 && health < 40), "Critical crack boundary " + health);
                }
                var stoppedPosition = manager.rupture.rupturedLeft.transform.position;
                manager.Apply(Data(100, "J-01"));
                Assert(manager.rupture.emergencyStop, "Healthy packet for another joint cannot clear rupture");
                Assert(manager.rupture.rupturedLeft.GetComponent<MeshFilter>().sharedMesh.vertexCount > 0 &&
                    manager.rupture.rupturedRight.GetComponent<MeshFilter>().sharedMesh.vertexCount > 0,
                    "Both rupture pieces contain clipped CAD geometry");
                manager.Apply(Data(5));
                Assert(manager.rupture.rupturedLeft.transform.position == stoppedPosition, "Repeated rupture does not accumulate separation");
                manager.rupture.startButton.StartBelt();
                Assert(!manager.rupture.startButton.start, "Start command blocked by rupture interlock");
                manager.ResetDemo(); manager.Apply(Data(100));
                Assert(!manager.rupture.emergencyStop && manager.rupture.beltNormal.activeSelf, "Explicit reset restores normal belt");
                manager.rupture.operatorPaused = true;
                manager.Apply(Data(100));
                Assert(!manager.rupture.startButton.start, "Telemetry cannot cancel operator pause");
                manager.rupture.operatorPaused = false;
                string passport = Path.Combine(manager.store.storageDirectory, "joint-passports.json");
                Assert(File.Exists(passport) && File.ReadAllText(passport).Contains("J-03"), "Joint passport persisted to disk");
                Assert(!manager.Apply(Data(float.NaN)), "Nonfinite health rejected");
                Assert(!manager.Apply(Data(50, "J-99")), "Unknown joint rejected");
                manager.demoMode = false;
                manager.Apply(Data(5));
                var reset = Data(100); reset.reset_demo = true; manager.Apply(reset);
                Assert(manager.rupture.emergencyStop, "Network reset ignored outside demo mode");
                manager.Apply(Data(5));
                string testStore = Path.Combine(Application.temporaryCachePath, "BeltHealthValidation-" + Guid.NewGuid().ToString("N"));
                manager.store.Configure(manager, testStore); manager.store.Save(manager);
                manager.rupture.ResetLatch(); manager.Initialize(); manager.latest = null;
                manager.store.Configure(manager, testStore);
                Assert(manager.rupture.emergencyStop && manager.rupture.ruptureEvent && !manager.rupture.startButton.start,
                    "Saved live rupture is restored without restarting motion");
                Assert(manager.WorstJoint().health_score == 5, "Saved joint health survives reload");
                manager.AcknowledgeMaintenance();
                Assert(manager.rupture.emergencyStop, "Maintenance acknowledgement rejects missing telemetry");
                for (int joint = 1; joint <= 5; joint++)
                {
                    var repaired = Data(100, $"J-{joint:00}"); repaired.belt_speed = 0; manager.Apply(repaired);
                }
                manager.AcknowledgeMaintenance();
                Assert(!manager.rupture.emergencyStop && manager.rupture.operatorPaused && !manager.rupture.startButton.start,
                    "Healthy stopped inspections allow acknowledgement but require explicit resume");
                var simulator = Data(100); simulator.data_source = "simulator";
                Assert(!manager.Apply(simulator), "Live mode rejects explicitly simulated packets");
                File.WriteAllText(Path.Combine(manager.store.storageDirectory, "joint-passports.json"), "{broken");
                manager.store.Configure(manager, testStore);
                Assert(manager.rupture.emergencyStop && !manager.rupture.startButton.start, "Corrupt live passport inhibits motion");
                manager.demoMode = true; manager.ResetDemo();
                manager.store.Configure(manager);
                receiver.acceptPackets = true;
                Send("{broken json");
                var fallback = Data(-1); fallback.damage_severity = 100; fallback.vibration = 10;
                fallback.temperature = 80; fallback.motor_current = 5;
                string json = JsonUtility.ToJson(fallback).Replace("\"health_score\":-1.0,", "").Replace("\"health_score\":-1,", "");
                Assert(!json.Contains("health_score"), "Fallback test omits health_score");
                Send(json);
            }
            else if (step == 1)
            {
                Assert(receiver.rejectedPackets > 0, "Malformed UDP rejected");
                Assert(manager.latest != null && manager.latest.health_score == 0, "Real UDP reception and missing-health formula");
                Assert(manager.rupture.emergencyStop, "UDP rupture reaches main-thread visuals");
                manager.ResetDemo(); manager.Apply(Data(100)); manager.staleAfterSeconds = 0.01f;
                staleStart = Time.unscaledTime;
            }
            else if (step == 2)
            {
                Assert(manager.dataStatus.StartsWith("STALE") && !manager.rupture.startButton.start, "Stale telemetry stops simulated motion");
                if (Array.IndexOf(Environment.GetCommandLineArgs(), "-nographics") >= 0) { Complete(); return; }
                manager.staleAfterSeconds = 60;
                manager.ResetDemo(); manager.Apply(Data(100));
                Directory.CreateDirectory("Tools/Preview");
            }
            else if (step == 3) Capture("healthy");
            else if (step == 4) manager.Apply(Data(25));
            else if (step == 5) Capture("critical");
            else if (step == 6) manager.Apply(Data(5));
            else if (step == 7) Capture("rupture");
            else if (step == 8)
            {
                manager.ResetDemo(); manager.Apply(Data(25));
                Camera.main.GetComponent<IndustrialOperatorCamera>().Select(IndustrialOperatorCamera.View.Joint);
            }
            else if (step == 9) Capture("joint_closeup");
            else if (step == 10) Complete();
            step++;
        }
        catch (Exception e)
        {
            Debug.LogError("[HEALTH TEST FAILED] " + e);
            EditorApplication.update -= Tick;
            EditorApplication.Exit(1);
        }
    }
    private static void Complete()
    {
                Debug.Log("[HEALTH TESTS COMPLETE] All checks passed.");
                EditorApplication.update -= Tick;
                EditorApplication.Exit(0);
    }
    private static void AssertFinite(Vector3 point)
    {
        if (!BeltHealthData.Finite(point.x) || !BeltHealthData.Finite(point.y) || !BeltHealthData.Finite(point.z))
            throw new Exception("Nonfinite position on belt loop");
    }
    private static void Capture(string name)
    {
        var camera = Camera.main;
        Assert(camera != null, "Main camera available for preview");
        var target = new RenderTexture(1280, 720, 24);
        var oldTarget = camera.targetTexture;
        var oldActive = RenderTexture.active;
        var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            pixels.Apply();
            File.WriteAllBytes("Tools/Preview/" + name + ".png", pixels.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = oldTarget;
            RenderTexture.active = oldActive;
            UnityEngine.Object.DestroyImmediate(pixels);
            target.Release(); UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
