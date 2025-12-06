// ARCHIVO: BreedDefinition.cs (MODIFICADO)

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations; // Necesario si usas versiones anteriores, pero generalmente opcional.

[CreateAssetMenu(
    fileName = "BreedDefinition_",
    menuName = "MyDoggy/Breed Definition",
    order = 1
)]
public class BreedDefinition : ScriptableObject
{
    public string breedId;
    public string breedName;
    public BreedSize size;

    public List<AvatarOption> avatars = new List<AvatarOption>();
}

[Serializable]
public class AvatarOption
{
    public string id;
    public Sprite sprite; // Para la UI estática (la que se muestra en CreatePetUI)

    // 🔥 NUEVA PROPIEDAD CLAVE: Almacena las animaciones específicas de esta variación.
    public AnimatorOverrideController overrideController;
}

public enum BreedSize
{
    Pequeño,
    Mediano,
    Grande,
    Gigante
}
