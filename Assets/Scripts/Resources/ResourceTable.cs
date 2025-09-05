using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "QTH/ResourceTable", fileName = "ResourceTable")]
public class ResourceTable : ScriptableObject
{
    [Serializable] public class SpriteEntry { public string key; public Sprite sprite; }
    [Serializable] public class AnimEntry { public string key; public AnimationClip clip; }

    public List<SpriteEntry> sprites = new();
    public List<AnimEntry> anims = new();
}
