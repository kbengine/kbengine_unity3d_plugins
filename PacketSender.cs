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
		包发送模块(与服务端网络部分的名称对应)
		处理网络数据的发送
	*/
    public class PacketSender 
    {
		private byte[] _buffer;
		
		private static int BUFFER_BLOCK_SIZE = 1460;
		
		public int wpos = 0;				// 写入的数据位置
		public int spos = 0;				// 发送完毕的数据位置
		public int sending = 0;
		
		private NetworkInterface _networkInterface = null;
		
        public PacketSender(NetworkInterface networkInterface)
        {
        	init(networkInterface);
        }

		void init(NetworkInterface networkInterface)
		{
			_networkInterface = networkInterface;
			BUFFER_BLOCK_SIZE = KBEngineApp.app.getInitArgs().SEND_BUFFER_MAX;
			
			_buffer = new byte[BUFFER_BLOCK_SIZE];
			
			wpos = 0; 
			spos = 0;
			sending = 0;
		}

		public NetworkInterface networkInterface()
		{
			return _networkInterface;
		}
		
		public bool send(byte[] datas)
		{
			if(datas.Length <= 0)
				return true;

			int t_spos = Interlocked.Add(ref spos, 0);
			
			// 数据长度溢出则返回错误
			// 剩余空间与已经发送的空间都是可以使用的空间
			if (datas.Length > (_buffer.Length - wpos + t_spos))
			{
				return false;
			}

			int expect_total = wpos + datas.Length;
			
			if(expect_total <= _buffer.Length)
			{
				Array.Copy(datas, 0, _buffer, wpos, datas.Length);
				Interlocked.Add(ref wpos, datas.Length);
			}
			else
			{
				int remain = _buffer.Length - wpos;
				Array.Copy(datas, 0, _buffer, wpos, remain);
				Interlocked.Exchange(ref wpos, expect_total - _buffer.Length);
				Array.Copy(datas, remain, _buffer, 0, wpos);
			}
			
			if(Interlocked.Add(ref sending, 0) == 0)
			{
				Interlocked.Exchange(ref sending, 1);
				startSend();
			}
			
			return true;
		}
		
		public void startSend()
		{
			if(spos >= _buffer.Length)
				Interlocked.Exchange(ref spos, 0);
			
			int sendSize = Interlocked.Add(ref wpos, 0) - spos;
			if(sendSize < 0)
				sendSize = _buffer.Length - spos;
			
			try
			{
				_networkInterface.sock().BeginSend(_buffer, spos, sendSize, 0,
         		   new AsyncCallback(_onSent), this);
			}
			catch (Exception e) 
			{
				Dbg.ERROR_MSG("PacketSender::startSend(): is err: " + e.ToString());
				_networkInterface.close();
			}
		}
		
		private static void _onSent(IAsyncResult ar)
		{
			// Retrieve the socket from the state object.
			PacketSender state = (PacketSender) ar.AsyncState;

			try 
			{
				// 由于多线程问题，networkInterface可能已被丢弃了
				// 例如：在连接loginapp之后自动开始连接到baseapp之前会先关闭并丢弃networkInterface
				if(!state.networkInterface().valid())
					return;

				Socket client = state.networkInterface().sock();
				
				// Complete sending the data to the remote device.
				int bytesSent = client.EndSend(ar);
				
				int spos = Interlocked.Add(ref state.spos, bytesSent);
				
				// 如果数据没有发送完毕需要继续投递发送
				if(spos != Interlocked.Add(ref state.wpos, 0))
				{
					state.startSend();
				}
				else
				{
					// 所有数据发送完毕了
					Interlocked.Exchange(ref state.sending, 0);
				}
			} 
			catch (Exception e) 
			{
				Dbg.ERROR_MSG(string.Format("PacketSender::_processSent(): is error({0})!", e.ToString()));
				state.networkInterface().close();
			}
		}
	}
} 
