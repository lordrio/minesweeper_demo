using UnityEngine;
using UnityEngine.UI;
using UniRx;
using UniRx.Triggers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class CreateScene : MonoBehaviour
{
    // 9 x 9
    private Dictionary<Tuple<int,int>, int> bombMap = new Dictionary<Tuple<int, int>, int>(10);

    [SerializeField]
    private Transform lowerBase;
    [SerializeField]
    private GameObject basePrefab;

    [SerializeField]
    private Button confirmButton;
    [SerializeField]
    private Text confirmText;
    private string[] confirmStr = new string[] { "Auto Generate", "Save" };
    [SerializeField]
    private Text bombPlacementLeftText; 
    private const string placementStr = "Bomb Placement Left : {0}";

    private FieldObject[,] fieldList = new FieldObject[9,9];

    public ReactiveProperty<int> bombToPlace = new ReactiveProperty<int>(10);

    private Tuple<int,int>[] sides = new Tuple<int,int>[8]
    {
        Tuple.Create(-1, -1),
        Tuple.Create(-1, 0),
        Tuple.Create(-1, 1),

        Tuple.Create(0, -1),
        Tuple.Create(0, 1),

        Tuple.Create(1, -1),
        Tuple.Create(1, 0),
        Tuple.Create(1, 1),
    };

    private void Awake()
    {
        bombToPlace.Subscribe(v =>
            {
                confirmText.text = v == 0 ? confirmStr[1] : confirmStr[0];
                bombPlacementLeftText.text = string.Format(placementStr, v);
            });

        confirmButton.OnClickAsObservable()
            .Subscribe(_ =>
            {
                    if(bombToPlace.Value == 0)
                    {
                        var data = "";
                        foreach(var i in bombMap)
                        {
                            data += string.Format("{0}:{1}:{2},", i.Key.Item1, i.Key.Item2, i.Value);
                        }

                        // save to cloud
                        //ApiManager.SaveMine();
                        ModalObject.Popup("EnterDetail", "","","")
                            .OnSuccess(delegate(Dictionary<string, ModalComponent> component)
                                {
                                    var name = component["namefield"].FetchComponent<InputField>();
                                    var time = component["timefield"].FetchComponent<InputField>();

                                    var mod = ModalObject.Popup("LoadingPopup", "", "", "");
                                    ApiManager.SaveMine(name.text,
                                        data,
                                        time.text.Length == 0 ? 200 : int.Parse(time.text)
                                        , () =>
                                        {
                                            mod.DismissPopup();
                                        });
                                });
                    }
                    else
                    {
                        AutoFillIn();
                    }
            });
    }

    public void AutoFillIn()
    {
        bombMap.Clear();

        // fill in
        for (int i = 0; i < 10; ++i)
        {
            while (true)
            {
                var randX = Random.Range(0, 9);
                var randY = Random.Range(0, 9);
                var key = Tuple.Create(randX, randY);

                if (bombMap.ContainsKey(key))
                    continue;

                bombMap[key] = Random.Range(0, 3);
                break;
            }
        }

        bombToPlace.Value = 0;
        RebuildBombPlacement();
    }

    public void Clear()
    {
        bombMap.Clear();
        bombToPlace.Value = 10;
        RebuildBombPlacement();
    }

    private void Start ()
    {
        // done setup
        var keys = bombMap.Keys;

        for (int y = 0; y < 9; ++y)
        {
            for (int x = 0; x < 9; ++x)
            {
                var b = (Instantiate(basePrefab, lowerBase) as GameObject).GetComponent<FieldObject>();
                b.transform.localScale = new Vector3(1, 1, 1);

                fieldList[x, y] = b;
                var cx = x;
                var cy = y;
                b.transform.OnLongPointerDownAsObservable()
                    .Subscribe(res =>
                        {
                            Debug.Log("pressed" + res);
                            var key = Tuple.Create(cx, cy);
                            if (bombMap.ContainsKey(key))
                            {
                                if (++bombMap[key] >= 3)
                                {
                                    // remove
                                    bombMap.Remove(key);
                                    bombToPlace.Value++;
                                }
                            }
                            else if(bombToPlace.Value > 0)
                            {
                                bombMap[key] = 0;
                                bombToPlace.Value--;
                            }
                            else
                            {
                                return;
                            }

                            RebuildBombPlacement();
                        });
            }
        }
    }

    public void RebuildBombPlacement()
    {
        foreach (var i in fieldList)
        {
            i.Reset();
        }

        var items = bombMap;
        foreach (var item in items)
        {
            var x = item.Key.Item1;
            var y = item.Key.Item2;
            var type = item.Value;
            fieldList[x, y].Setup(true, type);

            for (int i = 0; i < 8; ++i)
            {
                var side = sides[i];
                if (x + side.Item1 < 0 || x + side.Item1 >= 9 ||
                    y + side.Item2 < 0 || y + side.Item2 >= 9)
                    continue;

                fieldList[x + side.Item1, y + side.Item2].AddCount();
            }
        }
    }

    public void ReturnToMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(Config.MenuScene);
    }
}