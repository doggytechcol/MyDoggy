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

        // --- LISTENERS ---
        sizeDropdown.onValueChanged.AddListener(OnSizeChanged);
        prevBreedButton.onClick.AddListener(OnPrevBreed);
        nextBreedButton.onClick.AddListener(OnNextBreed);
        createButton.onClick.AddListener(OnCreatePressed);
        logoutButton.onClick.AddListener(OnLogoutPressed);

        // Hace que los toggles se desmarquen mutuamente
        maleToggle.onValueChanged.AddListener((isOn) => { if (isOn) femaleToggle.isOn = false; });
        femaleToggle.onValueChanged.AddListener((isOn) => { if (isOn) maleToggle.isOn = false; });

        // Inicializar
        OnSizeChanged(sizeDropdown.value);
    }


    // ============================================================
    // ✅ CARGA DE RAZAS (SCRIPTABLEOBJECTS)
    // ============================================================

    private void LoadBreedsFromResources()
    {
        smallBreeds = new List<BreedDefinition>(Resources.LoadAll<BreedDefinition>("Breeds/Small"));
        mediumBreeds = new List<BreedDefinition>(Resources.LoadAll<BreedDefinition>("Breeds/Medium"));
        largeBreeds = new List<BreedDefinition>(Resources.LoadAll<BreedDefinition>("Breeds/Large"));
        giantBreeds = new List<BreedDefinition>(Resources.LoadAll<BreedDefinition>("Breeds/Giant"));
    }


    // ============================================================
    // ✅ CONFIGURAR FECHA DE NACIMIENTO
    // ============================================================

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


    // ============================================================
    // ✅ CAMBIO DE CATEGORÍA (PEQUEÑO / MEDIANO / GRANDE / GIGANTE)
    // ============================================================

    public void OnSizeChanged(int index)
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


    // ============================================================
    // ✅ BOTONES DE NAVEGACIÓN (SIGUIENTE / ANTERIOR)
    // ============================================================

    public void OnPrevBreed()
    {
        if (currentBreedList == null || currentBreedList.Count == 0) return;

        currentBreedIndex = (currentBreedIndex - 1 + currentBreedList.Count) % currentBreedList.Count;
        currentAvatarIndex = 0;

        UpdateBreedDisplay();
    }

    public void OnNextBreed()
    {
        if (currentBreedList == null || currentBreedList.Count == 0) return;

        currentBreedIndex = (currentBreedIndex + 1) % currentBreedList.Count;
        currentAvatarIndex = 0;

        UpdateBreedDisplay();
    }


    // ============================================================
    // ✅ MOSTRAR RAZA Y AVATAR ACTUAL
    // ============================================================

    private void UpdateBreedDisplay()
    {
        if (currentBreedList == null || currentBreedList.Count == 0)
        {
            breedNameLabel.text = "Sin razas";
            avatarDisplay.sprite = null;
            return;
        }

        var breed = currentBreedList[currentBreedIndex];
        breedNameLabel.text = breed.breedName;

        // Validación de avatares
        if (breed.avatars == null || breed.avatars.Count == 0)
        {
            avatarDisplay.sprite = null;
            return;
        }

        if (currentAvatarIndex >= breed.avatars.Count)
            currentAvatarIndex = 0;

        avatarDisplay.sprite = breed.avatars[currentAvatarIndex].sprite;
    }


    // ============================================================
    // ✅ CREAR MASCOTA
    // ============================================================

    public async void OnCreatePressed()
    {
        string petName = petNameInput.text.Trim();
        if (string.IsNullOrEmpty(petName))
        {
            Debug.LogWarning("⚠ Debes escribir un nombre.");
            return;
        }

        string gender = maleToggle.isOn ? "male" : "female";

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

        int day = int.Parse(dayDropdown.options[dayDropdown.value].text);
        int month = int.Parse(monthDropdown.options[monthDropdown.value].text);
        int year = int.Parse(yearDropdown.options[yearDropdown.value].text);

        DateTime birth = new DateTime(year, month, day);
        if (birth > DateTime.Now)
        {
            Debug.LogError("⚠ Fecha futura inválida.");
            return;
        }

        PetModel pet = new PetModel(
            petName,
            breed.breedName,
            gender,
            avatarId,
            day, month, year
        );

        await FirestoreService.Instance.SavePetAsync(pet);

        Debug.Log("✅ Mascota creada correctamente");

        UnityEngine.SceneManagement.SceneManager.LoadScene("PetProfile");
    }


    // ============================================================
    // ✅ LOGOUT
    // ============================================================

    public void OnLogoutPressed()
    {
        AuthService.Instance.Logout();
    }
}
