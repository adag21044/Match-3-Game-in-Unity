using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

public sealed class Board : MonoBehaviour
{
    public static Board Instance { get; private set; }
    public Row[] rows;
    public Tile[,] Tiles { get; private set; }
    public int Width => Tiles.GetLength(0);
    public int Height => Tiles.GetLength(1);

    private readonly List<Tile> _selection = new List<Tile>();

    private const float TweenDuration = 0.25f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Destroy duplicate instance
            return;
        }
        Instance = this;
    }


    private void Start()
    {
        Tiles = new Tile[rows.Max(row => row.tiles.Length), rows.Length];

        for(var y = 0; y < Height; y++)
        {
            for(var x = 0; x < Width; x++)
            {
                var tile = rows[y].tiles[x];
                tile.x = x;
                tile.y = y;
                tile.Item = ItemDatabase.Items[Random.Range(0, ItemDatabase.Items.Length)];
                Tiles[x, y] = tile;
            }
        }

    }

    

    public async Task Select(Tile tile)
    {
        if (_selection.Count >= 2) return;

        if (!_selection.Contains(tile))
            _selection.Add(tile);
            
        if (_selection.Count < 2) return;

        Debug.Log($"Selected: {_selection[0].x}, {_selection[0].y} and {_selection[1].x}, {_selection[1].y}");

        await Swap(_selection[0], _selection[1]);

        if (CanPop()) 
            await Pop();
        else 
            await Swap(_selection[0], _selection[1]);

        _selection.Clear();
    }



    public async Task Swap(Tile tile1, Tile tile2)
    {
        // Mevcut icon ve item bilgilerini sakla
        var icon1 = tile1.icon;
        var icon2 = tile2.icon;

        var item1 = tile1.Item;
        var item2 = tile2.Item;

        // Transform pozisyonlarını al
        var icon1Transform = tile1.transform;
        var icon2Transform = tile2.transform;

        // Animasyonlar için DOTween kullanalım
        var sequence = DOTween.Sequence();
        sequence.Join(icon1Transform.DOMove(icon2Transform.position, TweenDuration))
                .Join(icon2Transform.DOMove(icon1Transform.position, TweenDuration));

        await sequence.Play().AsyncWaitForCompletion();

        // Sprite'ları ve Item'leri değiştir
        tile1.icon = icon2;
        tile2.icon = icon1;

        tile1.Item = item2;
        tile2.Item = item1;

        // Tiles dizisinde yerlerini değiştir
        var tempTile = Tiles[tile1.x, tile1.y];
        Tiles[tile1.x, tile1.y] = Tiles[tile2.x, tile2.y];
        Tiles[tile2.x, tile2.y] = tempTile;

        // x ve y koordinatlarını değiştir
        int tempX = tile1.x;
        int tempY = tile1.y;
        tile1.x = tile2.x;
        tile1.y = tile2.y;
        tile2.x = tempX;
        tile2.y = tempY;
    }

    private bool CanPop()
    {
        for(var y = 0; y < Height; y++)
        {
            for(var x = 0; x < Width; x++)
            {
                if(Tiles[x, y].GetConnectedTiles().Skip(1).Count() >= 2) return true;
            }
        }

        return false; 
    }   

    private async Task Pop()
    {
        bool anyMatchFound = false;

        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                var tile = Tiles[x, y];
                var connectedTiles = tile.GetConnectedTiles();

                if (connectedTiles.Skip(1).Count() < 2) continue;

                anyMatchFound = true;

                var deflateSequence = DOTween.Sequence();

                foreach (var connectedTile in connectedTiles)
                {
                    deflateSequence.Join(connectedTile.icon.transform.DOScale(Vector3.zero, TweenDuration));
                    connectedTile.Item = null; // Clear the item after deflating
                }

                await deflateSequence.Play().AsyncWaitForCompletion();
            }
        }

        if (anyMatchFound)
        {
            await RefillBoard(); // Refill the board and check for additional matches
        }
    }



    public Tile GetTile(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return null;
        return Tiles[x, y];
    }

    private async Task RefillBoard()
    {
        bool needsRefill = false;

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (Tiles[x, y].Item == null) // Empty spot
                {
                    needsRefill = true;

                    for (int ny = y + 1; ny < Height; ny++)
                    {
                        if (Tiles[x, ny].Item != null)
                        {
                            // Move tile down
                            await Swap(Tiles[x, y], Tiles[x, ny]);
                            break;
                        }
                    }

                    // If no tile found above, spawn a new one
                    if (Tiles[x, y].Item == null)
                    {
                        Tiles[x, y].Item = ItemDatabase.Items[Random.Range(0, ItemDatabase.Items.Length)];
                        Tiles[x, y].icon.transform.localScale = Vector3.zero;
                        Tiles[x, y].icon.transform.DOScale(Vector3.one, TweenDuration);
                    }
                }
            }
        }

        if (needsRefill)
        {
            await Task.Delay((int)(TweenDuration * 1000)); // Wait for the refill animations
            await CheckForMatches(); // Check for new matches after the refill
        }
    }

    private async Task CheckForMatches()
    {
        while (CanPop())
        {
            await Pop();
            await RefillBoard(); // Refill the board after popping tiles
        }
    }


    
}
