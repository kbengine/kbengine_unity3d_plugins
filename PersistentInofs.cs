using UnityEngine;
using KBEngine;
using System; 
using System.IO;  
using System.Text;
using System.Collections;

namespace KBEngine
{
	/*
		持久化引擎协议，在检测到协议版本发生改变时会清理协议
	*/
	public class PersistentInofs
	{
		string _persistentDataPath = "";
		bool _isGood = false;
		
	    public PersistentInofs(string path)
	    {
	    	_persistentDataPath = path;
	    	installEvents();
	    	_isGood = loadAll();
	    }
	        
		void installEvents()
		{
			KBEngine.Event.registerOut("onImportClientMessages", this, "onImportClientMessages");
			KBEngine.Event.registerOut("onImportServerErrorsDescr", this, "onImportServerErrorsDescr");
			KBEngine.Event.registerOut("onImportClientEntityDef", this, "onImportClientEntityDef");
			KBEngine.Event.registerOut("onVersionNotMatch", this, "onVersionNotMatch");
			KBEngine.Event.registerOut("onScriptVersionNotMatch", this, "onScriptVersionNotMatch");
			KBEngine.Event.registerOut("onServerDigest", this, "onServerDigest");
		}
		
		public bool isGood()
		{
			return _isGood;
		}
		
		string _getSuffix()
		{
			return KBEngineApp.app.clientVersion + "." + KBEngineApp.app.clientScriptVersion + "." + 
							KBEngineApp.app.getInitArgs().ip + "." + KBEngineApp.app.getInitArgs().port;
		}
		
		public bool loadAll()
		{

			
			byte[] loginapp_onImportClientMessages = loadFile (_persistentDataPath, "loginapp_clientMessages." + _getSuffix());

			byte[] baseapp_onImportClientMessages = loadFile (_persistentDataPath, "baseapp_clientMessages." + _getSuffix());

			byte[] onImportServerErrorsDescr = loadFile (_persistentDataPath, "serverErrorsDescr." + _getSuffix());

			byte[] onImportClientEntityDef = loadFile (_persistentDataPath, "clientEntityDef." + _getSuffix());

			if(loginapp_onImportClientMessages.Length > 0 && baseapp_onImportClientMessages.Length > 0)
			{
				try
				{
					if(!KBEngineApp.app.importMessagesFromMemoryStream (loginapp_onImportClientMessages, 
							baseapp_onImportClientMessages, onImportClientEntityDef, onImportServerErrorsDescr))
						
						clearMessageFiles();
						return false;
				}
				catch(Exception e)
				{
					Dbg.ERROR_MSG("PersistentInofs::loadAll(): is error(" + e.ToString() + ")!");  
					clearMessageFiles();
					return false;
				}
			}
			
			return true;
		}
		
		public void onImportClientMessages(string currserver, byte[] stream)
		{
			if(currserver == "loginapp")
				createFile (_persistentDataPath, "loginapp_clientMessages." + _getSuffix(), stream);
			else
				createFile (_persistentDataPath, "baseapp_clientMessages." + _getSuffix(), stream);
		}

		public void onImportServerErrorsDescr(byte[] stream)
		{
			createFile (_persistentDataPath, "serverErrorsDescr." + _getSuffix(), stream);
		}
		
		public void onImportClientEntityDef(byte[] stream)
		{
			createFile (_persistentDataPath, "clientEntityDef." + _getSuffix(), stream);
		}
		
		public void onVersionNotMatch(string verInfo, string serVerInfo)
		{
			clearMessageFiles();
		}

		public void onScriptVersionNotMatch(string verInfo, string serVerInfo)
		{
			clearMessageFiles();
		}
		
		public void onServerDigest(string currserver, string serverProtocolMD5, string serverEntitydefMD5)
		{
			// 我们不需要检查网关的协议， 因为登录loginapp时如果协议有问题已经删除了旧的协议
			if(currserver == "baseapp")
			{
				return;
			}
			
			if(loadFile(_persistentDataPath, serverProtocolMD5 + serverEntitydefMD5 + "." + 
				KBEngineApp.app.getInitArgs().ip + "." + KBEngineApp.app.getInitArgs().port).Length == 0)
			{
				clearMessageFiles();
				createFile(_persistentDataPath, serverProtocolMD5 + serverEntitydefMD5 + "." + 
					KBEngineApp.app.getInitArgs().ip + "." + KBEngineApp.app.getInitArgs().port, new byte[1]);
			}
		}
			
		public void clearMessageFiles()
		{
			deleteFile(_persistentDataPath, "loginapp_clientMessages." + _getSuffix());
			deleteFile(_persistentDataPath, "baseapp_clientMessages." + _getSuffix());
			deleteFile(_persistentDataPath, "serverErrorsDescr." + _getSuffix());
			deleteFile(_persistentDataPath, "clientEntityDef." + _getSuffix());
			KBEngineApp.app.resetMessages();
		}
		
		public void createFile(string path, string name, byte[] datas)  
		{  
			deleteFile(path, name);
			Dbg.DEBUG_MSG("createFile: " + path + "/" + name);
			FileStream fs = new FileStream (path + "/" + name, FileMode.OpenOrCreate, FileAccess.Write);
			fs.Write (datas, 0, datas.Length);
			fs.Close ();
			fs.Dispose ();
		}  
	   
	   public byte[] loadFile(string path, string name)  
	   {  
			FileStream fs;

			try{
				fs = new FileStream (path + "/" + name, FileMode.Open, FileAccess.Read);
			}
			catch (Exception e)
			{
				Dbg.DEBUG_MSG("loadFile: " + path + "/" + name);
				Dbg.DEBUG_MSG(e.ToString());
				return new byte[0];
			}

			byte[] datas = new byte[fs.Length];
			fs.Read (datas, 0, datas.Length);
			fs.Close ();
			fs.Dispose ();

			Dbg.DEBUG_MSG("loadFile: " + path + "/" + name + ", datasize=" + datas.Length);
			return datas;
	   }  
	   
	   public void deleteFile(string path, string name)  
	   {  
			Dbg.DEBUG_MSG("deleteFile: " + path + "/" + name);
			
			try{
	        	File.Delete(path + "/"+ name);  
			}
			catch (Exception e)
			{
				Debug.LogError(e.ToString());
			}
	   }  
	}

}
