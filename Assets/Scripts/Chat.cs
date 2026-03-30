using UnityEngine;
using Fusion;
using TMPro;

public class Chat : MonoBehaviour
{
    public TMP_InputField inputField;
    public GameObject Content;
    public GameObject messagePrefab;

    private static Chat _instance;

    void Awake()
    {
        _instance = this;

        if (inputField == null)
        {
            inputField = GetComponentInChildren<TMP_InputField>(true);
        }
    }

    void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    public void SendMessage()
    {
        if (inputField == null)
        {
            Debug.LogWarning("Chat: inputField is not assigned.");
            return;
        }

        string textToSend = inputField.text != null ? inputField.text.Trim() : string.Empty;
        if (string.IsNullOrEmpty(textToSend))
        {
            return;
        }

        NetworkRunner runner = FindFirstObjectByType<NetworkRunner>();
        if (runner != null && runner.IsRunning)
        {
            var playerObj = runner.GetPlayerObject(runner.LocalPlayer);
            if (playerObj != null)
            {
                var controller = playerObj.GetComponent<TankController>();
                if (controller != null)
                {
                    controller.RPC_SendChat(textToSend);
                }
                else
                {
                    AddMessageToChat(textToSend);
                    Debug.LogWarning("Chat: Local player has no TankController, message displayed locally only.");
                }
            }
            else
            {
                AddMessageToChat(textToSend);
                Debug.LogWarning("Chat: Local player object not found, message displayed locally only.");
            }
        }
        else
        {
            AddMessageToChat(textToSend);
            Debug.LogWarning("Chat: NetworkRunner is not running, message displayed locally only.");
        }

        inputField.text = string.Empty;
        inputField.ActivateInputField();
    }

    void AddMessageToChat(string message)
    {
        if (Content == null || messagePrefab == null)
        {
            Debug.LogWarning("Chat: Content or messagePrefab is not assigned.");
            return;
        }

        var msg = Instantiate(messagePrefab, Content.transform);
        var messageText = msg.GetComponentInChildren<TMP_Text>();
        if (messageText != null)
        {
            messageText.text = message;
        }
        else
        {
            Debug.LogWarning("Chat: messagePrefab has no TMP_Text child.");
        }
    }

    public static void ReceiveNetworkMessage(string message)
    {
        if (_instance != null)
        {
            _instance.AddMessageToChat(message);
        }
    }

}
