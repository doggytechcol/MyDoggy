// Este archivo debe estar en Assets/Scripts/

// CRÍTICO: La clase debe ser estática para ser accesible globalmente sin instanciar.
public static class Constants 
{
    // --- Escenas ---
    public const string SCENE_BOOT = "Boot";
    public const string SCENE_LOGIN = "Login";
    public const string SCENE_CREATE_PET = "CreatePet";
    public const string SCENE_MAP = "Map";
    public const string SCENE_PET_PROFILE = "PetProfile";

    // --- Validación ---
    // CRÍTICO: 'const' ya implica 'static', pero la clase debe ser estática también.
    public const int MIN_PASSWORD_LENGTH = 8; // Mínimo de 8 caracteres.
    
    // Regex: Mínimo 8 caracteres, al menos 1 mayúscula, 1 minúscula y 1 número.
    // (Los caracteres especiales son opcionales)
    public const string PASSWORD_REGEX = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$";
}