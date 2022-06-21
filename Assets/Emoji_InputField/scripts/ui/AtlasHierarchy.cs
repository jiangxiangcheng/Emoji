using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class AtlasHierarchy : MonoBehaviour
{
	public AtlasData atlas;

    public void SetAtlasData(List<Sprite> sprites, Material mat)
    {
        atlas = new AtlasData();
        atlas.sprites = new List<Sprite>();
        for (int i = 0; i < sprites.Count; i++)
        {
            atlas.sprites.Add(sprites[i]);
        }
        atlas.material = mat;
    }

    public List<string> GetSpriteDataName()
    {
        List<string> names = new List<string>();
        for (int i = 0; i < atlas.sprites.Count; i++)
        {
            names.Add(atlas.sprites[i].name);
        }
        return names;
    }
}


// ------------------------------------------------------------------------------------------------
[System.Serializable]
public class AtlasData
{

	public Material material;                   // 材质
	public List<Sprite> sprites;            // Sprite列表

}
