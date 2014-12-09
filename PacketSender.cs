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
		
		private NetworkInterface _networkInterface = null;
		SocketAsyncEventArgs _socketEventArgs = null;
		
        public PacketSender(NetworkInterface networkInterface)
        {
        	init(networkInterface);
        }

		void init(NetworkInterface networkInterface)
		{
			_networkInterface = networkInterface;
			_buffer = new byte[KBEngineApp.app.getInitArgs().SEND_BUFFER_MAX];
			
			_wpos = 0; 
			_spos = 0;
		}
		
		public bool send(byte[] datas)
		{
			if(datas.Length <= 0)
				return true;

			// 数据长度溢出则返回错误
			if (datas.Length > _buffer.Length - _wpos)
			{
				Dbg.ERROR_MSG("PacketSender::send(), data length > " + (_buffer.Length - _wpos) + ", _wpos=" + _wpos + ", _spos=" + _spos);
				return false;
			}

			if(_wpos == 0)
			{
				_wpos = datas.Length;
				Array.Copy(datas, 0, _buffer, 0, datas.Length);
				startSend();
			}
			else
			{
				Array.Copy(datas, 0, _buffer, _wpos, datas.Length);
				_wpos += datas.Length;
			}
			
			return true;
		}
		
		public void startSend()
		{
			_socketEventArgs = new SocketAsyncEventArgs();
			_socketEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(_onSend);
			_socketEventArgs.SetBuffer(_buffer, _spos, _wpos - _spos);
			
			try
			{
				if (!_networkInterface.sock().SendAsync(_socketEventArgs))
				{
					_processSent(_socketEventArgs);
				}
			}
			catch (SocketException err)
			{
				Dbg.ERROR_MSG("PacketSender::startSend(): call ReceiveAsync() is err: " + err);
				_networkInterface.close();
			}
		}
		
		void _onSend(object sender, SocketAsyncEventArgs e)
		{
			_processSent(e);
		}
		
		void _processSent(SocketAsyncEventArgs e)
		{
			if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
			{
				_spos += e.BytesTransferred;
				
				// 如果数据没有发送完毕需要继续投递发送
				if(_spos < _wpos)
				{
					startSend();
				}
				else
				{
					// 所有数据发送完毕了
					_spos = 0;
					_wpos = 0;
				}
			}
			else
			{
				Dbg.WARNING_MSG(string.Format("PacketSender::_processSent(): is error({0})! BytesTransferred: {1}", e.SocketError, e.BytesTransferred));
				_networkInterface.close();
			}
		}
	}
} 
