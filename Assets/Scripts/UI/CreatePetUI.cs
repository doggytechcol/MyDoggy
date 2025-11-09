using UnityEngine;
using TMPro;
using System;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using System.Collections.Generic;

// Script para manejar la interfaz de creación de la mascota (Registro y Setup Inicial)
public class CreatePetUI : MonoBehaviour
{
    // --- Referencias de UI ---
    [Header("Input Fields")]
    public TMP_InputField petNameInput;
    public TMP_Dropdown breedCategoryDropdown; // Dropdown para la categoría (e.g., Pequeño, Mediano, Grande)
    public TMP_Dropdown petBreedDropdown;      // Dropdown para la raza específica
    public TMP_Dropdown petBirthYearDropdown;  // Dropdown para el año de nacimiento

    [Header("Avatar & Appearance")]
    public TMP_Text currentAvatarText; // Muestra el nombre del avatar actual (ej. 'Perro Gris')
    private int currentAvatarIndex = 0;
    private readonly List<string> availableAvatars = new List<string> { "Dálmata", "Labrador", "Chihuahua", "Pug", "Pastor Alemán" }; // Mock de avatares

    [Header("Feedback & Status")]
    public TMP_Text statusLabel;
    public GameObject loadingPanel;
    
    // --- Datos de Razas (Mock para el ejemplo) ---
    private readonly Dictionary<string, List<string>> breedCategories = new Dictionary<string, List<string>>()
    {
        {"Pequeña", new List<string> {"Chihuahua", "Pug", "Yorkshire Terrier"}},
        {"Mediana", new List<string> {"Beagle", "Basset Hound", "Bulldog"}},
        {"Grande", new List<string> {"Pastor Alemán", "Golden Retriever", "Labrador Retriever"}}
    };
    
    // --- Inicialización ---

    private void Start()
    {
        SetFeedback("", false);
        if (loadingPanel) loadingPanel.SetActive(false);
        
        // 1. Chequeo de sesión
        if (AuthService.Instance == null || AuthService.Instance.CurrentUserId == null)
        {
            SetFeedback("Error: No hay sesión activa. Volviendo a Login.", true);
            Invoke(nameof(GoToLogin), 2.0f);
            return;
        }

        SetFeedback("¡Bienvenido! Cuéntanos sobre tu mascota.", false);

        // 2. Inicializar Dropdowns
        InitializeBreedDropdowns();
        InitializeBirthYearDropdown();
        InitializeAvatarDisplay();
    }
    
    /// <summary>
    /// Inicializa los dropdowns de Categoría y Raza.
    /// </summary>
    private void InitializeBreedDropdowns()
    {
        // 2.1 Llenar Dropdown de Categoría
        breedCategoryDropdown.ClearOptions();
        List<string> categories = new List<string>(breedCategories.Keys);
        breedCategoryDropdown.AddOptions(categories);

        // 2.2 Suscribirse al cambio de categoría para actualizar las razas
        breedCategoryDropdown.onValueChanged.AddListener(delegate {
            OnBreedCategoryChanged(breedCategoryDropdown.value);
        });

        // 2.3 Cargar la primera categoría de razas
        OnBreedCategoryChanged(0);
    }

    /// <summary>
    /// Se llama cuando se selecciona una nueva categoría de raza.
    /// </summary>
    public void OnBreedCategoryChanged(int index)
    {
        // Obtener la categoría seleccionada
        string selectedCategory = breedCategoryDropdown.options[index].text;

        // Limpiar y llenar el Dropdown de Raza
        petBreedDropdown.ClearOptions();
        if (breedCategories.ContainsKey(selectedCategory))
        {
            petBreedDropdown.AddOptions(breedCategories[selectedCategory]);
        }
    }

    /// <summary>
    /// Inicializa el dropdown de año de nacimiento (últimos 20 años).
    /// </summary>
    private void InitializeBirthYearDropdown()
    {
        petBirthYearDropdown.ClearOptions();
        List<string> years = new List<string>();
        int currentYear = DateTime.Now.Year;
        // Permite seleccionar el año actual y los 20 años anteriores
        for (int y = currentYear; y >= currentYear - 20; y--)
        {
            years.Add(y.ToString());
        }
        petBirthYearDropdown.AddOptions(years);
    }
    
    /// <summary>
    /// Inicializa la visualización del avatar.
    /// </summary>
    private void InitializeAvatarDisplay()
    {
        if (availableAvatars.Count > 0)
        {
            currentAvatarText.text = availableAvatars[currentAvatarIndex];
        }
    }

    // --- Lógica de Botones ---
    
    /// <summary>
    /// Cambia el avatar mostrado al siguiente de la lista.
    /// </summary>
    public void OnNextAvatarPressed()
    {
        currentAvatarIndex = (currentAvatarIndex + 1) % availableAvatars.Count;
        InitializeAvatarDisplay(); // Actualiza el texto
    }

    /// <summary>
    /// Cambia el avatar mostrado al anterior de la lista.
    /// </summary>
    public void OnPreviousAvatarPressed()
    {
        // Usamos una fórmula que funciona con el módulo para envolver hacia atrás
        currentAvatarIndex = (currentAvatarIndex - 1 + availableAvatars.Count) % availableAvatars.Count;
        InitializeAvatarDisplay(); // Actualiza el texto
    }

    /// <summary>
    /// Intenta crear y guardar la mascota.
    /// </summary>
    public async void OnCreatePetPressed()
    {
        // 1. Validación de entradas
        string name = petNameInput.text.Trim();
        
        if (string.IsNullOrEmpty(name))
        {
            SetFeedback("El nombre de la mascota no puede estar vacío.", true);
            return;
        }

        // Obtener la raza seleccionada del dropdown de raza
        string breed = petBreedDropdown.options[petBreedDropdown.value].text;

        // Obtener el año de nacimiento seleccionado (siempre es un string en el dropdown)
        string yearStr = petBirthYearDropdown.options[petBirthYearDropdown.value].text;
        int birthYear;

        if (!int.TryParse(yearStr, out birthYear))
        {
            SetFeedback("Error: No se pudo determinar el año de nacimiento.", true);
            return;
        }

        if (AuthService.Instance == null || AuthService.Instance.CurrentUserId == null)
        {
            SetFeedback("Error de sesión. Por favor, reinicia la sesión.", true);
            return;
        }
        
        // 2. Crear modelo y guardar
        if (loadingPanel) loadingPanel.SetActive(true);
        SetFeedback("Creando y registrando a tu mascota...", false);
        
        // Crear el modelo de datos (PetModel ahora incluye PetStatsModel)
        PetModel newPet = new PetModel(name, breed, birthYear);
        
        // Opcional: Agregar el avatar seleccionado al modelo antes de guardarlo.
        // newPet.avatarName = availableAvatars[currentAvatarIndex]; // Asumiendo que PetModel tiene 'avatarName'

        // Llamar al servicio de Firestore para guardar
        (bool success, string errorMessage) result = await FirestoreService.Instance.SavePetAsync(newPet);

        if (loadingPanel) loadingPanel.SetActive(false);

        if (result.success)
        {
            Debug.Log("[CreatePetUI] ✅ Mascota creada y guardada exitosamente.");
            SetFeedback($"¡{name} ha sido registrada! Cargando perfil...", false);
            
            // 3. Redirección a PetProfile (Usamos Constants)
            await Task.Delay(1000); 
            SceneManager.LoadScene(Constants.SCENE_PET_PROFILE);
        }
        else
        {
            // Muestra el error de Firestore
            SetFeedback($"Error al crear la mascota: {result.errorMessage}", true);
        }
    }
    
    // --- Funciones Auxiliares y de Estado ---

    /// <summary>
    /// Muestra un mensaje al usuario en la etiqueta de estado.
    /// </summary>
    private void SetFeedback(string message, bool isError)
    {
        if (statusLabel == null) return;
        statusLabel.text = message;
        // Opcional: Cambiar color del texto para indicar error o éxito.
        // statusLabel.color = isError ? Color.red : Color.green;
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
        // El OnAuthStateChanged de AuthService se encargará de la redirección
        // a la escena de Login, lo cual es más robusto.
    }
    
    // Puedes agregar una función para intercambiar nombre/raza si tienes un botón para eso
    public void OnSwapNameBreedPressed()
    {
        // Solo como ejemplo de funcionalidad, aunque es raro.
        // Podrías habilitar/deshabilitar campos aquí.
        SetFeedback("Funcionalidad de intercambio de campos activa, pero no implementada.", false);
    }
}