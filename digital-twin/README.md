# Unity Digital Twin demonstrator

This is a **scenario-based Digital Twin demonstrator**. It includes conveyor visualization, joint health/state management, visible damage and rupture states, heuristic maintenance advice and simulated degradation. Simulation values do not establish real sensor measurements, predictive RUL or field validation.

## Open

1. Install/use Unity **6000.5.3f1** (from `ProjectSettings/ProjectVersion.txt`).
2. Unity Hub > Add project from disk > select this `digital-twin` folder.
3. Allow the editor to resolve `Packages/manifest.json` and regenerate `Library`.
4. Open `Assets/Scenes/SampleScene.unity`, the enabled build scene, and enter Play mode.

All supplied `Assets` (including `.meta` files), `Packages` and `ProjectSettings` were copied. No Library, generated IDE project, logs or build output was needed. The project uses URP and Unity's Input System. Structural completeness was checked, but Unity Editor compilation and scene playback have not been verified in this preparation.

## Controls found in source

With the Game view focused and demo mode enabled:

| Key | Action |
| --- | --- |
| 1–5 | Select condition/degradation scenario |
| N | Select next joint |
| R | Reset the demonstration |
| Space | Toggle simulated operator pause |
| E | Trigger the simulated emergency state |
| U | Release keyboard scenario control and accept external UDP input |
| F1 / F | Operator camera |
| F2 / V | Top camera |
| F3 / C | Joint camera |
| F4 | Free camera |
| F5 | Presentation camera |

These actions affect the demonstrator; they are not certified controls for physical conveyor equipment.

## Python simulation and integration boundary

The existing paths under `CP VS CODE` are retained because the supplied tests reference them. Only the two relevant health bridge/simulator scripts are included; unrelated older inspection experiments and training folders were excluded.

From `digital-twin`, using Python (standard library only):

```powershell
python "CP VS CODE/simulate_belt_health_udp.py" --count 30
python -B Tools/test_health_simulator.py
```

Use **U** in Unity to accept the sender. `BeltHealthUDPReceiver` listens on localhost port **5055**. The simulator emits synthetic JSON scenarios. `belt_health_bridge.py` validates and combines sensor/vision dictionaries or reads JSON lines from stdin. It supplies an integration boundary, not a camera/serial/ESP32 adapter. No supplied connection demonstrates that physical sensing drives this health view live.

`Tools/unity_udp_sim_test.py` is a retained legacy conveyor/bolt UDP test using ports 5065/5066/5068/5069; it is separate from the joint-health simulator. The C# check utilities in `Tools` are optional source utilities outside Unity's imported `Assets` tree. The five supplied Python health simulator/bridge tests passed.

Third-party fonts, TextMesh Pro resources, examples and asset notices remain intact to preserve project references. Their original terms apply; see [third-party notes](../docs/THIRD_PARTY_NOTICES.md).
