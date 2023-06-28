using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

// About this issue:
// Modifying the position/scale property through 'TransformStreamHandle' does not take effect when using Humanoid animation.
// 
// How to reproduce:
// 1. Open the "Sample" scene.
// 2. Enter the Play mode.
// 3. Select the "Player" GameObject in the Hierarchy.
// 4. Expand the "ModifyBoneTest" component in the Inspector.
// 5. Observe the "Bone Value For Read" property(or observe the size of the player's head in the Scene view).
// Expected result: The value of "Bone Value For Read" should be the same as the value of "Bone Value" (the size of the player's head should also be the same as the value of "Bone Value").
// Actual result: The value of "Bone Value For Read" is not the same as the value of "Bone Value" (or the size of the player's head is not changed).
// 
// You can modify the "Mode", "Bone Value" and "Alpha" properties to see the effect.


public enum ModifyBoneMode : byte
{
    None,
    Scale,
    Rotation,
    Position,
}

public struct ModifyBoneJob : IAnimationJob
{
    public TransformStreamHandle boneHandle;
    public NativeReference<ModifyBoneMode> modeRef;
    public NativeReference<Vector3> boneValueRef;
    public NativeReference<float> alphaValueRef;

    public void ProcessRootMotion(AnimationStream stream) { }

    public void ProcessAnimation(AnimationStream stream)
    {
        switch (modeRef.Value)
        {
            case ModifyBoneMode.None:
                break;

            case ModifyBoneMode.Scale:
                var oriLocalScale = boneHandle.GetLocalScale(stream);
                var newLocalScale = Vector3.Lerp(oriLocalScale, boneValueRef.Value, alphaValueRef.Value);
                boneHandle.SetLocalScale(stream, newLocalScale);
                break;

            case ModifyBoneMode.Rotation:
                var oriLocalRotation = boneHandle.GetLocalRotation(stream);
                var newLocalRotation = Quaternion.Slerp(oriLocalRotation, Quaternion.Euler(boneValueRef.Value), alphaValueRef.Value);
                boneHandle.SetLocalRotation(stream, newLocalRotation);
                break;

            case ModifyBoneMode.Position:
                var oriLocalPosition = boneHandle.GetLocalPosition(stream);
                var newLocalPosition = Vector3.Lerp(oriLocalPosition, boneValueRef.Value, alphaValueRef.Value);
                boneHandle.SetLocalPosition(stream, newLocalPosition);
                break;

            default:
                throw new System.ArgumentOutOfRangeException();
        }
    }
}

public struct ReadBoneJob : IAnimationJob
{
    public TransformStreamHandle boneHandle;
    public NativeReference<ModifyBoneMode> modeRef;
    public NativeReference<Vector3> boneValueForReadRef;

    public void ProcessRootMotion(AnimationStream stream) { }

    public void ProcessAnimation(AnimationStream stream)
    {
        switch (modeRef.Value)
        {
            case ModifyBoneMode.None:
                boneValueForReadRef.Value = Vector3.zero;
                break;

            case ModifyBoneMode.Scale:
                boneValueForReadRef.Value = boneHandle.GetLocalScale(stream);
                break;

            case ModifyBoneMode.Rotation:
                boneValueForReadRef.Value = boneHandle.GetLocalRotation(stream).eulerAngles;
                break;

            case ModifyBoneMode.Position:
                boneValueForReadRef.Value = boneHandle.GetLocalPosition(stream);
                break;

            default:
                throw new System.ArgumentOutOfRangeException();
        }
    }
}

[RequireComponent(typeof(Animator))]
public class ModifyBoneTest : MonoBehaviour
{
    public AnimationClip animClip;
    public Transform bone;
    public ModifyBoneMode mode;
    public Vector3 boneValue;
    public Vector3 boneValueForRead;
    [Range(0f, 1f)]
    public float alpha;

    private PlayableGraph _graph;
    private NativeReference<ModifyBoneMode> _modeRef;
    private NativeReference<Vector3> _boneValueRef;
    private NativeReference<Vector3> _boneValueForReadRef;
    private NativeReference<float> _alphaValueRef;

    private void Start()
    {
        var animator = GetComponent<Animator>();
        _graph = PlayableGraph.Create("ModifyBoneTest");
        _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        _modeRef = new NativeReference<ModifyBoneMode>(mode, Allocator.Persistent);
        _boneValueRef = new NativeReference<Vector3>(boneValue, Allocator.Persistent);
        _boneValueForReadRef = new NativeReference<Vector3>(Vector3.zero, Allocator.Persistent);
        _alphaValueRef = new NativeReference<float>(alpha, Allocator.Persistent);

        var acp = AnimationClipPlayable.Create(_graph, animClip);

        var boneHandle = animator.BindStreamTransform(bone);
        var jobData = new ModifyBoneJob
        {
            boneHandle = boneHandle,
            modeRef = _modeRef,
            boneValueRef = _boneValueRef,
            alphaValueRef = _alphaValueRef,
        };
        var asp = AnimationScriptPlayable.Create(_graph, jobData);
        asp.AddInput(acp, 0, 1f);

        var jobDataForRead = new ReadBoneJob
        {
            boneHandle = boneHandle,
            modeRef = _modeRef,
            boneValueForReadRef = _boneValueForReadRef,
        };
        var aspForRead = AnimationScriptPlayable.Create(_graph, jobDataForRead);
        aspForRead.AddInput(asp, 0, 1f);

        var output = AnimationPlayableOutput.Create(_graph, "Animation", animator);
        output.SetSourcePlayable(aspForRead);

        _graph.Play();
    }

    private void Update()
    {
        _modeRef.Value = mode;
        _boneValueRef.Value = boneValue;
        _alphaValueRef.Value = alpha;
    }

    private void LateUpdate()
    {
        boneValueForRead = _boneValueForReadRef.Value;
    }

    private void OnDestroy()
    {
        if (_graph.IsValid()) _graph.Destroy();
        if (_modeRef.IsCreated) _modeRef.Dispose();
        if (_boneValueRef.IsCreated) _boneValueRef.Dispose();
        if (_boneValueForReadRef.IsCreated) _boneValueForReadRef.Dispose();
        if (_alphaValueRef.IsCreated) _alphaValueRef.Dispose();
    }
}
