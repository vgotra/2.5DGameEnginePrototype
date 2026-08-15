using System.Numerics;

namespace Engine.Assets;

public readonly record struct GltfAnimationTrack(int Node, byte Path, byte Interpolation, float[] Times, Vector4[] Values);
public readonly record struct GltfPose(Matrix4x4[] NodeTransforms, Matrix4x4[] JointMatrices);

public static class GltfPoseEvaluator
{
    public static bool TryEvaluate(in GltfModelAsset model, ReadOnlySpan<GltfAnimationTrack> tracks, float time, out GltfPose pose, out string error)
    {
        error = string.Empty;
        Matrix4x4[] locals = new Matrix4x4[model.Nodes.Length];
        for (int i = 0; i < model.Nodes.Length; i++) locals[i] = Matrix4x4.CreateScale(model.Nodes[i].Scale) * Matrix4x4.CreateFromQuaternion(model.Nodes[i].Rotation) * Matrix4x4.CreateTranslation(model.Nodes[i].Translation);
        for (int i = 0; i < tracks.Length; i++)
        {
            GltfAnimationTrack track = tracks[i];
            if (track.Node < 0 || track.Node >= model.Nodes.Length || track.Times.Length != track.Values.Length || track.Times.Length == 0 || track.Interpolation != 1) { error = "Animation track is invalid or uses unsupported interpolation."; pose = default; return false; }
            int key = FindKey(track.Times, time);
            int next = Math.Min(key + 1, track.Times.Length - 1);
            float span = track.Times[next] - track.Times[key];
            float amount = span <= 0f ? 0f : Math.Clamp((time - track.Times[key]) / span, 0f, 1f);
            Vector4 value = Vector4.Lerp(track.Values[key], track.Values[next], amount);
            GltfNode node = model.Nodes[track.Node];
            Vector3 translation = node.Translation, scale = node.Scale;
            Quaternion rotation = node.Rotation;
            if (track.Path == 0) translation = new Vector3(value.X, value.Y, value.Z);
            else if (track.Path == 1) rotation = Quaternion.Normalize(new Quaternion(value.X, value.Y, value.Z, value.W));
            else if (track.Path == 2) scale = new Vector3(value.X, value.Y, value.Z);
            else { error = "Animation channel path is unsupported."; pose = default; return false; }
            locals[track.Node] = Matrix4x4.CreateScale(scale) * Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateTranslation(translation);
        }
        Matrix4x4[] globals = new Matrix4x4[locals.Length];
        for (int i = 0; i < locals.Length; i++) globals[i] = model.Nodes[i].Parent >= 0 ? locals[i] * globals[model.Nodes[i].Parent] : locals[i];
        Matrix4x4[] joints = new Matrix4x4[model.Joints.Length];
        for (int i = 0; i < joints.Length; i++) { int node = model.Joints[i]; joints[i] = node >= 0 && node < globals.Length ? globals[node] : Matrix4x4.Identity; }
        pose = new GltfPose(globals, joints);
        return true;
    }

    private static int FindKey(float[] times, float time)
    {
        int key = 0;
        for (int i = 1; i < times.Length && times[i] <= time; i++) key = i;
        return key;
    }
}
