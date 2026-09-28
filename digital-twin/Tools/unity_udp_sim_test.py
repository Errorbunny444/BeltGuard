import socket
import threading
import time


HOST = "127.0.0.1"
PORT_BOLT_TYPE = 5065
PORT_SPEED = 5066
PORT_SERVO_REPLY = 5068
PORT_PROX = 5069
UNITY_RUN_RPM = 75


def listen_for_unity_reply(stop_event):
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.settimeout(0.5)

    try:
        sock.bind((HOST, PORT_SERVO_REPLY))
        print(f"[Python] Listening for Unity reply on {PORT_SERVO_REPLY}")

        while not stop_event.is_set():
            try:
                data, addr = sock.recvfrom(1024)
                print(f"[Python] From Unity {addr}: {data.decode('utf-8', errors='replace')}")
            except socket.timeout:
                pass
    except OSError as exc:
        print(f"[Python] Could not bind {PORT_SERVO_REPLY}: {exc}")
        print("[Python] Continue test without listening for Unity reply.")
    finally:
        sock.close()


def send(sock, message, port):
    print(f"[Python] -> {port}: {message}")
    sock.sendto(message.encode("utf-8"), (HOST, port))


def main():
    stop_event = threading.Event()
    listener = threading.Thread(target=listen_for_unity_reply, args=(stop_event,), daemon=True)
    listener.start()

    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

    try:
        send(sock, f"SPEED:{UNITY_RUN_RPM}", PORT_SPEED)
        time.sleep(7)

        send(sock, "PROX:DETECTED", PORT_PROX)
        time.sleep(2)

        send(sock, "Good", PORT_BOLT_TYPE)
        time.sleep(2)

        send(sock, "PROX:CLEAR", PORT_PROX)
        time.sleep(2)
    finally:
        sock.close()
        stop_event.set()
        listener.join(timeout=1)

    print("[Python] Test complete.")


if __name__ == "__main__":
    main()
