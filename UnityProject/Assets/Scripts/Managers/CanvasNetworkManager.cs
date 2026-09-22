using UnityEngine;
using Firebase.Firestore;
using Firebase.Functions;
using Firebase.Storage;
using Firebase.Auth;
using Firebase.Extensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class CanvasNetworkManager : MonoBehaviour
{
    [Header("References")]
    public CanvasUIManager uiManager;

    private FirebaseFirestore db;
    private FirebaseFunctions functions;
    private FirebaseStorage storage;
    
    private ListenerRegistration listener;
    private Timestamp lastUpdateTime;
    
    // Safety limit for downloading the image
    private const long MAX_ALLOWED_SIZE = 5 * 1024 * 1024; 
    
    // Timeout setting in milliseconds
    private const int NETWORK_TIMEOUT_MS = 10000; 
    
    // Retry settings
    private const int MAX_RETRIES = 3;
    private const int DELAY_BETWEEN_RETRIES_MS = 2000;
    
    private bool isUploading = false;
    private bool pendingDownload = false;

    private async void Start()
    {
        try
        {
            db = FirebaseFirestore.DefaultInstance;
            functions = FirebaseFunctions.DefaultInstance;
            storage = FirebaseStorage.DefaultInstance;
        
            await FirebaseUtil.FetchPlayerInventory(GameSession.LocalPlayer.firebasePlayerId);
        
            StartListeningForCanvasChanges();
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

    private void StartListeningForCanvasChanges()
    {
        DocumentReference docRef = db.Collection("canvas").Document("global-canvas");

        listener = docRef.Listen(snapshot => 
        {
            if (snapshot.Exists && snapshot.TryGetValue("lastChanged", out Timestamp updatedTime))
            {
                if (updatedTime.CompareTo(lastUpdateTime) > 0)
                {
                    lastUpdateTime = updatedTime;
                    
                    // prevent download during upload
                    if (isUploading)
                    {
                        pendingDownload = true;
                        Debug.Log("Download queued because local player is currently uploading.");
                    }
                    else
                    {
                        DownloadCanvasFromStorage();
                    }
                }
            }
        });
    }

    private async void DownloadCanvasFromStorage()
    {
        uiManager.SetNetworkBusy(true);

        StorageReference canvasRef = storage.GetReference("canvas/globalCanvas.png");

        bool success = false;
        int currentTry = 0;

        while (currentTry < MAX_RETRIES && !success)
        {
            currentTry++;
            
            Task<byte[]> downloadTask = canvasRef.GetBytesAsync(MAX_ALLOWED_SIZE);
            Task timeoutTask = Task.Delay(NETWORK_TIMEOUT_MS);

            Task completedTask = await Task.WhenAny(downloadTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                Debug.LogWarning($"Download timeout on attempt {currentTry}.");
            }
            else if (downloadTask.IsFaulted || downloadTask.IsCanceled)
            {
                Debug.LogWarning($"Download failed on attempt {currentTry}: {downloadTask.Exception}");
            }
            else
            {
                byte[] imageBytes = downloadTask.Result;
                uiManager.LoadCanvasFromBytes(imageBytes);
                Debug.Log("Successfully downloaded new canvas.png from Storage!");
                success = true; 
            }

            if (!success && currentTry < MAX_RETRIES)
            {
                await Task.Delay(DELAY_BETWEEN_RETRIES_MS);
            }
        }

        if (!success)
        {
            Debug.LogError("All download attempts failed. Please check your internet connection.");
        }

        uiManager.SetNetworkBusy(false);
    }

    public async Task<bool> UploadPixel(int xCoord, int yCoord, Color color)
    {
        isUploading = true;
        uiManager.SetNetworkBusy(true);

        string playerId = GameSession.LocalPlayer.firebasePlayerId;
        string hexCode = "#" + ColorUtility.ToHtmlStringRGB(color);

        var data = new Dictionary<string, object>
        {
            { "playerId", playerId },
            { "xCoord", xCoord },
            { "yCoord", yCoord },
            { "hexCode", hexCode }
        };

        bool success = false;
        int currentTry = 0;

        while (currentTry < MAX_RETRIES && !success)
        {
            currentTry++;

            try
            {
                Task<HttpsCallableResult> uploadTask = functions.GetHttpsCallable("placePixelOnGlobalCanvas").CallAsync(data);
                Task timeoutTask = Task.Delay(NETWORK_TIMEOUT_MS);

                Task completedTask = await Task.WhenAny(uploadTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    Debug.LogWarning($"Upload timeout on attempt {currentTry}.");
                }
                else if (uploadTask.IsFaulted || uploadTask.IsCanceled)
                {
                    Debug.LogWarning($"Cloud Function failed on attempt {currentTry}: {uploadTask.Exception}");
                }
                else
                {
                    Debug.Log("Successfully sent pixel to Cloud Function!");
                    success = true;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Cloud Function error on attempt {currentTry}: {e.Message}");
            }

            if (!success && currentTry < MAX_RETRIES)
            {
                await Task.Delay(DELAY_BETWEEN_RETRIES_MS);
            }
        }

        isUploading = false;
        uiManager.SetNetworkBusy(false); 

        if (!success)
        {
            Debug.LogError("All upload attempts failed. Reverting local changes.");

            GameSession.LocalPlayer.colorInventory.AddPixelAmount(ColorInventory.ColorToHex(color), 1);
        }
        else if (pendingDownload)
        {
            pendingDownload = false;
            DownloadCanvasFromStorage();
        }

        return success;
    }

    void OnDestroy()
    {
        listener?.Stop();
    }
}