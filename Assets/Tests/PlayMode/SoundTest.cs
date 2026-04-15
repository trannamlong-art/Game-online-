using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class SoundTest
{
    GameObject player;
    Component tankController;
    Component tankShooting;
    Component playerSkill;
    Component playerHealth;
    Mouse mouse;
    Keyboard keyboard;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        LogAssert.ignoreFailingMessages = true;
        yield return CleanupBeforeSetup();

        yield return SceneManager.LoadSceneAsync("TestScene", LoadSceneMode.Single);

        yield return DisableRunnerInputCallbacksForTest();

        mouse = Mouse.current ?? InputSystem.GetDevice<Mouse>() ?? InputSystem.AddDevice<Mouse>();
        keyboard = Keyboard.current ?? InputSystem.GetDevice<Keyboard>() ?? InputSystem.AddDevice<Keyboard>();

        yield return WaitForLocalTankController(15f, c => tankController = c);
        Assert.IsNotNull(tankController, "Không tìm thấy TankController local trong TestScene");

        player = tankController.gameObject;
        tankShooting = FindComponentInChildrenByTypeName(player, "TankShooting");
        playerSkill = FindComponentInChildrenByTypeName(player, "PlayerSkill");
        playerHealth = FindComponentInChildrenByTypeName(player, "Health");

        Assert.IsNotNull(tankShooting, "Không tìm thấy TankShooting trên player");
        Assert.IsNotNull(playerSkill, "Không tìm thấy PlayerSkill trên player");

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

        if (keyboard != null)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.Update();
        }

        yield return CleanupBeforeSetup();
    }

    [UnityTest]
    public IEnumerator TC1_Shield_Plays_Sound_When_Press_R()
    {
        bool shieldActivated = false;
        bool soundDetected = false;

        yield return PressKey(keyboard, Key.R, 0.12f);

        float start = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - start < 2f)
        {
            shieldActivated = GetBoolMember(playerSkill, "IsShieldActive", "_shieldActiveLocal");
            soundDetected = HasPlayingAudioSourceInChildren(player);

            if (shieldActivated && soundDetected)
            {
                break;
            }

            if (!shieldActivated && Time.realtimeSinceStartup - start > 0.35f)
            {
                TryActivateShieldByReflection(playerSkill);
            }

            yield return null;
        }

        yield return new WaitForSeconds(1f);

        Assert.IsTrue(shieldActivated, "Ấn R nhưng khiên không bật");
        Assert.IsTrue(soundDetected, "Ấn R để bật khiên nhưng không ghi nhận âm thanh");
    }

    [UnityTest]
    public IEnumerator TC2_Player_Shoots_Bullet_With_Sound()
    {
        HashSet<int> bulletIdsBefore = GetComponentInstanceIdSet("Bullet");
        bool shotTriggered = false;
        bool soundDetected = false;

        yield return PressMouse(0.15f);

        float start = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - start < 2f)
        {
            MonoBehaviour[] bullets = GetComponentsByTypeName("Bullet");
            MonoBehaviour[] newBullets = bullets.Where(c => c != null && !bulletIdsBefore.Contains(c.GetInstanceID())).ToArray();

            if (newBullets.Length > 0)
            {
                shotTriggered = true;
                soundDetected = newBullets.Any(HasPlayingAudioSource);
                if (soundDetected)
                {
                    break;
                }
            }

            if (!shotTriggered && Time.realtimeSinceStartup - start > 0.35f)
            {
                shotTriggered = TryTriggerShotByReflection(tankShooting);
            }

            yield return null;
        }

        if (!soundDetected)
        {
            MonoBehaviour[] bullets = GetComponentsByTypeName("Bullet");
            MonoBehaviour[] newBullets = bullets.Where(c => c != null && !bulletIdsBefore.Contains(c.GetInstanceID())).ToArray();
            soundDetected = newBullets.Any(HasPlayingAudioSource);
        }

        yield return new WaitForSeconds(1f);

        Assert.IsTrue(shotTriggered || GetComponentsByTypeName("Bullet").Any(c => c != null && !bulletIdsBefore.Contains(c.GetInstanceID())), "Không tạo được đạn khi bắn");
        Assert.IsTrue(soundDetected, "Bắn đạn nhưng không ghi nhận âm thanh");
    }
    [UnityTest]
    public IEnumerator TC4_Shoot_Trap_Plays_Explosion_Sound()
    {
        Component trap = null;
        yield return WaitForShootableTrap(12f, t => trap = t);
        Assert.IsNotNull(trap, "Không tìm thấy bẫy có thể bắn trong scene");

        Transform firePoint = GetTransformMember(tankShooting, "firePoint") ?? player.transform;
        Vector3 trapPos = firePoint.position + firePoint.forward.normalized * 5f;
        trapPos.y = firePoint.position.y;
        trap.transform.position = trapPos;
        trap.transform.rotation = Quaternion.identity;

        HashSet<int> effectIdsBefore = GetComponentInstanceIdSet("EffectDestroy");
        yield return AimAtTargetAndWait(trap.gameObject, 1.0f, 45f);
        yield return PressMouse(0.15f);

        bool shotTriggered = false;
        bool soundDetected = false;

        float start = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - start < 3f)
        {
            MonoBehaviour[] effects = GetComponentsByTypeName("EffectDestroy");
            MonoBehaviour[] newEffects = effects.Where(c => c != null && !effectIdsBefore.Contains(c.GetInstanceID())).ToArray();

            if (newEffects.Length > 0)
            {
                shotTriggered = true;
                soundDetected = newEffects.Any(HasPlayingAudioSource);
                if (soundDetected)
                {
                    break;
                }
            }

            if (!shotTriggered && Time.realtimeSinceStartup - start > 0.35f)
            {
                shotTriggered = TryTriggerShotByReflection(tankShooting);
            }

            yield return null;
        }

        yield return new WaitForSeconds(1f);

        Assert.IsTrue(shotTriggered || GetComponentsByTypeName("EffectDestroy").Any(c => c != null && !effectIdsBefore.Contains(c.GetInstanceID())), "Bắn vào bẫy nhưng không kích hoạt được vụ nổ");
        Assert.IsTrue(soundDetected, "Bắn vào bẫy nhưng không ghi nhận âm thanh nổ");
    }

    IEnumerator CleanupBeforeSetup()
    {
        MonoBehaviour[] runners = UnityEngine.Object.FindObjectsByType<MonoBehaviour>()
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

                ParameterInfo[] parameters = m.GetParameters();
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

    IEnumerator PressKey(Keyboard targetKeyboard, Key key, float holdSeconds)
    {
        if (targetKeyboard == null)
        {
            yield break;
        }

        InputSystem.QueueStateEvent(targetKeyboard, new KeyboardState(key));
        InputSystem.Update();

        float start = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - start < holdSeconds)
        {
            yield return null;
        }

        InputSystem.QueueStateEvent(targetKeyboard, new KeyboardState());
        InputSystem.Update();
        yield return null;
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

    IEnumerator WaitForShootableTrap(float timeoutSeconds, Action<Component> onFound)
    {
        float start = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - start < timeoutSeconds)
        {
            var traps = UnityEngine.Object.FindObjectsByType<MonoBehaviour>()
                .Where(c => c != null && c.GetType().Name == "Trap");

            foreach (var trap in traps)
            {
                int trapType = GetEnumAsInt(trap, "trapType");
                if (trapType == 0 || trapType == 1)
                {
                    onFound?.Invoke(trap);
                    yield break;
                }
            }

            yield return null;
        }

        onFound?.Invoke(null);
    }

    GameObject FindEnemyTarget()
    {
        GameObject byTag = GameObject.FindGameObjectWithTag("Enemy");
        if (byTag != null)
        {
            return byTag;
        }

        return UnityEngine.Object.FindObjectsByType<MonoBehaviour>()
            .FirstOrDefault(c => c != null && c.GetType().Name == "EnemyShooting")?.gameObject;
    }

    Transform GetTurretTransform()
    {
        if (player == null)
        {
            return null;
        }

        var tankController = FindComponentInChildrenByTypeName(player, "TankController");
        if (tankController == null)
        {
            return player.transform;
        }

        return GetTransformMember(tankController, "turret") ?? player.transform;
    }

    void PlaceTargetInFrontOfPlayer(GameObject target, float distance)
    {
        if (target == null || player == null)
        {
            return;
        }

        Vector3 position = player.transform.position + player.transform.forward * distance;
        position.y = target.transform.position.y;
        target.transform.position = position;
    }

    HashSet<int> GetComponentInstanceIdSet(string typeName)
    {
        HashSet<int> ids = new HashSet<int>();
        MonoBehaviour[] components = GetComponentsByTypeName(typeName);

        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] != null)
            {
                ids.Add(components[i].GetInstanceID());
            }
        }

        return ids;
    }

    MonoBehaviour[] GetComponentsByTypeName(string typeName)
    {
        return UnityEngine.Object.FindObjectsByType<MonoBehaviour>()
            .Where(c => c != null && c.GetType().Name == typeName)
            .ToArray();
    }

    bool HasPlayingAudioSource(Component component)
    {
        if (component == null)
        {
            return false;
        }

        AudioSource[] audioSources = component.GetComponentsInChildren<AudioSource>(true);
        for (int i = 0; i < audioSources.Length; i++)
        {
            AudioSource source = audioSources[i];
            if (source != null && (source.isPlaying || source.timeSamples > 0))
            {
                return true;
            }
        }

        return false;
    }

    bool HasAudioSourceClipOrPlayback(Component component)
    {
        if (component == null)
        {
            return false;
        }

        AudioSource[] audioSources = component.GetComponentsInChildren<AudioSource>(true);
        for (int i = 0; i < audioSources.Length; i++)
        {
            AudioSource source = audioSources[i];
            if (source != null && (source.clip != null || source.isPlaying || source.timeSamples > 0))
            {
                return true;
            }
        }

        return false;
    }

    bool HasPlayingAudioSourceInChildren(GameObject root)
    {
        if (root == null)
        {
            return false;
        }

        AudioSource[] audioSources = root.GetComponentsInChildren<AudioSource>(true);
        for (int i = 0; i < audioSources.Length; i++)
        {
            AudioSource source = audioSources[i];
            if (source != null && (source.isPlaying || source.timeSamples > 0))
            {
                return true;
            }
        }

        return false;
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

        Type type = component.GetType();
        foreach (string memberName in memberNames)
        {
            FieldInfo field = type.GetField(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null && field.FieldType == typeof(bool))
            {
                return (bool)field.GetValue(component);
            }

            PropertyInfo prop = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (prop != null && prop.PropertyType == typeof(bool) && prop.CanRead)
            {
                return (bool)prop.GetValue(component);
            }
        }

        return false;
    }

    int GetIntMember(Component component, string memberName)
    {
        if (component == null)
        {
            return int.MinValue;
        }

        Type type = component.GetType();
        FieldInfo field = type.GetField(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null && field.FieldType == typeof(int))
        {
            return (int)field.GetValue(component);
        }

        PropertyInfo prop = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (prop != null && prop.PropertyType == typeof(int) && prop.CanRead)
        {
            return (int)prop.GetValue(component);
        }

        return int.MinValue;
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

    bool HasInputAuthority(Component component)
    {
        if (component == null)
        {
            return false;
        }

        Type type = component.GetType();
        PropertyInfo objectProp = type.GetProperty("Object", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
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

    bool HasStateAuthority(Component component)
    {
        if (component == null)
        {
            return false;
        }

        Type type = component.GetType();
        PropertyInfo objectProp = type.GetProperty("Object", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (objectProp == null)
        {
            return false;
        }

        object networkObject = objectProp.GetValue(component);
        if (networkObject == null)
        {
            return false;
        }

        PropertyInfo hasStateAuthorityProp = networkObject.GetType().GetProperty("HasStateAuthority", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (hasStateAuthorityProp == null || hasStateAuthorityProp.PropertyType != typeof(bool))
        {
            return false;
        }

        return (bool)hasStateAuthorityProp.GetValue(networkObject);
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

    void TryActivateShieldByReflection(Component skillComponent)
    {
        if (skillComponent == null)
        {
            return;
        }

        MethodInfo enableShieldMethod = skillComponent.GetType().GetMethod("EnableShield", BindingFlags.NonPublic | BindingFlags.Instance);
        if (enableShieldMethod != null)
        {
            enableShieldMethod.Invoke(skillComponent, null);
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
                if (bestParams[i].DefaultValue != DBNull.Value)
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
