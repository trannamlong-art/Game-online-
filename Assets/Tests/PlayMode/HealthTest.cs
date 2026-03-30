//using System.Collections;
//using NUnit.Framework;
//using UnityEngine;
//using UnityEngine.TestTools;
//using Fusion;

//public class HealthTest
//{
//    private GameObject player;
//    public Health health;  

//    [UnitySetUp]
//    public IEnumerator Setup()
//    {
//        player = new GameObject("Player");
//        health = player.AddComponent<Health>();

//        // đợi 1 frame để Awake() chạy
//        yield return null;
//    }

//    [UnityTest]
//    public IEnumerator Player_Should_Have_Initial_Health()  
//    {
//        // kiểm tra có máu không
//        Assert.IsNotNull(health);
//        Assert.Greater(health.HP, 0, "Player chưa có máu!");

//        yield return null;
//    }

//    [UnityTest]
//    public IEnumerator Player_TakeDamage_Should_Reduce_Health()
//    {
//        int before = health.HP;

//        health.RPC_TakeDamage(20);

//        yield return null;

//        Assert.Less(health.HP, before, "Máu không giảm sau khi bị damage!");
//        Assert.AreEqual(before - 20, health.HP);
//    }

//    [UnityTest]
//    public IEnumerator Player_Health_Should_Not_Be_Negative()
//    {
//        health.RPC_TakeDamage(999); // damage lớn

//        yield return null;

//        Assert.GreaterOrEqual(health.HP, 0, "Máu bị âm!");
//        Assert.AreEqual(0, health.HP);
//    }

//    [UnityTearDown]
//    public IEnumerator TearDown()
//    {
//        GameObject.Destroy(player);
//        yield return null;
//    }
//}