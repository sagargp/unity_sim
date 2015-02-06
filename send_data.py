#!/usr/bin/env python

import json
import socket
import argparse

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--host", "-c", default="localhost", help="Hostname or IP to connect to")
    parser.add_argument("--port", "-p", default=9999, type=int, help="UDP port to connect on")
    args = parser.parse_args()

    socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    accelerations = []
    rotations = []

    for x in range(0, 3600):
        deg = float(x/10)
        rotations.append((deg, deg, deg))

    for rot in rotations:
        data = {"rotation": 
                {"x": rot[0],
                 "y": rot[1],
                 "z": rot[2]},
               }
        socket.sendto(json.dumps(data), (args.host, args.port))
