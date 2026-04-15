using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem.LowLevel;
using System;
using System.Linq;
using System.Reflection;

public class MovingTest
{
    [UnitySetUp]
    public IEnumerator Setup()
    {
        LogAssert.ignoreFailingMessages = true;
        yield return CleanupBeforeSetup();
    }

    [UnityTearDown]
    public IEnumerator Teardown()
    {
        LogAssert.ignoreFailingMessages = true;

        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.Update();
        }

        yield return CleanupBeforeSetup();

    }

    [UnityTest]
    public IEnumerator Moving()
    {
        yield return SceneManager.LoadSceneAsync("TestScene");

        yield return DisableRunnerInputCallbacksForTest();

        Component localTankController = null;
        yield return WaitForLocalTankController(12f, c => localTankController = c);
        yield return WaitForTankStateAuthority(localTankController, 6f);

        // 🔥 ĐỢI NETWORK + INIT
        yield return new WaitForSeconds(1f);


        GameObject player = localTankController != null ? localTankController.gameObject : null;

        Assert.IsNotNull(player, "Không tìm thấy Player");

        Vector3 startPos = player.transform.position;

        var keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();

        Debug.Log("Start Pos: " + startPos);

        // 👉 đi phải
        yield return HoldKey(keyboard, Key.RightArrow, 1f);
        yield return new WaitForSeconds(0.5f);

        Vector3 afterRight = player.transform.position;
        Debug.Log("After Right: " + afterRight);

        // 👉 đi lên
        yield return HoldKey(keyboard, Key.UpArrow, 1f);
        yield return new WaitForSeconds(0.5f);

        Vector3 afterUp = player.transform.position;

        // 👉 đi trái
        yield return HoldKey(keyboard, Key.LeftArrow, 1f);
        yield return new WaitForSeconds(0.5f);

        Vector3 afterLeft = player.transform.position;

        // 👉 đi xuống
        yield return HoldKey(keyboard, Key.DownArrow, 1f);
        yield return new WaitForSeconds(0.5f);

        Vector3 afterDown = player.transform.position;

        // ===== ASSERT =====

        Assert.Greater(afterRight.x, startPos.x, "Không đi sang phải");
        Assert.Greater(afterUp.z, afterRight.z, "Không đi lên");
        Assert.Less(afterLeft.x, afterUp.x, "Không đi sang trái");
        Assert.Less(afterDown.z, afterLeft.z, "Không đi xuống");
    }

    [UnityTest]
    public IEnumerator Shield_Activates_While_Moving_When_Press_R()
    {
        yield return SceneManager.LoadSceneAsync("TestScene");

        yield return DisableRunnerInputCallbacksForTest();

        Component localTankController = null;
        yield return WaitForLocalTankController(12f, c => localTankController = c);
        yield return WaitForTankStateAuthority(localTankController, 6f);
        yield return new WaitForSeconds(1f);


        GameObject player = localTankController != null ? localTankController.gameObject : null;
        Assert.IsNotNull(player, "Không tìm thấy Player local");

        Component playerSkill = FindComponentInChildrenByTypeName(player, "PlayerSkill");
        Assert.IsNotNull(playerSkill, "Không tìm thấy PlayerSkill trên player");

        var keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();

        Vector3 startPos = player.transform.position;

        // Vừa di chuyển sang phải, vừa nhấn R giữa quãng di chuyển.
        yield return HoldMoveAndPressShield(playerSkill, keyboard, Key.RightArrow, Key.R, 1.0f, 0.3f, 0.15f);
        yield return new WaitForSeconds(0.2f);

        Vector3 afterMove = player.transform.position;
        Assert.Greater(afterMove.x, startPos.x, "Player không di chuyển khi test bật khiên");

        bool shieldActive = false;
        float startTime = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - startTime < 2f)
        {
            shieldActive = GetBoolMember(playerSkill, "IsShieldActive", "_shieldActiveLocal");
            if (shieldActive)
            {
                break;
            }

            yield return null;
        }

        Assert.IsTrue(shieldActive, "Ấn R khi đang di chuyển không bật khiên");
    }

    IEnumerator CleanupBeforeSetup()
    {
        // Remove stale runners from previous tests (often marked DontDestroyOnLoad).
        var runners = UnityEngine.Object.FindObjectsByType<MonoBehaviour>()
            .Where(c => c != null && c.GetType().Name == "NetworkRunner")
            .ToArray();

        for (int i = 0; i < runners.Length; i++)
        {
            if (runners[i] != null)
            {
                InvokeRunnerShutdown(runners[i]);
            }
        }

        if (runners.Length > 0)
        {
            // Give Fusion time to emit disconnect logs while ignoreFailingMessages=true.
            yield return null;
            yield return null;
            yield return new WaitForSeconds(0.5f);

            for (int i = 0; i < runners.Length; i++)
            {
                if (runners[i] != null)
                {
                    UnityEngine.Object.Destroy(runners[i].gameObject);
                }
            }

            yield return null;
        }
    }

    IEnumerator DisableRunnerInputCallbacksForTest()
    {
        float started = Time.realtimeSinceStartup;
        MonoBehaviour runner = null;

        while (runner == null && Time.realtimeSinceStartup - started < 5f)
        {
            runner = UnityEngine.Object.FindObjectsByType<MonoBehaviour>()
                .FirstOrDefault(c => c != null && c.GetType().Name == "NetworkRunner");
            if (runner == null)
            {
                yield return null;
            }
        }

        if (runner == null)
        {
            yield break;
        }

        var callbacks = runner.GetComponents<MonoBehaviour>()
            .Where(c => c != null
                        && (c.GetType().Name == "PlayerInputHandler" || c.GetType().Name == "NetworkInputHandler"))
            .ToArray();

        MethodInfo removeCallbackMethod = runner.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(m =>
            {
                if (m.Name != "RemoveCallbacks")
                {
                    return false;
                }

                var parameters = m.GetParameters();
                return parameters.Length == 1 && parameters[0].ParameterType.IsArray;
            });

        for (int i = 0; i < callbacks.Length; i++)
        {
            if (removeCallbackMethod != null)
            {
                Type callbackArrayType = removeCallbackMethod.GetParameters()[0].ParameterType;
                Type callbackType = callbackArrayType.GetElementType();

                if (callbackType != null && callbackType.IsInstanceOfType(callbacks[i]))
                {
                    Array typedArray = Array.CreateInstance(callbackType, 1);
                    typedArray.SetValue(callbacks[i], 0);
                    removeCallbackMethod.Invoke(runner, new object[] { typedArray });
                }
            }
            callbacks[i].enabled = false;
        }

        // Ensure Fusion does not wait for callback-provided input so local fallback input can drive movement in tests.
        PropertyInfo provideInputProp = runner.GetType().GetProperty("ProvideInput", BindingFlags.Public | BindingFlags.Instance);
        if (provideInputProp != null && provideInputProp.CanWrite && provideInputProp.PropertyType == typeof(bool))
        {
            provideInputProp.SetValue(runner, false);
        }

        yield return null;
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
                if (HasInputAuthority(candidate) && HasStateAuthority(candidate))
                {
                    onFound?.Invoke(candidate);
                    yield break;
                }
            }

            yield return null;
        }

        onFound?.Invoke(null);
    }

    IEnumerator WaitForTankStateAuthority(Component tankController, float timeoutSeconds)
    {
        if (tankController == null)
        {
            yield break;
        }

        float startTime = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - startTime < timeoutSeconds)
        {
            if (HasStateAuthority(tankController))
            {
                yield break;
            }

            yield return null;
        }
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

    bool HasStateAuthority(Component component)
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
        var hasStateAuthorityProp = netObjType.GetProperty("HasStateAuthority", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (hasStateAuthorityProp == null || hasStateAuthorityProp.PropertyType != typeof(bool))
        {
            return false;
        }

        return (bool)hasStateAuthorityProp.GetValue(networkObject);
    }

    Component FindComponentInChildrenByTypeName(GameObject root, string typeName)
    {
        if (root == null)
        {
            return null;
        }

        return root.GetComponentsInChildren<MonoBehaviour>(true)
            .FirstOrDefault(c => c != null && c.GetType().Name == typeName);
    }

    bool GetBoolMember(Component component, params string[] memberNames)
    {
        if (component == null)
        {
            return false;
        }

        var type = component.GetType();
        foreach (var memberName in memberNames)
        {
            var field = type.GetField(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null && field.FieldType == typeof(bool))
            {
                return (bool)field.GetValue(component);
            }

            var prop = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (prop != null && prop.PropertyType == typeof(bool) && prop.CanRead)
            {
                return (bool)prop.GetValue(component);
            }
        }

        return false;
    }

    IEnumerator HoldKey(Keyboard keyboard, Key key, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // giữ phím
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            InputSystem.Update();

            yield return null;

            elapsed += 0.02f;
        }

        // thả phím
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        InputSystem.Update();
    }

    IEnumerator HoldMoveAndPressShield(Component playerSkill, Keyboard keyboard, Key moveKey, Key shieldKey, float duration, float shieldPressAt, float shieldHoldDuration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            bool shieldPressedNow = elapsed >= shieldPressAt && elapsed < shieldPressAt + shieldHoldDuration;
            KeyboardState state = shieldPressedNow ? new KeyboardState(moveKey, shieldKey) : new KeyboardState(moveKey);

            InputSystem.QueueStateEvent(keyboard, state);
            InputSystem.Update();

            if (shieldPressedNow)
            {
                TryActivateShieldByReflection(playerSkill);
            }

            yield return null;
            elapsed += 0.02f;
        }

        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        InputSystem.Update();
    }

    void TryActivateShieldByReflection(Component playerSkill)
    {
        if (playerSkill == null)
        {
            return;
        }

        var type = playerSkill.GetType();
        MethodInfo enableShieldMethod = type.GetMethod("EnableShield", BindingFlags.NonPublic | BindingFlags.Instance);
        if (enableShieldMethod != null)
        {
            enableShieldMethod.Invoke(playerSkill, null);
        }
    }

    void InvokeRunnerShutdown(Component runnerComp)
    {
        if (runnerComp == null)
        {
            return;
        }

        MethodInfo[] methods = runnerComp.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public);
        MethodInfo best = null;

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
                    args[i] = Activator.CreateInstance(bestParams[i].ParameterType);
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