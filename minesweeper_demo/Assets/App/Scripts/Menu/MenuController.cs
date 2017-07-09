using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using UniRx;
using System.Linq;

public class MenuController : MonoBehaviour
{
    [SerializeField]
    private ToggleGroup toggle;
    [SerializeField]
    private MineItem togglePrefab;
    [SerializeField]
    private Button playMinefieldButton;


    public void Start()
    {
        Screen.SetResolution(480, 768, false);

        playMinefieldButton.interactable = false;
        toggle.ObserveEveryValueChanged(t => t.AnyTogglesOn())
            .DistinctUntilChanged()
            .Where(res => res)
            .Subscribe(_ =>
            {
                    playMinefieldButton.interactable = true;
            });
        
        var mod = ModalObject.Popup("LoadingPopup", "", "", "");
        ApiManager.LoadMine(res =>
            {
                mod.DismissPopup();

                foreach(var i in res.data)
                {
                    var ins = Instantiate<MineItem>(togglePrefab);
                    ins.toggle.group = toggle;
                    ins.transform.RectTrans().SetParent(toggle.transform, false);
                    ins.mineName.text = i.created_by;

                    ins.data = i;
                }
            });
    }

    public void CreateMinefield()
    {
        SceneManager.LoadScene(Config.CreateScene);
    }

    public void PlayMineField()
    {
        var t = toggle.ActiveToggles().FirstOrDefault();
        if (t == null)
            return;
        var ct = t.GetComponent<MineItem>();

        GameScene.data = ct.data;

        SceneManager.LoadScene(Config.PlayScene);
    }

    public void CustomizeCharacter()
    {
    }
}
