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
    	public delegate void ConnectCallback(string ip, int port, bool success, object userData);
    	
		// for connect
		string _connectIP;
		int _connectPort;
		ConnectCallback _connectCB;
		object _userData;
		
        private Socket socket_ = null;
		private List<MemoryStream> packets_ = null;
		private MessageReader msgReader = new MessageReader();
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
			
			_connectIP = "";
			_connectPort = 0;
			_connectCB = null;
			_userData = null;
		}
		
		public Socket sock()
		{
			return socket_;
		}
		
		public bool valid()
		{
			return ((socket_ != null) && (socket_.Connected == true));
		}
		
		private void connectCB(object sender, SocketAsyncEventArgs e)
		{
			Dbg.INFO_MSG(string.Format("NetworkInterface::connectCB(), connect callback. ip: {0}:{1}, {2}", _connectIP, _connectPort, e.SocketError));
		
			switch (e.SocketError)
			{
			case SocketError.Success:
				if (_connectCB != null)
					_connectCB( _connectIP, _connectPort, true, _userData );
			
				Event.fireAll("onConnectStatus", new object[]{true});
				break;

			default:
				if (_connectCB != null)
					_connectCB( _connectIP, _connectPort, false, _userData );
			
				Event.fireAll("onConnectStatus", new object[]{false});
				break;
			}
		}
	    
		public bool connectTo(string ip, int port, ConnectCallback callback, object userData) 
		{
			if (valid())
				throw new InvalidOperationException( "Have already connected!" );
			
			if(!(new Regex( @"((?:(?:25[0-5]|2[0-4]\d|((1\d{2})|([1-9]?\d)))\.){3}(?:25[0-5]|2[0-4]\d|((1\d{2})|([1-9]?\d))))")).IsMatch(ip))
			{
				IPHostEntry ipHost = Dns.GetHostEntry (ip);
				ip = ipHost.AddressList[0].ToString();
			}

			reset();
			
			_connectIP = ip;
			_connectPort = port;
			_connectCB = callback;
			_userData = userData;
			
			bool result = false;
	        
			// Security.PrefetchSocketPolicy(ip, 843);
			socket_ = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); 
			socket_.SetSocketOption (System.Net.Sockets.SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, MemoryStream.BUFFER_MAX);
			
			SocketAsyncEventArgs connectEventArgs = new SocketAsyncEventArgs();
			connectEventArgs.RemoteEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
			connectEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(connectCB);
			Dbg.DEBUG_MSG("connect to " + ip + ":" + port + " ...");
			
			try 
			{ 
				result = socket_.ConnectAsync(connectEventArgs);
            } 
            catch (Exception e) 
            {
				Dbg.WARNING_MSG(string.Format("NetworkInterface::connect(): is error（0）! ip: {1}:{2}", e.ToString(), _connectIP, _connectPort));
				Event.fireAll("onConnectStatus", new object[]{false});
            } 

			if (!result)
			{
				// Completed immediately
				connectCB(this, connectEventArgs);
			}
			
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
