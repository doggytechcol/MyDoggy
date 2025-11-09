using System;
using Newtonsoft.Json;

[System.Serializable]
public class PetModel
{
    // --- Datos principales ---
    [JsonProperty("name")]
    public string name;

    [JsonProperty("breed")]
    public string breed;

    [JsonProperty("gender")]
    public string gender; // "male" o "female"

    [JsonProperty("avatarId")]
    public string avatarId; // ej: "border_black", "border_red", etc.

    [JsonProperty("birthDay")]
    public int birthDay;

    [JsonProperty("birthMonth")]
    public int birthMonth;

    [JsonProperty("birthYear")]
    public int birthYear;

    // --- Metadatos ---
    [JsonProperty("creationTimestamp")]
    public long creationTimestamp;

    // --- Campos gamificados ---
    [JsonProperty("level")]
    public int level;

    [JsonProperty("xp")]
    public int xp;

    [JsonProperty("stats")]
    public PetStatsModel stats;

    public PetModel(
        string name,
        string breed,
        string gender,
        string avatarId,
        int birthDay,
        int birthMonth,
        int birthYear)
    {
        this.name = name;
        this.breed = breed;
        this.gender = gender;
        this.avatarId = avatarId;

        this.birthDay = birthDay;
        this.birthMonth = birthMonth;
        this.birthYear = birthYear;

        this.creationTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        this.level = 1;
        this.xp = 0;

        this.stats = new PetStatsModel();
    }

    public PetModel() { }
}
