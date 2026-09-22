using UnityEngine;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class PermissionManager : MonoBehaviour
{
    private void Start()
    {
        #if UNITY_ANDROID
        RequestAndroidPermissions();
        #endif
    }

#if UNITY_ANDROID
    private void RequestAndroidPermissions()
    {
        string[] requiredPermissions = new string[]
        {
            Permission.Camera,
            Permission.FineLocation
        };
        
        bool permissionsMissing = false;
        foreach (string permission in requiredPermissions)
        {
            if (!Permission.HasUserAuthorizedPermission(permission))
            {
                permissionsMissing = true;
                break;
            }
        }
        
        if (permissionsMissing)
        {
            Debug.Log("Requesting Camera and Location permissions...");
            Permission.RequestUserPermissions(requiredPermissions);
        }
        else
        {
            Debug.Log("All permissions are already granted.");
        }
    }
#endif
}