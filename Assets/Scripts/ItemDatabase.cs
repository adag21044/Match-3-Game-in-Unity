using UnityEngine;

public static class ItemDatabase 
{
    public static Item[] Items { get; private set; }

    // This method is called before the first scene is loaded
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] private static void Initialize() => Items = Resources.LoadAll<Item>("Items/");
}
