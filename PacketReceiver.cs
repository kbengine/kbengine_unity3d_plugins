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
		private MessageReader messageReader = null;
		private NetworkInterface _networkInterface = null;
		private static int BUFFER_BLOCK_SIZE = 1460;
		
		private byte[] _buffer;
		
		// socket向缓冲区写的起始位置
		public int wpos = 0;	
		
		// 主线程读取数据的起始位置
		public int rpos = 0;	
		
		// 当前能写入的区块大小
		public int block = BUFFER_BLOCK_SIZE;
		
        public PacketReceiver(NetworkInterface networkInterface)
        {
        	init(networkInterface);
        }

		void init(NetworkInterface networkInterface)
		{
			_networkInterface = networkInterface;
			BUFFER_BLOCK_SIZE = KBEngineApp.app.getInitArgs().RECV_BUFFER_BLOCK;
			
			_buffer = new byte[BUFFER_BLOCK_SIZE * KBEngineApp.app.getInitArgs().RECV_BUFFER_BLOCK_LIST_SIZE];
			
			messageReader = new MessageReader();
		}
		
		public NetworkInterface networkInterface()
		{
			return _networkInterface;
		}
		
		public void process()
		{
			int t_wpos = Interlocked.Add(ref wpos, 0);
				
			if(rpos < t_wpos)
			{
				messageReader.process(_buffer, (UInt32)rpos, (UInt32)(t_wpos - rpos));
				Interlocked.Exchange(ref rpos, t_wpos);
			} 
			else if(t_wpos < rpos)
			{
				messageReader.process(_buffer, (UInt32)rpos, (UInt32)(_buffer.Length - rpos));
				Interlocked.Exchange(ref rpos, 0);
			}
			else
			{
				// 没有可读数据
			}
		}
		
		public void updateStates()
		{
			int t_rpos = Interlocked.Add(ref rpos, 0);

			block = BUFFER_BLOCK_SIZE;
			
			if(t_rpos <= wpos)
			{
				int iblock = _buffer.Length - wpos;
				if(iblock < BUFFER_BLOCK_SIZE)
				{
					if(iblock == 0)
						Interlocked.Exchange(ref wpos, 0);
					else
						block = iblock;
				}
			}
			else
			{
				int iblock = t_rpos - wpos;
				if(iblock > BUFFER_BLOCK_SIZE)
				{
					block = BUFFER_BLOCK_SIZE;
				}
				else
				{
					block = iblock;
				}
			}
		}
		
		public void startRecv()
		{
			// 必须有空间可写，否则我们阻塞在线程中直到有空间为止
			int first = 0;
			
			while(block <= 0)
			{
				updateStates();
				System.Threading.Thread.Sleep(5);
				
				if(first > 0)
					Dbg.WARNING_MSG("PacketReceiver::startRecv(): wait for space! retries=" + first);
				
				first += 1;
			}
			
			try
			{
				// 此时可以不加锁，没有任何地方会改变wpos, block
				_networkInterface.sock().BeginReceive(_buffer, wpos, block, 0,
			            new AsyncCallback(_onRecv), this);
			}
			catch (Exception e) 
			{
				Dbg.ERROR_MSG("PacketReceiver::startRecv(): call ReceiveAsync() is err: " + e.ToString());
				_networkInterface.close();
			}
		}
		
		private static void _onRecv(IAsyncResult ar)
		{	
			// Retrieve the socket from the state object.
			PacketReceiver state = (PacketReceiver) ar.AsyncState;
				
			try 
			{
				// 由于多线程问题，networkInterface可能已被丢弃了
				// 例如：在连接loginapp之后自动开始连接到baseapp之前会先关闭并丢弃networkInterface
				if(!state.networkInterface().valid())
					return;
				
				Socket client = state.networkInterface().sock();
				
		        // Read data from the remote device.
		        int bytesRead = client.EndReceive(ar);

		        if (bytesRead > 0) 
		        {
		        	// 更新写位置
		        	Interlocked.Add(ref state.wpos, bytesRead);
					state.block = 0;

		            state.startRecv();
		        }
				else
		        {
		        	if (bytesRead == 0) 
		        	{
		        		Dbg.WARNING_MSG(string.Format("PacketReceiver::_processRecved(): disconnect!"));
		        		state.networkInterface().close();
		        		return;
		        	}
		        	else
		        	{
		        		state.startRecv();
		        	}
		        }
			} 
			catch (Exception e) 
			{
				Dbg.ERROR_MSG(string.Format("PacketReceiver::_processRecved(): is error({0})!", e.ToString()));
				state.networkInterface().close();
			}
		}
	}
} 
