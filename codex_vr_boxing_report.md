# VR Boxing Combat Refactor Report

## Repo Facts Confirmed From Inspection

- Active multiplayer scene: `Assets/Scenes/2 Game Scene.unity`
- `NetworkManager.PlayerPrefab` in that scene points to `Assets/PlayerAvatar.prefab`
- Owner-driven XR pose copy is handled by `Assets/Scripts/networkplayer.cs`
- Owner-authoritative NGO transform sync is handled by `Assets/myscripts/NetworkTransformClient.cs`
- The live networked Atom puppet script on `PlayerAvatar.prefab` is `Assets/Scripts/AtomNetworkAnimator.cs` (`AtomNetworkAnimator_V13`)
- Local XR rig references come from `Assets/Scripts/VRRIgReferences.cs`
- Existing avatar anchors already present on the prefab include:
  - `head collision`
  - `upper body`
  - `lower body`
  - `lefthand`
  - `righthand`
  - `left arm IK_target`
  - `right arm IK_target`
- Existing project layers already include:
  - `HIT_DETECTOR`
  - `BOXING_GLOVES`
- The current `PlayerAvatar.prefab` root still contains a dynamic `Rigidbody`, which is the main physics-risk item this refactor neutralizes

## Architecture Decisions Applied

- Preserved the current owner-driven tracked transform sync for head, left hand, right hand, and body/root
- Kept the Atom visual puppet transform-driven
- Patched the live Atom animator so the visual boxer position is based on:
  - `network body/root world position`
  - `roomscale offset`
  - `server-authoritative combat offset`
- Added a separate combat runtime layer:
  - `NetworkPlayerCombatState` for server-owned stun and knockback offset
  - `CombatHurtbox` for head/chest/belly hit zones
  - `CombatGloveHitDetector` for owner-only punch reporting
  - `CombatAnchorFollower` for kinematic trigger followers
  - `BoxingHitResolver` for server-side hit validation and stun application
- Avoided `NetworkRigidbody`, `OnCollisionEnter`, and `AddForce` on the live avatar root
- Added editor tooling so the combat rig and resolver can be created or repaired from Unity instead of hand-editing prefab/scene YAML
- Added runtime safety that forces the live avatar root rigidbody into a kinematic non-gravity setup even before the editor repair tool is run

## Files Changed

- `Assets/Scripts/networkplayer.cs`
- `Assets/Scripts/AtomNetworkAnimator.cs`

## Files Added

- `Assets/Scripts/Combat/CombatAnchorFollower.cs`
- `Assets/Scripts/Combat/CombatHurtbox.cs`
- `Assets/Scripts/Combat/CombatGloveHitDetector.cs`
- `Assets/Scripts/Combat/BoxingHitResolver.cs`
- `Assets/Scripts/Combat/NetworkPlayerCombatState.cs`
- `Assets/Editor/VRBoxingCombatSetupEditor.cs`
- Unity `.meta` files for the new folders and scripts

## Assumptions Made

- `PlayerAvatar.prefab` remains the network-spawned avatar that should receive the combat rig
- The current `AtomNetworkAnimator_V13` child on that prefab is the version that must stay live
- `left arm IK_target` and `right arm IK_target` are the best default glove anchors because they match the visual IK result better than the raw tracker roots
- Existing dirty worktree changes in `Assets/PlayerAvatar.prefab`, `Assets/Scenes/2 Game Scene.unity`, `ProjectSettings/TagManager.asset`, and a few XR settings assets were already present and were not manually rewritten by this refactor

## Unity Editor Steps After Pulling

1. Open the project in Unity 2022.3+.
2. Open `Assets/Scenes/2 Game Scene.unity`.
3. In the Project window, select `Assets/PlayerAvatar.prefab`.
4. Click `Tools/VR Boxing/Ensure Combat Layers`.
5. With `Assets/PlayerAvatar.prefab` still selected, click `Tools/VR Boxing/Setup Selected Player Avatar Combat Rig`.
6. Open the active game scene and click `Tools/VR Boxing/Setup Active Scene Boxing Resolver`.
7. Select the player avatar prefab or a scene instance of the avatar root and click `Tools/VR Boxing/Validate Selected Player Avatar`.
8. Save the prefab and the scene if Unity marks them dirty.

## Menu Items Added

- `Tools/VR Boxing/Ensure Combat Layers`
- `Tools/VR Boxing/Setup Selected Player Avatar Combat Rig`
- `Tools/VR Boxing/Setup Active Scene Boxing Resolver`
- `Tools/VR Boxing/Validate Selected Player Avatar`

## Manual Inspector Fields To Check

- Usually none if the prefab still uses the inspected names and hierarchy
- If validation reports missing anchors, check the `CombatAnchorFollower.anchor` fields on:
  - `HeadHurtboxRoot`
  - `ChestHurtboxRoot`
  - `BellyHurtboxRoot`
  - `LeftGloveHitboxRoot`
  - `RightGloveHitboxRoot`
- On `NetworkPlayerCombatState`, confirm `networkPlayer` and `atomAnimator` were auto-filled by the setup tool
- On `BoxingHitResolver`, tune punch-speed and knockback values if the default reaction feels too weak or too strong

## First Runtime Tests To Run

1. Verify that two players still spawn and mirror head/hands/body exactly as before.
2. Verify that the live avatar root no longer behaves like a dynamic rigidbody when spawned.
3. Verify that `CombatRig` followers stay aligned with the visual rig while ducking, rolling, and roomscale movement.
4. Verify that glove triggers only report hits from the owning player.
5. Verify that server-approved hits apply about 1 second of stun and a visible combat offset reaction.
6. Verify that stun blocks locomotion input but does not block headset tracking.
7. Verify that outgoing punch reporting is suppressed while stunned if that behavior is desired.

## Follow-Up Risks / Compile Checks

- Unity was not run in this workspace, so this was validated at repo level only
- The new combat scripts rely on NGO RPC and `NetworkVariable<Vector3>` behavior that should be compile-checked inside Unity
- The locomotion-blocking pass uses type-name filtering for move/turn/teleport providers, so test that it disables the intended XR locomotion components only
- Hit validation distances and punch-speed thresholds will likely need first-pass tuning in play mode
- If the avatar hierarchy names change from the inspected prefab, rerun validation and manually repair the anchor fields reported by the tool
