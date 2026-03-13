#!/bin/bash

# Test script to verify architecture detection logic

echo "Testing architecture detection..."
echo "Current architecture: $(uname -m)"

# Simulate the Dockerfile architecture detection logic
arch=$(uname -m)
if [ "$arch" = "aarch64" ]; then
    dotnet_arch="arm64"
elif [ "$arch" = "x86_64" ]; then
    dotnet_arch="x64"
else
    echo "Unsupported architecture: $arch"
    exit 1
fi

echo "Detected .NET architecture: $dotnet_arch"
echo "Expected .NET Runtime URL: https://dotnetcli.azureedge.net/dotnet/Runtime/3.1.23/dotnet-runtime-3.1.23-linux-$dotnet_arch.tar.gz"
echo "Architecture detection test passed!"
