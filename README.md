# Unity-Bug-Report-Playable-IN-45608

## About this issue

Modifying the position/scale property through 'TransformStreamHandle' does not take effect when using Humanoid animation.

## How to reproduce

1. Open the "Sample" scene.
2. Enter the Play mode.
3. Select the "Player" GameObject in the Hierarchy.
4. Expand the "ModifyBoneTest" component in the Inspector.
5. Observe the "Bone Value For Read" property(or observe the size of the player's head in the Scene view).

Expected result: The value of "Bone Value For Read" should be the same as the value of "Bone Value" (the size of the player's head should also be the same as the value of "Bone Value").

Actual result: The value of "Bone Value For Read" is not the same as the value of "Bone Value" (or the size of the player's head is not changed).

You can modify the "Mode", "Bone Value" and "Alpha" properties to see the effect.
