using RPG.Core;
using RPG.Services;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveTrigger : MonoBehaviour
{
    [SerializeField] private string zoneName = "";
    private bool _saving = false;

    private async void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (_saving) return;
        _saving = true;

        var pos = other.transform.position;
        GameManager.Instance.PosX = pos.x;
        GameManager.Instance.PosY = pos.y;
        GameManager.Instance.CurrentZone = string.IsNullOrEmpty(zoneName)
            ? SceneManager.GetActiveScene().name
            : zoneName;

        bool saved = await PlayerService.Instance.SyncAsync();
        Debug.Log(saved ? "[Save] Sauvegardé." : "[Save] Échec.");

        _saving = false;
    }
}