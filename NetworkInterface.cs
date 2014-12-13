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
    	
        Socket _socket = null;
		PacketReceiver _packetReceiver = null;
		PacketSender _packetSender = null;
		
		// for connect
		string _connectIP;
		int _connectPort;
		ConnectCallback _connectCB;
		object _userData;
		
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
		
		public PacketReceiver packetReceiver()
		{
			return _packetReceiver;
		}
		
		public bool valid()
		{
			return ((_socket != null) && (_socket.Connected == true));
		}
		
		public void _onConnectStatus(string error)
		{
			KBEngine.Event.deregisterIn(this);
			
			bool success = (error == "");
			
			if(success)
			{
				Dbg.INFO_MSG(string.Format("NetworkInterface::_onConnectStatus(), connected to {0}", sock().RemoteEndPoint.ToString()));
				_packetReceiver = new PacketReceiver(this);
				_packetReceiver.startRecv();
			}
			else
			{
				Dbg.ERROR_MSG(string.Format("NetworkInterface::_onConnectStatus(), connect is error! ip: {0}:{1}, err: {2}", _connectIP, _connectPort, error));
			}
			
			Event.fireAll("onConnectStatus", new object[]{success});
			
			if (_connectCB != null)
				_connectCB(_connectIP, _connectPort, success, _userData);
		}
		
		private static void connectCB(IAsyncResult ar)
		{
			try 
			{
				// Retrieve the socket from the state object.
				NetworkInterface networkInterface = (NetworkInterface) ar.AsyncState;

				// Complete the connection.
				networkInterface.sock().EndConnect(ar);

				Event.fireIn("_onConnectStatus", new object[]{""});
			} 
			catch (Exception e) 
			{
				Event.fireIn("_onConnectStatus", new object[]{e.ToString()});
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

			// Security.PrefetchSocketPolicy(ip, 843);
			_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); 
			_socket.SetSocketOption (System.Net.Sockets.SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, KBEngineApp.app.getInitArgs().getRecvBufferSize() * 2);
			
			_connectIP = ip;
			_connectPort = port;
			_connectCB = callback;
			_userData = userData;
			
			Dbg.DEBUG_MSG("connect to " + ip + ":" + port + " ...");
			
			// 先注册一个事件回调，该事件在当前线程触发
			Event.registerIn("_onConnectStatus", this, "_onConnectStatus");
			
			try 
			{ 
				_socket.BeginConnect(new IPEndPoint(IPAddress.Parse(ip), port), new AsyncCallback(connectCB), this);
            } 
            catch (Exception e) 
            {
				Event.fireIn("_onConnectStatus", new object[]{e.ToString()});
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
        
        public void process()
        {
        	if(!valid())
        		return;
        	
        	if(_packetReceiver != null)
        		_packetReceiver.process();
        }
	}
} 
