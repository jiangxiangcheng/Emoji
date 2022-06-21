using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameMain : MonoBehaviour {
	public AtlasHierarchy atlasHierarchy;
	public TextEx text;

	void Awake()
	{
		foreach(Sprite sprt in atlasHierarchy.atlas.sprites)
		{
			TextEx.AddEmoji(sprt.name, sprt);
		}
	}

	// Use this for initialization
	void Start () {
		if (text != null)
		{
			text.text = "s😁😀a";
		}
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
