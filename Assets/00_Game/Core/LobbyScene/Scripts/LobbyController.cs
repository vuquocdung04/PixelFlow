using UnityEngine;
using Cysharp.Threading.Tasks;
public class LobbyController : LeaderSingleton<LobbyController>
{
    public LobbyScene lobbyScene;
    [Header("UI Layers")]
    public Transform botCanvas;
    public Transform topCanvas;
    
    protected override void OnAwake()
    {
        base.OnAwake();
        Init();
    }

    private void Init()
    {
        lobbyScene.InitAsync().Forget();
        AudioManager.Instance.PlayMusic("Main Menu Music (Cover) 2");

    }
}
