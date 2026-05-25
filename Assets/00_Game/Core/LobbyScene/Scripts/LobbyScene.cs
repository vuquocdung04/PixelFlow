using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class LobbyScene : MonoBehaviour
{
    public NavController navController;
    public Button btnHeart;
    public async UniTask InitAsync()
    {           
        navController.Init();

        await PreLoad();

        btnHeart.OnClicked(delegate
        {
            HeartManager.Instance.TryShowHeartOffer(LobbyController.Instance.midCanvas);
        });
    }

    private static async UniTask PreLoad()
    {
        var lobbyTcs = new UniTaskCompletionSource();
        var shopTcs = new UniTaskCompletionSource();
        var rankTcs = new UniTaskCompletionSource();
        var holder = LobbyController.Instance.botCanvas;
        _ = LobbyBox.Setup(holder, box =>
        {
            box.Show();
            lobbyTcs.TrySetResult();
        });

        _ = ShopBox.Setup(holder, _ => shopTcs.TrySetResult());

        _ = RankBox.Setup(holder, _ => rankTcs.TrySetResult());
        
        await UniTask.WhenAll(lobbyTcs.Task, shopTcs.Task, rankTcs.Task);
        
        FXManager.Instance.isNextSceneReady = true;
    }
}