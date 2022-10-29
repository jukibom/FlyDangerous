using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

using Den.Tools;
using MapMagic.Nodes;

namespace MapMagic.Expose
{
	public static class Assigner
	{
		public static IUnit CopyAndAssign (IUnit unit, Exposed exp, Override ovd)
		{
			IUnit copy = unit.ShallowCopy();

			//overriding fields
			AssignOverrideToFields(copy, exp, ovd);

			//checking biome sub-override table
			if (copy is IBiome biome  &&  biome.Override != null)
			{
				biome.Override = new Override(biome.Override); //copying override table in copy too
				AssignOverrideToOverride(exp.EntriesById(unit.Id), biome.Override, ovd);
			}

			//overriding layers
			if (copy is IMultiLayer multi)  // or has overrided layer (to assign new 
			{
				bool overrideLayer = false;
				foreach (IUnit layer in multi.Layers)
					if (IsExposed(layer,exp))
						overrideLayer = true;

				if (overrideLayer)
				{
					//copy layers collection (since some will be re-assigned)
					ICollection<IUnit> srcLayers = multi.Layers;
					IUnit[] dstLayers = new IUnit[srcLayers.Count];
					srcLayers.CopyTo(dstLayers, 0);
				
					//copy/assign layers in copied collection
					for (int l=0; l<dstLayers.Length; l++)
						dstLayers[l] = CopyAndAssign(dstLayers[l], exp, ovd);
						
					//apply new collection itself
					multi.Layers = dstLayers;
				}
			}

			return copy;
		}


		public static bool IsExposed (IUnit unit, Exposed exp)
		/// If element or any of it's layers exposed
		{
			if (exp == null  ||  exp.Count == 0) //nothing exposed in exp
				return false;

			if (exp.Contains(unit.Id))
				return true;

			if (unit is IMultiLayer multi)  // or has overrided layer (to assign new 
			{
				foreach (IUnit layer in multi.Layers)
					if (IsExposed(layer, exp)) //recursively, not with Contains
						return true;
			}

			return false;
		}


		private static void AssignOverrideToFields (IUnit unit, Exposed exp, Override ovd)
		/// Assigns overriden values to standard (field) generator/layer
		{
			Type type = unit.GetType();
			foreach (Exposed.Entry entry in exp.EntriesById(unit.Id))
			{
				FieldInfo fieldInfo = type.GetField(entry.name);
				if (fieldInfo == null)
					continue; //might happen in biome, which has not fields, but ovd exposed

				if (fieldInfo.FieldType != entry.type  &&  !fieldInfo.FieldType.IsArray)
					throw new Exception("Recorded field type doesn't match generator field type. Possibly generator has changed.");


				object val;
				Calculator.Vector vec = entry.calculator.Calculate(ovd);

				if (entry.channel == -1) //non-channel
					val = vec.Convert(entry.type);
				else
				{
					object origVal = fieldInfo.GetValue(unit); //loading original value to take other channels
					val = vec.ConvertToChannel(origVal, entry.channel, entry.type);
				}

				if (entry.arrIndex == -1) //non-array
					fieldInfo.SetValue(unit, val);
				else if (unit is Nodes.MatrixGenerators.Blend200 blendGen) //special case for blend node
					blendGen.layers[entry.arrIndex].opacity = (float)val;
				else //array
				{
					Array arr = (Array)fieldInfo.GetValue(unit);
					arr.SetValue(val, entry.arrIndex);
					fieldInfo.SetValue(unit, arr);
				}
			}
		}


		private static void AssignOverrideToOverride (IEnumerable<Exposed.Entry> entries, Override ovdBase, Override ovdOverride)
		/// Analog of AssignOverrideToFields, but overrides sub-graph defaults instead of generator fields
		{
			foreach (Exposed.Entry entry in entries)
			{
				if (!ovdBase.TryGetValue(entry.name, out Type origType, out object origVal))
					continue;
					//do not override values that were not exposed

				if (origType != entry.type)
					continue;
					//do not override if they are of the same type
					

				object val;
				Calculator.Vector vec = entry.calculator.Calculate(ovdOverride);

				if (entry.channel == -1) //non-channel
					val = vec.Convert(entry.type);
				else
					val = vec.ConvertToChannel(origVal, entry.channel, entry.type);

				ovdBase[entry.name] = val;
			}
		}

	}
}