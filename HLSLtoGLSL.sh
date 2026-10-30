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
    -T vs_6_8 \
    -E VSMain \
    -I HLSL \
    -fspv-target-env=universal1.5 \
    HLSL/ShapeVertex.hlsl \
    -Fo "$PRECROSS/ShapeVertex.spv"
dxc -spirv \
    -T vs_6_8 \
    -E VSMain \
    -I HLSL \
    -fspv-target-env=universal1.5 \
    HLSL/GridAlignedQuadVertex.hlsl \
    -Fo "$PRECROSS/GridAlignedQuadVertex.spv"
dxc -spirv \
    -T ps_6_8 \
    -E PSMain \
    -I HLSL \
    -fspv-target-env=universal1.5 \
    HLSL/Fragment.hlsl \
    -Fo "$PRECROSS/Fragment.spv"
spirv-cross --glsl-emit-ubo-as-plain-uniforms \
    "$PRECROSS/ShapeVertex.spv" --output "$CROSS/ShapeVertex.glsl"
spirv-cross --glsl-emit-ubo-as-plain-uniforms \
    "$PRECROSS/GridAlignedQuadVertex.spv" --output "$CROSS/GridAlignedQuadVertex.glsl"
spirv-cross --glsl-emit-ubo-as-plain-uniforms \
    "$PRECROSS/Fragment.spv" --output "$CROSS/Fragment.glsl"

rm "$PRECROSS/ShapeVertex.spv" "$PRECROSS/GridAlignedQuadVertex.spv" "$PRECROSS/Fragment.spv"
rm -d "$PRECROSS"
echo "HLSL shaders compiled to generic GLSL in '$CROSS'."