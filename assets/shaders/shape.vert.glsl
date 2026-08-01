#version 450
layout(location = 0) in vec2 inPosition;
layout(location = 1) in vec4 inColor;
layout(location = 0) out vec4 outColor;
layout(push_constant) uniform Camera { vec2 viewport; } camera;
void main() {
    vec2 ndc = (inPosition / camera.viewport) * 2.0 - 1.0;
    gl_Position = vec4(ndc.x, -ndc.y, 0.0, 1.0);
    outColor = inColor;
}
