#!/usr/bin/env bash
set -e
cd "${0%/*}"

echo "Setting up Epsilon (this can take a bit)..."

echo "Loading submodules..."
git submodule init
git submodule update --recursive --remote

echo "Building executable..."
./build.bash
echo "Executable built"

echo "Setting up libraries..."
./scripts/buildlibs.bash

echo "Setting up venv..."
rm -rdf venv
python3 -m virtualenv venv
source venv/bin/activate
pip install --upgrade pip
pip install llvmlite
pip install orjson
deactivate
echo "venv setup complete"

echo "Setting up tests..."
./tests/setup.bash

ln -fs ../../scripts/pre-commit.py .git/hooks/pre-commit

mkdir -p temp

sudo ln -fs "$PWD/scripts/epslc.bash" /usr/local/bin/epslc || echo "Failed to add Epsilon to /usr/local/bin"

echo "Setup complete"
