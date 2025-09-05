using UnityEngine;

public static class ResourceRegistry
{
    private static ResourceTable _table;
    public static void Inject(ResourceTable table) => _table = table;

    public static Sprite GetSprite(string key)
    {
        if (_table == null || string.IsNullOrEmpty(key)) return null;
        for (int i = 0; i < _table.sprites.Count; i++)
            if (_table.sprites[i].key == key) return _table.sprites[i].sprite;
        return null;
    }

    public static AnimationClip GetAnim(string key)
    {
        if (_table == null || string.IsNullOrEmpty(key)) return null;
        for (int i = 0; i < _table.anims.Count; i++)
            if (_table.anims[i].key == key) return _table.anims[i].clip;
        return null;
    }
}
