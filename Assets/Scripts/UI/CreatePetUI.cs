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
        statusLabel.color = isError ? Color.red : Color.white; 
    }

    /// <summary>
    /// Valida los campos de entrada de la mascota.
    /// </summary>
    private bool ValidatePetInputs(string name, string breed, string yearStr)
    {
        // 1. Campos vacíos
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(breed) || string.IsNullOrEmpty(yearStr))
        {
            SetFeedback("Todos los campos (Nombre, Raza, Año) son obligatorios.", true);
            return false;
        }

        // 2. Validación de Año de Nacimiento (Debe ser un número válido)
        if (!int.TryParse(yearStr, out int birthYear))
        {
            SetFeedback("El año de nacimiento debe ser un número válido.", true);
            return false;
        }

        // 3. Validación de Rango (El año no puede ser futuro ni demasiado antiguo)
        int currentYear = DateTime.Now.Year;
        if (birthYear > currentYear || birthYear < currentYear - 50) 
        {
            SetFeedback("Por favor, ingresa un año de nacimiento razonable.", true);
            return false;
        }

        return true;
    }

    // --- Manejador de Eventos de Botón ---

    public async void OnCreatePetPressed()
    {
        string name = petNameInput.text.Trim();
        string breed = petBreedInput.text.Trim();
        string yearStr = petBirthYearInput.text.Trim();

        // 1. Validación de campos
        if (!ValidatePetInputs(name, breed, yearStr)) return;
        
        // 2. Pre-chequeos de servicio
        if (AuthService.Instance == null || FirestoreService.Instance == null)
        {
            SetFeedback("Error de servicio. Por favor, reinicia la aplicación.", true);
            return;
        }

        // 3. Chequeo de autenticación
        if (AuthService.Instance.CurrentUserId == null)
        {
            SetFeedback("No hay usuario autenticado. Volviendo a Login, reinicia la sesión.", true);
            Invoke(nameof(GoToLogin), 2.0f);
            return;
        }
        
        // 4. Ejecución de guardado
        if (loadingPanel) loadingPanel.SetActive(true);
        SetFeedback("Creando y registrando a tu mascota...", false);
        
        // Crear el modelo de datos
        int birthYear = int.Parse(yearStr);
        PetModel newPet = new PetModel(name, breed, birthYear);
        
        // Llamar al servicio de Firestore para guardar
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
        // El OnAuthStateChanged en AuthService se encargará de la redirección a Login
    }
}