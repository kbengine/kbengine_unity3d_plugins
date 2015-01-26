namespace KBEngine
{
  	using UnityEngine; 
	using System; 
	using System.Collections; 
	using System.Collections.Generic;
	
	/*
		KBEngine逻辑层的实体基础类
		所有扩展出的游戏实体都应该继承于该模块
	*/
    public class Entity 
    {
    	public Int32 id = 0;
		public string className = "";
		public Vector3 position = new Vector3(0.0f, 0.0f, 0.0f);
		public Vector3 direction = new Vector3(0.0f, 0.0f, 0.0f);
		public float velocity = 0.0f;
		
		public bool isOnGound = true;
		
		public object renderObj = null;
		
		public Mailbox baseMailbox = null;
		public Mailbox cellMailbox = null;
		
		public bool inWorld = false;
		
		public static Dictionary<string, Dictionary<string, Property>> alldefpropertys = 
			new Dictionary<string, Dictionary<string, Property>>();
		
		private Dictionary<string, Property> defpropertys_ = 
			new Dictionary<string, Property>();
		
		private Dictionary<UInt16, Property> iddefpropertys_ = 
			new Dictionary<UInt16, Property>();
		
		public static void clear()
		{
			alldefpropertys.Clear();
		}
		
		public Entity()
		{
			Dictionary<string, Property> datas = alldefpropertys[GetType().Name];
			foreach(Property e in datas.Values)
			{
				Property newp = new Property();
				newp.name = e.name;
				newp.properUtype = e.properUtype;
				newp.utype = e.utype;
				newp.val = e.val;
				newp.setmethod = e.setmethod;
				defpropertys_.Add(e.name, newp);
				iddefpropertys_.Add(e.properUtype, newp);
			}
		}
		
		public virtual void onDestroy ()
		{
		}
		
		public bool isPlayer()
		{
			return id == KBEngineApp.app.entity_id;
		}
		
		public void addDefinedPropterty(string name, object v)
		{
			Property newp = new Property();
			newp.name = name;
			newp.properUtype = 0;
			newp.val = v;
			newp.setmethod = null;
			defpropertys_.Add(name, newp);
		}
		
		public object getDefinedPropterty(string name)
		{
			Property obj = null;
			if(!defpropertys_.TryGetValue(name, out obj))
			{
				return null;
			}
		
			return defpropertys_[name].val;
		}
		
		public void setDefinedPropterty(string name, object val)
		{
			defpropertys_[name].val = val;
		}
		
		public object getDefinedProptertyByUType(UInt16 utype)
		{
			Property obj = null;
			if(!iddefpropertys_.TryGetValue(utype, out obj))
			{
				return null;
			}
			
			return iddefpropertys_[utype].val;
		}
		
		public void setDefinedProptertyByUType(UInt16 utype, object val)
		{
			iddefpropertys_[utype].val = val;
		}
		
		public virtual void __init__()
		{
		}

		public void baseCall(string methodname, object[] arguments)
		{			
			if(KBEngineApp.app.currserver == "loginapp")
			{
				Dbg.ERROR_MSG(className + "::baseCall(" + methodname + "), currserver=!" + KBEngineApp.app.currserver);  
				return;
			}

			Method method = null;
			if(!EntityDef.moduledefs[className].base_methods.TryGetValue(methodname, out method))
			{
				Dbg.ERROR_MSG(className + "::baseCall(" + methodname + "), not found method!");  
				return;
			}
			
			UInt16 methodID = method.methodUtype;
			
			if(arguments.Length != method.args.Count)
			{
				Dbg.ERROR_MSG(className + "::baseCall(" + methodname + "): args(" + (arguments.Length) + "!= " + method.args.Count + ") size is error!");  
				return;
			}
			
			baseMailbox.newMail();
			baseMailbox.bundle.writeUint16(methodID);
			
			try
			{
				for(var i=0; i<method.args.Count; i++)
				{
					method.args[i].addToStream(baseMailbox.bundle, arguments[i]);
				}
			}
			catch(Exception e)
			{
				Dbg.ERROR_MSG(className + "::baseCall(" + methodname + "): args is error(" + e.Message + ")!");  
				baseMailbox.bundle = null;
				return;
			}
			
			baseMailbox.postMail(null);
		}
		
		public void cellCall(string methodname, object[] arguments)
		{
			if(KBEngineApp.app.currserver == "loginapp")
			{
				Dbg.ERROR_MSG(className + "::cellCall(" + methodname + "), currserver=!" + KBEngineApp.app.currserver);  
				return;
			}
			
			Method method = null;
			if(!EntityDef.moduledefs[className].cell_methods.TryGetValue(methodname, out method))
			{
				Dbg.ERROR_MSG(className + "::cellCall(" + methodname + "), not found method!");  
				return;
			}
			
			UInt16 methodID = method.methodUtype;
			
			if(arguments.Length != method.args.Count)
			{
				Dbg.ERROR_MSG(className + "::cellCall(" + methodname + "): args(" + (arguments.Length) + "!= " + method.args.Count + ") size is error!");  
				return;
			}

			cellMailbox.newMail();
			cellMailbox.bundle.writeUint16(methodID);
				
			try
			{
				for(var i=0; i<method.args.Count; i++)
				{
					method.args[i].addToStream(cellMailbox.bundle, arguments[i]);
				}
			}
			catch(Exception e)
			{
				Dbg.ERROR_MSG(className + "::cellCall(" + methodname + "): args is error(" + e.Message + ")!");  
				cellMailbox.bundle = null;
				return;
			}

			cellMailbox.postMail(null);
		}
	
		public void enterWorld()
		{
			Dbg.DEBUG_MSG(className + "::enterWorld(" + getDefinedPropterty("uid") + "): " + id); 
			inWorld = true;
			onEnterWorld();
			Event.fireOut("onEnterWorld", new object[]{this});
		}
		
		public virtual void onEnterWorld()
		{
		}

		public void leaveWorld()
		{
			Dbg.DEBUG_MSG(className + "::leaveWorld: " + id); 
			inWorld = false;
			onLeaveWorld();
			Event.fireOut("onLeaveWorld", new object[]{this});
		}
		
		public virtual void onLeaveWorld()
		{
		}

		public virtual void enterSpace()
		{
			Dbg.DEBUG_MSG(className + "::enterSpace(" + getDefinedPropterty("uid") + "): " + id); 
			inWorld = true;
			onEnterSpace();
			Event.fireOut("onEnterSpace", new object[]{this});
		}
		
		public virtual void onEnterSpace()
		{
		}
		
		public virtual void leaveSpace()
		{
			Dbg.DEBUG_MSG(className + "::leaveSpace: " + id); 
			inWorld = false;
			onLeaveSpace();
			Event.fireOut("onLeaveSpace", new object[]{this});
		}

		public virtual void onLeaveSpace()
		{
		}
		
		public virtual void set_position(object old)
		{
			Vector3 v = (Vector3)getDefinedPropterty("position");
			position = v;
			Dbg.DEBUG_MSG(className + "::set_position: " + old + " => " + v); 
			
			if(isPlayer())
				KBEngineApp.app.entityServerPos(position);
			
			Event.fireOut("set_position", new object[]{this});
		}


		public virtual void set_direction(object old)
		{
			Vector3 v = (Vector3)getDefinedPropterty("direction");
			
			v.x = v.x * 360 / ((float)System.Math.PI * 2);
			v.y = v.y * 360 / ((float)System.Math.PI * 2);
			v.z = v.z * 360 / ((float)System.Math.PI * 2);
			
			direction = v;
			
			Dbg.DEBUG_MSG(className + "::set_direction: " + old + " => " + v); 
			Event.fireOut("set_direction", new object[]{this});
		}
    }
    
}
