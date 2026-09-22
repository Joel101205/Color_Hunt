using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUIManager : MonoBehaviour
{
    public void OnCanvasButtonClick()
    {
        SceneManager.LoadSceneAsync("Canvas");
    }
}
