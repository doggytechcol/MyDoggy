using UnityEngine;
using UnityEngine.UI;
using Facebook.Unity;
using System.Collections.Generic;
using System.Threading.Tasks;

public class SocialLoginController : MonoBehaviour
{
    [Header("UI References")]
    public Button facebookSignInButton;

    private void Awake()
    {
        if (!FB.IsInitialized)
        {
            FB.Init(OnInitComplete, OnHideUnity);
        }
        else
        {
            FB.ActivateApp();
        }
    }

    private void Start()
    {
        if (facebookSignInButton != null)
        {
            facebookSignInButton.onClick.AddListener(OnFacebookSignInClicked);
        }
    }

    private void OnInitComplete()
    {
        if (FB.IsInitialized)
        {
            // 🔥 AHORA SÍ — SOLO DESPUÉS DE ESTAR INICIALIZADO
            FB.Mobile.SetAutoLogAppEventsEnabled(false);
            FB.Mobile.SetAdvertiserIDCollectionEnabled(false);

            FB.ActivateApp();
            Debug.Log("[Facebook] SDK Inicializado correctamente.");
        }
        else
        {
            Debug.LogError("[Facebook] ❌ Falló la inicialización del SDK.");
        }
    }

    private void OnHideUnity(bool isGameShown)
    {
        // opcional
    }

    public void OnFacebookSignInClicked()
    {
        if (AuthService.Instance == null || !AuthService.Instance.IsInitialized)
        {
            Debug.LogError("[Facebook] Firebase todavía no está inicializado.");
            return;
        }

        if (!FB.IsInitialized)
        {
            Debug.LogError("[Facebook] El SDK no está inicializado.");
            return;
        }

        facebookSignInButton.interactable = false;

        var permissions = new List<string>() { "public_profile", "email" };
        FB.LogInWithReadPermissions(permissions, HandleFacebookLoginResult);
    }

    private async void HandleFacebookLoginResult(ILoginResult result)
    {
        facebookSignInButton.interactable = true;

        if (result == null)
        {
            Debug.LogError("[Facebook] ❌ Resultado nulo.");
            return;
        }

        if (result.Cancelled)
        {
            Debug.Log("[Facebook] Login cancelado.");
            return;
        }

        if (result.Error != null)
        {
            Debug.LogError("[Facebook] ❌ Error Login: " + result.Error);
            return;
        }

        if (!FB.IsLoggedIn)
        {
            Debug.LogError("[Facebook] ❌ No se consiguió iniciar sesión.");
            return;
        }

        string facebookAccessToken = AccessToken.CurrentAccessToken.TokenString;
        Debug.Log("[Facebook] Access Token: " + facebookAccessToken);

        facebookSignInButton.interactable = false;

        var authResult = await AuthService.Instance.FacebookLoginAsync(facebookAccessToken);

        facebookSignInButton.interactable = true;

        if (!authResult.success)
        {
            Debug.LogError("[Facebook] ❌ Error Firebase: " + authResult.errorMessage);
        }
    }
}
