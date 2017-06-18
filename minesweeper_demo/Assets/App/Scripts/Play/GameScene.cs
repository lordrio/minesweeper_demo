using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UniRx.Triggers;

public class GameScene : MonoBehaviour
{
    // 9 x 9
    private Dictionary<Tuple<int,int>, bool> bombMap = new Dictionary<Tuple<int, int>, bool>(10);

    [SerializeField]
    private Transform coverBase;
    [SerializeField]
    private Transform lowerBase;
    [SerializeField]
    private GameObject coverPrefab;
    [SerializeField]
    private GameObject basePrefab;
    [SerializeField]
    private int flagCount = 10;
    [SerializeField]
    private long timeLeft = 60 * 2; // 2 mins

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
        for (int i = 0; i < 10; ++i)
        {
            while (true)
            {
                var randX = Random.Range(0, 9);
                var randY = Random.Range(0, 9);
                var key = Tuple.Create(randX, randY);

                if (bombMap.ContainsKey(key))
                    continue;

                bombMap[key] = true;
                break;
            }
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



        var items = bombMap.Keys;
        foreach (var item in items)
        {
            var x = item.Item1;
            var y = item.Item2;
            fieldList[x, y].Setup(true);

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

    private void OnClick(bool longHold, CoverObject c)
    {
        if (!c.isActive)
            return;

        Debug.Log(longHold);
        if(longHold)
        {
            c.SetFlag();
        }
        else
        {
            if (c.isFlagged)
                return;
            
            c.SetOpen();

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
        }
    }
}
