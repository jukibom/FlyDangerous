using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Den.Tools.Serialization
{
	public static class Tools
	{
		public static string ReadBlock (string str, int start, char blockOpen, char blockClose, bool skipStrings=true)
		/// Reads some block (area marked with open and close symbols) block with internal blocks: (A, B(c,d,e), F(g.h(i)), J)
		/// Block start and end symbols (like '(' ) are included in return string. It starts with '(' and ends with ')'
		{
			int blockOpenings = 0;
			bool blockStarted = false; //have we ever reached block start (return on blockOpenings==0 if so)
			bool stringOpened = false; //don't process if we've met "
			
			List<char> blockChars = new List<char>();

			for (int i=start; i<str.Length; i++)
			{
				char ch = str[i];

				if (skipStrings && ch=='"')
				{
					if (stringOpened  &&  i!=0  &&  str[i-1]=='\\') //it's " inside the string
						{ blockChars.Add('"'); continue; }

					stringOpened = !stringOpened;
				}

				if (!stringOpened)
				{
					if (ch==blockOpen)
					{
						blockOpenings ++;
						blockStarted = true;
					}

					if (blockStarted)
					{
						blockChars.Add(ch);

						if (ch==blockClose)
							blockOpenings --;
				
						if (blockOpenings == 0)
							break;
					}
				}

				else
					blockChars.Add(ch);
			}

			return new string(blockChars.ToArray());
		}


		public static object SplitBlock (string str, char splitSymbol, char blockOpen, char blockClose, bool skipStrings=true, bool trim=true)
		/// Splits one string in block arrays: [string, string, [string,string,string], [string,string], string]
		/// Returns either one string, or object[] if block has internal ones
		/// Open/close and split symbols are not included
		/// Note that open and close symbols kinda treated like split symbols too
		/// skipStrings will take internal strings into account and will not use open/close/split symbol from them
		/// trim will remove spaces for each member
		{
			bool stringOpened = false; //don't process if we've met "

			List<object> currMember = new List<object>();
			List<char> currString = new List<char>();

			Stack< List<object> > upperMembers = new Stack< List<object> >();
			Stack< List<char> > upperStrings = new Stack< List<char> >();

			for (int i=0; i<str.Length; i++)
			{
				char ch = str[i];

				//checking if internal string is opening
				if (skipStrings && ch=='"')
				{
					if (stringOpened  &&  i!=0  &&  str[i-1]=='\\') //it's " inside the string
						{ currString.Add('"'); continue; }

					stringOpened = !stringOpened;
				}

				//just reading internal string if it's opened
				if (stringOpened)
					currString.Add(ch);

				//common case (non-string)
				else
				{
					if (ch==splitSymbol)
					{
						AddString(currMember, currString, trim);
						currString.Clear();
					}

					else if (ch==blockOpen)
					{
						upperMembers.Push(currMember);
						currMember = new List<object>();

						upperStrings.Push(currString);
						currString = new List<char>();
					}

					else if (ch==blockClose)
					{
						AddString(currMember, currString, trim);
						object[] intMember = currMember.ToArray();

						currMember = upperMembers.Pop();
						currString = upperStrings.Pop();

						currMember.Add(intMember);
					}

					else
						currString.Add(ch);
				}

			}

			//finalizing current member
			AddString(currMember, currString, trim);
			return currMember.ToArray();
		}


		private static void AddString (List<object> strList, List<char> charArr, bool trim=true)
		/// Adds char array to string list (as new string) if it has anything
		/// Called on all charArr closings (split symbol, close symbol, end)
		{
			string aplString = new string(charArr.ToArray());
			if (trim) aplString = aplString.Trim();
			if (aplString.Length!=0)
				strList.Add(aplString);
		}


		public static int CheckEquality (object srcObj, object dstObj, HashSet<object> used=null)
		/// Testing if the class was copied right
		{
			if (used == null) used = new HashSet<object>();
			if (used.Contains(srcObj)) return 0;
			used.Add(srcObj);
			int numChecked = 1;

			if (srcObj==null && dstObj==null) return 1;
			if (srcObj==null && dstObj!=null) throw new Exception("CheckEquality: Src is null while dst is " + dstObj);
			if (srcObj!=null && dstObj==null) throw new Exception("CheckEquality: Dst is null while src is " + srcObj);

			Type type = srcObj.GetType();

			if (type.IsSubclassOf(typeof(UnityEngine.Object))) return 0;
			if (type.IsSubclassOf(typeof(MemberInfo))) return 0; //do not check reflections - they are same
			if (typeof(IDictionary).IsAssignableFrom(type)) return 0;
				  
			if (type != dstObj.GetType())
				throw new Exception("CheckEquality: Types differ " + type + " and " + dstObj.GetType());

			if (!type.IsValueType  &&  type!=typeof(string)  &&  srcObj==dstObj)
					throw new Exception("CheckEquality: Are same " + srcObj);   

			//array
			if (type.IsArray)
			{
				Array srcArray = (Array)srcObj;
				Array dstArray = (Array)dstObj;
					
				if (type.GetElementType().IsValueType)
				{
					for (int i=0; i<srcArray.Length; i++)
						if (!srcArray.GetValue(i).Equals(dstArray.GetValue(i))) 
							throw new Exception("CheckEquality: arrays not equal "  + srcObj + " and " + dstArray);
				}
					
				else
					for (int i=0; i<srcArray.Length; i++)
						numChecked += CheckEquality(srcArray.GetValue(i), dstArray.GetValue(i), used);
			}

			//common case
			else
			{
					//fields
					FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
					for (int i=0; i<fields.Length; i++)
					{
						FieldInfo field = fields[i];

						if (field.IsLiteral) continue; //leaving constant fields blank
						if (field.FieldType.IsPointer) continue; //skipping pointers (they make unity crash. Maybe require unsafe)
						//if (field.IsNotSerialized) continue;
						
						if (field.FieldType.IsValueType)
						{
							if (!field.GetValue(srcObj).Equals(field.GetValue(dstObj)))
								throw new Exception("CheckEquality: not equal " + field.Name + " " + field.GetValue(srcObj) + " and " + field.GetValue(dstObj));
						}
						else
						{
							numChecked += CheckEquality(field.GetValue(srcObj), field.GetValue(dstObj), used);
						}
					}
			}

			return numChecked;
		}
	}
}
