import socket
import os
from pathlib import Path
import qrcode
from PIL import Image, ImageDraw, ImageFont

def get_lan_ip():
    """Detect active local IPv4 address."""
    s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    try:
        s.connect(('8.8.8.8', 80))
        ip = s.getsockname()[0]
    except Exception:
        ip = '127.0.0.1'
    finally:
        s.close()
    return ip

def generate_qr():
    root_dir = Path(__file__).resolve().parent.parent
    ip = get_lan_ip()
    port = 5173
    url = f"http://{ip}:{port}"
    output_path = root_dir / "BELTGUARD_DEMO_QR.png"

    print("=" * 60)
    print("      BELTGUARD SIH DEMO — QR CODE GENERATOR")
    print("=" * 60)
    print(f"[*] Detected Laptop LAN IP : {ip}")
    print(f"[*] Mobile Dashboard URL   : {url}")
    print("=" * 60)

    # Generate QR Code
    qr = qrcode.QRCode(
        version=1,
        error_correction=qrcode.constants.ERROR_CORRECT_H,
        box_size=10,
        border=3,
    )
    qr.add_data(url)
    qr.make(fit=True)

    qr_img = qr.make_image(fill_color="black", back_color="white").convert('RGB')

    # Create a nice branded card around the QR code
    card_w = qr_img.width + 80
    card_h = qr_img.height + 140
    card = Image.new("RGB", (card_w, card_h), (15, 23, 42)) # Slate-900 industrial dark
    draw = ImageDraw.Draw(card)

    # Paste QR code in center
    offset_x = (card_w - qr_img.width) // 2
    offset_y = 60
    card.paste(qr_img, (offset_x, offset_y))

    # Add text banner
    draw.text((card_w // 2, 30), "BELTGUARD INDUSTRIAL MONITORING", fill=(56, 189, 248), anchor="mm")
    draw.text((card_w // 2, card_h - 50), f"Scan with phone: {url}", fill=(255, 255, 255), anchor="mm")
    draw.text((card_w // 2, card_h - 25), "Must be on the same Wi-Fi / Hotspot", fill=(148, 163, 184), anchor="mm")

    card.save(output_path)
    print(f"[OK] Generated high-res demo QR: {output_path}")
    print(f"[OK] Scan URL: {url}")
    return str(output_path), url

if __name__ == "__main__":
    generate_qr()
