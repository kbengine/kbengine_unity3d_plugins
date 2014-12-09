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
		
        Socket _socket = null;
		PacketReceiver _packetReceiver = null;
		PacketSender _packetSender = null;
		
        public NetworkInterface()
        {
        	reset();
        }
		
		public void reset()
		{
			if(valid())
			{
         	   _socket.Close(0);
			}
			_socket = null;
			_packetReceiver = null;
			_packetSender = null;
			
			_connectIP = "";
			_connectPort = 0;
			_connectCB = null;
			_userData = null;
		}
		
		public Socket sock()
		{
			return _socket;
		}
		
		public bool valid()
		{
			return ((_socket != null) && (_socket.Connected == true));
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
				
				_packetReceiver = new PacketReceiver(this);
				_packetReceiver.startRecv();
				
				break;

			default:
				if (_connectCB != null)
					_connectCB( _connectIP, _connectPort, false, _userData );
			
				Event.fireAll("onConnectStatus", new object[]{false});
				break;
			}
		}
	    
		public void connectTo(string ip, int port, ConnectCallback callback, object userData) 
		{
			if (valid())
				throw new InvalidOperationException( "Have already connected!" );
			
			if(!(new Regex( @"((?:(?:25[0-5]|2[0-4]\d|((1\d{2})|([1-9]?\d)))\.){3}(?:25[0-5]|2[0-4]\d|((1\d{2})|([1-9]?\d))))")).IsMatch(ip))
			{
				IPHostEntry ipHost = Dns.GetHostEntry (ip);
				ip = ipHost.AddressList[0].ToString();
			}
			
			_connectIP = ip;
			_connectPort = port;
			_connectCB = callback;
			_userData = userData;
			
			bool result = false;
	        
			// Security.PrefetchSocketPolicy(ip, 843);
			_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); 
			_socket.SetSocketOption (System.Net.Sockets.SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, MemoryStream.BUFFER_MAX);
			
			SocketAsyncEventArgs connectEventArgs = new SocketAsyncEventArgs();
			connectEventArgs.RemoteEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
			connectEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(connectCB);
			Dbg.DEBUG_MSG("connect to " + ip + ":" + port + " ...");
			
			try 
			{ 
				result = _socket.ConnectAsync(connectEventArgs);
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
		}
        
        public void close()
        {
           if(_socket != null)
			{
				_socket.Close(0);
				_socket = null;
				Event.fireAll("onDisableConnect", new object[]{});
            }

            _socket = null;
        }

        public bool send(byte[] datas)
        {
			if(!valid()) 
			{
			   throw new ArgumentException ("invalid socket!");
			}
			
			if(_packetSender == null)
				_packetSender = new PacketSender(this);
			
			try
			{
				return _packetSender.send(datas);
			}
			catch (SocketException err)
			{
				Dbg.ERROR_MSG(string.Format("NetworkInterface::send(): socket error(" + err.ErrorCode + ")!"));
				close();
			}
			
			return false;
        }
	}
} 
