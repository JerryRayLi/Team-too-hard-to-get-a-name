using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public sealed class FirebaseAnalyticsUploader : MonoBehaviour
{
    private const string ApiKey = "AIzaSyDeQFw3oqQXOZNg49jAeePkECT-7SldcxM";
    private const string ProjectId = "color-blink-troll";
    private const string DatabaseUrl = "https://color-blink-troll-default-rtdb.firebaseio.com";

    [Serializable]
    private sealed class AnonymousAuthResponse
    {
        public string idToken;
    }

    [Serializable]
    private sealed class AnalyticsEvent
    {
        public string session_id;
        public string event_type;
        public string level_name;
        public string result;
        public int deaths;
        public float duration_seconds;
        public int visit_number;
        public bool was_personal_best;
        public string game_version;
        public string client_timestamp_utc;
    }

    private static FirebaseAnalyticsUploader instance;

    private readonly Queue<AnalyticsEvent> pendingEvents = new();
    private readonly string sessionId = Guid.NewGuid().ToString("N");

    private string idToken;
    private bool authenticationStarted;
    private bool uploadInProgress;

    public static void RecordLevelCompleted(
        string levelName,
        int deaths,
        float durationSeconds,
        int visitNumber,
        bool wasPersonalBest)
    {
        FirebaseAnalyticsUploader uploader = EnsureInstance();
        uploader.Enqueue(new AnalyticsEvent
        {
            session_id = uploader.sessionId,
            event_type = "level_result",
            level_name = levelName,
            result = "completed",
            deaths = deaths,
            duration_seconds = durationSeconds,
            visit_number = visitNumber,
            was_personal_best = wasPersonalBest,
            game_version = Application.version,
            client_timestamp_utc = DateTime.UtcNow.ToString("o")
        });
    }

    public static void RecordLevelSkipped(string levelName, int visitNumber)
    {
        FirebaseAnalyticsUploader uploader = EnsureInstance();
        uploader.Enqueue(new AnalyticsEvent
        {
            session_id = uploader.sessionId,
            event_type = "level_result",
            level_name = levelName,
            result = "skipped",
            deaths = -1,
            duration_seconds = -1f,
            visit_number = visitNumber,
            was_personal_best = false,
            game_version = Application.version,
            client_timestamp_utc = DateTime.UtcNow.ToString("o")
        });
    }

    private static FirebaseAnalyticsUploader EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        GameObject uploaderObject = new GameObject(nameof(FirebaseAnalyticsUploader));
        instance = uploaderObject.AddComponent<FirebaseAnalyticsUploader>();
        DontDestroyOnLoad(uploaderObject);
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Enqueue(AnalyticsEvent analyticsEvent)
    {
        pendingEvents.Enqueue(analyticsEvent);

        if (!authenticationStarted)
        {
            authenticationStarted = true;
            StartCoroutine(SignInAnonymously());
        }
        else
        {
            TryUploadNext();
        }
    }

    private IEnumerator SignInAnonymously()
    {
        string url =
            $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={ApiKey}";

        using UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes("{\"returnSecureToken\":true}"));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning(
                $"Firebase analytics sign-in failed ({request.responseCode}). " +
                "Enable Anonymous Authentication in Firebase Console. " +
                request.error
            );
            yield break;
        }

        AnonymousAuthResponse response =
            JsonUtility.FromJson<AnonymousAuthResponse>(request.downloadHandler.text);
        idToken = response?.idToken;

        if (string.IsNullOrEmpty(idToken))
        {
            Debug.LogWarning("Firebase analytics sign-in returned no ID token.");
            yield break;
        }

        TryUploadNext();
    }

    private void TryUploadNext()
    {
        if (uploadInProgress || string.IsNullOrEmpty(idToken) || pendingEvents.Count == 0)
        {
            return;
        }

        StartCoroutine(UploadNext());
    }

    private IEnumerator UploadNext()
    {
        uploadInProgress = true;
        AnalyticsEvent analyticsEvent = pendingEvents.Peek();
        string json = JsonUtility.ToJson(analyticsEvent);
        string url = $"{DatabaseUrl}/analytics_events.json?auth={UnityWebRequest.EscapeURL(idToken)}";

        using UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            pendingEvents.Dequeue();
            Debug.Log(
                $"FIREBASE_ANALYTICS uploaded level={analyticsEvent.level_name} " +
                $"result={analyticsEvent.result} project={ProjectId}"
            );

            uploadInProgress = false;
            TryUploadNext();
            yield break;
        }

        Debug.LogWarning(
            $"Firebase analytics upload failed ({request.responseCode}): {request.error}"
        );
        uploadInProgress = false;
    }
}
