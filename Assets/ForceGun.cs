using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class ForceGun : MonoBehaviour
{
    public List<Rigidbody> hitTargets = new List<Rigidbody>();

    [Header("Gun")]
    public float ReloadTime = 5f;
    public float Timer;
    public float mult = 1f;
    public Transform ShootPoint;
    public float Force = 15f;

    [Header("Self Push")]
    public float SelfForce = 2f;

    [Header("Grab")]
    [Range(1, 9)]
    public int MaxHeldItems = 9;

    public float HoldDistance = 5f;
    public float MinHoldDistance = 1.5f;
    public float MaxHoldDistance = 15f;
    public float ScrollDistanceSpeed = 2f;
    public float GrabSpring = 65f;
    public float GrabDamping = 8f;
    public float MaxGrabForce = 120f;
    public float GrabAngularDamping = 0.5f;
    public float HoldRotationStrength = 12f;
    public float HoldRotationDamping = 4f;
    public float MaxHoldTorque = 30f;

    [Header("Grab Layout")]
    public float GrabSpacing = 1.5f;

    [Header("Throwing")]
    public float ThrowForce = 25f;
    public float ThrowUpForce = 3f;

    public Image Indicator;

    [Header("Particle Effects")]
    public GameObject ShootEffectPrefab;
    public GameObject HoldEffectPrefab;
    public Transform EffectParent;
    public float ShootEffectLifetime = 2f;

    private const int BuiltInMaxHeldItems = 9;

    private class HeldTargetData
    {
        public Rigidbody rb;
        public Vector3 localGrabPoint;
        public bool usedGravity;
        public bool wasKinematic;
        public Quaternion rotationOffset;
    }

    private readonly List<HeldTargetData> heldTargets =
        new List<HeldTargetData>();

    private readonly Dictionary<Rigidbody, int> collidersInside =
        new Dictionary<Rigidbody, int>();

    private GameObject currentHoldEffect;
    private PlayerMoveTeck MoveTeck;
    private bool grabHeld;

    void Start()
    {
        MoveTeck = FindAnyObjectByType<PlayerMoveTeck>();

        MaxHeldItems =
            Mathf.Clamp(
                MaxHeldItems,
                1,
                BuiltInMaxHeldItems
            );

        if (EffectParent == null)
        {
            EffectParent =
                ShootPoint != null
                    ? ShootPoint
                    : transform;
        }
    }

    void OnValidate()
    {
        MaxHeldItems =
            Mathf.Clamp(
                MaxHeldItems,
                1,
                BuiltInMaxHeldItems
            );
    }

    void OnTriggerEnter(Collider other)
    {
        Rigidbody target = other.attachedRigidbody;

        if (target == null)
            target = other.GetComponentInParent<Rigidbody>();

        if (target == null)
            return;

        if (target == MoveTeck?.rb)
            return;

        if (collidersInside.ContainsKey(target))
        {
            collidersInside[target]++;
        }
        else
        {
            collidersInside.Add(target, 1);

            if (!hitTargets.Contains(target))
                hitTargets.Add(target);
        }

        if (grabHeld)
            TryGrabTarget(target);
    }

    void OnTriggerExit(Collider other)
    {
        Rigidbody target = other.attachedRigidbody;

        if (target == null)
            target = other.GetComponentInParent<Rigidbody>();

        if (target == null)
            return;

        if (!collidersInside.ContainsKey(target))
            return;

        collidersInside[target]--;

        if (collidersInside[target] <= 0)
        {
            collidersInside.Remove(target);

            if (!IsHeld(target))
                hitTargets.Remove(target);
        }
    }

    void Update()
    {
        CleanDestroyedTargets();

        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            grabHeld = true;

            SpawnHoldEffect();

            GrabAllAvailableTargets();
        }

        if (grabHeld && Input.GetKey(KeyCode.Mouse1))
        {
            GrabAllAvailableTargets();

            float scroll =
                Input.mouseScrollDelta.y;

            if (Mathf.Abs(scroll) > 0.01f)
            {
                HoldDistance +=
                    scroll *
                    ScrollDistanceSpeed;

                HoldDistance =
                    Mathf.Clamp(
                        HoldDistance,
                        MinHoldDistance,
                        MaxHoldDistance
                    );
            }
        }

        if (Input.GetKeyUp(KeyCode.Mouse1))
        {
            grabHeld = false;

            DestroyHoldEffect();

            DropTargets();
        }

        if (Timer < ReloadTime)
        {
            Timer +=
                Time.deltaTime *
                mult;

            Timer =
                Mathf.Min(
                    Timer,
                    ReloadTime
                );
        }

        bool fullyCharged =
            Timer >= ReloadTime;

        float chargeAmount =
            Mathf.Clamp01(
                Timer /
                Mathf.Max(
                    ReloadTime,
                    0.01f
                )
            );

        if (Indicator != null)
        {
            Color indicatorColor =
                fullyCharged
                    ? Color.green
                    : Color.red;

            Indicator.color =
                new Color(
                    indicatorColor.r,
                    indicatorColor.g,
                    indicatorColor.b,
                    chargeAmount
                );
        }

        if (
            heldTargets.Count > 0 &&
            fullyCharged &&
            Input.GetKeyDown(KeyCode.Mouse0)
        )
        {
            SpawnShootEffect();

            ThrowTargets();

            grabHeld = false;

            DestroyHoldEffect();

            Timer = 0f;
        }
        else if (
            heldTargets.Count == 0 &&
            fullyCharged &&
            Input.GetKeyDown(KeyCode.Mouse0)
        )
        {
            SpawnShootEffect();

            Fire();

            Timer = 0f;
        }
    }

    void FixedUpdate()
    {
        if (heldTargets.Count == 0)
            return;

        Camera cam = Camera.main;

        if (cam == null)
            return;

        Quaternion cameraRotation =
            Quaternion.LookRotation(
                cam.transform.forward,
                cam.transform.up
            );

        for (
            int i = heldTargets.Count - 1;
            i >= 0;
            i--
        )
        {
            HeldTargetData data =
                heldTargets[i];

            if (
                data == null ||
                data.rb == null
            )
            {
                heldTargets.RemoveAt(i);
                continue;
            }

            Rigidbody target =
                data.rb;

            target.WakeUp();

            Vector3 layoutOffset =
                GetHoldOffset(i);

            Vector3 desiredPosition =
                cam.transform.position +
                cam.transform.forward *
                HoldDistance +
                cam.transform.right *
                layoutOffset.x +
                cam.transform.up *
                layoutOffset.y;

            Vector3 worldGrabPoint =
                target.transform.TransformPoint(
                    data.localGrabPoint
                );

            Vector3 difference =
                desiredPosition -
                worldGrabPoint;

            Vector3 pointVelocity =
                target.GetPointVelocity(
                    worldGrabPoint
                );

            Vector3 grabForce =
                difference *
                GrabSpring -
                pointVelocity *
                GrabDamping;

            grabForce =
                Vector3.ClampMagnitude(
                    grabForce,
                    MaxGrabForce
                );

            target.AddForceAtPosition(
                grabForce,
                worldGrabPoint,
                ForceMode.Acceleration
            );

            Quaternion desiredRotation =
                cameraRotation *
                data.rotationOffset;

            Quaternion rotationDifference =
                desiredRotation *
                Quaternion.Inverse(
                    target.rotation
                );

            if (rotationDifference.w < 0f)
            {
                rotationDifference.x =
                    -rotationDifference.x;

                rotationDifference.y =
                    -rotationDifference.y;

                rotationDifference.z =
                    -rotationDifference.z;

                rotationDifference.w =
                    -rotationDifference.w;
            }

            rotationDifference.ToAngleAxis(
                out float angle,
                out Vector3 axis
            );

            if (angle > 180f)
                angle -= 360f;

            if (
                !float.IsNaN(axis.x) &&
                !float.IsNaN(axis.y) &&
                !float.IsNaN(axis.z)
            )
            {
                Vector3 rotationTorque =
                    axis *
                    angle *
                    Mathf.Deg2Rad *
                    HoldRotationStrength -
                    target.angularVelocity *
                    HoldRotationDamping;

                rotationTorque =
                    Vector3.ClampMagnitude(
                        rotationTorque,
                        MaxHoldTorque
                    );

                target.AddTorque(
                    rotationTorque,
                    ForceMode.Acceleration
                );
            }

            if (GrabAngularDamping > 0f)
            {
                target.AddTorque(
                    -target.angularVelocity *
                    GrabAngularDamping,
                    ForceMode.Acceleration
                );
            }
        }
    }

    void GrabAllAvailableTargets()
    {
        if (
            heldTargets.Count >=
            GetMaxHeldItems()
        )
        {
            return;
        }

        List<Rigidbody> targets =
            new List<Rigidbody>(
                hitTargets
            );

        for (
            int i = 0;
            i < targets.Count;
            i++
        )
        {
            if (
                heldTargets.Count >=
                GetMaxHeldItems()
            )
            {
                break;
            }

            TryGrabTarget(
                targets[i]
            );
        }
    }

    void TryGrabTarget(Rigidbody target)
    {
        if (target == null)
            return;

        if (
            heldTargets.Count >=
            GetMaxHeldItems()
        )
        {
            return;
        }

        if (target == MoveTeck?.rb)
            return;

        if (IsHeld(target))
            return;

        SimpleEnemy enemy =
            target.GetComponentInParent<SimpleEnemy>();

        if (enemy != null)
            return;

        HeldTargetData data =
            new HeldTargetData();

        data.rb = target;

        data.localGrabPoint =
            target.transform.InverseTransformPoint(
                target.worldCenterOfMass
            );

        data.usedGravity =
            target.useGravity;

        data.wasKinematic =
            target.isKinematic;

        Camera cam =
            Camera.main;

        if (cam != null)
        {
            Quaternion cameraRotation =
                Quaternion.LookRotation(
                    cam.transform.forward,
                    cam.transform.up
                );

            data.rotationOffset =
                Quaternion.Inverse(
                    cameraRotation
                ) *
                target.rotation;
        }
        else
        {
            data.rotationOffset =
                Quaternion.identity;
        }

        target.isKinematic = false;
        target.useGravity = true;

        target.WakeUp();

        heldTargets.Add(data);

        ThrowableObject throwable =
            target.GetComponentInParent<ThrowableObject>();

        if (throwable == null)
        {
            throwable =
                target.GetComponentInChildren<ThrowableObject>();
        }

        if (throwable != null)
            throwable.MarkGrabbed();

        MissleScript missile =
            target.GetComponentInParent<MissleScript>();

        if (missile == null)
        {
            missile =
                target.GetComponentInChildren<MissleScript>();
        }

        if (missile != null)
            missile.SetGrabbed(true);
    }

    int GetMaxHeldItems()
    {
        return Mathf.Clamp(
            MaxHeldItems,
            1,
            BuiltInMaxHeldItems
        );
    }

    Vector3 GetHoldOffset(int index)
    {
        float spacing =
            GrabSpacing;

        switch (index)
        {
            case 0:
                return new Vector3(
                    0f,
                    spacing,
                    0f
                );

            case 1:
                return new Vector3(
                    -spacing,
                    spacing,
                    0f
                );

            case 2:
                return new Vector3(
                    spacing,
                    spacing,
                    0f
                );

            case 3:
                return new Vector3(
                    -spacing,
                    0f,
                    0f
                );

            case 4:
                return Vector3.zero;

            case 5:
                return new Vector3(
                    spacing,
                    0f,
                    0f
                );

            case 6:
                return new Vector3(
                    -spacing,
                    -spacing,
                    0f
                );

            case 7:
                return new Vector3(
                    0f,
                    -spacing,
                    0f
                );

            case 8:
                return new Vector3(
                    spacing,
                    -spacing,
                    0f
                );
        }

        return Vector3.zero;
    }

    void DropTargets()
    {
        if (heldTargets.Count == 0)
            return;

        List<HeldTargetData> targetsToDrop =
            new List<HeldTargetData>(
                heldTargets
            );

        heldTargets.Clear();

        foreach (
            HeldTargetData data
            in targetsToDrop
        )
        {
            if (
                data == null ||
                data.rb == null
            )
            {
                continue;
            }

            Rigidbody target =
                data.rb;

            target.useGravity =
                data.usedGravity;

            target.isKinematic =
                data.wasKinematic;

            target.WakeUp();

            ThrowableObject throwable =
                target.GetComponentInParent<ThrowableObject>();

            if (throwable == null)
            {
                throwable =
                    target.GetComponentInChildren<ThrowableObject>();
            }

            if (throwable != null)
                throwable.MarkDropped();

            MissleScript missile =
                target.GetComponentInParent<MissleScript>();

            if (missile == null)
            {
                missile =
                    target.GetComponentInChildren<MissleScript>();
            }

            if (missile != null)
            {
                Vector3 direction =
                    target.linearVelocity.sqrMagnitude >
                    0.01f
                        ? target.linearVelocity.normalized
                        : transform.forward;

                missile.ReleaseFromGrab(
                    direction
                );
            }

            if (
                !collidersInside.ContainsKey(
                    target
                )
            )
            {
                hitTargets.Remove(
                    target
                );
            }
        }
    }

    void ThrowTargets()
    {
        if (heldTargets.Count == 0)
            return;

        Camera cam =
            Camera.main;

        if (cam == null)
            return;

        Vector3 throwDirection =
            (
                cam.transform.forward +
                Vector3.up * 0.08f
            ).normalized;

        List<HeldTargetData> targetsToThrow =
            new List<HeldTargetData>(
                heldTargets
            );

        heldTargets.Clear();

        foreach (
            HeldTargetData data
            in targetsToThrow
        )
        {
            if (
                data == null ||
                data.rb == null
            )
            {
                continue;
            }

            Rigidbody target =
                data.rb;

            target.isKinematic = false;
            target.useGravity = true;

            target.WakeUp();

            target.AddForce(
                throwDirection *
                ThrowForce +
                Vector3.up *
                ThrowUpForce,
                ForceMode.VelocityChange
            );

            target.AddTorque(
                Random.onUnitSphere *
                ThrowForce *
                0.35f,
                ForceMode.VelocityChange
            );

            ThrowableObject throwable =
                target.GetComponentInParent<ThrowableObject>();

            if (throwable == null)
            {
                throwable =
                    target.GetComponentInChildren<ThrowableObject>();
            }

            if (throwable != null)
                throwable.MarkThrown();

            MissleScript missile =
                target.GetComponentInParent<MissleScript>();

            if (missile == null)
            {
                missile =
                    target.GetComponentInChildren<MissleScript>();
            }

            if (missile != null)
            {
                missile.ReleaseFromGrab(
                    throwDirection
                );
            }

            hitTargets.Remove(target);
            collidersInside.Remove(target);
        }
    }

    void Fire()
    {
        bool hitGroundLayer = false;

        Camera cam =
            Camera.main;

        if (cam != null)
        {
            RaycastHit[] hits =
                Physics.RaycastAll(
                    cam.transform.position,
                    cam.transform.forward,
                    20f,
                    1 << 6,
                    QueryTriggerInteraction.Ignore
                );

            if (hits.Length > 0)
                hitGroundLayer = true;
        }

        if (hitGroundLayer)
            ApplySelfPush();

        for (
            int i = hitTargets.Count - 1;
            i >= 0;
            i--
        )
        {
            Rigidbody target =
                hitTargets[i];

            if (target == null)
            {
                hitTargets.RemoveAt(i);
                continue;
            }

            if (target == MoveTeck?.rb)
                continue;

            Vector3 shootOrigin =
                ShootPoint != null
                    ? ShootPoint.position
                    : transform.position;

            Vector3 direction =
                (
                    target.worldCenterOfMass -
                    shootOrigin
                ).normalized;

            SimpleEnemy enemy =
                target.GetComponentInParent<SimpleEnemy>();

            if (enemy != null)
                enemy.KnockDown();

            ThrowableObject throwable =
                target.GetComponentInParent<ThrowableObject>();

            if (throwable == null)
            {
                throwable =
                    target.GetComponentInChildren<ThrowableObject>();
            }

            if (throwable != null)
                throwable.MarkForcePushed();

            target.isKinematic = false;

            target.WakeUp();

            target.AddForce(
                direction * Force +
                Vector3.up * 5f,
                ForceMode.VelocityChange
            );

            target.AddTorque(
                Random.onUnitSphere * 5f,
                ForceMode.VelocityChange
            );

            MissleScript missile =
                target.GetComponentInParent<MissleScript>();

            if (missile == null)
            {
                missile =
                    target.GetComponentInChildren<MissleScript>();
            }

            if (missile != null)
                missile.spinOut = true;
        }
    }

    bool IsHeld(Rigidbody target)
    {
        for (
            int i = 0;
            i < heldTargets.Count;
            i++
        )
        {
            if (
                heldTargets[i] != null &&
                heldTargets[i].rb == target
            )
            {
                return true;
            }
        }

        return false;
    }

    void ApplySelfPush()
    {
        if (
            MoveTeck == null ||
            MoveTeck.rb == null
        )
        {
            return;
        }

        Camera cam =
            Camera.main;

        if (cam == null)
            return;

        MoveTeck.rb.AddForce(
            -cam.transform.forward *
            SelfForce,
            ForceMode.VelocityChange
        );
    }

    void SpawnShootEffect()
    {
        if (ShootEffectPrefab == null)
            return;

        Transform parent =
            EffectParent != null
                ? EffectParent
                : transform;

        GameObject effect =
            Instantiate(
                ShootEffectPrefab,
                parent
            );

        effect.transform.localPosition =
            Vector3.zero;

        effect.transform.localRotation =
            Quaternion.identity;

        if (ShootEffectLifetime > 0f)
        {
            Destroy(
                effect,
                ShootEffectLifetime
            );
        }
    }

    void SpawnHoldEffect()
    {
        DestroyHoldEffect();

        if (HoldEffectPrefab == null)
            return;

        Transform parent =
            EffectParent != null
                ? EffectParent
                : transform;

        currentHoldEffect =
            Instantiate(
                HoldEffectPrefab,
                parent
            );

        currentHoldEffect.transform.localPosition =
            Vector3.zero;

        currentHoldEffect.transform.localRotation =
            Quaternion.identity;
    }

    void DestroyHoldEffect()
    {
        if (currentHoldEffect == null)
            return;

        Destroy(currentHoldEffect);

        currentHoldEffect = null;
    }

    void CleanDestroyedTargets()
    {
        hitTargets.RemoveAll(
            target => target == null
        );

        heldTargets.RemoveAll(
            data =>
                data == null ||
                data.rb == null
        );

        List<Rigidbody> deadTargets =
            new List<Rigidbody>();

        foreach (
            KeyValuePair<Rigidbody, int> pair
            in collidersInside
        )
        {
            if (pair.Key == null)
                deadTargets.Add(pair.Key);
        }

        foreach (
            Rigidbody target
            in deadTargets
        )
        {
            collidersInside.Remove(target);
        }
    }

    void OnDestroy()
    {
        DestroyHoldEffect();
    }
}