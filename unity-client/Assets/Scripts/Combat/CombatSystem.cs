using RPG.Core;
using RPG.Services;
using System.Threading.Tasks;
using UnityEngine;

public class CombatSystem : MonoBehaviour
{
    private long _dmgDealtThisFight;
    private long _dmgTakenThisFight;
    private EnemyResponse _currentEnemy;
    private int _enemyCurrentHP;

    public void StartCombat(EnemyResponse enemy)
    {
        _currentEnemy = enemy;
        _enemyCurrentHP = enemy.maxHP;
        _dmgDealtThisFight = 0;
        _dmgTakenThisFight = 0;
    }

    public int PlayerPhysicalAttack()
    {
        var p = GameManager.Instance.Player;
        int dmg = Mathf.RoundToInt(p.Strength * 2 * _currentEnemy.physicalResistance);
        dmg = Mathf.Max(1, dmg + Random.Range(-dmg / 5, dmg / 5));

        _enemyCurrentHP -= dmg;
        _dmgDealtThisFight += dmg;
        return dmg;
    }

    public int EnemyAttack()
    {
        var p = GameManager.Instance.Player;
        int dmg = Mathf.Max(1, _currentEnemy.strength - p.Strength / 4);
        dmg = dmg + Random.Range(-dmg / 5, dmg / 5);

        _dmgTakenThisFight += dmg;
        GameManager.Instance.TakeDamage(dmg);
        return dmg;
    }

    public bool IsEnemyDead => _enemyCurrentHP <= 0;
    public bool IsPlayerDead => GameManager.Instance.IsPlayerDead;
    public int EnemyHP => _enemyCurrentHP;

    public async Task EndCombatAsync(bool playerWon)
    {
        var p = GameManager.Instance.Player;

        if (playerWon && _currentEnemy != null)
        {
            p.Experience += _currentEnemy.experienceReward;
            p.Gold += _currentEnemy.goldReward;
            CheckLevelUp();
        }
        else if (!playerWon)
        {
            p.CurrentHP = 1;
        }

        GameManager.Instance.RecordCombatResult(
            won: playerWon,
            dmgDealt: _dmgDealtThisFight,
            dmgTaken: _dmgTakenThisFight
        );

        await PlayerService.Instance.SyncAsync();
    }

    private void CheckLevelUp()
    {
        var p = GameManager.Instance.Player;
        int xpRequired = p.Level * 100;

        while (p.Experience >= xpRequired)
        {
            p.Experience -= xpRequired;
            p.Level++;
            p.MaxHP += 10; p.CurrentHP = p.MaxHP;
            p.MaxMP += 5; p.CurrentMP = p.MaxMP;
            p.Strength += 2;
            p.Intelligence += 2;
            xpRequired = p.Level * 100;
            Debug.Log($"[Combat] LEVEL UP ! Niveau {p.Level}");
        }
    }
}