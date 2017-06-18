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

    private void Awake()
    {
        isActive = true;
        isFlagged = false;
    }

    public void SetOpen()
    {
        isActive = false;
        background.enabled = false;
    }

    public void SetFlag()
    {
    }
}
