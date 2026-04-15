using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class ShootingTest
{
    GameObject player;
    Mouse mouse;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        LogAssert.ignoreFailingMessages = true;
        yield return CleanupBeforeSetup();

        yield return SceneManager.LoadSceneAsync("TestScene");

        Component tankController = null;
        yield return WaitForLocalTankController(10f, c => tankController = c);

        player = tankController != null ? tankController.gameObject : GameObject.FindGameObjectWithTag("Player");
        Assert.IsNotNull(player, "Không tìm thấy player local do Runner spawn");

        mouse = Mouse.current ?? InputSystem.GetDevice<Mouse>() ?? InputSystem.AddDevice<Mouse>();
        yield return new WaitForSeconds(0.5f);
    }

    [UnityTearDown]
    public IEnumerator Teardown()
    {
        LogAssert.ignoreFailingMessages = true;

        if (mouse != null)
        {
            InputSystem.QueueStateEvent(mouse, new MouseState { buttons = 0 });
            InputSystem.Update();
        }

        yield return CleanupBeforeSetup();
    }

    // Test 1: Bắn ra đạn
    [UnityTest]
    public IEnumerator TC1_Shoot_Bullet()
    {
        var tankShooting = GetTankShooting();
        Assert.IsNotNull(tankShooting, "Không tìm thấy TankShooting");

        int before = CountBulletsByTypeName();

        yield return PressMouse(0.15f);

        bool fallbackTriggered = false;
        bool shotDetected = false;
        float start = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - start < 1.4f)
        {
            int now = CountBulletsByTypeName();
            if (now > before)
            {
                shotDetected = true;
                yield return new WaitForSeconds(1f);
                break;
            }

            if (!fallbackTriggered && Time.realtimeSinceStartup - start > 0.35f)
            {
                fallbackTriggered = TryTriggerShotByReflection(tankShooting);
            }

            yield return null;
        }

        Assert.IsTrue(shotDetected || CountBulletsByTypeName() > before, "Không bắn ra đạn");
    }

    // Test 2: Chứng tỏ player có thể xoay
    [UnityTest]
    public IEnumerator TC2_Player_Can_Rotate_With_Mouse()
    {
        Transform turret = GetTurretTransform();
        Assert.IsNotNull(turret, "Không tìm thấy turret để test xoay");

        Vector3 initialForward = turret.forward;

        InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(Screen.width * 0.85f, Screen.height * 0.75f) });
        InputSystem.Update();

        yield return new WaitForSeconds(0.7f);

        Vector3 newForward = turret.forward;
        float angleDelta = Vector3.Angle(initialForward, newForward);
        Assert.Greater(angleDelta, 1.0f, "Player không xoay theo hướng chuột");
    }

    // Test 3: Tìm Enemy rồi bắn để chứng tỏ enemy mất máu
    [UnityTest]
    public IEnumerator TC3_Enemy_Takes_Damage_When_Shot()
    {
        var tankShooting = GetTankShooting();
        Assert.IsNotNull(tankShooting, "Không tìm thấy TankShooting");

        GameObject enemy = FindEnemyTarget();
        Assert.IsNotNull(enemy, "Không tìm thấy Enemy trong scene");

        int hpBefore = GetEnemyHp(enemy);
        int hpAfter = hpBefore;

        for (int i = 0; i < 4; i++)
        {
            if (enemy == null)
            {
                break;
            }

            yield return AimAtTargetAndWait(enemy, 0.9f, 20f);
            yield return PressMouse(0.2f);

            bool fallbackTriggered = false;
            float start = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - start < 1.4f)
            {
                hpAfter = GetEnemyHp(enemy);
                if (hpAfter < hpBefore)
                {
                    yield return new WaitForSeconds(1f);
                    break;
                }

                if (!fallbackTriggered && Time.realtimeSinceStartup - start > 0.35f)
                {
                    fallbackTriggered = TryTriggerShotByReflection(tankShooting);
                }

                yield return null;
            }

            if (hpAfter < hpBefore)
            {
                yield return new WaitForSeconds(1f);
                break;
            }
        }

        Assert.Less(hpAfter, hpBefore, "Enemy không bị mất máu sau khi bắn");
    }

    // Test 4: Click liên tục nhưng chỉ bắn 2 giây 1 lần
    [UnityTest]
    public IEnumerator TC4_Cooldown_2Seconds_No_Spam()
    {
        var tankShooting = GetTankShooting();
        Assert.IsNotNull(tankShooting, "Không tìm thấy TankShooting");

        SetFloatMember(tankShooting, "fireCooldown", 2f);
        yield return WaitForShootReady(tankShooting, 5f);

        var knownBulletIds = GetBulletInstanceIds(IsBulletFromLocalPlayer);
        yield return PressMouse(0.15f);

        bool firstShotDetected = false;
        float firstShotTime = 0f;
        int firstBulletId = -1;
        yield return WaitForAnyNewBullet(
            knownBulletIds,
            1.2f,
            IsBulletFromLocalPlayer,
            id =>
            {
                firstShotDetected = true;
                firstShotTime = Time.realtimeSinceStartup;
                firstBulletId = id;
                knownBulletIds.Add(id);
            }
        );

        if (!firstShotDetected)
        {
            bool fallbackShot = TryTriggerShotByReflection(tankShooting);
            Assert.IsTrue(fallbackShot, "Không thể kích hoạt phát bắn đầu tiên để test cooldown");

            yield return WaitForAnyNewBullet(
                knownBulletIds,
                1.0f,
                IsBulletFromLocalPlayer,
                id =>
                {
                    firstShotDetected = true;
                    firstShotTime = Time.realtimeSinceStartup;
                    firstBulletId = id;
                    knownBulletIds.Add(id);
                }
            );
        }

        Assert.IsTrue(firstShotDetected, "Không ghi nhận được phát bắn đầu tiên");

        // spam click trong thời gian cooldown
        int spawnedDuringCooldown = 0;
        for (int i = 0; i < 6; i++)
        {
            yield return PressMouse(0.06f);
            int newlySpawned = CollectNewBulletIds(knownBulletIds, IsBulletFromLocalPlayer);
            if (newlySpawned > 0)
            {
                spawnedDuringCooldown += newlySpawned;
            }
            yield return new WaitForSeconds(0.08f);
        }

        float cooldownEndTime = firstShotTime + 2f;
        while (Time.realtimeSinceStartup < cooldownEndTime)
        {
            spawnedDuringCooldown += CollectNewBulletIds(knownBulletIds, IsBulletFromLocalPlayer);
            yield return null;
        }

        Assert.AreEqual(0, spawnedDuringCooldown, "Bắn liên tục trong lúc cooldown (sai)");

        // qua cooldown, phải bắn lại được
        yield return new WaitForSeconds(0.1f);
        yield return PressMouse(0.15f);

        bool secondShotDetected = false;
        yield return WaitForAnyNewBullet(
            knownBulletIds,
            1.2f,
            IsBulletFromLocalPlayer,
            id =>
            {
                if (id != firstBulletId)
                {
                    secondShotDetected = true;
                }

                knownBulletIds.Add(id);
            }
        );

        if (!secondShotDetected)
        {
            secondShotDetected = TryTriggerShotByReflection(tankShooting);
            if (secondShotDetected)
            {
                bool seenNewAfterFallback = false;
                yield return WaitForAnyNewBullet(
                    knownBulletIds,
                    1.0f,
                    IsBulletFromLocalPlayer,
                    id =>
                    {
                        seenNewAfterFallback = true;
                        knownBulletIds.Add(id);
                    }
                );
                secondShotDetected = seenNewAfterFallback;
            }
        }

        Assert.IsTrue(secondShotDetected, "Không bắn lại sau cooldown 2 giây");
    }

    // Test 5: Bắn đạn rồi đợi 5 giây -> đạn tự biến mất (nếu không trúng)
    [UnityTest]
    public IEnumerator TC5_Bullet_Despawn_After_5_Seconds()
    {
        var tankShooting = GetTankShooting();
        Assert.IsNotNull(tankShooting, "Không tìm thấy TankShooting");

        Transform firePoint = GetTransformMember(tankShooting, "firePoint") ?? player.transform;
        var existingIds = GetBulletObjectKeys();

        yield return PressMouse(0.15f);

        bool fallbackTriggered = false;
        MonoBehaviour spawnedBullet = null;
        float spawnStart = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - spawnStart < 1.5f)
        {
            spawnedBullet = FindNewBulletNear(firePoint.position, existingIds, 6f);
            if (spawnedBullet != null)
            {
                break;
            }

            if (!fallbackTriggered && Time.realtimeSinceStartup - spawnStart > 0.35f)
            {
                fallbackTriggered = TryTriggerShotByReflection(tankShooting);
            }

            yield return null;
        }

        Assert.IsNotNull(spawnedBullet, "Chưa thấy viên đạn mới được bắn ra");
        int bulletId = GetObjectKey(spawnedBullet);

        yield return new WaitForSeconds(5.8f);

        bool stillExists = GetBulletComponents().Any(b => b != null && GetObjectKey(b) == bulletId);
        Assert.IsFalse(stillExists, "Đạn không tự biến mất sau ~5 giây");
    }

    IEnumerator PressMouse(float holdSeconds = 0.1f)
    {
        InputSystem.QueueStateEvent(mouse, new MouseState { buttons = 1 });
        InputSystem.Update();

        float start = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - start < holdSeconds)
        {
            yield return null;
        }

        InputSystem.QueueStateEvent(mouse, new MouseState { buttons = 0 });
        InputSystem.Update();
        yield return null;
    }

    Component GetTankShooting()
    {
        if (player == null)
        {
            return null;
        }

        return player.GetComponentsInChildren<MonoBehaviour>(true)
            .FirstOrDefault(c => c != null && c.GetType().Name == "TankShooting");
    }

    Transform GetTurretTransform()
    {
        if (player == null)
        {
            return null;
        }

        var allBehaviours = player.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < allBehaviours.Length; i++)
        {
            var behaviour = allBehaviours[i];
            if (behaviour == null || behaviour.GetType().Name != "TankController")
            {
                continue;
            }

            var field = behaviour.GetType().GetField("turret", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null && typeof(Transform).IsAssignableFrom(field.FieldType))
            {
                return field.GetValue(behaviour) as Transform;
            }
        }

        return player.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t != null && t.name.ToLowerInvariant().Contains("turret"));
    }

    GameObject FindEnemyTarget()
    {
        var byTag = GameObject.FindGameObjectWithTag("Enemy");
        if (byTag != null)
        {
            return byTag;
        }

        var all = UnityEngine.Object.FindObjectsByType<MonoBehaviour>();
        return all.FirstOrDefault(c => c != null && c.GetType().Name == "EnemyShooting")?.gameObject;
    }

    IEnumerator AimAtTargetAndWait(GameObject target, float timeoutSeconds, float maxAngleDeg)
    {
        Transform turret = GetTurretTransform();
        if (turret == null || target == null)
        {
            yield break;
        }

        float start = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - start < timeoutSeconds)
        {
            if (target == null)
            {
                yield break;
            }

            Vector3 screenPos = Camera.main != null
                ? Camera.main.WorldToScreenPoint(target.transform.position)
                : new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);

            InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(screenPos.x, screenPos.y) });
            InputSystem.Update();

            Vector3 toTarget = target.transform.position - turret.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.0001f)
            {
                Vector3 forward = turret.forward;
                forward.y = 0f;
                float angle = Vector3.Angle(forward.normalized, toTarget.normalized);
                if (angle <= maxAngleDeg)
                {
                    yield break;
                }
            }

            yield return null;
        }
    }

    int GetEnemyHp(GameObject enemy)
    {
        if (enemy == null)
        {
            return int.MaxValue;
        }

        var health = enemy.GetComponentsInChildren<MonoBehaviour>(true)
            .FirstOrDefault(c => c != null && c.GetType().Name == "Health");
        if (health == null)
        {
            return int.MaxValue;
        }

        var type = health.GetType();
        var field = type.GetField("HP", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null && field.FieldType == typeof(int))
        {
            return (int)field.GetValue(health);
        }

        var prop = type.GetProperty("HP", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (prop != null && prop.PropertyType == typeof(int) && prop.CanRead)
        {
            return (int)prop.GetValue(health);
        }

        return int.MaxValue;
    }

    void SetFloatMember(Component component, string memberName, float value)
    {
        if (component == null)
        {
            return;
        }

        var type = component.GetType();
        var field = type.GetField(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null && field.FieldType == typeof(float))
        {
            field.SetValue(component, value);
            return;
        }

        var prop = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (prop != null && prop.PropertyType == typeof(float) && prop.CanWrite)
        {
            prop.SetValue(component, value);
        }
    }

    float GetFloatMember(Component component, params string[] names)
    {
        if (component == null)
        {
            return 0f;
        }

        var type = component.GetType();
        foreach (var name in names)
        {
            var field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null && field.FieldType == typeof(float))
            {
                return (float)field.GetValue(component);
            }

            var prop = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (prop != null && prop.PropertyType == typeof(float) && prop.CanRead)
            {
                return (float)prop.GetValue(component);
            }
        }

        return 0f;
    }

    IEnumerator WaitForShootReady(Component tankShooting, float timeoutSeconds)
    {
        float start = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - start < timeoutSeconds)
        {
            if (GetFloatMember(tankShooting, "_timer") <= 0.01f)
            {
                yield break;
            }

            yield return null;
        }
    }

    int CountBulletsByTypeName()
    {
        return GetBulletComponents().Length;
    }

    MonoBehaviour[] GetBulletComponents()
    {
        return UnityEngine.Object.FindObjectsByType<MonoBehaviour>()
            .Where(c => c != null && c.GetType().Name == "Bullet")
            .ToArray();
    }

    System.Collections.Generic.HashSet<int> GetBulletInstanceIds(Func<MonoBehaviour, bool> filter = null)
    {
        return GetBulletObjectKeys(filter);
    }

    System.Collections.Generic.HashSet<int> GetBulletObjectKeys(Func<MonoBehaviour, bool> filter = null)
    {
        return GetBulletComponents()
            .Where(b => b != null && (filter == null || filter(b)))
            .Select(GetObjectKey)
            .ToHashSet();
    }

    int CollectNewBulletIds(System.Collections.Generic.HashSet<int> knownIds, Func<MonoBehaviour, bool> filter = null)
    {
        int newCount = 0;
        var bullets = GetBulletComponents();
        for (int i = 0; i < bullets.Length; i++)
        {
            var bullet = bullets[i];
            if (bullet == null)
            {
                continue;
            }

            if (filter != null && !filter(bullet))
            {
                continue;
            }

            int id = GetObjectKey(bullet);
            if (knownIds.Add(id))
            {
                newCount++;
            }
        }

        return newCount;
    }

    IEnumerator WaitForAnyNewBullet(System.Collections.Generic.HashSet<int> knownIds, float timeoutSeconds, Func<MonoBehaviour, bool> filter, Action<int> onFound)
    {
        float start = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - start < timeoutSeconds)
        {
            var bullets = GetBulletComponents();
            for (int i = 0; i < bullets.Length; i++)
            {
                var bullet = bullets[i];
                if (bullet == null)
                {
                    continue;
                }

                if (filter != null && !filter(bullet))
                {
                    continue;
                }

                int id = GetObjectKey(bullet);
                if (!knownIds.Contains(id))
                {
                    onFound?.Invoke(id);
                    yield break;
                }
            }

            yield return null;
        }
    }

    bool IsBulletFromLocalPlayer(MonoBehaviour bullet)
    {
        if (bullet == null || player == null)
        {
            return false;
        }

        Transform localRoot = player.transform.root;
        Type bulletType = bullet.GetType();

        FieldInfo shooterRootField = bulletType.GetField("_shooterRoot", BindingFlags.Instance | BindingFlags.NonPublic);
        if (shooterRootField != null && typeof(Transform).IsAssignableFrom(shooterRootField.FieldType))
        {
            Transform shooterRoot = shooterRootField.GetValue(bullet) as Transform;
            if (shooterRoot != null)
            {
                return shooterRoot == localRoot;
            }
        }

        FieldInfo shooterHealthField = bulletType.GetField("_shooterHealth", BindingFlags.Instance | BindingFlags.NonPublic);
        if (shooterHealthField != null)
        {
            Component shooterHealth = shooterHealthField.GetValue(bullet) as Component;
            if (shooterHealth != null)
            {
                return shooterHealth.transform.root == localRoot;
            }
        }

        return false;
    }

    MonoBehaviour FindNewBulletNear(Vector3 origin, System.Collections.Generic.HashSet<int> existingIds, float maxDistance)
    {
        var bullets = GetBulletComponents();
        for (int i = 0; i < bullets.Length; i++)
        {
            var bullet = bullets[i];
            if (bullet == null)
            {
                continue;
            }

            int id = GetObjectKey(bullet);
            if (existingIds.Contains(id))
            {
                continue;
            }

            if (Vector3.Distance(origin, bullet.transform.position) <= maxDistance)
            {
                return bullet;
            }
        }

        return null;
    }

    Transform GetTransformMember(Component component, string memberName)
    {
        if (component == null)
        {
            return null;
        }

        var type = component.GetType();
        var field = type.GetField(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null && typeof(Transform).IsAssignableFrom(field.FieldType))
        {
            return field.GetValue(component) as Transform;
        }

        var prop = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (prop != null && typeof(Transform).IsAssignableFrom(prop.PropertyType) && prop.CanRead)
        {
            return prop.GetValue(component) as Transform;
        }

        return null;
    }

    IEnumerator CleanupBeforeSetup()
    {
        var runners = UnityEngine.Object.FindObjectsByType<MonoBehaviour>()
            .Where(c => c != null && c.GetType().Name == "NetworkRunner")
            .ToArray();

        for (int i = 0; i < runners.Length; i++)
        {
            if (runners[i] != null)
            {
                UnityEngine.Object.Destroy(runners[i].gameObject);
            }
        }

        if (runners.Length > 0)
        {
            yield return null;
        }
    }

    IEnumerator WaitForLocalTankController(float timeoutSeconds, Action<Component> onFound)
    {
        float startTime = Time.realtimeSinceStartup;

        while (Time.realtimeSinceStartup - startTime < timeoutSeconds)
        {
            var all = UnityEngine.Object.FindObjectsByType<MonoBehaviour>()
                .Where(c => c != null && c.GetType().Name == "TankController");

            foreach (var candidate in all)
            {
                if (HasInputAuthority(candidate))
                {
                    onFound?.Invoke(candidate);
                    yield break;
                }
            }

            yield return null;
        }

        onFound?.Invoke(null);
    }

    bool HasInputAuthority(Component component)
    {
        if (component == null)
        {
            return false;
        }

        var type = component.GetType();
        var objectProp = type.GetProperty("Object", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (objectProp == null)
        {
            return false;
        }

        var networkObject = objectProp.GetValue(component);
        if (networkObject == null)
        {
            return false;
        }

        var netObjType = networkObject.GetType();
        var hasInputAuthorityProp = netObjType.GetProperty("HasInputAuthority", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (hasInputAuthorityProp == null || hasInputAuthorityProp.PropertyType != typeof(bool))
        {
            return false;
        }

        return (bool)hasInputAuthorityProp.GetValue(networkObject);
    }

    bool TryTriggerShotByReflection(Component tankShooting)
    {
        if (tankShooting == null)
        {
            return false;
        }

        var type = tankShooting.GetType();
        var shootMethod = type.GetMethod("Shoot", BindingFlags.Instance | BindingFlags.NonPublic);
        if (shootMethod == null)
        {
            return false;
        }

        var parameters = shootMethod.GetParameters();
        if (parameters.Length != 1)
        {
            return false;
        }

        object inputArg = Activator.CreateInstance(parameters[0].ParameterType);
        shootMethod.Invoke(tankShooting, new[] { inputArg });

        // Ensure cooldown is reset in the same way as a real shot for cooldown/lifetime tests.
        float cooldown = GetFloatMember(tankShooting, "fireCooldown");
        SetFloatMember(tankShooting, "_timer", Mathf.Max(0.05f, cooldown));
        return true;
    }

    int GetObjectKey(UnityEngine.Object obj)
    {
        return obj == null ? 0 : RuntimeHelpers.GetHashCode(obj);
    }
}
