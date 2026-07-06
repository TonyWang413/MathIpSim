#!/bin/bash
# Exit immediately if any command fails
set -e

# Automatically change directory to where the script is located
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$SCRIPT_DIR"

echo "======================================="
echo "   Building Math IP C Demo (macOS)"
echo "======================================="

# Check if clang compiler is installed
if ! command -v clang &> /dev/null; then
    echo "[ERROR] clang compiler not found."
    echo "Please install Xcode Command Line Tools by running: xcode-select --install"
    exit 1
fi

echo "[INFO] Compiling main.c and math_ip_driver.c..."
clang main.c math_ip_driver.c -I . -o c_demo

echo "---------------------------------------"
echo "Build SUCCESSFUL! Created executable: c_demo"
echo "---------------------------------------"
echo "To run the demo (make sure the C# Daemon is running in the background):"
echo "  ./c_demo"
