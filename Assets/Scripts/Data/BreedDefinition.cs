using System;
using System.Collections.Generic;
using UnityEngine;

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
    public Sprite sprite;
}

public enum BreedSize
{
    Pequeño,
    Mediano,
    Grande,
    Gigante
}
