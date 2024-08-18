using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class Tile : MonoBehaviour
{
    public int x;
    public int y;

    private Item _item;

    public Item Item
    {
        get => _item;
        set
        {
            if (_item == value) return;
            _item = value;
            
            if (icon != null && _item != null)
            {
                icon.sprite = _item.sprite; 
            }
            else
            {
                Debug.LogWarning("Icon or Item is null.");
            }
        }
    }


    public Image icon;

    public Button button;

    public Tile Left => Board.Instance.GetTile(x - 1, y);
    public Tile Top => Board.Instance.GetTile(x, y - 1);
    public Tile Right => Board.Instance.GetTile(x + 1, y);
    public Tile Bottom => Board.Instance.GetTile(x, y + 1);


    public Tile[] Neighbours => new[]
    {
        Left,
        Top,
        Right,
        Bottom,
    };

    private void Start() => button.onClick.AddListener(OnTileClicked);

    private async void OnTileClicked()
    {
        await Board.Instance.Select(this);
    }

    public List<Tile> GetConnectedTiles(List<Tile> exclude = null)
    {
        var result = new List<Tile>{this,};

        if(exclude == null) exclude = new List<Tile>{this,};
        else exclude.Add(this);

        foreach(var neighbour in Neighbours)
        {
            if(neighbour == null || exclude.Contains(neighbour) || neighbour.Item != Item) continue;
            result.AddRange(neighbour.GetConnectedTiles(exclude));
        }

        return result;
    }

}