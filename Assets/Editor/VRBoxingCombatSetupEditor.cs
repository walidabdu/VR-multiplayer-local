using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class VRBoxingCombatSetupEditor
{
    private const string HitDetectorLayerName = "HIT_DETECTOR";
    private const string BoxingGlovesLayerName = "BOXING_GLOVES";

    [MenuItem("Tools/VR Boxing/Ensure Combat Layers")]
    public static void EnsureCombatLayers()
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layersProperty = tagManager.FindProperty("layers");

        EnsureLayerName(layersProperty, 6, HitDetectorLayerName);
        EnsureLayerName(layersProperty, 7, BoxingGlovesLayerName);

        tagManager.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        Debug.Log("VR Boxing: combat layers validated.");
    }

    [MenuItem("Tools/VR Boxing/Setup Selected Player Avatar Combat Rig")]
    public static void SetupSelectedPlayerAvatarCombatRig()
    {
        SetupSelectedAvatarRoot(root =>
        {
            EnsureCombatLayers();
            SetupCombatRig(root);
            Debug.Log($"VR Boxing: combat rig setup completed for {root.name}.");
        });
    }

    [MenuItem("Tools/VR Boxing/Setup Active Scene Boxing Resolver")]
    public static void SetupActiveSceneBoxingResolver()
    {
        BoxingHitResolver resolver = Object.FindObjectOfType<BoxingHitResolver>();
        if (resolver == null)
        {
            GameObject resolverObject = new GameObject("BoxingHitResolver");
            Undo.RegisterCreatedObjectUndo(resolverObject, "Create BoxingHitResolver");
            resolver = Undo.AddComponent<BoxingHitResolver>(resolverObject);
        }

        EditorSceneManager.MarkSceneDirty(resolver.gameObject.scene);
        Selection.activeGameObject = resolver.gameObject;
        Debug.Log("VR Boxing: BoxingHitResolver scene object is ready.");
    }

    [MenuItem("Tools/VR Boxing/Validate Selected Player Avatar")]
    public static void ValidateSelectedPlayerAvatar()
    {
        SetupSelectedAvatarRoot(root =>
        {
            List<string> issues = new List<string>();

            if (root.GetComponent<NetworkPlayer>() == null)
            {
                issues.Add("Missing NetworkPlayer on root.");
            }

            if (root.GetComponent<NetworkPlayerCombatState>() == null)
            {
                issues.Add("Missing NetworkPlayerCombatState on root.");
            }

            if (root.GetComponentInChildren<AtomNetworkAnimator_V13>(true) == null)
            {
                issues.Add("Missing AtomNetworkAnimator_V13 in children.");
            }

            Transform combatRig = root.transform.Find("CombatRig");
            if (combatRig == null)
            {
                issues.Add("Missing CombatRig child.");
            }
            else
            {
                ValidateCombatChild(combatRig, "HeadHurtboxRoot", typeof(CombatHurtbox), issues);
                ValidateCombatChild(combatRig, "ChestHurtboxRoot", typeof(CombatHurtbox), issues);
                ValidateCombatChild(combatRig, "BellyHurtboxRoot", typeof(CombatHurtbox), issues);
                ValidateCombatChild(combatRig, "LeftGloveHitboxRoot", typeof(CombatGloveHitDetector), issues);
                ValidateCombatChild(combatRig, "RightGloveHitboxRoot", typeof(CombatGloveHitDetector), issues);
            }

            Rigidbody rootRigidbody = root.GetComponent<Rigidbody>();
            if (rootRigidbody != null && (!rootRigidbody.isKinematic || rootRigidbody.useGravity))
            {
                issues.Add("Root Rigidbody is still configured as a dynamic body.");
            }

            if (issues.Count == 0)
            {
                Debug.Log($"VR Boxing: validation passed for {root.name}.");
                return;
            }

            foreach (string issue in issues)
            {
                Debug.LogWarning($"VR Boxing validation: {issue}", root);
            }
        });
    }

    private static void SetupSelectedAvatarRoot(System.Action<GameObject> setupAction)
    {
        Object selectedObject = Selection.activeObject;
        GameObject selectedGameObject = Selection.activeGameObject;

        if (selectedObject == null && selectedGameObject == null)
        {
            Debug.LogError("VR Boxing: select a player avatar prefab or an avatar root in the scene first.");
            return;
        }

        string assetPath = AssetDatabase.GetAssetPath(selectedObject);
        if (!string.IsNullOrEmpty(assetPath) && PrefabUtility.GetPrefabAssetType(selectedObject) != PrefabAssetType.NotAPrefab)
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);
            try
            {
                GameObject avatarRoot = ResolveAvatarRoot(prefabRoot);
                setupAction(avatarRoot);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            return;
        }

        if (selectedGameObject == null)
        {
            Debug.LogError("VR Boxing: no valid scene object selected.");
            return;
        }

        GameObject sceneRoot = ResolveAvatarRoot(selectedGameObject);
        Undo.RegisterFullObjectHierarchyUndo(sceneRoot, "Setup VR Boxing Combat Rig");
        setupAction(sceneRoot);
        EditorSceneManager.MarkSceneDirty(sceneRoot.scene);
    }

    private static GameObject ResolveAvatarRoot(GameObject selectedObject)
    {
        NetworkPlayer networkPlayer = selectedObject.GetComponentInParent<NetworkPlayer>();
        return networkPlayer != null ? networkPlayer.gameObject : selectedObject;
    }

    private static void SetupCombatRig(GameObject avatarRoot)
    {
        NetworkPlayer networkPlayer = GetOrAddComponent<NetworkPlayer>(avatarRoot);
        NetworkPlayerCombatState combatState = GetOrAddComponent<NetworkPlayerCombatState>(avatarRoot);
        AtomNetworkAnimator_V13 animator = avatarRoot.GetComponentInChildren<AtomNetworkAnimator_V13>(true);

        combatState.networkPlayer = networkPlayer;
        combatState.atomAnimator = animator;

        Rigidbody rootRigidbody = avatarRoot.GetComponent<Rigidbody>();
        if (rootRigidbody != null)
        {
            rootRigidbody.useGravity = false;
            rootRigidbody.isKinematic = true;
            rootRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }

        Transform combatRig = GetOrCreateChild(avatarRoot.transform, "CombatRig");

        Transform headAnchor = FindAnchor(
            avatarRoot.transform,
            animator != null ? animator.HeadAnchorTransform : null,
            "head collision",
            "head target",
            "head");

        Transform chestAnchor = FindAnchor(
            avatarRoot.transform,
            FindChildRecursive(avatarRoot.transform, "upper body"),
            "upper body",
            "body pivot");

        Transform bellyAnchor = FindAnchor(
            avatarRoot.transform,
            FindChildRecursive(avatarRoot.transform, "lower body"),
            "lower body",
            "body pivot");

        Transform leftGloveAnchor = FindAnchor(
            avatarRoot.transform,
            animator != null ? animator.LeftGloveAnchorTransform : null,
            "left arm IK_target",
            "lefthand",
            "left hand target");

        Transform rightGloveAnchor = FindAnchor(
            avatarRoot.transform,
            animator != null ? animator.RightGloveAnchorTransform : null,
            "right arm IK_target",
            "righthand",
            "right hand target");

        SetupHeadHurtbox(GetOrCreateChild(combatRig, "HeadHurtboxRoot").gameObject, headAnchor, combatState);
        SetupChestHurtbox(GetOrCreateChild(combatRig, "ChestHurtboxRoot").gameObject, chestAnchor, combatState);
        SetupBellyHurtbox(GetOrCreateChild(combatRig, "BellyHurtboxRoot").gameObject, bellyAnchor, combatState);
        SetupLeftGlove(GetOrCreateChild(combatRig, "LeftGloveHitboxRoot").gameObject, leftGloveAnchor, combatState);
        SetupRightGlove(GetOrCreateChild(combatRig, "RightGloveHitboxRoot").gameObject, rightGloveAnchor, combatState);

        combatState.CacheCombatRigReferences();
    }

    private static void SetupHeadHurtbox(GameObject hurtboxObject, Transform anchor, NetworkPlayerCombatState combatState)
    {
        hurtboxObject.layer = LayerMask.NameToLayer(HitDetectorLayerName);
        SetupCommonFollower(hurtboxObject, anchor);

        SphereCollider sphereCollider = GetOrAddComponent<SphereCollider>(hurtboxObject);
        sphereCollider.isTrigger = true;
        sphereCollider.radius = 0.16f;
        sphereCollider.center = Vector3.zero;

        RemoveComponentIfPresent<CapsuleCollider>(hurtboxObject);

        CombatHurtbox hurtbox = GetOrAddComponent<CombatHurtbox>(hurtboxObject);
        hurtbox.ownerCombatant = combatState;
        hurtbox.hurtboxType = CombatHurtboxType.Head;
        hurtbox.hurtboxCollider = sphereCollider;
        hurtbox.validationRadius = 0.16f;
    }

    private static void SetupChestHurtbox(GameObject hurtboxObject, Transform anchor, NetworkPlayerCombatState combatState)
    {
        hurtboxObject.layer = LayerMask.NameToLayer(HitDetectorLayerName);
        SetupCommonFollower(hurtboxObject, anchor);

        CapsuleCollider capsuleCollider = GetOrAddComponent<CapsuleCollider>(hurtboxObject);
        capsuleCollider.isTrigger = true;
        capsuleCollider.direction = 1;
        capsuleCollider.radius = 0.18f;
        capsuleCollider.height = 0.42f;
        capsuleCollider.center = new Vector3(0f, 0.06f, 0f);

        RemoveComponentIfPresent<SphereCollider>(hurtboxObject);

        CombatHurtbox hurtbox = GetOrAddComponent<CombatHurtbox>(hurtboxObject);
        hurtbox.ownerCombatant = combatState;
        hurtbox.hurtboxType = CombatHurtboxType.Chest;
        hurtbox.hurtboxCollider = capsuleCollider;
        hurtbox.validationRadius = 0.24f;
    }

    private static void SetupBellyHurtbox(GameObject hurtboxObject, Transform anchor, NetworkPlayerCombatState combatState)
    {
        hurtboxObject.layer = LayerMask.NameToLayer(HitDetectorLayerName);
        SetupCommonFollower(hurtboxObject, anchor);

        CapsuleCollider capsuleCollider = GetOrAddComponent<CapsuleCollider>(hurtboxObject);
        capsuleCollider.isTrigger = true;
        capsuleCollider.direction = 1;
        capsuleCollider.radius = 0.17f;
        capsuleCollider.height = 0.34f;
        capsuleCollider.center = new Vector3(0f, -0.02f, 0f);

        RemoveComponentIfPresent<SphereCollider>(hurtboxObject);

        CombatHurtbox hurtbox = GetOrAddComponent<CombatHurtbox>(hurtboxObject);
        hurtbox.ownerCombatant = combatState;
        hurtbox.hurtboxType = CombatHurtboxType.Belly;
        hurtbox.hurtboxCollider = capsuleCollider;
        hurtbox.validationRadius = 0.22f;
    }

    private static void SetupLeftGlove(GameObject gloveObject, Transform anchor, NetworkPlayerCombatState combatState)
    {
        SetupGlove(gloveObject, anchor, combatState, CombatGloveType.Left);
    }

    private static void SetupRightGlove(GameObject gloveObject, Transform anchor, NetworkPlayerCombatState combatState)
    {
        SetupGlove(gloveObject, anchor, combatState, CombatGloveType.Right);
    }

    private static void SetupGlove(GameObject gloveObject, Transform anchor, NetworkPlayerCombatState combatState, CombatGloveType gloveType)
    {
        gloveObject.layer = LayerMask.NameToLayer(BoxingGlovesLayerName);
        SetupCommonFollower(gloveObject, anchor);

        SphereCollider sphereCollider = GetOrAddComponent<SphereCollider>(gloveObject);
        sphereCollider.isTrigger = true;
        sphereCollider.radius = 0.12f;
        sphereCollider.center = Vector3.zero;

        RemoveComponentIfPresent<CapsuleCollider>(gloveObject);

        CombatGloveHitDetector gloveDetector = GetOrAddComponent<CombatGloveHitDetector>(gloveObject);
        gloveDetector.ownerCombatant = combatState;
        gloveDetector.gloveType = gloveType;
        gloveDetector.gloveTrigger = sphereCollider;
        gloveDetector.minimumPunchSpeed = 1.25f;
        gloveDetector.minimumApproachDot = 0.15f;
        gloveDetector.repeatHitCooldown = 0.15f;
    }

    private static void SetupCommonFollower(GameObject combatObject, Transform anchor)
    {
        CombatAnchorFollower follower = GetOrAddComponent<CombatAnchorFollower>(combatObject);
        follower.anchor = anchor;
        follower.followPosition = true;
        follower.followRotation = true;
        follower.useFixedUpdate = true;
        follower.useRigidbodyMove = true;
        follower.localPositionOffset = Vector3.zero;
        follower.localEulerOffset = Vector3.zero;

        Rigidbody rigidbody = GetOrAddComponent<Rigidbody>(combatObject);
        rigidbody.useGravity = false;
        rigidbody.isKinematic = true;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    private static void EnsureLayerName(SerializedProperty layersProperty, int index, string expectedLayerName)
    {
        SerializedProperty layerProperty = layersProperty.GetArrayElementAtIndex(index);
        if (string.IsNullOrEmpty(layerProperty.stringValue) || layerProperty.stringValue == expectedLayerName)
        {
            layerProperty.stringValue = expectedLayerName;
            return;
        }

        if (LayerMask.NameToLayer(expectedLayerName) == index)
        {
            return;
        }

        Debug.LogWarning($"VR Boxing: layer slot {index} is occupied by '{layerProperty.stringValue}'. Validate your layer mapping manually.");
    }

    private static void ValidateCombatChild(Transform combatRig, string childName, System.Type componentType, List<string> issues)
    {
        Transform child = combatRig.Find(childName);
        if (child == null)
        {
            issues.Add($"Missing {childName} under CombatRig.");
            return;
        }

        if (child.GetComponent(componentType) == null)
        {
            issues.Add($"{childName} is missing {componentType.Name}.");
        }
    }

    private static Transform FindAnchor(Transform root, Transform preferredAnchor, params string[] candidateNames)
    {
        if (preferredAnchor != null)
        {
            return preferredAnchor;
        }

        foreach (string candidateName in candidateNames)
        {
            Transform foundAnchor = FindChildRecursive(root, candidateName);
            if (foundAnchor != null)
            {
                return foundAnchor;
            }
        }

        return root;
    }

    private static Transform FindChildRecursive(Transform root, string targetName)
    {
        if (root.name == targetName)
        {
            return root;
        }

        for (int index = 0; index < root.childCount; index++)
        {
            Transform child = root.GetChild(index);
            Transform found = FindChildRecursive(child, targetName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static Transform GetOrCreateChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null)
        {
            return child;
        }

        GameObject childObject = new GameObject(childName);
        Undo.RegisterCreatedObjectUndo(childObject, $"Create {childName}");
        child = childObject.transform;
        child.SetParent(parent, false);
        child.localPosition = Vector3.zero;
        child.localRotation = Quaternion.identity;
        child.localScale = Vector3.one;
        return child;
    }

    private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        if (component != null)
        {
            return component;
        }

        return Undo.AddComponent<T>(gameObject);
    }

    private static void RemoveComponentIfPresent<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        if (component != null)
        {
            Undo.DestroyObjectImmediate(component);
        }
    }
}
