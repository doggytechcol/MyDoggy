using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions; // Necesario para la validación Regex
using System.Threading.Tasks;
using System;
using System.Collections.Generic; // Agregado por si es necesario para futuras validaciones

// Script para manejar la interfaz de usuario de Login y Registro.
public class LoginUI : MonoBehaviour
{
    // --- Referencias de UI ---
    [Header("Input Fields")]
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    
    [Header("Feedback & Status")]
    public TMP_Text statusLabel; 
    public GameObject loadingPanel; 
    
    // Referencia al servicio de autenticación
    private AuthService authService;

    private void Start()
    {
        // 1. Inicialización
        SetFeedback("", false);
        if (loadingPanel) loadingPanel.SetActive(false);

        // 2. Obtener la instancia del servicio
        authService = AuthService.Instance;
        if (authService == null)
        {
            SetFeedback("*Error: Servicio de Autenticación no cargado.", true);
        }
    }
    
    /// <summary>
    /// Muestra un mensaje al usuario en la etiqueta de estado.
    /// </summary>
    private void SetFeedback(string message, bool isError)
    {
        if (statusLabel == null) return;
        statusLabel.text = message;
        // Opcional: statusLabel.color = isError ? Color.red : Color.green;
    }
    
    // --- Manejo de Botones ---

    public void OnLoginPressed()
    {
        // 1. Validación básica de inputs antes de llamar al servicio
        if (!ValidateInputs(false)) return;

        // 2. Ejecutar la acción de autenticación
        // CRÍTICO: La función lambda ahora encapsula la llamada asíncrona y devuelve el Task esperado.
        HandleAuthAction(() => authService.LoginAsync(emailInput.text, passwordInput.text));
    }

    public void OnRegisterPressed()
    {
        // 1. Validación estricta de inputs (incluyendo regex de contraseña)
        if (!ValidateInputs(true)) return;

        // 2. Ejecutar la acción de autenticación
        // CRÍTICO: La función lambda ahora encapsula la llamada asíncrona y devuelve el Task esperado.
        HandleAuthAction(() => authService.RegisterAsync(emailInput.text, passwordInput.text), true);
    }
    
    /// <summary>
    /// Realiza la validación de los campos de email y contraseña.
    /// </summary>
    private bool ValidateInputs(bool isRegistration)
    {
        string email = emailInput.text;
        string password = passwordInput.text;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            SetFeedback("Correo y contraseña no pueden estar vacíos.", true);
            return false;
        }

        if (isRegistration)
        {
            // Validar la contraseña solo en registro (Firebase valida el login implícitamente)
            if (password.Length < Constants.MIN_PASSWORD_LENGTH)
            {
                SetFeedback($"La contraseña debe tener al menos {Constants.MIN_PASSWORD_LENGTH} caracteres.", true);
                return false;
            }
            // Validación Regex avanzada para registro
            if (!Regex.IsMatch(password, Constants.PASSWORD_REGEX))
            {
                SetFeedback($"La contraseña debe tener {Constants.MIN_PASSWORD_LENGTH} caracteres, 1 mayúscula, 1 minúscula y 1 número.", true);
                return false;
            }
        }


        SetFeedback("", false); // Limpiar feedback si pasa la validación
        return true;
    }

    /// <summary>
    /// Maneja la ejecución de la acción de autenticación (Login o Registro), mostrando carga y feedback.
    /// </summary>
    /// <param name="authFunction">Función asíncrona que llama a AuthService.LoginUser o RegisterUser.</param>
    private async void HandleAuthAction(Func<Task<(bool success, string errorMessage)>> authFunction, bool isRegistration = false)
    {
        if (authService == null || !authService.IsInitialized)
        {
            SetFeedback("Servicio no listo. Intenta de nuevo.", true);
            return;
        }

        // Mostrar indicador de carga
        if (loadingPanel) loadingPanel.SetActive(true);
        SetFeedback(isRegistration ? "Procesando registro..." : "Iniciando sesión...", false);

        // Llama al AuthService y recibe la tupla con el resultado y el mensaje de error
        (bool success, string errorMessage) result = await authFunction();

        // Ocultar indicador de carga
        if (loadingPanel) loadingPanel.SetActive(false);

        if (result.success)
        {
            Debug.Log($"[LoginUI] ✅ { (isRegistration ? "Registro" : "Login") } Exitoso");
            SetFeedback(isRegistration ? "Registro exitoso. ¡Bienvenido! Redirigiendo..." : "¡Bienvenido de vuelta! Redirigiendo...", false);

        }
        else
        {
            // Muestra el mensaje de error limpio de Firebase/AuthService
            SetFeedback($"{ (isRegistration ? "Registro" : "Login") } fallido: {result.errorMessage}", true);
        }
    }
}