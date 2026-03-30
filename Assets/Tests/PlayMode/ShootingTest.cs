using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem.LowLevel;

public class ShootingTest
{
    GameObject player;
    Keyboard keyboard;
    Mouse mouse;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        yield return SceneManager.LoadSceneAsync("SampleScene");

        yield return new WaitUntil(() => GameObject.FindGameObjectWithTag("Player") != null);

        player = GameObject.FindGameObjectWithTag("Player");

        keyboard = InputSystem.GetDevice<Keyboard>();
        mouse = InputSystem.GetDevice<Mouse>();
    }

    // ================= TC1 =================
    // Mũi tên bắn đi theo con chuột
    [UnityTest]
    public IEnumerator TC1_Mouse_Aim_Follow()
    {
        Vector3 initialForward = player.transform.forward;

        // Di chuyển chuột
        InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(500, 500) });
        InputSystem.Update();

        yield return new WaitForSeconds(0.5f);

        Vector3 newForward = player.transform.forward;

        Assert.AreNotEqual(initialForward, newForward);
    }

    // ================= TC2 =================
    // Đầu xe tăng quay theo hướng bắn
    [UnityTest]
    public IEnumerator TC2_Tank_Rotate_With_Aim()
    {
        float initialY = player.transform.eulerAngles.y;

        InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(800, 300) });
        InputSystem.Update();

        yield return new WaitForSeconds(0.5f);

        float newY = player.transform.eulerAngles.y;

        Assert.AreNotEqual(initialY, newY);
    }

    // ================= TC3 =================
    // Bắn đạn đúng hướng
    [UnityTest]
    public IEnumerator TC3_Shoot_Bullet()
    {
        int bulletBefore = GameObject.FindGameObjectsWithTag("Bullet").Length;

        // Click chuột trái
        PressMouse();

        yield return new WaitForSeconds(0.5f);

        int bulletAfter = GameObject.FindGameObjectsWithTag("Bullet").Length;

        Assert.Greater(bulletAfter, bulletBefore);
    }

    // ================= TC4 =================
    // Cooldown 3s
    [UnityTest]
    public IEnumerator TC4_Shoot_Cooldown()
    {
        PressMouse();
        yield return new WaitForSeconds(0.2f);

        int bulletAfterFirst = GameObject.FindGameObjectsWithTag("Bullet").Length;

        // spam click
        PressMouse();
        yield return new WaitForSeconds(0.2f);

        int bulletAfterSpam = GameObject.FindGameObjectsWithTag("Bullet").Length;

        // Không được bắn thêm ngay
        Assert.AreEqual(bulletAfterFirst, bulletAfterSpam);
    }

    // ================= TC5 =================
    // Gây damage
    [UnityTest]
    public IEnumerator TC5_Damage_Target()
    {
        GameObject enemy = GameObject.FindGameObjectWithTag("Enemy");
        var hp = enemy.GetComponent<Health>();

        float hpBefore = hp.HP;

        PressMouse();

        yield return new WaitForSeconds(1f);

        float hpAfter = hp.HP;

        Assert.Less(hpAfter, hpBefore);
    }

    // ================= TC6 =================
    // Hiệu ứng nổ
    [UnityTest]
    public IEnumerator TC6_Explosion_Effect()
    {
        PressMouse();

        yield return new WaitForSeconds(1f);

        var explosion = GameObject.FindGameObjectsWithTag("Explosion");

        Assert.IsTrue(explosion.Length > 0);
    }

    // ===== Helper =====
    void PressMouse()
    {
        InputSystem.QueueStateEvent(mouse, new MouseState { buttons = 1 });
        InputSystem.Update();
    }
}