from pathlib import Path
from ultralytics import YOLO
import cv2

# Load Green Belt model
model = YOLO(str(Path(__file__).resolve().parents[1] / "models" / "best.pt"))

# Open camera
cap = cv2.VideoCapture(2)

if not cap.isOpened():
    print("ERROR: Camera could not be opened.")
    exit()

print("Green Belt detection started.")
print("Press Q to quit.")

while True:

    ret, frame = cap.read()

    if not ret:
        print("ERROR: Could not read camera frame.")
        break

    # Run YOLO detection
    results = model(frame, conf=0.30)

    # Draw detections
    annotated_frame = results[0].plot()

    # Display
    cv2.imshow("BeltGuard - Green Belt Detection", annotated_frame)

    # Quit
    if cv2.waitKey(1) & 0xFF == ord("q"):
        break

cap.release()
cv2.destroyAllWindows()