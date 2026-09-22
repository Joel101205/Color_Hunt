using System;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using Firebase.Firestore;
using Firebase.Functions;

#if UNITY_EDITOR
public static class FirebaseDebugTools
{
    public static async Task DeleteAllGames(string collectionPath)
    {
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        
        CollectionReference collectionRef = db.Collection(collectionPath); 

        try
        {
            QuerySnapshot snapshot = await collectionRef.GetSnapshotAsync();
            
            int deletedCount = 0;
            
            WriteBatch batch = db.StartBatch();

            foreach (DocumentSnapshot document in snapshot.Documents)
            {
                batch.Delete(document.Reference);
                deletedCount++;

                // Firestore has a hard limit of 500 operations per batch.
                if (deletedCount % 500 == 0)
                {
                    await batch.CommitAsync();
                    batch = db.StartBatch(); 
                }
            }
            
            if (deletedCount % 500 != 0)
            {
                await batch.CommitAsync();
            }

            Debug.Log($"Successfully deleted {deletedCount} documents from the collection.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to delete collection: {e.Message}");
        }
    }
    
    public static async Task DeleteAllGuestPlayers()
    {
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        CollectionReference collectionRef = db.Collection("players");
    
        try
        {
            QuerySnapshot snapshot = await collectionRef.GetSnapshotAsync();
        
            int deletedCount = 0;
        
            WriteBatch batch = db.StartBatch();

            foreach (DocumentSnapshot document in snapshot.Documents)
            {
                if (!document.TryGetValue("userName", out string userName) || !userName.StartsWith("Guest_")) continue;
                
                batch.Delete(document.Reference);
                deletedCount++;

                // Firestore has a hard limit of 500 operations per batch.
                if (deletedCount % 500 == 0)
                {
                    await batch.CommitAsync();
                    batch = db.StartBatch(); 
                }
            }
        
            // Only commit the final batch if there were deletions made
            if (deletedCount > 0 && deletedCount % 500 != 0)
            {
                await batch.CommitAsync();
            }

            Debug.Log($"Successfully deleted {deletedCount} guest documents from the collection.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to delete guest players: {e.Message}");
        }
    }
    
    public static async Task DebugAddPixels(Color colorToAdd, int amountToAdd)
    {
        string playerId = GameSession.LocalPlayer.firebasePlayerId;
        string hexColor = "#" + ColorUtility.ToHtmlStringRGB(colorToAdd);

        Debug.Log($"Writing {amountToAdd} pixels of {hexColor} directly to Firestore for player {playerId}...");

        try
        {
            FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
            DocumentReference playerDocRef = db.Collection("players").Document(playerId);
            
            Dictionary<string, object> updates = new Dictionary<string, object>
            {
                {
                    "pixels", new Dictionary<string, object>
                    {
                        { hexColor, FieldValue.Increment(amountToAdd) }
                    }
                }
            };

            await playerDocRef.SetAsync(updates, SetOptions.MergeAll);
            
            GameSession.LocalPlayer.colorInventory.AddPixelAmount(ColorInventory.ColorToHex(colorToAdd), amountToAdd);
            
            Debug.Log("Successfully updated Firestore and local inventory!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Firestore write failed: {e.Message}");
        }
    }
}
    

#endif
