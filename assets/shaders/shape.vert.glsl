#version 450
layout(location = 0) in vec2 inPosition;
layout(location = 1) in vec2 inSize;
layout(location = 2) in vec4 inColor;
layout(location = 3) in vec4 inBottomColor;
layout(location = 4) in vec2 inUvScale;
layout(location = 5) in vec2 inUvOffset;
layout(location = 6) in uint inTextureIndex;
layout(location = 0) out vec4 outColor;
layout(location = 1) out vec2 outUv;
layout(location = 2) flat out uint outTextureIndex;
layout(location = 3) out vec4 outBottomColor;
layout(push_constant) uniform Camera { vec2 viewport; } camera;
void main() {
    const vec2 positions[6] = vec2[6](vec2(0.0, -0.5), vec2(0.5, 0.0), vec2(0.0, 0.5), vec2(0.0, -0.5), vec2(0.0, 0.5), vec2(-0.5, 0.0));
    const vec2 uvs[6] = vec2[6](vec2(0.5, 0.0), vec2(1.0, 0.5), vec2(0.5, 1.0), vec2(0.5, 0.0), vec2(0.5, 1.0), vec2(0.0, 0.5));
    vec2 position = inPosition + positions[gl_VertexIndex] * inSize;
    vec2 ndc = (position / camera.viewport) * 2.0 - 1.0;
    gl_Position = vec4(ndc.x, ndc.y, 0.0, 1.0);
    outColor = inColor;
    outBottomColor = inBottomColor;
    outUv = inUvOffset + uvs[gl_VertexIndex] * inUvScale;
    outTextureIndex = inTextureIndex;
}
