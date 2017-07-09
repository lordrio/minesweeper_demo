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
    [SerializeField]
    private GameObject[] bombImages;
    private int currentBombType = -1;

    public int CurrentBombType
    {
        get
        {
            return currentBombType;
        }
    }

    public Tuple<bool, int, int> Data
    {
        get
        {
            return Tuple.Create(isBomb, count, currentBombType);
        }
    }

    private void Awake()
    {
        countText.gameObject.SetActive(false);
    }

    public void Reset()
    {
        count = 0;
        countText.gameObject.SetActive(false);
        this.isBomb = false;
        background.color = Color.white;
        countText.gameObject.SetActive(false);

        if (currentBombType >= 0)
        {
            bombImages[currentBombType].SetActive(false);
        }
        currentBombType = -1;
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

    public void Setup(bool isBomb, int bombType = 0)
    {
        this.isBomb = isBomb;
        background.color = Color.red;
        countText.gameObject.SetActive(false);

        if (isBomb)
        {
            if (currentBombType >= 0)
            {
                bombImages[currentBombType].SetActive(false);
            }

            currentBombType = bombType;
            bombImages[currentBombType].SetActive(true);
        }
    }
}