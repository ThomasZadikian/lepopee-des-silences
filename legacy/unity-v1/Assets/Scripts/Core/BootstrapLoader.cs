using UnityEngine;
using UnityEngine.SceneManagement;

public class BootstrapLoader : MonoBehaviour
{
    private void Start()
    {
        SceneManager.LoadScene("LoginScene");
    }
}