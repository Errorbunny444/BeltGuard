"""SIH26008 JSON demo sender. Standard library only; does not control hardware."""
import argparse
import json
import queue
import socket
import threading
import time

SCENARIOS = ("healthy", "watch", "warning", "critical", "rupture", "reset")


def packet_for(scenario, joint="J-03", position=3.4, omit_health=False):
    stage = SCENARIOS.index(scenario)
    health = (96, 72, 52, 25, 5, 100)[stage]
    packet = {
        "data_source": "simulator",
        "belt_speed": 0.0 if scenario == "rupture" else 0.42,
        "motor_current": (1.2, 1.8, 2.6, 3.7, 4.8, 1.2)[stage],
        "vibration": (1.0, 2.8, 5.2, 8.0, 10.0, 1.0)[stage],
        "temperature": (30.0, 38.0, 49.0, 65.0, 80.0, 30.0)[stage],
        "encoder_position": round(position, 3),
        "joint_id": joint,
        "damage_detected": stage in (1, 2, 3, 4),
        "damage_type": "splice_crack" if stage in (1, 2, 3, 4) else "none",
        "damage_severity": (0, 15, 45, 78, 100, 0)[stage],
        "confidence": 0.91,
        "belt_position": round(position, 3),
        "risk_level": "healthy" if scenario == "reset" else scenario,
        "maintenance_action": "",  # Unity generates its local advice.
    }
    if not omit_health:
        packet["health_score"] = health
    return packet


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--port", type=int, default=5055)
    parser.add_argument("--interval", type=float, default=1.0)
    parser.add_argument("--hold", type=int, default=5, help="Packets per automatic stage")
    parser.add_argument("--scenario", choices=SCENARIOS, help="Hold one scenario")
    parser.add_argument("--manual", action="store_true", help="Type 1-5/R plus Enter; U resumes cycle")
    parser.add_argument("--joint", choices=[f"J-{i:02}" for i in range(1, 6)], default="J-03")
    parser.add_argument("--omit-health", action="store_true", help="Exercise Unity's fallback formula")
    parser.add_argument("--count", type=int, default=0, help="Exit after N packets; 0 = continuous")
    args = parser.parse_args()
    if not 1 <= args.port <= 65535 or args.interval <= 0 or args.hold < 1 or args.count < 0:
        parser.error("Invalid port, interval, hold, or count")
    commands = queue.Queue()

    def read_commands():
        while True:
            try:
                commands.put(input().strip().lower())
            except EOFError:
                return

    if args.manual:
        threading.Thread(target=read_commands, daemon=True).start()
    stage = SCENARIOS.index(args.scenario) if args.scenario else 0
    automatic = not (args.manual or args.scenario)
    sent = stage_packets = 0
    position = 0.0
    reset_pending = SCENARIOS[stage] == "reset"
    print(f"Unity target localhost:{args.port}. 1-5/R + Enter; U = cycle; Q = quit. Ctrl+C stops.")
    try:
        with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as sender:
            while not args.count or sent < args.count:
                while not commands.empty():
                    command = commands.get_nowait()
                    if command == "q":
                        return
                    if command == "u":
                        automatic = True
                    elif command in ("1", "2", "3", "4", "5", "r"):
                        stage = 5 if command == "r" else int(command) - 1
                        automatic = False
                        stage_packets = 0
                        reset_pending = stage == 5
                data = packet_for(SCENARIOS[stage], args.joint, position, args.omit_health)
                if reset_pending:
                    # Explicit demo-only reset extension. Normal healthy telemetry cannot clear a latch.
                    data["reset_demo"] = True
                    reset_pending = False
                sender.sendto(json.dumps(data, allow_nan=False).encode("utf-8"), ("127.0.0.1", args.port))
                print(f"{sent + 1:04} {SCENARIOS[stage]:8} {args.joint} health={data.get('health_score', 'calculated')}", flush=True)
                position += data["belt_speed"] * args.interval
                sent += 1
                stage_packets += 1
                if automatic and stage_packets >= args.hold:
                    stage = (stage + 1) % len(SCENARIOS)
                    stage_packets = 0
                    reset_pending = stage == 5
                if not args.count or sent < args.count:
                    time.sleep(args.interval)
    except KeyboardInterrupt:
        print("\nSimulator stopped.")


if __name__ == "__main__":
    main()
