namespace KBEngine
{
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
		包接收模块(与服务端网络部分的名称对应)
		处理网络数据的接收
	*/
    public class PacketReceiver 
    {
    	public const MessageLength RECV_BUFFER_MAX = NetworkInterface.TCP_PACKET_MAX * 3;
    	
		private MessageReader messageReader = null;
		private byte[] _buffer;
		private NetworkInterface _networkInterface = null;
		SocketAsyncEventArgs _socketEventArgs = null;
		
        public PacketReceiver(NetworkInterface networkInterface)
        {
        	init(networkInterface, RECV_BUFFER_MAX);
        }

		public PacketReceiver(NetworkInterface networkInterface, MessageLength buffLen)
		{
			init(networkInterface, buffLen);
		}

		void init(NetworkInterface networkInterface, MessageLength buffLen)
		{
			_networkInterface = networkInterface;
			_buffer = new byte[buffLen];
			messageReader = new MessageReader();
		}
		
		public void startRecv()
		{
			_socketEventArgs = new SocketAsyncEventArgs();
			_socketEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(_onRecv);
			_socketEventArgs.SetBuffer(_buffer, 0, _buffer.Length);
			
			try
			{
				if (!_networkInterface.sock().ReceiveAsync(_socketEventArgs))
				{
					_processRecved(_socketEventArgs);
				}
			}
			catch (SocketException err)
			{
				Dbg.ERROR_MSG("PacketReceiver::startRecv(): call ReceiveAsync() is err: " + err);
				_networkInterface.close();
			}
		}
		
		void _onRecv(object sender, SocketAsyncEventArgs e)
		{
			_processRecved(e);
		}
		
		void _processRecved(SocketAsyncEventArgs e)
		{
			if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
			{
				messageReader.process(_buffer, (MessageLength)e.BytesTransferred);
				startRecv();
			}
			else
			{
				if(e.BytesTransferred == 0)
					Dbg.WARNING_MSG(string.Format("PacketReceiver::_processRecved(): disconnect!"));
				else
					Dbg.ERROR_MSG(string.Format("PacketReceiver::_processRecved(): is error({0})! BytesTransferred: {1}", e.SocketError, e.BytesTransferred));
				
				_networkInterface.close();
			}
		}
	}
} 
