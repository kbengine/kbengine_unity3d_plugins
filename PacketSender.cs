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

		int _wpos = 0;				// 写入的数据位置
		int _spos = 0;				// 发送完毕的数据位置
		int _sending = 0;
		
		private NetworkInterface _networkInterface = null;
		
        public PacketSender(NetworkInterface networkInterface)
        {
        	_init(networkInterface);
        }

		void _init(NetworkInterface networkInterface)
		{
			_networkInterface = networkInterface;
			
			_buffer = new byte[KBEngineApp.app.getInitArgs().SEND_BUFFER_MAX];
			
			_wpos = 0; 
			_spos = 0;
			_sending = 0;
		}

		public NetworkInterface networkInterface()
		{
			return _networkInterface;
		}
		
		int _free(int t_spos)
		{
			// 数据长度溢出则返回错误
			// 剩余空间与已经发送的空间都是可以使用的空间
			int space = t_spos - _wpos;
			if(space <= 0)
				space = (_buffer.Length - _wpos + t_spos);

			return space;
		}
		
		public bool send(byte[] datas)
		{
			if(datas.Length <= 0)
				return true;
			
			int t_spos = Interlocked.Add(ref _spos, 0);
			
			int space= _free(t_spos);
			if (datas.Length > space)
			{
				Dbg.ERROR_MSG("PacketSender::hasFree(): no space! data(" + datas.Length 
					+ ") > space(" + space + "), wpos=" + _wpos + ", spos=" + t_spos);
				
				return false;
			}

			int expect_total = _wpos + datas.Length;
			
			// 如果总长度不超过_buffer，那么结合前面获得的space大小可以断定
			// _spos小于_wpos, _wpos后面的空间可以全部用来填充
			// 否则先填充尾部数据，剩余的数据从头部开始填充
			if(expect_total <= _buffer.Length)
			{
				if(t_spos > _wpos && expect_total >= t_spos)
				{
					Dbg.ERROR_MSG("wpos=" + _wpos + " > spos=" + t_spos + ", expect_total=" + 
						expect_total + ", buffer=" + _buffer.Length);
					
					throw new Exception("t_spos > _wpos");
				}
				
				Array.Copy(datas, 0, _buffer, _wpos, datas.Length);
				Interlocked.Add(ref _wpos, datas.Length);
			}
			else
			{
				if(t_spos > _wpos)
				{
					int remain = t_spos - _wpos;
					Array.Copy(datas, 0, _buffer, _wpos, remain);
					Interlocked.Add(ref _wpos, t_spos);
				}
				else
				{
					int remain = _buffer.Length - _wpos;
					Array.Copy(datas, 0, _buffer, _wpos, remain);
					Interlocked.Exchange(ref _wpos, expect_total - _buffer.Length);
					Array.Copy(datas, remain, _buffer, 0, _wpos);
				}
			}
			
			if(Interlocked.Add(ref _sending, 0) == 0)
			{
				Interlocked.Exchange(ref _sending, 1);
				_startSend();
			}
			
			return true;
		}
		
		void _startSend()
		{
			if(_spos >= _buffer.Length)
				Interlocked.Exchange(ref _spos, 0);
			
			int sendSize = Interlocked.Add(ref _wpos, 0) - _spos;
			if(sendSize < 0)
				sendSize = _buffer.Length - _spos;
			
			try
			{
				_networkInterface.sock().BeginSend(_buffer, _spos, sendSize, 0,
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
				
				int spos = Interlocked.Add(ref state._spos, bytesSent);
				
				// 如果数据没有发送完毕需要继续投递发送
				if(spos != Interlocked.Add(ref state._wpos, 0))
				{
					state._startSend();
				}
				else
				{
					// 所有数据发送完毕了
					Interlocked.Exchange(ref state._sending, 0);
				}
			} 
			catch (Exception e) 
			{
				Dbg.ERROR_MSG(string.Format("PacketSender::_processSent(): is error({0})!", e.ToString()));
				state.networkInterface().close();
				Interlocked.Exchange(ref state._sending, 0);
			}
		}
	}
} 
