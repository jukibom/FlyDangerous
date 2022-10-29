using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

using Den.Tools;

[assembly:System.Runtime.CompilerServices.InternalsVisibleTo("MapMagic.Tests.Editor")]

namespace MapMagic.Expose
{
	//non-serialized (serialize string expressions instead)
	public partial class Calculator
	{
		public enum DataType { Null, Operator, Reference, Number };
		public DataType dataType;

		//operator
		public char action;
		public Calculator left;
		public Calculator right;

		//final variable
		public string reference;
		public float number; //'42' in expression

		public string error; //here is written string that could not be parsed


		public Vector Calculate (Dictionary<string,object> refVals=null)
		/// Performs recursive calculations
		{
			if (dataType == DataType.Operator)
			{
				Vector leftVal = left.Calculate(refVals);
				Vector rightVal = right.Calculate(refVals);
				return Action(leftVal, rightVal, action);
			}

			else if (dataType == DataType.Reference)
			{
				if (refVals.TryGetValue(reference, out object obj))
					return new Vector(obj);

				else 
					return (Vector)0;
			}

			else if (dataType == DataType.Number)
				return (Vector)number;

			else 
				throw new Exception("Could not perform calculation: operator type is Null");
		}


		public Vector Calculate (Override ovd)
		/// The same but with Override
		{
			if (dataType == DataType.Operator)
			{
				Vector leftVal = left.Calculate(ovd);
				Vector rightVal = right.Calculate(ovd);
				return Action(leftVal, rightVal, action);
			}

			else if (dataType == DataType.Reference)
			{
				if (ovd.TryGetValue(reference, out Type type, out object val))
					return new Vector(val);

				else 
					return (Vector)0;
			}

			else if (dataType == DataType.Number)
				return (Vector)number;

			else 
				throw new Exception("Could not perform calculation: operator type is Null");
		}


		public static Vector Action (Vector left, Vector right, char action)
		{
			switch (action)
			{
				case '+': return left+right;
				case '-': return left-right;
				case '*': return left*right;
				case '/': return left/right;
				case '^': return left^right;
				case '.': return (Vector)left[(int)right];

				default: return new Vector();
			}
		}



		#region Parsing

			public static Calculator Parse (string str, bool prevActionIsDot=false)
			/// Reads string and converts to CalcVar
			/// prevActionIsDot just to properly treat channel .x
			{
				Calculator var = new Calculator();

				//if string contains actions - filling left/right operators recursively
				if (ContainsAction(str))
				{
					//looking most low-priority action
					char minChr = ' ';
					int minPos = -1;
					int minPriority = int.MaxValue;

					foreach ((char chr, int pos, int priority) in StringActions(str))
					{
						if (priority < minPriority)
							{ minChr = chr; minPos = pos; minPriority = priority; }
					}

					if (minPos == -1  ||  minChr == ' ')
						return null;

					//creating operator
					string str1 = str.Substring(0, minPos);
					string str2 = str.Substring(minPos + 1); //after action

					var.dataType = DataType.Operator;
					var.action = minChr;

					var.left = Parse(str1);
					var.right = Parse(str2, prevActionIsDot:var.action=='.');

					return var;
				}


				//trying to load channel
				if (prevActionIsDot)
				{
					int channel = StringToChannel(str);
					if (channel >= 0)
					{
						var.dataType = Calculator.DataType.Number;
						var.number = channel;

						return var;
					}
				}


				//trying to load reference
				string reference = StringToReference(str);
				if (reference != null)
				{
					var.dataType = Calculator.DataType.Reference;
					var.reference = reference;

					return var;
				}


				//trying to load number
				string numStr = str.Replace('(',' ').Replace(')',' '); //getting rid of ()
				bool isNum = float.TryParse(numStr, out float num);

				if (isNum)
				{
					var.dataType = DataType.Number;
					var.number = num;

					return var;
				}


				//could not parse
				var.dataType = Calculator.DataType.Null;
				if (str.Length == 0 || str == " ")
					var.error = "Empty expression member";
				else
					var.error = "Could not parse '" + str + "'";
				return var;
			}


			internal static bool ContainsAction (string str)
			{
				if (str.IndexOfAny(actionChars) >= 0)
					return true;

				else 
				{
					int indexOfDot = str.IndexOf('.');
					if (indexOfDot >= 0  &&  indexOfDot < str.Length-1  &&  !char.IsDigit(str[indexOfDot+1]))
						return true;

					else
						return false;
				}
					
			}
			private static readonly char[] actionChars = new char[] { '+', '-', '*', '/', '^' };


			internal static IEnumerable<(char chr, int pos, int priority)> StringActions (string str)
			/// Iterates all +-*/ in string, and returns their [num] and execution priority (high eecuted first)
			{
				int order = 0;
				int bracket = 0;

				for (int i=0; i<str.Length; i++)
				{
					char chr = str[i];

					bool isAction = false;
					bool isMult = false;
					bool isPow = false;
					bool isDot = false;

					if (chr=='(') bracket++;
					if (chr==')') bracket--;

					if (chr=='+' || chr=='-') isAction=true; 
					if (chr=='*' || chr=='/') { isAction=true; isMult=true; }
					if (chr=='^') { isAction=true; isPow=true; }
					if (chr=='.' && i<str.Length-1 && !char.IsDigit(str[i+1])) { isAction=true; isDot=true; }
				
					if (isAction)
					{
						int priority = bracket*10000000 + (isDot ? 1000000 : 0) + (isPow ? 100000 : 0) + (isMult ? 10000 : 0) + order;
						yield return (chr, i, priority);
						order--;
					}
				}
			}


			internal static string StringToReference (string str)
			/// Checks whether string is possible reference and formats it properly if so (null if starts with digit could not be formatted)
			{
				string result = null;
				bool ended = false; //space or bracket or other non letter/digit symbol met

				foreach (char chr in str)
				{
					if (result == null) //starting read
					{
						if (chr == ' '  ||  chr == '(')  //skipping opening spaces and brackets
							continue;

						if (Char.IsDigit(chr))  //first is digit - error
							return null;

						if (Char.IsLetter(chr))
							result = "" + chr;
					}

					else //already reading
					{
						if (Char.IsLetterOrDigit(chr))
						{
							if (ended) 
								return null;
							else
								result += chr;
						}

						else if (chr == ' '  ||  chr == ')')
							ended = true;

						else //if not letter or digit
							return null;
					}
				}

				return result;
			}

			internal static int StringToChannel (string str)
			{
				int ch = -1;

				foreach (char chr in str)
				{
					if (chr == ' ') continue;

					if (ch >= 0) //if already read char, and following isn't space
						return -1;

					if (chr == 'x' || chr == 'r')
						{ ch = 0; continue; }
					if (chr == 'y' || chr == 'g')
						{ ch = 1; continue; }
					if (chr == 'z' || chr == 'b')
						{ ch = 2; continue; }
					if (chr == 'w' || chr == 'a')
						{ ch = 3; continue; }
					
					return -1;
				}

				return ch;
			}


			public bool CheckValidity (out string error)
			/// Checks if it was loaded correctly, no nulls for left/right and reference
			{
				error = null;

				if (dataType == DataType.Null)
					{ error = this.error; return false; }

				if (dataType == DataType.Operator)
				{
					if (!(action=='+' || action=='-' || action=='*' || action=='/' || action=='^' || action=='.'))
						{ error = "Unknown operator type " + action; return false; }

					if (left == null)
						{ error = "Left operator not defined"; return false; }

					if (right == null)
						{ error = "Right operator not defined"; return false; }

					bool leftValid = left.CheckValidity(out string leftError);
					if (!leftValid) { error = leftError; return false; }

					bool rightValid = right.CheckValidity(out string rightError);
					if (!rightValid) { error = rightError; return false; }
				}

				if (dataType == DataType.Reference  &&  reference == null)
					{ error = "Reference not defined"; return false; }
			
				return true;
			}


			public string CheckOverrideAssign (Override ovd)
			/// Checks that override contains all the calculator (and it's subs) references
			/// Returns the first occurrence of reference that is not in override. Null if all assigned
			{
				if (dataType == DataType.Reference)
				{
					if (!ovd.Contains(reference)  ||  !ovd.Contains(reference))
						return reference;
				}

				if (dataType == DataType.Operator)
				{
					string leftRef = left.CheckOverrideAssign(ovd);
					if (leftRef != null) return leftRef;

					string rightRef = right.CheckOverrideAssign(ovd);
					if (rightRef != null) return rightRef;
				}

				return null;
			}


			public IEnumerable<string> AllRefenrences ()
			{
				if (dataType == DataType.Reference)
					yield return reference;
				
				if (dataType == DataType.Operator)
				{
					foreach (string leftReference in left.AllRefenrences())
						yield return leftReference;

					foreach (string rightReference in right.AllRefenrences())
						yield return rightReference;
				}
			}


			public bool ContainsReference (string reference)
			{
				if (dataType == DataType.Reference)
					return this.reference == reference;
				
				if (dataType == DataType.Operator)
				{
					if (left.ContainsReference(reference)) return true;
					if (right.ContainsReference(reference)) return true;
				}

				return false;
			}

			/*	public string CheckOverrideType (Override ovd)
			/// Checks that overridden values (have the same type as calculator refs) are floats or ints
			/// Returns the first occurrence of reference whose type don't match
			/// Skips references not in override
			{
				if (dataType == DataType.Reference)
				{
					if (!ovd.TryGetValue(reference, out object val, out Type type))
						return null;

					if (type != typeof(float)  &&  type != typeof(int))
						return reference;
				}

				if (dataType == DataType.Operator)
				{
					string leftRef = left.CheckOverrideType(ovd);
					if (leftRef != null) return leftRef;

					string rightRef = right.CheckOverrideType(ovd);
					if (rightRef != null) return rightRef;
				}

				return null;
			}*/

		#endregion

		
		#region ToString

			public override string ToString ()
			{
				if (dataType == DataType.Operator)
				{
					bool leftBrackets = left.dataType == DataType.Operator;
					bool rightBrackets = right.dataType == DataType.Operator;

					/*if ((action == '+' || action == '-')  &&  (left.action == '+' || left.action == '-')) leftBrackets = false;
					if ((action == '*' || action == '/')  &&  (left.action == '*' || left.action == '/')) leftBrackets = false;
					if ((action == '+' || action == '-')  &&  (left.action == '*' || left.action == '/')) leftBrackets = false;
					if ((action == '+' || action == '-')  &&  (right.action == '*' || right.action == '/')) rightBrackets = false;*/

					if (!leftBrackets && !rightBrackets)
						return $"{left.ToString()} {action} {right.ToString()}";
					else if (leftBrackets && !rightBrackets)
						return $"({left.ToString()}) {action} {right.ToString()}";
					else if (!leftBrackets && rightBrackets)
						return $"{left.ToString()} {action} ({right.ToString()})";
					else 
						return $"({left.ToString()}) {action} ({right.ToString()})";
				}

				else if (dataType == DataType.Reference)
					return reference;

				else
					return number.ToString();
			}

		#endregion

	}

}