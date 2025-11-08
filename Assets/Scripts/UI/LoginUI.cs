using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions; // Necesario para la validación Regex
using System.Threading.Tasks;
using System;

// Script para manejar la interfaz de usuario de Login y Registro.
// Nota: Este script ahora usa la clase 'Constants' externa.
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
        // CRÍTICO: Debe existir en la escena (ServiceInitializer)
        authService = AuthService.Instance;
        if (authService == null)
        {
            SetFeedback("Error: Servicio de Autenticación no cargado.", true);
        }
    }
    
    /// <summary>
    /// Muestra un mensaje al usuario en la etiqueta de estado.
    /// </summary>
    private void SetFeedback(string message, bool isError)
    {
        if (statusLabel == null) return;
        
        statusLabel.text = message;
        // Rojo para errores, Blanco/Gris para mensajes informativos
        statusLabel.color = isError ? Color.red : Color.white; 
    }

    /// <summary>
    /// Realiza validaciones básicas de los campos de entrada antes de llamar a Firebase.
    /// </summary>
    private bool ValidateInputs(string email, string password, bool isRegistration)
    {
        // 1. Campos vacíos
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            SetFeedback("Correo o Contraseña no pueden estar vacíos.", true);
            return false;
        }

        // 2. Validación de formato de email (simple)
        if (!email.Contains("@") || !email.Contains("."))
        {
            SetFeedback("Por favor, ingresa un formato de correo válido.", true);
            return false;
        }

        // 3. Validación de Longitud y Fortaleza (Solo para registro)
        if (isRegistration)
        {
            // Validación de longitud mínima (8 caracteres)
            if (password.Length < Constants.MIN_PASSWORD_LENGTH)
            {
                SetFeedback($"La contraseña debe tener al menos {Constants.MIN_PASSWORD_LENGTH} caracteres.", true);
                return false;
            }
            
            // NUEVA VALIDACIÓN: Regex para Mayúscula, Minúscula y Número
            if (!Regex.IsMatch(password, Constants.PASSWORD_REGEX))
            {
                SetFeedback("La contraseña debe incluir al menos 1 mayúscula, 1 minúscula y 1 número.", true);
                return false;
            }
        }

        return true;
    }

    // --- Manejadores de Eventos de Botón (Llamados desde el Inspector) ---

    public async void OnLoginPressed()
    {
        string email = emailInput.text;
        string password = passwordInput.text;
        
        if (!ValidateInputs(email, password, false)) return;

        // Llama a la lógica centralizada de ejecución de autenticación
        await ExecuteAuthAction(() => authService.LoginAsync(email, password), false);
    }

    public async void OnRegisterPressed()
    {
        string email = emailInput.text;
        string password = passwordInput.text;

        if (!ValidateInputs(email, password, true)) return;
        
        // Llama a la lógica centralizada de ejecución de autenticación
        await ExecuteAuthAction(() => authService.RegisterAsync(email, password), true);
    }

    public async void OnGoogleLoginPressed() 
    { 
        // Lógica de validación previa si es necesaria
        await ExecuteAuthAction(() => authService.LoginWithGoogleAsync());
    }

    public async void OnFacebookLoginPressed() 
    { 
        // Lógica de validación previa si es necesaria
        await ExecuteAuthAction(() => authService.LoginWithFacebookAsync());
    }
    
    // --- Lógica Centralizada para Ejecutar Autenticación ---

    private async Task ExecuteAuthAction(System.Func<Task<(bool success, string errorMessage)>> authFunction, bool isRegistration = false)
    {
        if (authService == null || !authService.IsInitialized)
        {
            SetFeedback("Servicio no listo. Intenta de nuevo cuando Firebase esté cargado.", true);
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
            SetFeedback(isRegistration ? "Registro exitoso. ¡Bienvenido!" : "¡Bienvenido de vuelta!", false);
            
            // NOTA CRÍTICA: La redirección a la escena "CreatePet" o "PetProfile"
            // ahora es manejada por el listener de AuthService.OnAuthStateChanged
            // para evitar errores de doble salto o condiciones de carrera.
            
        }
        else
        {
            // Muestra el mensaje de error limpio de Firebase/AuthService
            SetFeedback($"{ (isRegistration ? "Registro" : "Login") } fallido: {result.errorMessage}", true);
        }
    }
}