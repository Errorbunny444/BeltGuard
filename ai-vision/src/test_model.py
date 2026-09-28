from pathlib import Path
from ultralytics import YOLO

model = YOLO(str(Path(__file__).resolve().parents[1] / "models" / "best.pt"))

print("BeltGuard Green Belt model loaded successfully!")
print("Model classes:")
print(model.names)