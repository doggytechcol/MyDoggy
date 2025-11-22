using System;
using Newtonsoft.Json;

[Serializable]
public class PetModel
{
    // Basic Data
    [JsonProperty("name")]
    public string name;

    [JsonProperty("breed")]
    public string breed;

    [JsonProperty("gender")]
    public string gender;

    [JsonProperty("avatarId")]
    public string avatarId;

    [JsonProperty("birthDay")]
    public int birthDay;

    [JsonProperty("birthMonth")]
    public int birthMonth;

    [JsonProperty("birthYear")]
    public int birthYear;

    // Metadata
    [JsonProperty("creationTimestamp")]
    public long creationTimestamp;

    // Game Progress
    [JsonProperty("level")]
    public int level;

    [JsonProperty("xp")]
    public int xp;

    // Stats (hunger, energy, etc.)
    [JsonProperty("stats")]
    public PetStatsModel stats;

    // Last scene visited by the pet
    [JsonProperty("lastScene")]
    public string lastScene;

    // Constructor used when creating the pet for the first time
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

        creationTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        level = 1;
        xp = 0;

        stats = new PetStatsModel();

        // Default scene when no last scene was stored yet
        lastScene = "Map";
    }

    // Empty constructor required for JSON deserialization
    public PetModel() { }
}

