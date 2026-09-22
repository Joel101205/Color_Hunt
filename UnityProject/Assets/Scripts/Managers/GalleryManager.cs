using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Firestore;
using Firebase.Extensions;
using Firebase.Storage;
using UnityEngine;

public class GalleryManager : MonoBehaviour
{
    [SerializeField] private GalleryUIManager galleryUIManager;

    private List<MatchHistoryEntry> recentMatches = new List<MatchHistoryEntry>();
    private FirebaseFirestore db;
    private Dictionary<string, Texture2D> imageCache = new Dictionary<string, Texture2D>();
    
    private bool isPreloading = false;
    private bool dataIsReady = false;

    private void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        PreloadGalleryDataAsync();
    }
    
    private async void PreloadGalleryDataAsync()
    {
        string myId = GameSession.LocalPlayer?.firebasePlayerId;

        if (string.IsNullOrEmpty(myId) || isPreloading || dataIsReady) return;

        isPreloading = true;
        Debug.Log("Preloading Gallery...");

        try
        {
            recentMatches = await FetchLastMatchesAsync(myId, 10);
            
            await DownloadImagesForMatchesAsync(recentMatches);
            
            Debug.Log("Gallery Preload complete.");
            dataIsReady = true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Gallery Preload failed: {e}");
        }
        finally
        {
            isPreloading = false;
        }
    }

    public async void RefreshGallery()
    {
        if (dataIsReady && !isPreloading)
        {
            galleryUIManager?.PopulateGallery(recentMatches);
            return;
        }

        if (isPreloading)
        {
            Debug.Log("Gallery is still preloading in the background");
            
            while (isPreloading)
            {
                await Task.Yield();
            }

            if (dataIsReady) galleryUIManager?.PopulateGallery(recentMatches);
            return;
        }

        PreloadGalleryDataAsync(); 
        
        while (isPreloading) await Task.Yield();
        if (dataIsReady) galleryUIManager?.PopulateGallery(recentMatches);
    }

    private async Task<List<MatchHistoryEntry>> FetchLastMatchesAsync(string myId, int amount)
    {
        List<MatchHistoryEntry> combinedResults = new List<MatchHistoryEntry>();

        Query queryP1 = db.Collection("finished-games")
                          .WhereEqualTo("player1", myId)
                          .OrderByDescending("created") 
                          .Limit(amount);

        Query queryP2 = db.Collection("finished-games")
                          .WhereEqualTo("player2", myId)
                          .OrderByDescending("created")
                          .Limit(amount);

        Task<QuerySnapshot> p1Task = queryP1.GetSnapshotAsync();
        Task<QuerySnapshot> p2Task = queryP2.GetSnapshotAsync();

        await Task.WhenAll(p1Task, p2Task);

        ParseSnapshotsIntoHistoryEntries(p1Task.Result, myId, combinedResults);
        ParseSnapshotsIntoHistoryEntries(p2Task.Result, myId, combinedResults);

        return combinedResults.OrderByDescending(match => match.matchDate).Take(amount).ToList();
    }

    private void ParseSnapshotsIntoHistoryEntries(QuerySnapshot snapshot, string myId, List<MatchHistoryEntry> targetList)
    {
        foreach (DocumentSnapshot doc in snapshot.Documents)
        {
            if (!doc.Exists) continue;

            MatchHistoryEntry entry = new MatchHistoryEntry(doc.Id);

            if (doc.TryGetValue("hexCode", out string hexCode)) entry.hexCode = hexCode;
            
            if (doc.TryGetValue("created", out string createdStr))
            {
                if (DateTime.TryParse(createdStr, out DateTime parsedDate))
                {
                    entry.matchDate = parsedDate;
                }
            }

            doc.TryGetValue("player1", out string p1Id);
            doc.TryGetValue("player2", out string p2Id);

            bool iAmPlayer1 = (p1Id == myId);
            string opponentId = iAmPlayer1 ? p2Id : p1Id;
            entry.opponentIdentity = new PlayerIdentity(opponentId);

            entry.myResult = new GameResult(myId);
            entry.opponentResult = new GameResult(opponentId);

            string myPrefix = iAmPlayer1 ? "player1_" : "player2_";
            string oppPrefix = iAmPlayer1 ? "player2_" : "player1_";

            if (doc.TryGetValue(myPrefix + "score", out object myScore)) entry.myResult.scorePoints = Convert.ToSingle(myScore);
            if (doc.TryGetValue(oppPrefix + "score", out object oppScore)) entry.opponentResult.scorePoints = Convert.ToSingle(oppScore);

            if (doc.TryGetValue(myPrefix + "time", out object myTime)) entry.myResult.timeUsedMilliseconds = Convert.ToInt32(myTime);
            if (doc.TryGetValue(oppPrefix + "time", out object oppTime)) entry.opponentResult.timeUsedMilliseconds = Convert.ToInt32(oppTime);

            if (doc.TryGetValue(myPrefix + "colorAccuracy", out object myAcc)) entry.myResult.colorAccuracyPercent = Convert.ToSingle(myAcc);
            if (doc.TryGetValue(oppPrefix + "colorAccuracy", out object oppAcc)) entry.opponentResult.colorAccuracyPercent = Convert.ToSingle(oppAcc);

            if (doc.TryGetValue(myPrefix + "distance", out object myDist)) entry.myResult.distanceMetres = Convert.ToInt32(myDist);
            if (doc.TryGetValue(oppPrefix + "distance", out object oppDist)) entry.opponentResult.distanceMetres = Convert.ToInt32(oppDist);

            if (doc.TryGetValue(myPrefix + "image", out string myImg)) entry.myResult.imageStoragePath = myImg;
            if (doc.TryGetValue(oppPrefix + "image", out string oppImg)) entry.opponentResult.imageStoragePath = oppImg;

            targetList.Add(entry);
        }
    }
    
    private async Task DownloadImagesForMatchesAsync(List<MatchHistoryEntry> matches)
    {
        List<Task> downloadTasks = new List<Task>();

        foreach (MatchHistoryEntry match in matches)
        {
            if (!string.IsNullOrEmpty(match.myResult.imageStoragePath))
            {
                downloadTasks.Add(AssignTextureAsync(match.myResult, match.myResult.imageStoragePath));
            }

            if (!string.IsNullOrEmpty(match.opponentResult.imageStoragePath))
            {
                downloadTasks.Add(AssignTextureAsync(match.opponentResult, match.opponentResult.imageStoragePath));
            }
        }

        await Task.WhenAll(downloadTasks);
    }
    
    private async Task AssignTextureAsync(GameResult targetResult, string storagePath)
    {
        if (imageCache.TryGetValue(storagePath, out var imageTex))
        {
            targetResult.localPhotoTexture = imageTex;
            return;
        }

        try
        {
            byte[] imageBytes = await FirebaseUtil.DownloadImageAsync(storagePath);

            if (imageBytes != null && imageBytes.Length > 0)
            {
                Texture2D downloadedTex = new Texture2D(2, 2);
                downloadedTex.LoadImage(imageBytes);
                
                imageCache[storagePath] = downloadedTex; 
                
                targetResult.localPhotoTexture = downloadedTex; 
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Error processing image bytes for {storagePath}: {e}");
        }
    }
}