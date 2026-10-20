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
    private const string PendingEventsPlayerPrefsKey = "firebase_analytics_pending_events_v1";
    private const float InitialRetryDelaySeconds = 2f;
    private const float MaximumRetryDelaySeconds = 60f;

    [Serializable]
    private sealed class AnonymousAuthResponse
    {
        public string idToken;
    }

    [Serializable]
    private sealed class PendingEventStore
    {
        public List<AnalyticsEvent> events = new();
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
    private int retryAttempt;
    private Coroutine retryCoroutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        instance = null;
    }

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
        RestorePendingEvents();

        if (pendingEvents.Count > 0)
        {
            BeginAuthentication();
        }
    }

    private void Enqueue(AnalyticsEvent analyticsEvent)
    {
        pendingEvents.Enqueue(analyticsEvent);
        SavePendingEvents();

        if (string.IsNullOrEmpty(idToken))
        {
            BeginAuthentication();
        }
        else
        {
            TryUploadNext();
        }
    }

    private void BeginAuthentication()
    {
        if (authenticationStarted || pendingEvents.Count == 0)
        {
            return;
        }

        authenticationStarted = true;
        StartCoroutine(SignInAnonymously());
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
            authenticationStarted = false;
            Debug.LogWarning(
                $"Firebase analytics sign-in failed ({request.responseCode}). " +
                "Enable Anonymous Authentication in Firebase Console. " +
                GetSafeErrorMessage(request)
            );
            ScheduleRetry();
            yield break;
        }

        AnonymousAuthResponse response =
            JsonUtility.FromJson<AnonymousAuthResponse>(request.downloadHandler.text);
        idToken = response?.idToken;

        if (string.IsNullOrEmpty(idToken))
        {
            authenticationStarted = false;
            Debug.LogWarning("Firebase analytics sign-in returned no ID token.");
            ScheduleRetry();
            yield break;
        }

        retryAttempt = 0;
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
            SavePendingEvents();
            retryAttempt = 0;
            Debug.Log(
                $"FIREBASE_ANALYTICS uploaded level={analyticsEvent.level_name} " +
                $"result={analyticsEvent.result} project={ProjectId}"
            );

            uploadInProgress = false;
            TryUploadNext();
            yield break;
        }

        Debug.LogWarning(
            $"Firebase analytics upload failed ({request.responseCode}): " +
            GetSafeErrorMessage(request)
        );
        uploadInProgress = false;

        if (request.responseCode == 401)
        {
            idToken = null;
            authenticationStarted = false;
        }

        ScheduleRetry();
    }

    private void ScheduleRetry()
    {
        if (retryCoroutine != null || pendingEvents.Count == 0)
        {
            return;
        }

        float delay = Mathf.Min(
            InitialRetryDelaySeconds * Mathf.Pow(2f, retryAttempt),
            MaximumRetryDelaySeconds
        );
        retryAttempt++;
        retryCoroutine = StartCoroutine(RetryAfterDelay(delay));
    }

    private IEnumerator RetryAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        retryCoroutine = null;

        if (string.IsNullOrEmpty(idToken))
        {
            BeginAuthentication();
        }
        else
        {
            TryUploadNext();
        }
    }

    private void RestorePendingEvents()
    {
        string json = PlayerPrefs.GetString(PendingEventsPlayerPrefsKey, string.Empty);
        if (string.IsNullOrEmpty(json))
        {
            return;
        }

        try
        {
            PendingEventStore store = JsonUtility.FromJson<PendingEventStore>(json);
            if (store?.events == null)
            {
                return;
            }

            foreach (AnalyticsEvent analyticsEvent in store.events)
            {
                if (analyticsEvent != null)
                {
                    pendingEvents.Enqueue(analyticsEvent);
                }
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Could not restore pending Firebase analytics: {exception.Message}");
            PlayerPrefs.DeleteKey(PendingEventsPlayerPrefsKey);
        }
    }

    private void SavePendingEvents()
    {
        if (pendingEvents.Count == 0)
        {
            PlayerPrefs.DeleteKey(PendingEventsPlayerPrefsKey);
            PlayerPrefs.Save();
            return;
        }

        PendingEventStore store = new PendingEventStore
        {
            events = new List<AnalyticsEvent>(pendingEvents)
        };
        PlayerPrefs.SetString(PendingEventsPlayerPrefsKey, JsonUtility.ToJson(store));
        PlayerPrefs.Save();
    }

    private static string GetSafeErrorMessage(UnityWebRequest request)
    {
        if (request.downloadHandler != null && !string.IsNullOrWhiteSpace(request.downloadHandler.text))
        {
            return request.downloadHandler.text;
        }

        return request.error;
    }
}
