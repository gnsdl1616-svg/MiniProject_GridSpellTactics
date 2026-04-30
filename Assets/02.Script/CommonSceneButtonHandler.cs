using UnityEngine;

public class CommonSceneButtonHandler : MonoBehaviour
{
    /// <summary>
    /// 게임 종료 버튼용 함수
    /// - Unity Editor에서는 플레이 모드 종료
    /// - 빌드된 게임에서는 실제 종료
    /// </summary>
    public void OnClickQuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}