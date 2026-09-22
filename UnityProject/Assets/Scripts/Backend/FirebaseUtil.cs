using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Firebase.Storage;
using System.Threading.Tasks;
using Firebase.Firestore;
using Firebase.Functions;

public static class FirebaseUtil
{
    public static async Task UploadImage(string playerId, string gameId, byte[] imageBytes)
    {
        FirebaseStorage storage = FirebaseStorage.DefaultInstance;

        string imageStoragePath = $"players/{playerId}/photos/{gameId}.png";

        StorageReference storageRef = storage
            .GetReference(imageStoragePath);

        await storageRef.PutBytesAsync(imageBytes);

        GameSession.OwnResult.imageStoragePath = imageStoragePath;
        
        Uri downloadUrl = await storageRef.GetDownloadUrlAsync();

        Debug.Log($"Uploaded: {downloadUrl}");
    }
    
    public static async Task<byte[]> DownloadImageAsync(string storagePath)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
        {
            Debug.LogError("Download failed: storage path is empty.");
            return null;
        }

        FirebaseStorage storage = FirebaseStorage.DefaultInstance;
        StorageReference storageRef = storage.GetReference(storagePath);

        try
        {
            // Set a maximum download size of 4 MB
            const long maxAllowedSize = 4 * 1024 * 1024; 
            
            byte[] imageBytes = await storageRef.GetBytesAsync(maxAllowedSize);
            return imageBytes;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to download image at {storagePath}: {e.Message}");
            return null; 
        }
    }
    
    public static async Task<string> GetUserNameAsync(string firebaseUid)
    {
        if (string.IsNullOrWhiteSpace(firebaseUid))
        {
            Debug.LogError("Cannot fetch username: provided UID is empty.");
            return "Anonymous Player";
        }

        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        
        DocumentReference docRef = db.Collection("players").Document(firebaseUid);

        try
        {
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

            if (snapshot.Exists)
            {
                if (snapshot.TryGetValue("userName", out string userName))
                {
                    return userName;
                }
                else
                {
                    Debug.LogWarning($"Document found for {firebaseUid}, but 'userName' field is missing.");
                    return "Anonymous Player";
                }
            }
            else
            {
                Debug.LogWarning($"No player document found for UID: {firebaseUid}");
                return "Anonymous Player";
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to fetch username: {e.Message}");
            return "Anonymous Player";
        }
    }

    public static async Task<MatchSettings> GetMatchSettingsAsync()
    {
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        
        DocumentReference docRef = db.Collection("game-settings").Document("frontend-settings");
        
        try
        {
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

            if (snapshot.Exists)
            {
                if (snapshot.TryGetValue("colorCutoff", out double colorCutoff) && snapshot.TryGetValue("colorCutoffPow", out double colorCutoffPow) &&  snapshot.TryGetValue("playTimeSeconds", out int playTimeSeconds))
                {
                    return new MatchSettings(playTimeSeconds, Convert.ToSingle(colorCutoff), Convert.ToSingle(colorCutoffPow));
                }
                else
                {
                    Debug.LogWarning($"Document found for matchSettings, but fields are missing. Returning default match settings");
                    return new MatchSettings() { ColorCutoff = 0.65f, ColorCutoffPow = 1.5f, PlayTimeSeconds = 180 };
                }
            }
            else
            {
                Debug.LogWarning($"MatchSettings document could not be found. Returning default match settings");
                return new MatchSettings() { ColorCutoff = 0.65f, ColorCutoffPow = 1.5f, PlayTimeSeconds = 180 };
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to fetch match settings, returning default match settings: {e.Message}");
            return new MatchSettings() { ColorCutoff = 0.65f, ColorCutoffPow = 1.5f, PlayTimeSeconds = 180 };;
        }
    }

    public static async Task FetchPlayerInventory(string playerId)
    {
        Dictionary<string, object> data = new Dictionary<string, object>()
        {
            { "playerId", playerId },
        };
    
        try 
        {
            HttpsCallableResult result = await FirebaseFunctions.DefaultInstance.GetHttpsCallable("getPlayerPixelMap").CallAsync(data);
            
            if (GameSession.LocalPlayer == null || GameSession.LocalPlayer.colorInventory == null)
            {
                Debug.LogError("Cannot fetch inventory: GameSession.LocalPlayer is not initialized.");
                return;
            }

            if (result.Data is Dictionary<string, object> objectDict && objectDict.TryGetValue("pixels", out object pixelsObj))
            {
                if (pixelsObj is System.Collections.IDictionary pixelDict)
                {
                    foreach (System.Collections.DictionaryEntry kvp in pixelDict)
                    {
                        string hexColor = kvp.Key.ToString();
                    
                        int amount = Convert.ToInt32(kvp.Value); 

                        GameSession.LocalPlayer.colorInventory.SetPixelAmount(hexColor, amount);
                    }
                
                    Debug.Log("Player inventory successfully fetched and parsed");
                }
                else 
                {
                    Debug.LogError("The 'pixels' object from Firebase was not a readable dictionary.");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error fetching inventory from Cloud Functions: {e.Message}");
        }
    }

    public static async Task<string> GetUnityIdByUsernameAsync(string username)
    {
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        Query query = db.Collection("players").WhereEqualTo("userName", username);
        QuerySnapshot snapshot = await query.GetSnapshotAsync();

        if (snapshot.Documents.Any())
        {
            return snapshot.Documents.First().GetValue<string>("unityPlayerId"); 
        }
        return null;
    }
    
    public static async Task<string> GetFirebaseIdByUnityIdAsync(string unityPlayerId)
    {
        try
        {
            FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        
            Query query = db.Collection("players").WhereEqualTo("unityPlayerId", unityPlayerId);
            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            if (snapshot.Documents.Any())
            {
                return snapshot.Documents.First().Id;
            }
        
            Debug.LogWarning($"No Firebase document found for Unity Player ID: {unityPlayerId}");
            return null;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to fetch Firebase ID by Unity ID: {e}");
            return null;
        }
    }

    public static async Task<Dictionary<string, object>> FetchPlayerStatistics(string firebaseId)
    {
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        DocumentSnapshot snapshot = await db.Collection("players").Document(firebaseId).GetSnapshotAsync();

        if (snapshot.Exists)
        {
            if (snapshot.TryGetValue("statistics", out Dictionary<string, object> playerStatistics))
            {
                return playerStatistics;
            }
        }

        return null;
    }
}
