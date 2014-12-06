namespace KBEngine
{
  	using UnityEngine; 
	using System; 
	using System.Net.Sockets; 
	using System.Net; 
	using System.Collections; 
	using System.Collections.Generic;
	using System.Text;
	using System.Text.RegularExpressions;
	using System.Threading;
	
	using MessageID = System.UInt16;
	using MessageLength = System.UInt16;
	
	/*
		网络模块
		处理连接、收发数据
	*/
    public class NetworkInterface 
    {
    	public const int TCP_PACKET_MAX = 1460;
    	
        private Socket socket_ = null;
		private List<MemoryStream> packets_ = null;
		private MessageReader msgReader = new MessageReader();
		private static ManualResetEvent TimeoutObject = new ManualResetEvent(false);
		private static byte[] _datas = new byte[MemoryStream.BUFFER_MAX];
		
        public NetworkInterface(KBEngineApp app)
        {
        	packets_ = new List<MemoryStream>();
        }
		
		public void reset()
		{
			if(valid())
			{
         	   socket_.Close(0);
			}
			socket_ = null;
			msgReader = new MessageReader();
			packets_.Clear();
			TimeoutObject.Set();
		}
		
		public Socket sock()
		{
			return socket_;
		}
		
		public bool valid()
		{
			return ((socket_ != null) && (socket_.Connected == true));
		}
		
		private static void connectCB(IAsyncResult asyncresult)
		{
			if(KBEngineApp.app.networkInterface().valid())
			{
				Dbg.DEBUG_MSG("connect is successfully!");
				KBEngineApp.app.networkInterface().sock().EndConnect(asyncresult);
			}
		
			TimeoutObject.Set();
		}
	    
		public bool connect(string ip, int port) 
		{
			if (valid())
				throw new InvalidOperationException( "Have already connected!" );
			
			int count = 0;
			
			Dbg.DEBUG_MSG("connect to " + ip + ":" + port + " ...");
			
			Regex rx = new Regex( @"((?:(?:25[0-5]|2[0-4]\d|((1\d{2})|([1-9]?\d)))\.){3}(?:25[0-5]|2[0-4]\d|((1\d{2})|([1-9]?\d))))");
			if (rx.IsMatch(ip))
			{
			}else
			{
				IPHostEntry ipHost = Dns.GetHostEntry (ip);
				ip = ipHost.AddressList[0].ToString();
			}
__RETRY:
			reset();
			TimeoutObject.Reset();
			
			// Security.PrefetchSocketPolicy(ip, 843);
			socket_ = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); 
			socket_.SetSocketOption (System.Net.Sockets.SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, MemoryStream.BUFFER_MAX);
			
            try 
            { 
                IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(ip), port); 
                
				socket_.BeginConnect(endpoint, new AsyncCallback(connectCB), socket_);
				
		        if (TimeoutObject.WaitOne(10000))
		        {
		        }
		        else
		        {
		        	reset();
		        }
        
            } 
            catch (Exception e) 
            {
                Dbg.WARNING_MSG(e.ToString());
                
                if(count < 3)
                {
                	Dbg.WARNING_MSG("connect to " + ip + ":" + port + " is error, try=" + (count++) + "!");
                	goto __RETRY;
           		 }
            
				return false;
            } 
			
			if(!valid())
			{
				Event.fireAll("onConnectStatus", new object[]{false});
				return false;
			}
			
			Event.fireAll("onConnectStatus", new object[]{true});
			return true;
		}
        
        public void close()
        {
           if(socket_ != null && socket_.Connected)
			{
				socket_.Close(0);
				socket_ = null;
				Event.fireAll("onDisableConnect", new object[]{});
               
            }

            socket_ = null;
        }

        public void send(byte[] datas)
        {
           if(!valid()) 
			{
               throw new ArgumentException ("invalid socket!");
            }
			
            if (datas == null || datas.Length == 0) 
			{
                throw new ArgumentException ("invalid datas!");
            }
			
			try
			{
				socket_.Send(datas);
			}
			catch (SocketException err)
			{
                if (err.ErrorCode == 10054 || err.ErrorCode == 10053)
                {
					Dbg.DEBUG_MSG(string.Format("NetworkInterface::send(): disable connect!"));
					
					if(socket_ != null && socket_.Connected)
						socket_.Close();
					
					socket_ = null;
					Event.fireAll("onDisableConnect", new object[]{});
                }
				else{
					Dbg.ERROR_MSG(string.Format("NetworkInterface::send(): socket error(" + err.ErrorCode + ")!"));
				}
			}
        }
		
		public void recv()
		{
           if(socket_ == null || socket_.Connected == false) 
			{
				throw new ArgumentException ("invalid socket!");
            }
			
            if (socket_.Poll(100000, SelectMode.SelectRead))
            {
	           if(socket_ == null || socket_.Connected == false) 
				{
					Dbg.WARNING_MSG("invalid socket!");
					return;
	            }
				
				int successReceiveBytes = 0;
				
				try
				{
					successReceiveBytes = socket_.Receive(_datas, MemoryStream.BUFFER_MAX, 0);
				}
				catch (SocketException err)
				{
                    if (err.ErrorCode == 10054 || err.ErrorCode == 10053)
                    {
						Dbg.DEBUG_MSG(string.Format("NetworkInterface::recv(): disable connect!"));
						
						if(socket_ != null && socket_.Connected)
							socket_.Close();
						
						socket_ = null;
                    }
					else{
						Dbg.ERROR_MSG(string.Format("NetworkInterface::recv(): socket error(" + err.ErrorCode + ")!"));
					}
					
					Event.fireAll("onDisableConnect", new object[]{});
					return;
				}
				
				if(successReceiveBytes > 0)
				{
				//	Dbg.DEBUG_MSG(string.Format("NetworkInterface::recv(): size={0}!", successReceiveBytes));
				}
				else if(successReceiveBytes == 0)
				{
					Dbg.DEBUG_MSG(string.Format("NetworkInterface::recv(): disable connect!"));
					if(socket_ != null && socket_.Connected)
						socket_.Close();
					
					socket_ = null;
					
					Event.fireAll("onDisableConnect", new object[]{});
				}
				else
				{
					Dbg.ERROR_MSG(string.Format("NetworkInterface::recv(): socket error!"));
					
					if(socket_ != null && socket_.Connected)
						socket_.Close();
					
					socket_ = null;
					
					Event.fireAll("onDisableConnect", new object[]{});
					return;
				}
				
				msgReader.process(_datas, (MessageLength)successReceiveBytes);
            }
		}
		
		public void process() 
		{
			if(valid())
			{
				recv();
			}
			else
			{
				System.Threading.Thread.Sleep(1);
			}
		}
	}
} 
