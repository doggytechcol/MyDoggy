// Este archivo debe estar en Assets/Scripts/
public static class Constants 
{
    // --- Escenas ---
    // Agregamos la escena Boot
    public const string SCENE_BOOT = "Boot";
    public const string SCENE_LOGIN = "Login";
    public const string SCENE_CREATE_PET = "CreatePet";
    public const string SCENE_PET_PROFILE = "PetProfile";

    // --- Validación ---
    public const int MIN_PASSWORD_LENGTH = 8; // Mínimo 8
    
    // Regex: Mínimo 8 caracteres, al menos 1 mayúscula, 1 minúscula y 1 número.
    // (Los caracteres especiales son opcionales)
    public const string PASSWORD_REGEX = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$";
}