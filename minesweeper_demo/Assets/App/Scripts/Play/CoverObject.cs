using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UniRx;
using UniRx.Triggers;

public class CoverObject : MonoBehaviour
{
    public struct Position
    {
        public int x;
        public int y;

        public Position(int _x, int _y)
        {
            x = _x;
            y = _y;
        }
    }

    public Position pos { get; set; }
    public bool isActive{ get; set; }
    public bool isFlagged{ get; set; }
    [SerializeField]
    private Image background;
	[SerializeField]
	private Image bombFlag;

    private void Awake()
    {
        isActive = true;
        isFlagged = false;
    }

    public void SetOpen(GameObject particlePrefab)
    {
        isActive = false;
        var part = Instantiate(particlePrefab) as GameObject;
        part.RectTrans().SetParent(background.transform);
        part.RectTrans().anchoredPosition = Vector2.zero;

        Observable.Timer(System.TimeSpan.FromSeconds(0.5f))
            .Subscribe(_ =>
            {
                    background.enabled = false;

                    Observable.Timer(System.TimeSpan.FromSeconds(2f))
                        .Subscribe(_2 =>
                            {
                                DestroyObject(part);
                            });
            });
    }

	public bool GetFlag()
	{
		return bombFlag.gameObject.activeSelf;
	}

	public bool SetFlag()
    {
		bombFlag.gameObject.SetActive (!bombFlag.gameObject.activeSelf);

		return GetFlag ();
    }
}
