using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class TrapTest
{
    GameObject player;
    Component playerHealth;
    Component tankShooting;
    Mouse mouse;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        LogAssert.ignoreFailingMessages = true;
        yield return CleanupBeforeSetup();

        yield return SceneManager.LoadSceneAsync("TestScene", LoadSceneMode.Single);

        Component tankController = null;
        yield return WaitForLocalTankController(20f, c => tankController = c);

        player = tankController != null ? tankController.gameObject : null;
        Assert.IsNotNull(player, "Khong tim thay local player trong TestScene");

        playerHealth = player.GetComponentsInChildren<MonoBehaviour>(true)
            .FirstOrDefault(c => c != null && c.GetType().Name == "Health");
        Assert.IsNotNull(playerHealth, "Player khong co Health");

        tankShooting = player.GetComponentsInChildren<MonoBehaviour>(true)
            .FirstOrDefault(c => c != null && c.GetType().Name == "TankShooting");
        Assert.IsNotNull(tankShooting, "Player khong co TankShooting");

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

    // Test 1: Spawn bay dam, player di chuyen vao -> bay no gay damage.
    [UnityTest]
    public IEnumerator TC1_Player_Move_Into_Mine_Explosion_Damages_Player()
    {
        Component mine = null;
        yield return WaitForTrapByType(1, 10f, t => mine = t);
        Assert.IsNotNull(mine, "Khong tim thay bay dam (TrapType=TouchOrShootMine)");

        Vector3 minePos = player.transform.position + player.transform.forward * 3f;
        minePos.y = player.transform.position.y;
        mine.transform.position = minePos;

        int hpBefore = GetIntMember(playerHealth, "HP");
        Assert.Greater(hpBefore, 0, "HP ban dau khong hop le");

        float start = Time.realtimeSinceStartup;
        bool damaged = false;
        bool exploded = false;

        while (Time.realtimeSinceStartup - start < 3.5f)
        {
            if (mine == null || mine.gameObject == null)
            {
                exploded = true;
            }
            else
            {
                // Move player step-by-step to ensure trigger enter is fired naturally.
                player.transform.position = Vector3.MoveTowards(
                    player.transform.position,
                    mine.transform.position,
                    5f * Time.deltaTime
                );
            }

            int hpNow = GetIntMember(playerHealth, "HP");
            if (hpNow < hpBefore)
            {
                damaged = true;
            }

            if (damaged && exploded)
            {
                yield return new WaitForSeconds(1f);
                break;
            }

            yield return null;
        }

        Assert.IsTrue(exploded, "Bay dam khong no khi player di vao");
        Assert.IsTrue(damaged, "Player khong bi tru mau khi bay no");
    }

    // Test 2: Spawn bay no, player ban vao bay -> bay no.
    [UnityTest]
    public IEnumerator TC2_Shoot_Mine_To_Explode()
    {
        Component mine = null;
        yield return WaitForTrapByType(1, 10f, t => mine = t);
        Assert.IsNotNull(mine, "Khong tim thay bay no (TouchOrShootMine)");

        Transform firePoint = GetTransformMember(tankShooting, "firePoint") ?? player.transform;
        Vector3 shootDir = firePoint.forward.sqrMagnitude > 0.0001f ? firePoint.forward.normalized : player.transform.forward;
        mine.transform.position = firePoint.position + shootDir * 6f;
        mine.transform.rotation = Quaternion.identity;
        yield return null;

        bool exploded = false;
        float start = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - start < 4f)
        {
            if (mine == null || mine.gameObject == null)
            {
                exploded = true;
                break;
            }

            bool shot = TryTriggerShotByReflection(tankShooting);
            Assert.IsTrue(shot, "Khong goi duoc Shoot() trong TC2");
            yield return new WaitForSeconds(0.15f);

            if (mine == null || mine.gameObject == null)
            {
                exploded = true;
                break;
            }

            yield return new WaitForSeconds(0.35f);
        }

        Assert.IsTrue(exploded, "Ban vao bay nhung bay khong no");
    }

    // Test 3: Spawn thung no, player ban vao thung -> thung no.
    [UnityTest]
    public IEnumerator TC3_Shoot_Barrel_To_Explode()
    {
        Component barrelTrap = null;
        yield return WaitForTrapByType(0, 10f, t => barrelTrap = t);
        Assert.IsNotNull(barrelTrap, "Khong tim thay thung no (TrapType=ShootOnlyBarrel)");

        Transform firePoint = GetTransformMember(tankShooting, "firePoint") ?? player.transform;
        Vector3 shootDir = firePoint.forward.sqrMagnitude > 0.0001f ? firePoint.forward.normalized : player.transform.forward;
        barrelTrap.transform.position = firePoint.position + shootDir * 4.5f;
        barrelTrap.transform.rotation = Quaternion.identity;
        yield return null;

        bool exploded = false;
        float start = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - start < 4f)
        {
            if (barrelTrap == null || barrelTrap.gameObject == null)
            {
                exploded = true;
                break;
            }

            bool shot = TryTriggerShotByReflection(tankShooting);
            Assert.IsTrue(shot, "Khong goi duoc Shoot() trong TC3");
            yield return new WaitForSeconds(0.15f);

            if (barrelTrap == null || barrelTrap.gameObject == null)
            {
                exploded = true;
                break;
            }

            yield return new WaitForSeconds(0.35f);
        }

        Assert.IsTrue(exploded, "Ban vao thung no nhung thung khong no");
    }

    void PlaceTargetInFrontOfPlayer(GameObject target, float distance)
    {
        Vector3 pos = player.transform.position + player.transform.forward * distance;
        pos.y = Mathf.Max(0.2f, player.transform.position.y + 0.1f);
        target.transform.position = pos;
    }

    IEnumerator AimAtWorldPosition(Vector3 worldPos, float holdSeconds)
    {
        Vector3 screenPos = Camera.main != null
            ? Camera.main.WorldToScreenPoint(worldPos)
            : new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);

        InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(screenPos.x, screenPos.y) });
        InputSystem.Update();

        float start = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - start < holdSeconds)
        {
            yield return null;
        }
    }

    IEnumerator PressMouse(float holdSeconds)
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

    IEnumerator WaitForTrapByType(int trapTypeInt, float timeoutSeconds, Action<Component> onFound)
    {
        float start = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - start < timeoutSeconds)
        {
            var traps = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Where(c => c != null && c.GetType().Name == "Trap");

            foreach (var trap in traps)
            {
                int currentType = GetEnumAsInt(trap, "trapType");
                if (currentType == trapTypeInt)
                {
                    onFound?.Invoke(trap);
                    yield break;
                }
            }

            yield return null;
        }

        onFound?.Invoke(null);
    }

    int GetEnumAsInt(Component component, string fieldName)
    {
        if (component == null)
        {
            return int.MinValue;
        }

        FieldInfo field = component.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field == null)
        {
            return int.MinValue;
        }

        object value = field.GetValue(component);
        if (value == null)
        {
            return int.MinValue;
        }

        return Convert.ToInt32(value);
    }

    Transform GetTransformMember(Component component, string memberName)
    {
        if (component == null)
        {
            return null;
        }

        FieldInfo field = component.GetType().GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null && typeof(Transform).IsAssignableFrom(field.FieldType))
        {
            return field.GetValue(component) as Transform;
        }

        PropertyInfo prop = component.GetType().GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (prop != null && prop.CanRead && typeof(Transform).IsAssignableFrom(prop.PropertyType))
        {
            return prop.GetValue(component) as Transform;
        }

        return null;
    }

    int GetIntMember(Component component, string memberName)
    {
        if (component == null)
        {
            return int.MinValue;
        }

        FieldInfo field = component.GetType().GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null && field.FieldType == typeof(int))
        {
            return (int)field.GetValue(component);
        }

        PropertyInfo prop = component.GetType().GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (prop != null && prop.CanRead && prop.PropertyType == typeof(int))
        {
            return (int)prop.GetValue(component);
        }

        return int.MinValue;
    }

    IEnumerator CleanupBeforeSetup()
    {
        var runners = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
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
            var all = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
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

        PropertyInfo objectProp = component.GetType().GetProperty("Object", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (objectProp == null)
        {
            return false;
        }

        object networkObject = objectProp.GetValue(component);
        if (networkObject == null)
        {
            return false;
        }

        PropertyInfo hasInputAuthorityProp = networkObject.GetType().GetProperty("HasInputAuthority", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (hasInputAuthorityProp == null || hasInputAuthorityProp.PropertyType != typeof(bool))
        {
            return false;
        }

        return (bool)hasInputAuthorityProp.GetValue(networkObject);
    }

    bool TryTriggerShotByReflection(Component shootingComponent)
    {
        if (shootingComponent == null)
        {
            return false;
        }

        MethodInfo shootMethod = shootingComponent.GetType().GetMethod("Shoot", BindingFlags.Instance | BindingFlags.NonPublic);
        if (shootMethod == null)
        {
            return false;
        }

        ParameterInfo[] parameters = shootMethod.GetParameters();
        if (parameters.Length != 1)
        {
            return false;
        }

        object inputArg = Activator.CreateInstance(parameters[0].ParameterType);
        shootMethod.Invoke(shootingComponent, new[] { inputArg });

        return true;
    }
}
