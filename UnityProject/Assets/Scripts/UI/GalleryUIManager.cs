using System.Collections.Generic;
using UnityEngine;

public class GalleryUIManager : MonoBehaviour
{
    [Header("UI Setup")]
    [SerializeField] private GameObject galleryItemPrefab;
    [SerializeField] private Transform contentContainer;

    public void PopulateGallery(List<MatchHistoryEntry> matchHistory)
    {
        ClearGallery();

        foreach (MatchHistoryEntry match in matchHistory)
        {
            GameObject newObj = Instantiate(galleryItemPrefab, contentContainer);
            
            GalleryItemUI itemUI = newObj.GetComponent<GalleryItemUI>();
            
            if (itemUI != null)
            {
                itemUI.Initialize(match);
            }
        }
    }

    private void ClearGallery()
    {
        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }
    }
}