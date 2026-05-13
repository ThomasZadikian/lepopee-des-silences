using RPG.Core;
using System.Collections.Generic;
using UnityEngine;

public class CombatSceneInit : MonoBehaviour
{
    private void Start()
    {
        var player = GameManager.Instance.CurrentEnemy;
        if (player == null)
        {
            Debug.LogError("[Combat] Aucun ennemi defini.");
            return;
        }

        var combatants = new List<Combatant>
        {
            Combatant.FromPlayer(GameManager.Instance.Player),
            Combatant.FromEnemy(GameManager.Instance.CurrentEnemy)
        };

        var combatSystem = gameObject.AddComponent<CombatSystem>();
        ATBManager.Instance.Initialize(combatants, combatSystem);
        CombatUIManager.Instance.Initialize(combatants);

        Debug.Log("[Combat] Scene initialisee.");
    }
}