using UnityEngine;

public partial class LevelController : StaffSingleton<LevelController>
{
    public override void Init()
    {
        GenerateTop();
        GenerateBottom();
    }
}