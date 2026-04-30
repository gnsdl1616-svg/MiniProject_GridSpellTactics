using UnityEngine;

public abstract class BaseSceneEntryPoint : MonoBehaviour
{
    private bool isInitialized;

    protected virtual void Start()
    {
        TryInitializeScene();
    }

    public void TryInitializeScene()
    {
        if (isInitialized)
            return;

        isInitialized = true;
        OnInitializeScene();
    }

    protected abstract void OnInitializeScene();
}
