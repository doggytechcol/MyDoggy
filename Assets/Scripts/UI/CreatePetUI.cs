using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

public class CreatePetUI : MonoBehaviour
{
    [Header("Name")]
    public TMP_InputField petNameInput;

    [Header("Size Category")]
    public TMP_Dropdown sizeDropdown;

    [Header("Breed Navigation")]
    public TMP_Text breedNameLabel;
    public Image avatarDisplay;
    public Button prevBreedButton;
    public Button nextBreedButton;

    [Header("Gender")]
    public Toggle maleToggle;
    public Toggle femaleToggle;

    [Header("Birthdate")]
    public TMP_Dropdown dayDropdown;
    public TMP_Dropdown monthDropdown;
    public TMP_Dropdown yearDropdown;

    [Header("Actions")]
    public Button createButton;
    public Button logoutButton;

    [Header("Data")]
    public List<BreedDefinition> smallBreeds;
    public List<BreedDefinition> mediumBreeds;
    public List<BreedDefinition> largeBreeds;
    public List<BreedDefinition> giantBreeds;

    // Estado dinámico
    private List<BreedDefinition> currentBreedList;
    private int currentBreedIndex = 0;
    private int currentAvatarIndex = 0;


    private void Start()
    {
        LoadBreedsFromResources();
        SetupBirthdateDropdowns();

        // Listeners
        sizeDropdown.onValueChanged.AddListener(OnSizeChanged);
        prevBreedButton.onClick.AddListener(OnPrevBreed);
        nextBreedButton.onClick.AddListener(OnNextBreed);
        createButton.onClick.AddListener(OnCreatePressed);
        logoutButton.onClick.AddListener(OnLogoutPressed);

        // Inicializar
        OnSizeChanged(sizeDropdown.value);
    }


    // ===============================
    // ✅ CARGA DE RAZAS (ScriptableObject)
    // ===============================

    private void LoadBreedsFromResources()
    {
        smallBreeds = new List<BreedDefinition>(Resources.LoadAll<BreedDefinition>("Breeds/Small"));
        mediumBreeds = new List<BreedDefinition>(Resources.LoadAll<BreedDefinition>("Breeds/Medium"));
        largeBreeds = new List<BreedDefinition>(Resources.LoadAll<BreedDefinition>("Breeds/Large"));
        giantBreeds = new List<BreedDefinition>(Resources.LoadAll<BreedDefinition>("Breeds/Giant"));
    }


    // ===============================
    // ✅ CONFIGURACIÓN DE FECHA
    // ===============================

    private void SetupBirthdateDropdowns()
    {
        // Día
        dayDropdown.ClearOptions();
        List<string> days = new List<string>();
        for (int d = 1; d <= 31; d++) days.Add(d.ToString());
        dayDropdown.AddOptions(days);

        // Mes
        monthDropdown.ClearOptions();
        List<string> months = new List<string>();
        for (int m = 1; m <= 12; m++) months.Add(m.ToString());
        monthDropdown.AddOptions(months);

        // Año
        yearDropdown.ClearOptions();
        List<string> years = new List<string>();
        int currentYear = DateTime.Now.Year;
        for (int y = currentYear; y >= currentYear - 25; y--) years.Add(y.ToString());
        yearDropdown.AddOptions(years);
    }


    // ===============================
    // ✅ CAMBIO DE CATEGORÍA (TAMAÑO)
    // ===============================

    private void OnSizeChanged(int index)
    {
        switch (index)
        {
            case 0: currentBreedList = smallBreeds; break;
            case 1: currentBreedList = mediumBreeds; break;
            case 2: currentBreedList = largeBreeds; break;
            case 3: currentBreedList = giantBreeds; break;
        }

        currentBreedIndex = 0;
        currentAvatarIndex = 0;

        UpdateBreedDisplay();
    }


    // ===============================
    // ✅ NAVEGACIÓN ENTRE RAZAS
    // ===============================

    private void OnPrevBreed()
    {
        if (currentBreedList == null || currentBreedList.Count == 0) return;

        currentBreedIndex = (currentBreedIndex - 1 + currentBreedList.Count) % currentBreedList.Count;
        currentAvatarIndex = 0;

        UpdateBreedDisplay();
    }

    private void OnNextBreed()
    {
        if (currentBreedList == null || currentBreedList.Count == 0) return;

        currentBreedIndex = (currentBreedIndex + 1) % currentBreedList.Count;
        currentAvatarIndex = 0;

        UpdateBreedDisplay();
    }


    // ===============================
    // ✅ ACTUALIZAR PANTALLA (RAZA + AVATAR)
    // ===============================

    private void UpdateBreedDisplay()
    {
        if (currentBreedList == null || currentBreedList.Count == 0)
        {
            breedNameLabel.text = "Sin razas disponibles";
            avatarDisplay.sprite = null;
            return;
        }

        var breed = currentBreedList[currentBreedIndex];
        breedNameLabel.text = breed.breedName;

        if (breed.avatars == null || breed.avatars.Count == 0)
        {
            avatarDisplay.sprite = null;
            return;
        }

        if (currentAvatarIndex >= breed.avatars.Count)
            currentAvatarIndex = 0;

        avatarDisplay.sprite = breed.avatars[currentAvatarIndex].sprite;
    }


    // ===============================
    // ✅ CREAR MASCOTA
    // ===============================

    public async void OnCreatePressed()
    {
        // Nombre
        string petName = petNameInput.text.Trim();
        if (string.IsNullOrEmpty(petName))
        {
            Debug.LogWarning("⚠ Debes escribir un nombre para tu mascota.");
            return;
        }

        // Género
        string gender = maleToggle.isOn ? "male" : "female";

        // Raza
        if (currentBreedList == null || currentBreedList.Count == 0)
        {
            Debug.LogError("⚠ No hay razas cargadas.");
            return;
        }

        var breed = currentBreedList[currentBreedIndex];

        if (breed.avatars.Count == 0)
        {
            Debug.LogError("⚠ La raza seleccionada no tiene avatares.");
            return;
        }

        string avatarId = breed.avatars[currentAvatarIndex].id;

        // Fecha de nacimiento
        int day = int.Parse(dayDropdown.options[dayDropdown.value].text);
        int month = int.Parse(monthDropdown.options[monthDropdown.value].text);
        int year = int.Parse(yearDropdown.options[yearDropdown.value].text);

        DateTime birth = new DateTime(year, month, day);
        if (birth > DateTime.Now)
        {
            Debug.LogError("⚠ No puedes elegir una fecha futura.");
            return;
        }

        // Crear modelo
        PetModel pet = new PetModel(
            petName,
            breed.breedName,
            gender,
            avatarId,
            day,
            month,
            year
        );

        // Guardar en Firestore
        await FirestoreService.Instance.SavePetAsync(pet);

        Debug.Log("✅ Mascota creada correctamente");

        UnityEngine.SceneManagement.SceneManager.LoadScene("PetProfile");
    }


    // ===============================
    // ✅ LOGOUT
    // ===============================

    private void OnLogoutPressed()
    {
        AuthService.Instance.Logout();
    }
}
