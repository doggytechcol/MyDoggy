using UnityEngine;
using TMPro;
using System;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

// Script para manejar la interfaz de creación de la mascota
// Nota: Depende de PetModel y FirestoreService, y utiliza la clase Constants.
public class CreatePetUI : MonoBehaviour
{
    // --- Referencias de UI ---
    [Header("Input Fields")]
    public TMP_InputField petNameInput;
    public TMP_InputField petBreedInput;
    public TMP_InputField petBirthYearInput; 

    [Header("Feedback & Status")]
    public TMP_Text statusLabel;
    public GameObject loadingPanel; 
    
    private void Start()
    {
        SetFeedback("", false);
        if (loadingPanel) loadingPanel.SetActive(false);
        
        // **CRÍTICO:** Chequeo de sesión. Si el usuario llega aquí sin estar loggeado, es un error.
        if (AuthService.Instance == null || AuthService.Instance.CurrentUserId == null)
        {
            SetFeedback("Error: No hay sesión activa. Volviendo a Login.", true);
            // Usamos Constants para la escena de Login
            Invoke(nameof(GoToLogin), 2.0f);
        }
        else
        {
            SetFeedback("¡Bienvenido! Cuéntanos sobre tu mascota.", false);
        }
    }
    
    private void SetFeedback(string message, bool isError)
    {
        if (statusLabel == null) return;
        
        statusLabel.text = message;
        // Cambia el color del texto si es un error
        statusLabel.color = isError ? Color.red : Color.black; 
    }
    
    /// <summary>
    /// Maneja la lógica al presionar el botón de "Crear Mascota".
    /// </summary>
    public async void OnCreatePetPressed()
    {
        // 1. Obtener y validar datos
        string name = petNameInput.text.Trim();
        string breed = petBreedInput.text.Trim();
        string yearStr = petBirthYearInput.text.Trim();

        if (string.IsNullOrEmpty(name))
        {
            SetFeedback("El nombre de la mascota es obligatorio.", true);
            return;
        }

        if (string.IsNullOrEmpty(yearStr) || !int.TryParse(yearStr, out int birthYear))
        {
            SetFeedback("Por favor, ingresa un año de nacimiento válido (ej: 2020).", true);
            return;
        }
        
        // Validación extra para el año de nacimiento
        int currentYear = DateTime.Now.Year;
        if (birthYear < 1980 || birthYear > currentYear) // Rango razonable
        {
            SetFeedback($"El año de nacimiento debe estar entre 1980 y {currentYear}.", true);
            return;
        }
        
        // Chequeo de sesión nuevamente, crítico antes de intentar guardar
        if (AuthService.Instance == null || AuthService.Instance.CurrentUserId == null)
        {
            SetFeedback("Error de sesión. Por favor, reinicia la sesión.", true);
            return;
        }
        
        // 2. Crear modelo y guardar
        if (loadingPanel) loadingPanel.SetActive(true);
        SetFeedback("Creando y registrando a tu mascota...", false);
        
        // Crear el modelo de datos
        // Si la raza es vacía, usamos un valor por defecto
        if (string.IsNullOrEmpty(breed)) breed = "Desconocida";
        
        PetModel newPet = new PetModel(name, breed, birthYear);
        
        // Llamar al servicio de Firestore para guardar
        // CRÍTICO: El PetModel se guarda con el userId del usuario autenticado
        (bool success, string errorMessage) result = await FirestoreService.Instance.SavePetAsync(newPet);

        if (loadingPanel) loadingPanel.SetActive(false);

        if (result.success)
        {
            Debug.Log("[CreatePetUI] ✅ Mascota creada y guardada exitosamente.");
            SetFeedback($"¡{name} ha sido registrada! Cargando perfil...", false);
            
            // 5. Redirección a PetProfile (Usamos Constants)
            // Ya que los datos se guardaron, AuthService.OnAuthStateChanged debería llevarnos a PetProfile
            // pero cargamos la escena directamente para una transición inmediata.
            await Task.Delay(1000); 
            SceneManager.LoadScene(Constants.SCENE_PET_PROFILE);
        }
        else
        {
            // Muestra el error de Firestore
            SetFeedback($"Error al crear la mascota: {result.errorMessage}", true);
        }
    }
    
    // Método privado para la redirección de error de sesión
    private void GoToLogin()
    {
        SceneManager.LoadScene(Constants.SCENE_LOGIN);
    }
    
    // Método para desloggearse (llamado desde un botón en la UI)
    public void OnLogoutPressed()
    {
        AuthService.Instance.Logout();
        // El OnAuthStateChanged en AuthService se encargará de redirigir a Login
    }
}