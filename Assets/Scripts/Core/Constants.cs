// Este archivo debe estar en Assets/Scripts/ y NO debe ser un MonoBehaviour.

/// <summary>
/// Contiene todas las constantes globales de la aplicación, como nombres de escenas,
/// reglas de validación y nombres de colecciones de la base de datos.
/// </summary>
public static class Constants 
{
    // --- NOMBRES DE ESCENA (CRÍTICO para AuthService) ---
    public const string SCENE_LOGIN = "Login"; // Nombre de la escena de inicio de sesión
    public const string SCENE_CREATE_PET = "CreatePet"; // Nombre de la escena de creación de la mascota
    public const string SCENE_PET_PROFILE = "PetProfile"; // Nombre de la escena principal del perfil

    // --- Validación de Contraseña ---
    public const int MIN_PASSWORD_LENGTH = 8; 
    
    // Regex: Mínimo 8 caracteres, al menos 1 mayúscula, 1 minúscula y 1 número.
    public const string PASSWORD_REGEX = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$";
}