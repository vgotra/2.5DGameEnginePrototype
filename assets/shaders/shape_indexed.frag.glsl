#version 450
#extension GL_EXT_nonuniform_qualifier : require
layout(set = 0, binding = 0) uniform sampler2D textures[];
layout(location = 0) in vec4 inColor;
layout(location = 1) in vec2 inUv;
layout(location = 2) flat in uint inTextureIndex;
layout(location = 0) out vec4 outColor;
void main() { outColor = texture(textures[nonuniformEXT(inTextureIndex)], inUv) * inColor; }
