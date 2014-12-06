using UnityEngine;
using KBEngine;
using System;
using System.Collections;

namespace KBEngine
{
	public class Dbg {
		public static string getHead()
		{
			return "[" + DateTime.Now.ToString() + "]";
		}

		public static void INFO_MSG(object s)
		{
			Debug.Log(getHead() + s);
		}
		
		public static void DEBUG_MSG(object s)
		{
			Debug.Log(getHead() + s);
		}
		
		public static void WARNING_MSG(object s)
		{
			Debug.LogWarning(getHead() + s);
		}
		
		public static void ERROR_MSG(object s)
		{
			Debug.LogError(getHead() + s);
		}
	}
}
