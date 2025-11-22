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
            FB.ActivateApp();
            Debug.Log("[Facebook] SDK Inicializado correctamente.");
        }
        else
        {
            Debug.LogError("[Facebook] No se pudo inicializar el SDK.");
        }
    }

    private void OnHideUnity(bool isGameShown) { }

    public void OnFacebookSignInClicked()
    {
        if (AuthService.Instance == null || !AuthService.Instance.IsInitialized)
        {
            Debug.LogWarning("[Facebook] Firebase no está listo. Reintentando...");
            return;
        }

        facebookSignInButton.interactable = false;

        // ⚡ Importante: solo public_profile
        var permissions = new List<string>() { "public_profile" };

        FB.LogInWithReadPermissions(permissions, HandleFacebookLoginResult);
    }

    private async void HandleFacebookLoginResult(ILoginResult result)
    {
        facebookSignInButton.interactable = true;

        if (result == null)
        {
            Debug.LogError("[Facebook] Resultado nulo.");
            return;
        }

        if (result.Cancelled)
        {
            Debug.Log("[Facebook] Login cancelado por el usuario.");
            return;
        }

        if (!string.IsNullOrEmpty(result.Error))
        {
            Debug.LogError("[Facebook] Error de login: " + result.Error);
            return;
        }

        if (!FB.IsLoggedIn)
        {
            Debug.LogError("[Facebook] FB.IsLoggedIn es falso.");
            return;
        }

        // ⚡ SDK 18: AccessToken viene en result
        string facebookAccessToken = result.AccessToken.TokenString;
        Debug.Log("[Facebook] Access Token obtenido.");

        await Task.Delay(300);

        var authResult = await AuthService.Instance.FacebookLoginAsync(facebookAccessToken);

        if (!authResult.success)
        {
            Debug.LogError("[Facebook] Error autenticando con Firebase: " + authResult.errorMessage);
            return;
        }

        Debug.Log("[Facebook] Login COMPLETO. Firebase autenticó correctamente.");
    }
}


