using UnityEngine;
using UnityEngine.UI;

public class GlobalButtonAnimator : MonoBehaviour
{
    void Start()
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsSortMode.None);
        foreach (var button in buttons)
        {
            if (button.GetComponent<AnimatedButton>() == null)
            {
                button.gameObject.AddComponent<AnimatedButton>();
            }
        }
    }
}
