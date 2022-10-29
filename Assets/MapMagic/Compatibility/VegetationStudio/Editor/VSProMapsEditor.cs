using System;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Profiling;

using Den.Tools;
using Den.Tools.GUI;
using MapMagic.Core;
using MapMagic.Products;
using MapMagic.Nodes;
using MapMagic.Nodes.GUI;

#if VEGETATION_STUDIO_PRO
using AwesomeTechnologies.Common;
using AwesomeTechnologies.VegetationSystem;
#endif

namespace MapMagic.VegetationStudio
{
	public static class VSProMapsOutEditor
	{
		#if VEGETATION_STUDIO_PRO //otherwise will log a warning
		static string[] maskNames = null;
		#endif
		static string[] channelNames = new string[] { "Red", "Green", "Blue", "Alpha" }; //should be readonly
		
		public static string[] objectNames = null;


		[UnityEditor.InitializeOnLoadMethod]
		static void EnlistInMenu ()
		{
			CreateRightClick.generatorTypes.Add(typeof(VSProMapsOut));
		}


		[Draw.Editor(typeof(VSProMapsOut))]
		public static void DrawVSProMapsOut (VSProMapsOut gen)
		{
			#if VEGETATION_STUDIO_PRO
			VegetationPackagePro package = null;

			Cell.current.fieldWidth = 0.49f;

			using (Cell.LinePx(0))
			using (Cell.Padded(1,1,0,0))
			{
				if (GraphWindow.current.mapMagic != null)
				{
					VegetationSystemPro system = GraphWindow.current.mapMagic.Globals.vegetationSystem as VegetationSystemPro;
					using (Cell.LineStd) GeneratorDraw.DrawGlobalVar(ref system, "System");
					GraphWindow.current.mapMagic.Globals.vegetationSystem = system;

					package = GraphWindow.current.mapMagic.Globals.vegetationPackage as VegetationPackagePro;
					using (Cell.LineStd) GeneratorDraw.DrawGlobalVar(ref package, "Package");
					GraphWindow.current.mapMagic.Globals.vegetationPackage = package;
				}
				else
					using (Cell.LinePx(18+18)) Draw.Label("Not assigned to current \nMapMagic object");
			
				//populating masks list to choose from
				if (package != null)
				{
					int masksCount = package.TextureMaskGroupList.Count;
					if (maskNames == null || maskNames.Length != masksCount)
						maskNames = new string[masksCount];

					for (int i=0; i<masksCount; i++)
					{
						maskNames[i] = (i + 1).ToString() + ". " + 
							package.TextureMaskGroupList[i].TextureMaskName + " - " +
							package.TextureMaskGroupList[i].TextureMaskType;
					}
				}

				Cell.EmptyLinePx(4);

				using (Cell.LineStd)
				{
					if (package != null)
						Draw.PopupSelector(ref gen.maskGroup, maskNames, "Group");
					else
						Draw.Field(ref gen.maskGroup, "Group");
				}
				using (Cell.LineStd)
					Draw.PopupSelector(ref gen.textureChannel, channelNames, "Channel");
			}


			Cell.EmptyLinePx(3);
			using (Cell.LineStd)
				using (new Draw.FoldoutGroup(ref gen.guiAdvanced, "Advanced", isLeft:false, padding:1))
					if (gen.guiAdvanced)
					{
						if (GraphWindow.current.mapMagic != null)
						using (Cell.LineStd) GeneratorDraw.DrawGlobalVar(
							ref GraphWindow.current.mapMagic.Globals.vegetationSystemCopy, 
							"Copy VS");

						using (Cell.LineStd) Draw.Field(ref gen.outputLevel, "Out Level");
					}

			#else

			using (Cell.LinePx(76))
				Draw.Helpbox("Vegetation Studio Pro doesn't seem to be installed, or Vegetation Studio Pro compatibility is not enabled in settings");

			using (Cell.LineStd)
				Draw.Field(ref gen.maskGroup, "Group");
			using (Cell.LineStd)
				Draw.PopupSelector(ref gen.textureChannel, channelNames, "Channel");

			#endif


		}
	}
}