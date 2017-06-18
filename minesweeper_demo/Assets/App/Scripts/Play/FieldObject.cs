using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UniRx;

public class FieldObject : MonoBehaviour
{
    private bool isBomb;
    private int count = 0;

    [SerializeField]
    private Image background;
    [SerializeField]
    private Text countText;

    public Tuple<bool, int> Data
    {
        get
        {
            return Tuple.Create(isBomb, count);
        }
    }

    private void Awake()
    {
        countText.gameObject.SetActive(false);
    }

    public void AddCount()
    {
        if (isBomb)
            return;
        
        ++count;
        countText.text = count.ToString();

        if (!countText.isActiveAndEnabled)
        {
            countText.gameObject.SetActive(true);
        }
    }

    public void Setup(bool isBomb)
    {
        this.isBomb = isBomb;
        background.color = Color.red;
        countText.gameObject.SetActive(false);
    }
}