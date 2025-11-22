using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System;
using UnityEngine.UI;

public class LoginUI : MonoBehaviour
{
    // ============================================================
    //  LOGIN & REGISTER
    // ============================================================

    [Header("Input Fields")]
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;

    [Header("Feedback & Status")]
    public TMP_Text statusLabel;
    public GameObject loadingPanel;

    private AuthService authService;


    // ============================================================
    //  FORGOT PASSWORD POPUP
    // ============================================================

    [Header("Forgot Password Popup")]
    public GameObject forgotPasswordPopup;
    public TMP_InputField resetEmailInput;
    public TMP_Text resetStatusLabel;
    public GameObject popupLoadingPanel;


    // ============================================================
    //  START
    // ============================================================

    private void Start()
    {
        SetFeedback("", false);
        if (loadingPanel) loadingPanel.SetActive(false);
        if (forgotPasswordPopup) forgotPasswordPopup.SetActive(false);
        if (popupLoadingPanel) popupLoadingPanel.SetActive(false);

        authService = AuthService.Instance;

        if (authService == null)
        {
            SetFeedback("*Error: Servicio de Autenticación no cargado.", true);
        }
    }


    // ============================================================
    //  FEEDBACK GENERAL
    // ============================================================

    private void SetFeedback(string message, bool isError)
    {
        if (statusLabel == null) return;
        statusLabel.text = message;
    }


    // ============================================================
    //  LOGIN
    // ============================================================

    public void OnLoginPressed()
    {
        if (!ValidateInputs(false)) return;

        HandleAuthAction(() =>
            authService.LoginAsync(emailInput.text, passwordInput.text)
        );
    }


    // ============================================================
    //  REGISTER
    // ============================================================

    public void OnRegisterPressed()
    {
        if (!ValidateInputs(true)) return;

        HandleAuthAction(() =>
            authService.RegisterAsync(emailInput.text, passwordInput.text),
            true
        );
    }


    // ============================================================
    //  VALIDACIÓN
    // ============================================================

    private bool ValidateInputs(bool isRegistration)
    {
        string email = emailInput.text.Trim();
        string password = passwordInput.text.Trim();

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            SetFeedback("Correo y contraseña no pueden estar vacíos.", true);
            return false;
        }

        if (isRegistration)
        {
            if (password.Length < Constants.MIN_PASSWORD_LENGTH)
            {
                SetFeedback(
                    $"La contraseña debe tener al menos {Constants.MIN_PASSWORD_LENGTH} caracteres.",
                    true
                );
                return false;
            }

            if (!Regex.IsMatch(password, Constants.PASSWORD_REGEX))
            {
                SetFeedback(
                    $"La contraseña debe tener {Constants.MIN_PASSWORD_LENGTH} caracteres, 1 mayúscula, 1 minúscula y 1 número.",
                    true
                );
                return false;
            }
        }

        SetFeedback("", false);
        return true;
    }


    // ============================================================
    //  CONTROL DE LOGIN/REGISTER ASYNC
    // ============================================================

    private async void HandleAuthAction(
        Func<Task<(bool success, string errorMessage)>> authFunction,
        bool isRegistration = false)
    {
        if (authService == null || !authService.IsInitialized)
        {
            SetFeedback("Servicio no listo. Intenta de nuevo.", true);
            return;
        }

        if (loadingPanel) loadingPanel.SetActive(true);
        SetFeedback(isRegistration ? "Procesando registro..." : "Iniciando sesión...", false);

        var result = await authFunction();

        if (loadingPanel) loadingPanel.SetActive(false);

        if (result.success)
        {
            SetFeedback(
                isRegistration ?
                    "Registro exitoso. ¡Bienvenido! Redirigiendo..." :
                    "¡Bienvenido de vuelta! Redirigiendo...",
                false
            );
        }
        else
        {
            SetFeedback($"{(isRegistration ? "Registro" : "Login")} fallido: {result.errorMessage}", true);
        }
    }


    // ============================================================
    //  FORGOT PASSWORD (POPUP)
    // ============================================================

    public void OnForgotPasswordPressed()
    {
        if (forgotPasswordPopup != null)
        {
            forgotPasswordPopup.SetActive(true);
            resetEmailInput.text = "";
            resetStatusLabel.text = "";
        }
    }

    public void OnCloseForgotPassword()
    {
        if (forgotPasswordPopup != null)
            forgotPasswordPopup.SetActive(false);
    }


    // ============================================================
    //  SEND PASSWORD RESET EMAIL
    // ============================================================

    public async void OnSendResetEmail()
    {
        string email = resetEmailInput.text.Trim();

        if (string.IsNullOrEmpty(email))
        {
            resetStatusLabel.text = "Ingresa tu correo.";
            return;
        }

        if (popupLoadingPanel) popupLoadingPanel.SetActive(true);
        resetStatusLabel.text = "Enviando correo...";

        var result = await authService.SendPasswordResetEmailAsync(email);

        if (popupLoadingPanel) popupLoadingPanel.SetActive(false);

        if (result.success)
        {
            resetStatusLabel.text = "Correo enviado. Revisa tu bandeja de entrada.";
        }
        else
        {
            resetStatusLabel.text = "Error: " + result.errorMessage;
        }
    }
}
