using UnityEngine;

public class Exit : MonoBehaviour
{
    public void QuitGame()
    {
        Debug.Log("A játék bezárása...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // 2. Kilépés a kész, buildelt alkalmazásban
            Application.Quit();
#endif
    }
}