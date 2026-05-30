using RPG.World;
using UnityEngine;
using UnityEngine.UI;

// MarkovStateIcon — affiche une icône au-dessus de l'ennemi selon son état Markov.
// Nécessite un Canvas en mode World Space enfant du GameObject ennemi.
//
// Setup dans Unity :
//   1. Ajouter ce script sur le même GameObject que EnemyWorldController
//   2. Créer un enfant Canvas (World Space, Scale 0.01)
//   3. Créer un enfant Image dans le Canvas → brancher dans stateIcon
//   4. Assigner les 4 sprites dans l'Inspector
public class MarkovStateIcon : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image stateIcon;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.2f, 0f);

    [Header("Sprites par état")]
    [SerializeField] private Sprite spriteRepos;       // Ex: ZZZ ou lune
    [SerializeField] private Sprite spritePatrouille;  // Ex: flèches circulaires
    [SerializeField] private Sprite spriteChasse;      // Ex: point d'exclamation rouge
    [SerializeField] private Sprite spriteFuite;       // Ex: flèche de fuite

    [Header("Couleurs par état")]
    [SerializeField] private Color colorRepos = new Color(0.5f, 0.5f, 1f, 0.8f); // bleu pâle
    [SerializeField] private Color colorPatrouille = new Color(0.8f, 0.8f, 0.8f, 0.8f); // gris
    [SerializeField] private Color colorChasse = new Color(1f, 0.2f, 0.2f, 1f);   // rouge
    [SerializeField] private Color colorFuite = new Color(1f, 0.8f, 0f, 1f);     // jaune

    [Header("Options")]
    [SerializeField] private bool hideInRepos = true; // Cacher l'icône en état REPOS

    private MarkovStateMachine _markov;
    private string _lastState;
    private Transform _iconTransform;

    private void Awake()
    {
        _markov = GetComponent<MarkovStateMachine>();

        if (stateIcon != null)
            _iconTransform = stateIcon.transform.parent; // Le Canvas parent
    }

    private void Update()
    {
        if (_markov == null || stateIcon == null) return;

        // Mettre à jour uniquement si l'état a changé
        if (_markov.CurrentState == _lastState) return;
        _lastState = _markov.CurrentState;
        UpdateIcon(_markov.CurrentState);
    }

    private void LateUpdate()
    {
        // Maintenir l'icône au-dessus de l'ennemi
        if (_iconTransform != null)
            _iconTransform.position = transform.position + offset;
    }

    private void UpdateIcon(string state)
    {
        switch (state)
        {
            case EnemyState.Repos:
                stateIcon.gameObject.SetActive(!hideInRepos);
                stateIcon.sprite = spriteRepos;
                stateIcon.color = colorRepos;
                break;

            case EnemyState.Patrouille:
                stateIcon.gameObject.SetActive(true);
                stateIcon.sprite = spritePatrouille;
                stateIcon.color = colorPatrouille;
                break;

            case EnemyState.Chasse:
                stateIcon.gameObject.SetActive(true);
                stateIcon.sprite = spriteChasse;
                stateIcon.color = colorChasse;
                break;

            case EnemyState.Fuite:
                stateIcon.gameObject.SetActive(true);
                stateIcon.sprite = spriteFuite;
                stateIcon.color = colorFuite;
                break;
        }

        // Animation scale rapide (pulse)
        if (stateIcon.gameObject.activeSelf)
            StartCoroutine(PulseAnimation());
    }

    private System.Collections.IEnumerator PulseAnimation()
    {
        var t = stateIcon.transform;
        var origin = Vector3.one;
        var target = Vector3.one * 1.4f;
        float elapsed = 0f;
        float duration = 0.15f;

        // Scale up
        while (elapsed < duration)
        {
            t.localScale = Vector3.Lerp(origin, target, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        elapsed = 0f;
        // Scale down
        while (elapsed < duration)
        {
            t.localScale = Vector3.Lerp(target, origin, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        t.localScale = origin;
    }
}