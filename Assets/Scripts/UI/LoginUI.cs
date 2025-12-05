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
    //  INSTANCIAS DE SERVICIOS
    // ============================================================

    [Header("Servicios de Autenticación")]
    private AuthService authService;
    private GoogleLoginController googleController;


    // ============================================================
    //  LOGIN & REGISTER
    // ============================================================

    [Header("Input Fields")]
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;

    [Header("Feedback & Status")]
    public TMP_Text statusLabel;
    public GameObject loadingPanel;


    // ============================================================
    //  FORGOT PASSWORD POPUP
    // ============================================================

    [Header("Forgot Password Popup")]
    public GameObject forgotPasswordPopup;
    public TMP_InputField resetEmailInput;
    public TMP_Text resetStatusLabel;
    public GameObject popupLoadingPanel;


    // ============================================================
    //  START & SETUP
    // ============================================================

    private void Start()
    {
        // Limpieza de estado inicial
        SetFeedback("", false);
        if (loadingPanel) loadingPanel.SetActive(false);
        if (forgotPasswordPopup) forgotPasswordPopup.SetActive(false);
        if (popupLoadingPanel) popupLoadingPanel.SetActive(false);

        // Obtener referencias a los Singletons
        authService = AuthService.Instance;

        // Corregido: Usamos FindAnyObjectByType para encontrar el controlador local (en esta escena)
        // Esto es necesario para iniciar el flujo nativo de Google.
        googleController = FindAnyObjectByType<GoogleLoginController>();

        if (authService == null)
        {
            SetFeedback("*Error: Servicio de Autenticación no cargado.", true);
        }
        else
        {
            // ⭐ AUTO-REGISTRO: Enviar la referencia a la instancia persistente (AuthService).
            // Esto permite que el servicio persistente envíe feedback de errores nativos a esta UI transitoria.
            authService.RegisterLoginUI(this);

            if (googleController == null)
            {
                Debug.LogWarning("[LoginUI] GoogleLoginController no encontrado. El login nativo de Google no funcionará.");
            }
        }
    }


    // ============================================================
    //  FEEDBACK GENERAL
    // ============================================================

    /// <summary>
    /// Establece el mensaje de feedback en la UI de Login.
    /// </summary>
    /// <param name="message">Mensaje a mostrar.</param>
    /// <param name="isError">Si es verdadero, el texto podría ser rojo/negrita.</param>
    public void SetFeedback(string message, bool isError)
    {
        if (statusLabel == null) return;
        statusLabel.text = message;
        // Aquí podrías cambiar el color de statusLabel si deseas 
        // (e.g., statusLabel.color = isError ? Color.red : Color.white)
    }


    // ============================================================
    //  LOGIN/REGISTER (Email/Password)
    // ============================================================

    public void OnLoginPressed()
    {
        if (!ValidateInputs(false)) return;

        HandleAuthAction(() =>
            authService.LoginAsync(emailInput.text, passwordInput.text)
        );
    }

    public void OnRegisterPressed()
    {
        if (!ValidateInputs(true)) return;

        HandleAuthAction(() =>
            authService.RegisterAsync(emailInput.text, passwordInput.text),
            true
        );
    }


    // ============================================================
    // ⭐ LOGIN SOCIAL: GOOGLE NATIVO
    // ============================================================

    /// <summary>
    /// Llamado por el botón de Google. Inicia el proceso nativo.
    /// </summary>
    public void OnGoogleLoginPressed()
    {
        if (googleController == null)
        {
            SetFeedback("Controlador de Google no encontrado. Verifica la configuración.", true);
            return;
        }

        // Activamos el panel de carga mientras se abre el Intent de Google
        if (loadingPanel) loadingPanel.SetActive(true);
        SetFeedback("Abriendo ventana de Google...", false);

        // La respuesta (éxito o fallo) se gestiona en AuthService después del callback nativo.
        googleController.StartGoogleSignIn();
    }

    // ⭐ LOGIN SOCIAL: FACEBOOK
    public void OnFacebookLoginPressed()
    {
        SetFeedback("Iniciando sesión con Facebook...", false);
        if (loadingPanel) loadingPanel.SetActive(true);

        // La lógica de Facebook, incluyendo el manejo de callbacks y feedback,
        // está centralizada en AuthService.
        authService.FacebookLoginUI();
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
