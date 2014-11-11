namespace KBEngine
{
  	using UnityEngine; 
	using System; 
	using System.Collections; 
	using System.Collections.Generic;
	
    public class Entity 
    {
    	public Int32 id = 0;
		public string classtype = "";
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
				Dbg.ERROR_MSG(classtype + "::baseCall(" + methodname + "), currserver=!" + KBEngineApp.app.currserver);  
				return;
			}
			
			Method method = EntityDef.moduledefs[classtype].base_methods[methodname];
			UInt16 methodID = method.methodUtype;
			
			if(arguments.Length != method.args.Count)
			{
				Dbg.ERROR_MSG(classtype + "::baseCall(" + methodname + "): args(" + (arguments.Length) + "!= " + method.args.Count + ") size is error!");  
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
				Dbg.ERROR_MSG(classtype + "::baseCall(" + methodname + "): args is error(" + e.Message + ")!");  
				baseMailbox.bundle = null;
				return;
			}
			
			baseMailbox.postMail(null);
		}
		
		public void cellCall(string methodname, object[] arguments)
		{
			if(KBEngineApp.app.currserver == "loginapp")
			{
				Dbg.ERROR_MSG(classtype + "::cellCall(" + methodname + "), currserver=!" + KBEngineApp.app.currserver);  
				return;
			}
			
			Method method = EntityDef.moduledefs[classtype].cell_methods[methodname];
			UInt16 methodID = method.methodUtype;
			
			if(arguments.Length != method.args.Count)
			{
				Dbg.ERROR_MSG(classtype + "::cellCall(" + methodname + "): args(" + (arguments.Length) + "!= " + method.args.Count + ") size is error!");  
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
				Dbg.ERROR_MSG(classtype + "::cellCall(" + methodname + "): args is error(" + e.Message + ")!");  
				cellMailbox.bundle = null;
				return;
			}

			cellMailbox.postMail(null);
		}
	
		public virtual void onEnterWorld()
		{
			Dbg.DEBUG_MSG(classtype + "::onEnterWorld(" + getDefinedPropterty("uid") + "): " + id); 
			inWorld = true;
			Event.fireOut("onEnterWorld", new object[]{this});
		}
		
		public virtual void onLeaveWorld()
		{
			Dbg.DEBUG_MSG(classtype + "::onLeaveWorld: " + id); 
			inWorld = false;
			Event.fireOut("onLeaveWorld", new object[]{this});
		}

		public virtual void onEnterSpace()
		{
			Dbg.DEBUG_MSG(classtype + "::onEnterSpace(" + getDefinedPropterty("uid") + "): " + id); 
			inWorld = true;
			Event.fireOut("onEnterSpace", new object[]{this});
		}
		
		public virtual void onLeaveSpace()
		{
			Dbg.DEBUG_MSG(classtype + "::onLeaveSpace: " + id); 
			inWorld = false;
			Event.fireOut("onLeaveSpace", new object[]{this});
		}
		
		public virtual void set_position(object old)
		{
			Vector3 v = (Vector3)getDefinedPropterty("position");
			position = v;
			Dbg.DEBUG_MSG(classtype + "::set_position: " + old + " => " + v); 
			
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
			
			Dbg.DEBUG_MSG(classtype + "::set_direction: " + old + " => " + v); 
			Event.fireOut("set_direction", new object[]{this});
		}
    }
    
}
