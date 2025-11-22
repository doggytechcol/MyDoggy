using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class BootSceneManager : MonoBehaviour
{
    [Header("UI Introduction")]
    public TMP_Text introductionText;
    public GameObject introPanel;
    public float introDuration = 8.0f;

    [Header("Skip Intro UI")]
    public Button skipButton;
    public Image fadePanel;
    public float skipFadeDuration = 0.6f;

    [Header("Service Prefabs")]
    public GameObject authServicePrefab;
    public GameObject firestoreServicePrefab;

    private const string WELCOME_MESSAGE =
        "... Este videojuego fue creado por profesionales en bienestar animal, su objetivo es brindarte un reflejo real de las necesidades de tu mascota y orientarte en su cuidado. ¡Disfrútalo!";

    private Coroutine introCoroutine;
    private bool servicesInitialized = false;
    private bool skipPressed = false;

    // 🔥 Loader eliminado COMPLETAMENTE
    // public SceneLoadingController sceneLoaderPrefab;
    // private SceneLoadingController sceneLoader;

    void Start()
    {
        introductionText.text = "";
        if (introPanel != null) introPanel.SetActive(true);

        // Asegurar que el fadePanel y el botón están ocultos al inicio
        if (fadePanel != null) fadePanel.color = new Color(0, 0, 0, 0);
        if (skipButton != null) skipButton.gameObject.SetActive(false);

        // Activamos el botón 1.5s después con un fade-in
        StartCoroutine(ShowSkipButtonDelayed());

        introCoroutine = StartCoroutine(IntroductionSequence());
    }

    private IEnumerator ShowSkipButtonDelayed()
    {
        yield return new WaitForSeconds(1.5f);

        skipButton.gameObject.SetActive(true);

        CanvasGroup group = skipButton.GetComponent<CanvasGroup>();
        if (group == null)
            group = skipButton.gameObject.AddComponent<CanvasGroup>();

        group.alpha = 0f;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / skipFadeDuration;
            group.alpha = Mathf.Lerp(0, 1, t);
            yield return null;
        }
    }

    private IEnumerator IntroductionSequence()
    {
        yield return new WaitForSeconds(1.0f);

        // Typing effect
        for (int i = 0; i < WELCOME_MESSAGE.Length; i++)
        {
            if (skipPressed) yield break;
            introductionText.text = WELCOME_MESSAGE.Substring(0, i + 1);
            yield return new WaitForSeconds(0.05f);
        }

        float remainingTime = introDuration - (WELCOME_MESSAGE.Length * 0.05f + 1.0f);
        if (remainingTime > 0)
        {
            float timer = 0f;
            while (timer < remainingTime)
            {
                if (skipPressed) yield break;
                timer += Time.deltaTime;
                yield return null;
            }
        }

        EndIntroduction();
    }

    /// <summary>
    /// Ejecutado por el botón de “Omitir”.
    /// </summary>
    public void SkipIntro()
    {
        if (skipPressed) return;
        skipPressed = true;

        if (introCoroutine != null)
            StopCoroutine(introCoroutine);

        StartCoroutine(FadeAndEndIntro());
    }

    private IEnumerator FadeAndEndIntro()
    {
        fadePanel.gameObject.SetActive(true);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / skipFadeDuration;
            fadePanel.color = new Color(0, 0, 0, Mathf.Lerp(0, 0.8f, t));
            yield return null;
        }

        EndIntroduction();
    }

    private void EndIntroduction()
    {
        if (introPanel != null)
            introPanel.SetActive(false);

        InitializeServices();
    }

    private void InitializeServices()
    {
        if (servicesInitialized) return;
        servicesInitialized = true;

        if (authServicePrefab != null && AuthService.Instance == null)
        {
            Instantiate(authServicePrefab);
            Debug.Log("[Boot] Services instantiated.");
        }
        else if (AuthService.Instance != null)
        {
            Debug.Log("[Boot] AuthService ya existía.");
        }
        else
        {
            Debug.LogError("[Boot] CRÍTICO: Falta el prefab de AuthService.");
            SceneManager.LoadScene(Constants.SCENE_LOGIN);
        }
    }
}
