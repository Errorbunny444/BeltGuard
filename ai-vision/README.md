# OpenCV / YOLO vision prototype

The supplied Python scripts load `models/best.pt` and draw YOLO detections on camera frames or an image. Checkpoint metadata lists four labels: Large hole, Large tear, Small Tear and Small hole. Metadata was inspected without executing the checkpoint. Model accuracy and successful inference on this machine have not been verified.

## Install

Use a Python version supported by the chosen Ultralytics/PyTorch release. A Python 3.11 or 3.12 environment is a reasonable starting point; original dependency versions were not recorded.

From `ai-vision`:

```powershell
python -m venv .venv
.\.venv\Scripts\Activate.ps1
python -m pip install -r requirements.txt
```

Only the imported external packages, `ultralytics` and `opencv-python`, are listed. Ultralytics installs its own transitive dependencies. Requirements are not a tested version lock.

## Run

```powershell
python src/live_detection.py
python src/detect_image.py "C:/path/to/your-belt-image.jpg"
python src/test_model.py
```

- Live input: camera index **2** in `src/live_detection.py`. Change that index in your local copy if required. Confidence is `0.30`; press **Q** in the preview to exit.
- Image input: an existing local image path. Confidence is `0.40`; press any key in the preview to exit.
- Output: annotated OpenCV windows. `test_model.py` prints model class names. These scripts do not send dashboard or Unity telemetry.

A desktop display and an accessible camera/image are required. All scripts locate the model relative to their own file, so the working directory does not break model loading. The image script now accepts a CLI path because the archive's ad-hoc test images were excluded. Detection logic is otherwise preserved.

## Model

`models/best.pt`: **5,474,010 bytes (5.22 MiB)**, copied byte-for-byte from `beltGuard.zip`. Normal Git storage is suitable; Git LFS is not required. The dashboard has a separate, different supplied checkpoint which was retained there. Do not interchange the files on the assumption they are identical. Review [licensing notes](../docs/THIRD_PARTY_NOTICES.md) before publication.
