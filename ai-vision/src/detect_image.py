from pathlib import Path
from ultralytics import YOLO
import cv2
import argparse

parser = argparse.ArgumentParser(description="Run BeltGuard on an image")
parser.add_argument("image", help="Path to a local test image")
args = parser.parse_args()

# Load BeltGuard model
model = YOLO(str(Path(__file__).resolve().parents[1] / "models" / "best.pt"))

# Load test image
image = cv2.imread(args.image)

if image is None:
    print("ERROR: Could not read the supplied image")
    exit()

# Run detection
results = model(image, conf=0.40)

# Draw detections
annotated = results[0].plot()

# Display result
cv2.imshow("BeltGuard - Detection Test", annotated)

print("Detection complete.")
print("Press any key on the image window to close.")

cv2.waitKey(0)
cv2.destroyAllWindows()