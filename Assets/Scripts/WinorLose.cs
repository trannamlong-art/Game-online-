using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WinorLose : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private string winMessage = "THẮNG";
    [SerializeField] private string loseMessage = "THUA";

    [Header("Match Check")]
    [SerializeField] private float checkInterval = 0.25f;

    private bool _matchFinished;
    private bool _seenAnyPlayers;
    private bool _seenAnyEnemies;
    private float _nextCheckAt;

    private void Awake()
    {
        CacheResultText();
        if (resultText != null)
        {
            resultText.text = string.Empty;
        }

        _nextCheckAt = Time.time + Mathf.Max(0.05f, checkInterval);
    }

    private void OnEnable()
    {
        CacheResultText();
    }

    private void Update()
    {
        if (_matchFinished || Time.time < _nextCheckAt)
        {
            return;
        }

        _nextCheckAt = Time.time + Mathf.Max(0.05f, checkInterval);

        int alivePlayers = CountAlivePlayers(out int totalPlayers);
        int aliveEnemies = CountAliveEnemies(out int totalEnemies);

        if (totalPlayers > 0)
        {
            _seenAnyPlayers = true;
        }

        if (totalEnemies > 0)
        {
            _seenAnyEnemies = true;
        }

        if (!_seenAnyPlayers || !_seenAnyEnemies)
        {
            return;
        }

        bool allPlayersDead = alivePlayers <= 0;
        bool allEnemiesDead = aliveEnemies <= 0;

        if (allPlayersDead)
        {
            ShowResult(loseMessage);
        }
        else if (allEnemiesDead)
        {
            ShowResult(winMessage);
        }
    }

    private void ShowResult(string message)
    {
        _matchFinished = true;

        if (resultText == null)
        {
            return;
        }

        resultText.gameObject.SetActive(true);
        resultText.enabled = true;
        resultText.text = message;
    }

    private void CacheResultText()
    {
        if (resultText != null)
        {
            return;
        }

        resultText = GetComponent<TMP_Text>();
        if (resultText == null)
        {
            resultText = GetComponentInChildren<TMP_Text>(true);
        }
    }

    private int CountAlivePlayers(out int totalPlayers)
    {
        totalPlayers = 0;
        int alive = 0;

        IReadOnlyList<TankController> tanks = TankController.GetActiveTanks();
        for (int i = 0; i < tanks.Count; i++)
        {
            TankController tank = tanks[i];
            if (tank == null)
            {
                continue;
            }

            totalPlayers++;
            Health health = tank.GetComponent<Health>();
            if (health == null || health.HP > 0)
            {
                alive++;
            }
        }

        return alive;
    }

    private int CountAliveEnemies(out int totalEnemies)
    {
        totalEnemies = 0;
        int alive = 0;

        EnemyShooting[] enemies = FindObjectsByType<EnemyShooting>(FindObjectsSortMode.None);
        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyShooting enemy = enemies[i];
            if (enemy == null)
            {
                continue;
            }

            totalEnemies++;
            Health health = enemy.GetComponent<Health>() ?? enemy.GetComponentInParent<Health>();
            if (health == null || health.HP > 0)
            {
                alive++;
            }
        }

        return alive;
    }
}
