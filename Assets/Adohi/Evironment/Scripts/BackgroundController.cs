using System.Collections.Generic;
using com.cyborgAssets.inspectorButtonPro;
using UnityEngine;
using ZombieRun.Adohi.Evironment;

public class BackgroundController : MonoBehaviour
{

    public List<BackgroundMover> backgroundMovers;
    public BackgroundObjectMover backgroundObjectMovers;


    [ProButton]
    public void MoveNextStage(int nextStage)
    {
        foreach (var backgroundMover in backgroundMovers)
        {
            backgroundMover.ChangeNextSprite(nextStage);
        }
        //backgroundObjectMovers.MoveNextStage();
    }
}
