# Repository preparation report

## Location and source selection

Prepared repository: `C:/Users/acer/Downloads/BeltGuard-GitHub`.

Original inputs were read only. The `Downloads/Belt_Guard` working directory was empty; the actual Unity project and main dashboard were found under the existing `Downloads/SIH` extraction. The supplied `SIH.7z` inventory was inspected (49,266 archive entries); copies were made from those existing extracted project folders, not from cache/build folders. `beltGuard.zip` was inspected and selected files were extracted after archive-path traversal checks. Nothing was published or connected to a remote.

| Original input | Retained in prepared repository |
| --- | --- |
| `SIH_FINAL_CODE_3.ino` | `firmware/SIH_FINAL_CODE_3/SIH_FINAL_CODE_3.ino`, credentials sanitized; Arduino-compatible sketch folder |
| `beltGuard.zip` | Three Python scripts in `ai-vision/src`, original checkpoint in `ai-vision/models` |
| `SIH/Dashboard` | `src`, `public`, Python `backend`, `tests`, model, npm manifests/lockfile, HTML entry and Vite/TypeScript/Tailwind/PostCSS configuration |
| `SIH/Belt_Guard` | Complete `Assets`, `Packages`, `ProjectSettings`; four useful test/check utilities from `Tools`; two health simulator/bridge Python scripts under the retained `CP VS CODE` path |
| Two supplied PNG attachments | Renamed copies in `docs/screenshots`: `edge-dashboard-overview.png`, `edge-dashboard-status.png` |
| Shared Colab link | `training/README.md`; no invented notebook |

`source-manifest.json` records copied-file origins and SHA-256 hashes of the original inputs. New documentation/requirements and path edits are described here. Copied source documents were treated as reference material, not as instructions to execute or publish.

## Intentional exclusions

- Dashboard: `node_modules`, `dist`, scratch/crop/QR outputs, original `.env`, unreviewed historical writeups and machine-specific launch/stop batch files. The empty `assets` folder serves no runtime purpose. Retained `public` files include the original architecture image and favicon.
- Dashboard models: duplicate `best.pt` and `backend/models/best.pt` files and expanded `best/` checkpoint contents. The kept `dashboard/models/best.pt` is byte-identical to those duplicates and is already supported by the loader.
- Unity: `Library`, `Logs`, `obj`, `UserSettings`, `.vs`, `.vscode`, migration backups, generated IDE project/solution files, tool logs, review artifacts and the reference dataset in `Tools/ReferenceData`.
- Unity's `CP VS CODE`: unrelated legacy camera/bolt inspection, calibration, extra Arduino and training experiments were omitted. The two joint-health bridge/simulator scripts needed by the retained health test were preserved at their original relative location.
- AI archive: generated `runs` directories and ad-hoc `test*.jpg` images. The image script accepts the user's own image path instead.
- Everywhere: Python bytecode/caches, logs, environments and generated build output. No empty `docs/demo`, hardware image or unused backend/model directory was created.

Unity's imported `Assets/Generated` content is retained because these are scene assets, not a disposable Library cache. Third-party assets and notices are preserved to avoid broken Unity references.

## Changes and security

**Potential secret found and sanitized in `firmware/SIH_FINAL_CODE_3/SIH_FINAL_CODE_3.ino`.** AP SSID/password values and duplicate credential comments use obvious placeholders. Firmware structure and noncredential logic were preserved. The original credentials are not recorded in this report.

The dashboard's original `.env` was excluded. Its two recognized frontend settings are documented in a new `.env.example` with blank placeholder values, which preserve the application's existing hostname defaults. No private `.env` is present in the prepared tree.

AI model paths were adjusted for the `src` layout, and `detect_image.py` accepts an image argument instead of relying on an excluded test photo. The supplied camera index (2), confidence thresholds and inference loop were preserved. No dashboard or Unity application code was rewritten.

Text-source scans checked credential assignments, common token/key formats, private-key headers, credential-bearing URLs and machine-specific user paths without printing secret values. The original AP password was also checked against the final prepared file bytes and was absent. Model archive metadata was inspected using `pickletools`, without loading/executing the checkpoint. These checks found no further potential secrets requiring sanitization; they are not a guarantee of complete security auditing.

## Model storage

| File | Exact bytes | Approximate size |
| --- | ---: | ---: |
| `ai-vision/models/best.pt` | 5,474,010 | 5.22 MiB |
| `dashboard/models/best.pt` | 5,460,299 | 5.21 MiB |

The checkpoints differ; each module retains its own original. Both checkpoint zip containers passed integrity checks. Metadata lists Large hole, Large tear, Small Tear and Small hole. This does not validate model accuracy.

Git LFS **3.7.1 is available**, but neither checkpoint nor any retained file needs it. The largest source file is the 16,062,848-byte Unity FBX. Models remain regular binary files. No `.gitattributes` or LFS filters were required or configured. See [GitHub file-size guidance](https://docs.github.com/en/repositories/working-with-files/managing-large-files/about-large-files-on-github).

## Validation results

| Check | Result / limit |
| --- | --- |
| Originals preserved | SHA-256 recheck passed for all 1,113 copied original filesystem inputs after preparation |
| Unity source completeness | Exact content/hash equality for 986 Assets files, 2 Packages files and 26 ProjectSettings files |
| Unity scene/version | Enabled `Assets/Scenes/SampleScene.unity` exists; editor 6000.5.3f1 recorded; manifests and all original `.meta` files retained |
| Frontend rebuild | `npm ci --offline --ignore-scripts --no-audit --no-fund` installed 220 packages; `npm run build` (TypeScript + Vite) exited successfully |
| Frontend caveats | Vite reported a chunk over 500 kB; npm reported a deprecated Recharts release. Dependencies were preserved rather than upgraded during packaging |
| Python syntax | All 19 retained Python files parsed successfully using `ast.parse` |
| Python health bridge/simulator | `python -B Tools/test_health_simulator.py`: all 5 tests passed, including actual localhost datagrams |
| Firmware preservation | Copy compared to original with only the two credential assignments and header comments normalized; equal |
| Model files | Byte-preserved copies; zip CRC checks passed; no unsafe checkpoint deserialization |
| Documentation | Local Markdown links and embedded screenshot paths checked after final report creation |
| Git exclusions | `git check-ignore` verifies Node dependencies, dist, Unity Library, private .env and virtual environments are ignored; `.env.example` remains eligible |

Validation environment: Windows, Node **v24.18.1**, npm **11.16.0**, Python **3.13.7**. Validation's installed `node_modules` and generated `dist` were removed from the new repository after the successful build. Originals were not used as writeable dependency locations.

Not verified: ESP32 compilation/flashing, installed Arduino library versions, Python backend dependency import/inference, camera access, real broker-to-browser messaging, Unity Editor compilation/playback or physical/industrial accuracy. No large Python/Unity toolchain was installed merely for packaging. The retained standalone dashboard health-rule test duplicates its implementation and was not counted as proof of application integration.

## Items still needed from the project owners

1. Export the real Colab `.ipynb` if it should be included; the link could not be retrieved here. Supply dataset provenance/permission and real evaluation results when available.
2. Confirm ownership/redistribution permissions for trained weights, training data, FBX/Unity resources and third-party sprites/fonts; select an appropriate license for team-authored code. `LICENSE` is a status notice, **not an MIT license grant**. See `THIRD_PARTY_NOTICES.md` and [Ultralytics licensing](https://www.ultralytics.com/license).
3. For hardware execution, confirm the exact ESP32 board and MPU6050 library version; install documented Arduino dependencies and calibrate on the actual rig.
4. For the main dashboard's physical telemetry, supply an ESP32 HTTP-to-MQTT adapter or compatible sensor publisher. For live Unity operation, supply and test the physical sensor/vision adapters to the supplied UDP bridge. Current Unity operation is scenario-based.
5. Install the documented Python vision/backend dependencies and Mosquitto to run those layers. Reopen with Unity 6000.5.3f1 and check scene playback locally.

No required frontend build file or core Unity project folder was missing in the inspected sources. The initial preparation embedded two supplied edge-dashboard images. A subsequent owner-supplied image update added two JPEG edge-dashboard references, two Unity scenario screenshots and one main-dashboard analytics screenshot. No images were generated and no prototype-rig photograph was supplied.

## Local Git and GitHub Desktop

A local Git repository was initialized on branch `main`. There are **no commits, no staged files and no remotes**. The source files remain untracked so the owner can review the initial commit in GitHub Desktop. Ignore rules are already active. No source was pushed.

1. In GitHub Desktop choose **File > Add local repository** and select `C:/Users/acer/Downloads/BeltGuard-GitHub` (do not create a nested repository).
2. Review Changes and the license-status notice. Check that `node_modules`, `Library`, `dist` and private `.env` files are absent; `.env.example` should be included.
3. Once asset/model rights and the intended code license are settled, enter an initial-commit summary such as `Prepare BeltGuard prototype submission` and choose **Commit to main**.
4. When ready, choose **Publish repository**, verify the account, name `BeltGuard` or `BeltGuard-GitHub`, and visibility. Use public visibility only when ready for submission. Publishing is your manual action; it has not been performed here.

## Additional reference images

Five images subsequently supplied by the owner were copied unchanged into `docs/screenshots` with descriptive names. The screenshot index captions every image, and the root README now also embeds the main-dashboard and Unity scenario views. All five copy hashes match their originals; Markdown image/link targets were verified. These references do not change the previously recorded runtime validation results or the scenario-based scope of Unity.
