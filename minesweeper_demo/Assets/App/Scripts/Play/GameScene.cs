using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UniRx.Triggers;
using Utils;
using DG.Tweening;
using System.Linq;

public class GameScene : MonoBehaviour
{
    // temp solution
    public static Data.MineData data;

    // 9 x 9
    private Dictionary<Tuple<int,int>, int> bombMap = new Dictionary<Tuple<int, int>, int>(10);

    [SerializeField]
    private Transform coverBase;
    [SerializeField]
    private Transform lowerBase;
    [SerializeField]
    private GameObject coverPrefab;
    [SerializeField]
    private GameObject basePrefab;
    [SerializeField]
    private GameObject openParticlePrefab;
    [SerializeField]
    private GameObject bombParticlePrefab;
    [SerializeField]
    private GameObject plasmaParticlePrefab;
    [SerializeField]
    private GameObject largeExploParticlePrefab;

    [SerializeField]
    private Image flashBangImg;
    [SerializeField]
    private Image freezeBangImage;

    /// <summary>
    ///  Game Data
    /// </summary>
    [SerializeField]
    private int flagCount = 10;
    [SerializeField]
    private long timeLeft = 60 * 2; // 2 mins
    [SerializeField]
    private bool gameOver = false;

	/// <summary>
	/// Player Object
	/// FIXME : get from data
	/// </summary>
	private PlayerObject playerObject = new PlayerObject();

	[SerializeField]
	private Text flagCountLabel;
	[SerializeField]
	private Text timer;
	[SerializeField]
	private Image hpGauge;

    private CoverObject[,] coverList = new CoverObject[9,9];
    private FieldObject[,] fieldList = new FieldObject[9,9];

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

	void Start ()
    {
	    // fill in
//        for (int i = 0; i < 10; ++i)
//        {
//            while (true)
//            {
//                var randX = Random.Range(0, 9);
//                var randY = Random.Range(0, 9);
//                var key = Tuple.Create(randX, randY);
//
//                if (bombMap.ContainsKey(key))
//                    continue;
//
//                bombMap[key] = true;
//                break;
//            }
//        }

        timeLeft = data.solve_time;
        var d = data.data.Split(new char[] { ',' });

        foreach (var i in d)
        {
            if (i.Length == 0 || !i.Contains(":"))
                continue;
            
            var s = i.Split(new char[] { ':' });
            var val = int.Parse(s[2]);
            var key = Tuple.Create(int.Parse(s[0]), int.Parse(s[1]));

            if (bombMap.ContainsKey(key))
                continue;

            bombMap[key] = val;
        }

        // done setup
        var keys = bombMap.Keys;

        for (int y = 0; y < 9; ++y)
        {
            for (int x = 0; x < 9; ++x)
            {
                var b = (Instantiate(basePrefab, lowerBase) as GameObject).GetComponent<FieldObject>();
                var c = (Instantiate(coverPrefab, coverBase) as GameObject).GetComponent<CoverObject>();
                b.transform.localScale = new Vector3(1, 1, 1);
                c.transform.localScale = new Vector3(1, 1, 1);

                coverList[x, y] = c;
                fieldList[x, y] = b;
                c.pos = new CoverObject.Position(x, y);

                c.transform.OnLongPointerDownAsObservable()
                    .Where(_ => c.isActive)
                    .Subscribe(res =>
                    {
                            OnClick(res, c);
                    });
            }
        }



        var items = bombMap;
        foreach (var item in items)
        {
            var x = item.Key.Item1;
            var y = item.Key.Item2;
            fieldList[x, y].Setup(true, item.Value);

            for (int i = 0; i < 8; ++i)
            {
                var side = sides[i];
                if (x + side.Item1 < 0 || x + side.Item1 >= 9 ||
                   y + side.Item2 < 0 || y + side.Item2 >= 9)
                    continue;

                fieldList[x + side.Item1, y + side.Item2].AddCount();
            }
        }

		StartTimer ();
	}

	private void StartTimer()
	{
		timer.text = string.Format("{0:00}:{1:00}", Mathf.FloorToInt(timeLeft/60.0f), Mathf.FloorToInt(timeLeft%60.0f));

		System.IDisposable idis = null;
		idis = Observable.Interval (System.TimeSpan.FromSeconds (1))
			.Select (_ => timeLeft)
            .Where (t => t > 0 && !gameOver)
			.Subscribe (t => {
				timeLeft--;
				timer.text = string.Format("{0:00}:{1:00}", Mathf.FloorToInt(t/60.0f), Mathf.FloorToInt(t%60));

				if(timeLeft <= 0)
				{
					idis.Dispose();
					idis = null;
					Debug.Log("Done");
					timer.text = "Time up";
					GameOver();
				}
			}).AddTo(this);
	}

	private void GameOver()
	{
        gameOver = true;
        ModalObject.Popup("GameOverPopup", "", "", "")
            .OnSuccess(_ =>
            {
                    ReturnToMenu();
            });
	}

	private void UpdateHP()
	{
		var max = (float)playerObject.maxHealth;
		var cur = (float)playerObject.currentHealth;
		float rate = cur/max;

        hpGauge.DOFillAmount(rate, 0.3f);
	}

	private void UpdateFlagCount()
	{
		flagCountLabel.text = string.Format ("{0}", flagCount);
	}

    private void OnClick(bool longHold, CoverObject c)
    {
        if (!c.isActive || gameOver)
            return;

        Debug.Log(longHold);
        if (longHold)
        {
            // trying to set but no more flag
            if (flagCount <= 0 && !c.GetFlag())
                return;
			
            if (c.SetFlag())
            {
                --flagCount;
            }
            else
            {
                ++flagCount;
            }

            UpdateFlagCount();
        }
        else if (!c.GetFlag())
        {
            if (c.isFlagged)
                return;
            
            c.SetOpen(openParticlePrefab);

            var val = fieldList[c.pos.x, c.pos.y].Data;

            if (!val.Item1 &&
                val.Item2 == 0)
            {
                for (int i = 0; i < 8; ++i)
                {
                    var side = sides[i];
                    if (c.pos.x + side.Item1 < 0 || c.pos.x + side.Item1 >= 9 ||
                        c.pos.y + side.Item2 < 0 || c.pos.y + side.Item2 >= 9)
                        continue;
                   
                    Observable.NextFrame()
                        .Subscribe(_ =>
                        {
                            OnClick(false, coverList[c.pos.x + side.Item1, c.pos.y + side.Item2]);
                        });
                }
            }
            else if (val.Item1)
            {
                // bomb
                --playerObject.currentHealth;
                UpdateHP();
                var type = val.Item3;

                if(type == 0)
                    c.SetExplosion(bombParticlePrefab);
                else if(type == 1)
                    c.SetExplosion(plasmaParticlePrefab, 1.8f);
                else if(type == 2)
                    c.SetExplosion(largeExploParticlePrefab);

                if (playerObject.currentHealth <= 0)
                {
                    GameOver();
                }
                else
                {
                    switch (type)
                    {
                        case 1:
                            // plasma is freeze
                            var col = freezeBangImage.color;
                            col.a = 0;
                            freezeBangImage.color = col;
                            freezeBangImage.gameObject.SetActive(true);

                            var seq = DOTween.Sequence();
                            seq.AppendInterval(0.3f);
                            col.a = 0.5f;
                            seq.Append(freezeBangImage.DOColor(col, 0.5f));
                            col.a = 0.8f;
                            seq.Append(freezeBangImage.DOColor(col, 2.0f).SetLoops(6, LoopType.Yoyo));
                            col.a = 0.0f;
                            seq.Append(freezeBangImage.DOColor(col, 0.5f));
                            seq.AppendCallback(() =>
                            {
                                freezeBangImage.gameObject.SetActive(false);
                            });

                            break;

                        case 2:
                            // flashbang
                            col = flashBangImg.color;
                            col.a = 0;
                            flashBangImg.color = col;
                            flashBangImg.gameObject.SetActive(true);

                            seq = DOTween.Sequence();
                            seq.AppendInterval(0.3f);
                            col.a = 0.9f;
                            seq.Append(flashBangImg.DOColor(col, 0.3f));
                            col.a = 1.0f;
                            seq.Append(flashBangImg.DOColor(col, 2.0f).SetLoops(6, LoopType.Yoyo));
                            col.a = 0.0f;
                            seq.Append(flashBangImg.DOColor(col, 0.5f));
                            seq.AppendCallback(() =>
                            {
                                flashBangImg.gameObject.SetActive(false);
                            });

                            break;
                    }
                }
            }
        }
    }

    public void ReturnToMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(Config.MenuScene);
    }
}