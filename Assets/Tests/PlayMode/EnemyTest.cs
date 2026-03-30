using System.Collections;
using Assert = NUnit.Framework.Assert;
using UnityEngine;
using UnityEngine.TestTools;
using Fusion;
using UnityEngine.SceneManagement;

public class EnemyTest
{
    private GameObject enemy;
    private GameObject player;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        // Load scene
        SceneManager.LoadScene("SampleScene");

        // ⏳ đợi scene load + Awake/Start chạy xong
        yield return null;
        yield return new WaitForSeconds(2f);

        // 🔥 tìm enemy
        enemy = GameObject.FindWithTag("Enemy");
        Assert.IsNotNull(enemy, "Không tìm thấy Enemy");

        // 🔥 tạo player fake nhưng "giống thật hơn"
        player = new GameObject("TestPlayer");

        // Tag
        player.tag = "Player";

        // 🔥 Layer (QUAN TRỌNG)
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer != -1)
            player.layer = playerLayer;

        // 🔥 Collider
        var collider = player.AddComponent<CapsuleCollider>();
        collider.height = 2f;
        collider.radius = 0.5f;
        collider.isTrigger = false;

        // 🔥 Rigidbody (nhiều AI cần cái này để detect)
        var rb = player.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;

        // 🔥 đặt vị trí SAU khi mọi thứ sẵn sàng
        yield return null;

        player.transform.position =
            enemy.transform.position + enemy.transform.forward * 5f;

        // ⏳ cho enemy detect player
        yield return new WaitForSeconds(1f);
    }

    [UnityTest]
    public IEnumerator Enemy_Should_Shoot_Player()
    {
        // ⏳ delay để enemy ổn định
        yield return new WaitForSeconds(1f);

        // 🔥 lấy danh sách object ban đầu
        var beforeObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude);

        float timer = 0f;
        bool detectedNewObject = false;

        while (timer < 5f)
        {
            var currentObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude);

            // 🔥 so sánh object mới xuất hiện
            if (currentObjects.Length > beforeObjects.Length)
            {
                // check object mới có gần enemy không (lọc rác)
                foreach (var obj in currentObjects)
                {
                    bool existed = false;

                    foreach (var old in beforeObjects)
                    {
                        if (obj == old)
                        {
                            existed = true;
                            break;
                        }
                    }

                    if (!existed)
                    {
                        // 🔥 object mới xuất hiện gần enemy → khả năng cao là đạn
                        float dist = Vector3.Distance(obj.transform.position, enemy.transform.position);

                        if (dist < 10f)
                        {
                            detectedNewObject = true;
                            break;
                        }
                    }
                }
            }

            if (detectedNewObject)
                break;

            timer += Time.deltaTime;
            yield return null;
        }
        yield return new WaitForSeconds(3f);
        Assert.IsTrue(detectedNewObject, "Enemy không tạo object mới (có thể không bắn)");
    }

    //// =============================
    //// TEST 2: Player bị damage
    //// =============================
    //[UnityTest]
    //public IEnumerator Enemy_Should_Damage_Player()
    //{
    //    var health = player.GetComponent<Health>();

    //    int startHP = health.HP;

    //    yield return new WaitForSeconds(3f);

    //    Assert.Less(health.HP, startHP, "Player không bị mất máu");
    //}

    //// =============================
    //// TEST 3: Không bắn ngoài range
    //// =============================
    //[UnityTest]
    //public IEnumerator Enemy_Should_Not_Shoot_Out_Of_Range()
    //{
    //    int before = Object.FindObjectsByType<Bullet>(FindObjectsInactive.Exclude).Length;

    //    player.transform.position = enemy.transform.position + Vector3.forward * 50f;

    //    yield return new WaitForSeconds(2f);

    //    int after = Object.FindObjectsByType<Bullet>(FindObjectsInactive.Exclude).Length;

    //    Assert.AreEqual(before, after, "Enemy vẫn bắn khi ngoài range");
    //}

    //[UnityTearDown]
    //public IEnumerator Cleanup()
    //{
    //    GameObject.Destroy(player);
    //    yield return null;
    //}
}