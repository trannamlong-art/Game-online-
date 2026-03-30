using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem.LowLevel;

public class MovingTest
{
    [UnityTest]
    public IEnumerator Moving()
    {
        yield return SceneManager.LoadSceneAsync("SampleScene");

        yield return new WaitUntil(() => GameObject.FindGameObjectWithTag("Player") != null);

        // 🔥 ĐỢI NETWORK + INIT
        yield return new WaitForSeconds(2f);

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        Assert.IsNotNull(player, "Không tìm thấy Player");

        Vector3 startPos = player.transform.position;

        var keyboard = InputSystem.GetDevice<Keyboard>();

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
}