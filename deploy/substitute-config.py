#!/usr/bin/env python3
"""Replace selected keys in a JSON config file from environment variables."""

import json
import os
import sys


def main() -> None:
    if len(sys.argv) < 3:
        print("usage: substitute-config.py <file> <key>...", file=sys.stderr)
        sys.exit(1)

    path, keys = sys.argv[1], sys.argv[2:]
    with open(path, encoding="utf-8") as handle:
        data = json.load(handle)

    missing = [key for key in keys if not os.environ.get(key)]
    if missing:
        print("Missing config values: " + ", ".join(missing), file=sys.stderr)
        sys.exit(1)

    for key in keys:
        value = os.environ[key]
        if key == "private_key":
            value = value.replace("\\n", "\n")
        data[key] = value

    with open(path, "w", encoding="utf-8", newline="\n") as handle:
        json.dump(data, handle, indent=2)
        handle.write("\n")


if __name__ == "__main__":
    main()
