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

    [Header("Variant Selector")]
    public TMP_Dropdown variantDropdown;   

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

        // LISTENERS
        sizeDropdown.onValueChanged.AddListener(OnSizeChanged);
        prevBreedButton.onClick.AddListener(OnPrevBreed);
        nextBreedButton.onClick.AddListener(OnNextBreed);
        variantDropdown.onValueChanged.AddListener(OnVariantChanged);  
        createButton.onClick.AddListener(OnCreatePressed);
        logoutButton.onClick.AddListener(OnLogoutPressed);

        // Toggle group manual
        maleToggle.onValueChanged.AddListener((isOn) => { if (isOn) femaleToggle.isOn = false; });
        femaleToggle.onValueChanged.AddListener((isOn) => { if (isOn) maleToggle.isOn = false; });

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
    // ✅ CONFIGURAR FECHA
    // ============================================================

    private void SetupBirthdateDropdowns()
    {
        dayDropdown.ClearOptions();
        monthDropdown.ClearOptions();
        yearDropdown.ClearOptions();

        List<string> days = new();
        for (int d = 1; d <= 31; d++) days.Add(d.ToString());
        dayDropdown.AddOptions(days);

        List<string> months = new();
        for (int m = 1; m <= 12; m++) months.Add(m.ToString());
        monthDropdown.AddOptions(months);

        List<string> years = new();
        int currentYear = DateTime.Now.Year;
        for (int y = currentYear; y >= currentYear - 25; y--) years.Add(y.ToString());
        yearDropdown.AddOptions(years);
    }

    // ============================================================
    // ✅ CAMBIO DE TAMAÑO
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
    // ✅ CAMBIO DE RAZA (PREV / NEXT)
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
    // ✅ CAMBIO DE VARIANTE DEL AVATAR
    // ============================================================

    public void OnVariantChanged(int index)
    {
        currentAvatarIndex = index;
        UpdateAvatarDisplay();
    }

    private void PopulateVariantDropdown(BreedDefinition breed)
    {
        variantDropdown.ClearOptions();

        List<string> names = new List<string>();
        foreach (var avatar in breed.avatars)
            names.Add(avatar.id); // Nombre visible → ID del avatar

        variantDropdown.AddOptions(names);

        currentAvatarIndex = 0;
        variantDropdown.value = 0;
        variantDropdown.RefreshShownValue();
    }

    // ============================================================
    // ✅ ACTUALIZAR RAZA Y VARIANTE
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

        if (breed.avatars == null || breed.avatars.Count == 0)
        {
            avatarDisplay.sprite = null;
            return;
        }

        // ✅ llenar el dropdown
        PopulateVariantDropdown(breed);

        // ✅ mostrar avatar inicial
        UpdateAvatarDisplay();
    }

    private void UpdateAvatarDisplay()
    {
        var breed = currentBreedList[currentBreedIndex];

        if (breed.avatars == null || breed.avatars.Count == 0) return;
        if (currentAvatarIndex >= breed.avatars.Count) currentAvatarIndex = 0;

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

        var breed = currentBreedList[currentBreedIndex];

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

        UnityEngine.SceneManagement.SceneManager.LoadScene("Map");
    }

    // ============================================================
    // ✅ LOGOUT
    // ============================================================

    public void OnLogoutPressed()
    {
        AuthService.Instance.Logout();
    }
}
