#!/bin/bash
set -e
PRECROSS="SpirV PreCross Cache"
CROSS="GLSL SpriV Translation"
missing=""
command -v dxc >/dev/null 2>&1 || missing="$missing dxc"
command -v spirv-cross >/dev/null 2>&1 || missing="$missing spirv-cross"
if [ -n "$missing" ]; then
  echo "Missing tools:$missing" >&2
  exit 3
fi

mkdir -p "$PRECROSS" "$CROSS"
dxc -spirv \
    -enable-16bit-types \
    -T vs_6_8 \
    -E VSMain \
    -fspv-target-env=universal1.5 \
    HLSL/Vertex.hlsl \
    -Fo "$PRECROSS/Vertex.spv"
dxc -spirv \
    -enable-16bit-types \
    -T ps_6_8 \
    -E PSMain \
    -fspv-target-env=universal1.5 \
    HLSL/Fragment.hlsl \
    -Fo "$PRECROSS/Fragment.spv"
spirv-cross --glsl-emit-ubo-as-plain-uniforms \
    "$PRECROSS/Vertex.spv" --output "$CROSS/Vertex.glsl"
spirv-cross --glsl-emit-ubo-as-plain-uniforms \
    "$PRECROSS/Fragment.spv" --output "$CROSS/Fragment.glsl"

rm "$PRECROSS/Vertex.spv" "$PRECROSS/Fragment.spv"
rm -d "$PRECROSS"
echo "HLSL shaders compiled to generic GLSL in '$CROSS'."