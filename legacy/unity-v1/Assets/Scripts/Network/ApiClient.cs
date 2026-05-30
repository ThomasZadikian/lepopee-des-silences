using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace RPG.Network
{
    // ApiClient est un MonoBehaviour Singleton
    // Un seul exemplaire existe dans toute l'application
    // Il porte le JWT token et expose GetAsync / PostAsync / PutAsync

    public class ApiClient: MonoBehaviour
    {
        public static ApiClient Instance { get; private set; }
        // Modifie cette URL selon ton environnement
        // En local avec Docker : "http://localhost:5000"
        // En production : "https://ton-domaine.com"
        #if UNITY_EDITOR
            [SerializeField] private string baseUrl = "http://localhost:5009";
        #else
            [SerializeField] private string baseUrl = "https://rpgesi07-production.up.railway.app";
        #endif
        // Le token JWT reçu après login
        // Null si l'utilisateur n'est pas connecté 
        private string _jwtToken = null; 

        private void Awake ()
        {
            // Patron Singleton : Si une instance existe déjà, on se détruit.
            if(Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return; 
            }
            Instance = this;
            // Cette ligne empêche Unity de détruire ce GameObject 
            // lors du chargement d'une nouvelle scène 
            DontDestroyOnLoad(gameObject); 
        }
        // Gestion du token JWT
        public void SetToken(string token) => _jwtToken = token;
        public void ClearToken() => _jwtToken = null;

        // Propriété utile pour vérifier si l'utilisateur est connecté. 
        public bool IsAuthenticated => !string.IsNullOrEmpty(_jwtToken);

        // GET /endpoint
        // Exemple d'appel : var profile = await ApiClient.Instance.GetAsync<PlayerProfileResponse>("api/playerprofile/me");
        public async Task<T> GetAsync<T>(string endpoint)
        {
            var url = baseUrl + endpoint;
            using var request = UnityWebRequest.Get(url);
            InjectToken(request); // Ajoute "Authorization: Bearer ... "
            await SendRequest(request);

            if (request.result != UnityWebRequest.Result.Success)
                throw new Exception($"GET {endpoint} échoué : {request.error}");

            return JsonUtility.FromJson<T>(request.downloadHandler.text); 
        }

        // POST /endpoint avec un body JSON
        public async Task<T> PostAsync<T>(string endpoint, object body)
        {
            var json = JsonUtility.ToJson(body);
            var bytes = Encoding.UTF8.GetBytes(json);
            var url = baseUrl + endpoint;

            using var request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(bytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            InjectToken(request);
            await SendRequest(request);

            if (request.result != UnityWebRequest.Result.Success)
                throw new Exception(
                    $"POST {endpoint} échoué : {request.error}\n{request.downloadHandler.text}");

            Debug.Log($"[API] Réponse sync : {request.downloadHandler.text}");
            return JsonUtility.FromJson<T>(request.downloadHandler.text);
        }

        // PUT /endpoint
        public async Task<T> PutAsync<T>(string endpoint, object body)
        {
            var json = JsonUtility.ToJson(body);
            var bytes = Encoding.UTF8.GetBytes(json); 
            var url = baseUrl + endpoint;

            using var request = new UnityWebRequest(url, "PUT");
            request.uploadHandler = new UploadHandlerRaw(bytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            InjectToken(request);
            await SendRequest(request);

            if (request.result != UnityWebRequest.Result.Success)
                throw new Exception($"PUT {endpoint} échoué : {request.error}");
            return JsonUtility.FromJson<T>(request.downloadHandler.text);
        }
        // ■■ Helpers privés ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■
        // Ajoute le header Authorization si un token est disponible.
        // Appelé automatiquement par Get/Post/Put.
        private void InjectToken(UnityWebRequest request)
        {
            if (!string.IsNullOrEmpty(_jwtToken))
                request.SetRequestHeader("Authorization", $"Bearer {_jwtToken}");
        }
        // Convertit l'opération asynchrone Unity en Task awaitable standard C#.
        // Sans ça, on ne peut pas utiliser "await" avec UnityWebRequest.
        private Task SendRequest(UnityWebRequest request)
        {
            var tcs = new TaskCompletionSource<bool>();
            var op = request.SendWebRequest();
            op.completed += _ => tcs.SetResult(true);
            return tcs.Task;
        }
    }
}