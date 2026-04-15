using System.Collections;
using System.Reflection;
using Assert = NUnit.Framework.Assert;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

public class EnemyTest
{
    private Component runner;
    private Component enemy;
    private GameObject enemyObject;
    private GameObject playerObject;
    private Component playerHealth;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        yield return ShutdownAllNetworkRunners();

        yield return SceneManager.LoadSceneAsync("TestScene", LoadSceneMode.Single);

        DisableAllComponentsByName("AfterMatch");

        yield return WaitForCondition(
            () =>
            {
                runner = FindFirstComponentByName("NetworkRunner");
                // In PlayMode tests cloud connect can be delayed; runner existence is enough.
                return runner != null;
            },
            10f,
            "Không tìm thấy NetworkRunner trong TestScene"
        );

        yield return WaitForCondition(
            () =>
            {
                enemy = FindFirstComponentByName("EnemyShooting");
                enemyObject = enemy != null ? enemy.gameObject : null;
                return enemyObject != null;
            },
            8f,
            "Không tìm thấy EnemyShooting"
        );

        yield return WaitForCondition(
            () =>
            {
                playerObject = FindLocalPlayerTankObject();
                if (playerObject == null)
                {
                    playerObject = FindFirstGameObjectWithComponent("TankController");
                }
                return playerObject != null;
            },
            20f,
            "Không tìm thấy TankController (player)"
        );

        playerHealth = playerObject.GetComponent("Health");
        Assert.IsNotNull(playerHealth, "Player không có Health");

        SetFloatField(enemy, "attackRange", 60f);
        SetFloatField(enemy, "searchRange", 80f);
        SetFloatField(enemy, "fireRate", 1.0f);

        Vector3 enemyPos = enemyObject.transform.position;

        yield return new WaitForSeconds(0.75f);
    }

    [UnityTearDown]
    public IEnumerator Teardown()
    {
        yield return ShutdownAllNetworkRunners();
    }

    [UnityTest]
    public IEnumerator Enemy_Should_Shoot_Player()
    {
        int bulletsBefore = CountObjectsWithComponent("Bullet");

        bool shot = false;
        float timeout = Time.time + 5f;
        while (Time.time < timeout)
        {
            int bulletsNow = CountObjectsWithComponent("Bullet");
            if (bulletsNow > bulletsBefore)
            {
                shot = true;
                yield return new WaitForSeconds(1f);
                break;
            }

            yield return null;
        }

        Assert.IsTrue(shot, "Enemy không bắn khi player trong tầm");
    }

    [UnityTest]
    public IEnumerator Enemy_Should_Rotate_Toward_Player()
    {
        Vector3 toPlayerStart = playerObject.transform.position - enemyObject.transform.position;
        toPlayerStart.y = 0f;
        float startAngle = Vector3.Angle(enemyObject.transform.forward, toPlayerStart.normalized);

        yield return new WaitForSeconds(1.5f);

        Vector3 toPlayerNow = playerObject.transform.position - enemyObject.transform.position;
        toPlayerNow.y = 0f;
        float currentAngle = Vector3.Angle(enemyObject.transform.forward, toPlayerNow.normalized);

        Assert.Less(currentAngle, startAngle, "Enemy không xoay gần hơn về phía player");
        Assert.Less(currentAngle, 20f, "Enemy chưa xoay đủ sát hướng player");
    }

    [UnityTest]
    public IEnumerator Enemy_Should_Damage_Player()
    {
        int startHp = GetIntField(playerHealth, "HP");

        bool damaged = false;
        float timeout = Time.time + 7f;
        while (Time.time < timeout)
        {
            if (GetIntField(playerHealth, "HP") < startHp)
            {
                damaged = true;
                yield return new WaitForSeconds(1f);
                break;
            }

            yield return null;
        }

        Assert.IsTrue(damaged, $"Player không bị mất máu. StartHP={startHp}, CurrentHP={GetIntField(playerHealth, "HP")}");
    }

    private static IEnumerator WaitForCondition(System.Func<bool> condition, float timeoutSeconds, string failMessage)
    {
        float timeout = Time.time + timeoutSeconds;
        while (Time.time < timeout)
        {
            if (condition())
            {
                yield break;
            }

            yield return null;
        }

        Assert.Fail(failMessage);
    }

    private static Component FindFirstComponentByName(string componentName)
    {
        GameObject[] all = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            Component c = all[i].GetComponent(componentName);
            if (c != null)
            {
                return c;
            }
        }

        return null;
    }

    private static GameObject FindFirstGameObjectWithComponent(string componentName)
    {
        Component component = FindFirstComponentByName(componentName);
        return component != null ? component.gameObject : null;
    }

    private static int CountObjectsWithComponent(string componentName)
    {
        int count = 0;
        GameObject[] all = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].GetComponent(componentName) != null)
            {
                count++;
            }
        }

        return count;
    }

    private static bool TryGetBoolProperty(Component component, string propertyName, out bool value)
    {
        value = false;
        if (component == null)
        {
            return false;
        }

        PropertyInfo property = component.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        if (property == null || property.PropertyType != typeof(bool))
        {
            return false;
        }

        value = (bool)property.GetValue(component);
        return true;
    }

    private static GameObject FindLocalPlayerTankObject()
    {
        GameObject[] all = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            GameObject go = all[i];
            if (go.GetComponent("TankController") == null)
            {
                continue;
            }

            Component networkObject = go.GetComponent("NetworkObject");
            if (networkObject == null)
            {
                continue;
            }

            if (TryGetBoolProperty(networkObject, "HasInputAuthority", out bool hasInputAuthority) && hasInputAuthority)
            {
                return go;
            }
        }

        return null;
    }

    private static void SetFloatField(Component component, string fieldName, float value)
    {
        Assert.IsNotNull(component, $"Component null khi set field {fieldName}");
        FieldInfo field = component.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Không tìm thấy field {fieldName} trong {component.GetType().Name}");
        field.SetValue(component, value);
    }

    private static int GetIntField(Component component, string fieldName)
    {
        Assert.IsNotNull(component, $"Component null khi đọc field {fieldName}");
        FieldInfo field = component.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Không tìm thấy field {fieldName} trong {component.GetType().Name}");
        return (int)field.GetValue(component);
    }

    private static void DisableAllComponentsByName(string componentName)
    {
        GameObject[] all = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            Component c = all[i].GetComponent(componentName);
            if (c == null)
            {
                continue;
            }

            PropertyInfo enabledProperty = c.GetType().GetProperty("enabled", BindingFlags.Instance | BindingFlags.Public);
            if (enabledProperty != null && enabledProperty.PropertyType == typeof(bool) && enabledProperty.CanWrite)
            {
                enabledProperty.SetValue(c, false);
            }
        }
    }

    private static IEnumerator ShutdownAllNetworkRunners()
    {
        bool previousIgnoreState = LogAssert.ignoreFailingMessages;
        LogAssert.ignoreFailingMessages = true;

        GameObject[] all = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            Component runnerComp = all[i].GetComponent("NetworkRunner");
            if (runnerComp == null)
            {
                continue;
            }

            InvokeShutdown(runnerComp);
        }

        // Give Fusion time to emit disconnect logs caused by shutdown.
        yield return null;
        yield return null;
        yield return new WaitForSeconds(0.5f);

        LogAssert.ignoreFailingMessages = previousIgnoreState;
    }

    private static void InvokeShutdown(Component runnerComp)
    {
        if (runnerComp == null)
        {
            return;
        }

        MethodInfo[] methods = runnerComp.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public);
        MethodInfo best = null;

        // Prefer exact zero-parameter Shutdown()
        for (int i = 0; i < methods.Length; i++)
        {
            if (methods[i].Name != "Shutdown")
            {
                continue;
            }

            ParameterInfo[] parameters = methods[i].GetParameters();
            if (parameters.Length == 0)
            {
                best = methods[i];
                break;
            }

            bool allOptional = true;
            for (int p = 0; p < parameters.Length; p++)
            {
                if (!parameters[p].IsOptional)
                {
                    allOptional = false;
                    break;
                }
            }

            if (allOptional && best == null)
            {
                best = methods[i];
            }
        }

        if (best == null)
        {
            return;
        }

        ParameterInfo[] bestParams = best.GetParameters();
        object[] args = null;
        if (bestParams.Length > 0)
        {
            args = new object[bestParams.Length];
            for (int i = 0; i < bestParams.Length; i++)
            {
                if (bestParams[i].DefaultValue != System.DBNull.Value)
                {
                    args[i] = bestParams[i].DefaultValue;
                }
                else if (bestParams[i].ParameterType.IsValueType)
                {
                    args[i] = System.Activator.CreateInstance(bestParams[i].ParameterType);
                }
                else
                {
                    args[i] = null;
                }
            }
        }

        best.Invoke(runnerComp, args);
    }
}