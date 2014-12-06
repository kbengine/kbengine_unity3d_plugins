namespace KBEngine
{
  	using UnityEngine; 
	using System; 
	
	/*
		抽象出一个entitydef中定义的属性
		该模块描述了属性的id以及数据类型等信息
	*/
    public class Property 
    {
		public string name = "";
    	public KBEDATATYPE_BASE utype = null;
		public UInt16 properUtype = 0;
		public Int16 aliasID = -1;
		
		public string defaultValStr = "";
		public System.Reflection.MethodInfo setmethod = null;
		
		public object val = null;
		
		public Property()
		{
			
		}
    }
    
} 
