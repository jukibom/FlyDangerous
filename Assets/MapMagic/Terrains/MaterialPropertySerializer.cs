using System;
using UnityEngine;

using Den.Tools;

namespace MapMagic.Terrains
{
	[ExecuteInEditMode]
	public class MaterialPropertySerializer : MonoBehaviour
	/// Serializes the assigned properties. To serialize property it should be assigned via MaterialPropertySerializer
	{
		[SerializeField] private Terrain terrain;
		[SerializeField] private string[] names = new string[0];
		[SerializeField] private Texture2D[] textures = new Texture2D[0];

		[NonSerialized] private MaterialPropertyBlock matProps;


		/*public void OnEnable()
		{
			if (terrain == null) return;

			//terrain.GetSplatMaterialPropertyBlock(matProps);
			matProps = new MaterialPropertyBlock();

			for (int i=0; i<textures.Length; i++)
				matProps.SetTexture(names[i], textures[i]);

			terrain.SetSplatMaterialPropertyBlock(matProps);

			//re-enabling terrain. Otherwise Unity will crash on changing material values
			//terrain.enabled = false;
			//terrain.enabled = true;
		}*/

		
		//public void OnEnable () //called after each lod change, causing blinking
		public void Start ()
		{
			if (terrain == null) return;

			matProps = new MaterialPropertyBlock();

			for (int i=0; i<textures.Length; i++)
				matProps.SetTexture(names[i], textures[i]);

			terrain.SetSplatMaterialPropertyBlock(matProps);

			#if UNITY_EDITOR

				#if UNITY_2019_3_OR_NEWER
				terrain.enabled = false;
				UnityEditor.SceneView.RepaintAll();
				terrain.enabled = true;
				#else
				updateVisibility = true;
				#endif

			#endif
		}

		//Re-enabling terrain (in next frame?) Otherwise Unity will crash on changing material values
		public bool updateVisibility = false;
		public void OnDrawGizmos () 
		{
			if (updateVisibility)
			{
				if (terrain.enabled) terrain.enabled = false;
				else { terrain.enabled = true; updateVisibility = false; }
			}

			//Debug.Log(terrain.enabled);
		}



		public void SetTexture (string name, Texture2D texture)
		{
			if (terrain == null) terrain = GetComponent<Terrain>();
			if (matProps == null) matProps = new MaterialPropertyBlock();

			matProps.SetTexture(name, texture);

			int index = names.Find(name);
			if (index < 0) 
			{ 
				ArrayTools.Add(ref names, name); 
				ArrayTools.Add(ref textures, texture);
			}
			else
				textures[index] = texture;
		}

		public Texture2D GetTexture (string name)
		{
			if (name == null) return null;
			if (terrain == null) terrain = GetComponent<Terrain>();
			if (matProps == null) matProps = new MaterialPropertyBlock();

			int index = names.Find(name);
			if (index < 0) return null;
			else return textures[index];
		}

		public void Apply ()
		{
			terrain.SetSplatMaterialPropertyBlock(matProps);
		}
	}

}
