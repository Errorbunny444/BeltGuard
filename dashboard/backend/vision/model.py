import os
import zipfile
from pathlib import Path
from ultralytics import YOLO

DEFECT_SEVERITY = {
    'Large hole': 'Critical',
    'Large tear': 'Critical',
    'Small Tear': 'High',
    'Small hole': 'Medium'
}

DEFECT_COLORS = {
    'Critical': (68, 68, 239),    # Red in BGR
    'High': (11, 158, 245),       # Amber in BGR
    'Medium': (248, 189, 56),     # Cyan/Sky in BGR
    'Normal': (74, 222, 128)      # Green in BGR
}

class ModelManager:
    def __init__(self, model_path="models/best.pt"):
        # Resolve project root (parent of backend/)
        self.project_root = Path(__file__).resolve().parent.parent.parent
        self.model_path = self._find_or_create_model(model_path)
        self.model = None
        self.is_loaded = False
        self.names = {}
        self.load_model()

    def _find_or_create_model(self, given_path):
        """Finds best.pt across multiple search paths or packages it from best/ folder."""
        candidate_paths = [
            Path(given_path),
            self.project_root / given_path,
            self.project_root / "models" / "best.pt",
            self.project_root / "best.pt",
            self.project_root / "backend" / "models" / "best.pt",
            Path("models/best.pt").resolve(),
            Path("best.pt").resolve()
        ]

        for p in candidate_paths:
            if p.exists() and p.is_file() and p.stat().st_size > 100000:
                return p

        # If best.pt is not found as a file, check if the unzipped 'best' folder exists
        best_dir_candidates = [
            self.project_root / "best",
            Path("best").resolve()
        ]

        for best_dir in best_dir_candidates:
            if best_dir.exists() and (best_dir / "data.pkl").exists():
                print(f"[Model] Found unzipped model folder at {best_dir}. Packaging into models/best.pt...")
                out_path = self.project_root / "models" / "best.pt"
                out_path.parent.mkdir(parents=True, exist_ok=True)
                
                # Zip archive with required 'archive/' prefix expected by PyTorch
                with zipfile.ZipFile(out_path, 'w', compression=zipfile.ZIP_STORED) as zf:
                    for root, dirs, files in os.walk(best_dir):
                        for file in files:
                            full = Path(root) / file
                            rel = full.relative_to(best_dir)
                            zf.write(full, f"archive/{rel.as_posix()}")

                print(f"[Model] Successfully compiled {out_path} ({out_path.stat().st_size} bytes).")
                return out_path

        return Path(given_path)

    def load_model(self):
        try:
            if not self.model_path or not self.model_path.exists():
                print(f"[Model] Warning: {self.model_path} not found.")
                self.is_loaded = False
                return False

            print(f"[Model] Loading YOLO weights from {self.model_path}...")
            self.model = YOLO(str(self.model_path))
            self.names = getattr(self.model, 'names', {
                0: 'Large hole',
                1: 'Large tear',
                2: 'Small Tear',
                3: 'Small hole'
            })
            self.is_loaded = True
            print(f"[Model] Ultralytics YOLO initialized successfully with classes: {self.names}")
            return True
        except Exception as e:
            print(f"[Model] Failed to load YOLO model: {e}")
            self.is_loaded = False
            return False

    def predict(self, frame, conf=0.25):
        if not self.is_loaded or self.model is None:
            return None
        try:
            results = self.model.predict(frame, conf=conf, verbose=False)
            return results[0] if results else None
        except Exception as e:
            print(f"[Model] Inference error: {e}")
            return None
