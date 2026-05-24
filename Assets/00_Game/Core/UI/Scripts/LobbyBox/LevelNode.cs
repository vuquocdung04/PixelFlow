using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelNode : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Image imageMain;
    [SerializeField] private Image imageLight;
    [SerializeField] private TextMeshProUGUI txtLevel;
    [SerializeField] private TextMeshProUGUI txtDifficulty;
    [SerializeField] private Transform difficultyParent;

    public void Setup(int level, bool isHard, Sprite hardMain = null, Sprite hardLight = null)
    {
        txtLevel.text = level.ToString();

        if (isHard)
        {
            difficultyParent.gameObject.SetActive(true);
            txtDifficulty.text = "Hard";
            SetColor(hardMain, hardLight);
        }
        else
        {
            difficultyParent.gameObject.SetActive(false);
        }
    }

    public void SetColor(Sprite mainSprite, Sprite lightSprite)
    {
        if (mainSprite != null) imageMain.sprite = mainSprite;
        if (lightSprite != null) imageLight.sprite = lightSprite;
    }
}