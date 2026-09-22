using System.Collections;
using UnityEngine;

public class LocationTrackerManager : MonoBehaviour
{
    [Header("GPS Settings")]
    [SerializeField] private float desiredAccuracyMeters = 1f;
    [SerializeField] private float updateDistanceMeters = 0.1f;

    [Header("Validation Settings")]
    [SerializeField] private float maxSpeedMetersPerSecond = 8f; 
    
    [SerializeField] private float maxAcceptableAccuracy = 20f; 
    
    [SerializeField] private float maxJumpDistanceMeters = 50f; 

    public bool isInitialized { get; private set; }
    
    private LocationInfo _startLocation;
    private LocationInfo _lastValidLocation;
    private bool _hasFoundGoodStartLocation = false;

    public IEnumerator InitializeGps()
    {
        if (!Input.location.isEnabledByUser)
        {
            Debug.LogError("GPS/location is disabled by the user.");
            yield break;
        }

        Input.location.Start(desiredAccuracyMeters, updateDistanceMeters);

        int maxWaitSeconds = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWaitSeconds > 0)
        {
            yield return new WaitForSeconds(1f);
            maxWaitSeconds--;
        }

        if (Input.location.status != LocationServiceStatus.Running)
        {
            Debug.LogError("GPS could not be started. Status: " + Input.location.status);
            yield break;
        }

        StartCoroutine(TrackLocationRoutine());
    }

    private IEnumerator TrackLocationRoutine()
    {
        while (Input.location.status == LocationServiceStatus.Running)
        {
            LocationInfo newLocation = Input.location.lastData;

            if (newLocation.horizontalAccuracy > 0 && newLocation.horizontalAccuracy <= maxAcceptableAccuracy)
            {
                if (!_hasFoundGoodStartLocation)
                {
                    _startLocation = newLocation;
                    _lastValidLocation = _startLocation;
                    _hasFoundGoodStartLocation = true;
                    isInitialized = true;
                    Debug.Log($"Clean start GPS position saved: {_startLocation.latitude}, {_startLocation.longitude}");
                }
                else if (newLocation.timestamp > _lastValidLocation.timestamp)
                {
                    ValidateAndUpdateLocation(newLocation);
                }
            }

            yield return new WaitForSeconds(1f);
        }
    }

    private void ValidateAndUpdateLocation(LocationInfo newLocation)
    {
        float distanceMeters = CalculateDistanceMeters(
            _lastValidLocation.latitude, _lastValidLocation.longitude, 
            newLocation.latitude, newLocation.longitude);
            
        float timeElapsedSeconds = (float)(newLocation.timestamp - _lastValidLocation.timestamp);

        if (timeElapsedSeconds > 0)
        {
            float speedMetersPerSecond = distanceMeters / timeElapsedSeconds;

            if (speedMetersPerSecond <= maxSpeedMetersPerSecond && distanceMeters <= maxJumpDistanceMeters)
            {
                _lastValidLocation = newLocation;
            }
            else
            {
                Debug.LogWarning($"Rejected jump. Distance: {distanceMeters}m over {timeElapsedSeconds}s. Speed: {speedMetersPerSecond}m/s");
            }
        }
    }

    public float GetCurrentDistanceMeters()
    {
        if (!isInitialized) return 0f;

        return CalculateDistanceMeters(_startLocation.latitude, _startLocation.longitude, _lastValidLocation.latitude, _lastValidLocation.longitude);
    }

    private float CalculateDistanceMeters(double latA, double lonA, double latB, double lonB)
    {
        const double earthRadiusMeters = 6371000.0;
        double latARad = latA * Mathf.Deg2Rad;
        double latBRad = latB * Mathf.Deg2Rad;
        double deltaLat = (latB - latA) * Mathf.Deg2Rad;
        double deltaLon = (lonB - lonA) * Mathf.Deg2Rad;

        double a = Mathf.Sin((float)(deltaLat / 2)) * Mathf.Sin((float)(deltaLat / 2)) +
                   Mathf.Cos((float)latARad) * Mathf.Cos((float)latBRad) *
                   Mathf.Sin((float)(deltaLon / 2)) * Mathf.Sin((float)(deltaLon / 2));

        double c = 2.0 * Mathf.Atan2(Mathf.Sqrt((float)a), Mathf.Sqrt((float)(1.0 - a)));
        return (float)(earthRadiusMeters * c);
    }

    private void OnDestroy()
    {
        if (Input.location.status == LocationServiceStatus.Running)
        {
            Input.location.Stop();
        }
    }
}