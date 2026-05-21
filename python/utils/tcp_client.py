"""
TCP Client for communicating with Unity's TCP Server.
Handles sending actions and receiving game state over a JSON-newline protocol.
"""

import socket
import json


class TCPClient:
    """TCP client that connects to Unity's TCP server for RL communication."""

    def __init__(self, host: str = "localhost", port: int = 9876):
        self.host = host
        self.port = port
        self.socket = None
        self.buffer = ""

    def connect(self):
        """Connect to Unity's TCP server."""
        self.socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.socket.connect((self.host, self.port))
        self.buffer = ""

    def receive_state(self) -> dict:
        """Read bytes from socket until newline, parse JSON, return dict."""
        while "\n" not in self.buffer:
            data = self.socket.recv(4096)
            if not data:
                raise ConnectionError("Unity server disconnected")
            self.buffer += data.decode("utf-8")

        line, self.buffer = self.buffer.split("\n", 1)
        return json.loads(line)

    def send_action(self, action_id: int):
        """Send an action to Unity: {"action": <id>}\\n"""
        message = json.dumps({"action": action_id}) + "\n"
        self.socket.sendall(message.encode("utf-8"))

    def send_reset(self):
        """Send a reset command to Unity: {"command": "reset"}\\n"""
        message = json.dumps({"command": "reset"}) + "\n"
        self.socket.sendall(message.encode("utf-8"))

    def close(self):
        """Cleanly shut down the socket."""
        if self.socket:
            self.socket.close()
            self.socket = None

    def is_connected(self) -> bool:
        return self.socket is not None
