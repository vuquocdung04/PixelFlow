using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuitLevelBox : BaseBox<QuitLevelBox>
{
    public enum Mode { Leave, Restart }

    public Button btnClose;
    public Button btnLeave;
    public Button btnRestart;
    public TextMeshProUGUI txtTitle;

    private Mode _mode;

    protected override void Init()
    {
        btnClose.OnClicked(Close);

        btnRestart.OnClicked(delegate
        {
            // logic restart
        });

        btnLeave.OnClicked(delegate
        {
            // logic leave (về lobby)
        });
    }

    protected override void InitState()
    {
        Refresh();
    }

    public void SetupAndShow(Mode mode)
    {
        _mode = mode;
        Show();
    }

    private void Refresh()
    {
        switch (_mode)
        {
            case Mode.Leave:
                txtTitle.text = "Quit";
                btnLeave.gameObject.SetActive(true);
                btnRestart.gameObject.SetActive(false);
                break;

            case Mode.Restart:
                txtTitle.text = "Restart";
                btnLeave.gameObject.SetActive(false);
                btnRestart.gameObject.SetActive(true);
                break;
        }
    }
}