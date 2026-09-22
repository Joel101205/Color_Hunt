using System;
using UnityEngine;
#if UNITY_EDITOR
public class DevScreenManager : MonoBehaviour
{
    [SerializeField] private int pixelsToAdd = 1;
    [SerializeField] private Color colorToAdd = Color.blue;
    
    public async void OnDeleteAllActiveGamesClicked()
    {
        try
        {
            Debug.Log("Starting deletion process...");
        
            await FirebaseDebugTools.DeleteAllGames("active-games");
        
            Debug.Log("Deletion finished!");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to delete collection: {e.Message}");
        }
    }
    
    public async void OnDeleteAllFinishedGamesClicked()
    {
        try
        {
            Debug.Log("Starting deletion process...");
        
            await FirebaseDebugTools.DeleteAllGames("finished-games");
        
            Debug.Log("Deletion finished!");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to delete collection: {e.Message}");
        }
    }
    
    public async void OnDeleteAllGuestPlayersClicked()
    {
        try
        {
            Debug.Log("Starting deletion process...");
        
            await FirebaseDebugTools.DeleteAllGuestPlayers();
        
            Debug.Log("Deletion finished!");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to delete collection: {e.Message}");
        }
    }

    public async void OnAddPixelsToInventoryClicked()
    {
        try
        {
            await FirebaseDebugTools.DebugAddPixels(colorToAdd, pixelsToAdd);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to add pixels to inventory: {e.Message}");
        }
    }
}
#endif
