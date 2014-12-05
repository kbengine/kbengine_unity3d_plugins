namespace KBEngine
{
  	using UnityEngine; 
	using System; 
	
	using MessageID = System.UInt16;
	using MessageLength = System.UInt16;
	using MessageLengthEx = System.UInt32;
	
    public class MessageReader
    {
		enum READ_STATE
		{
			// 消息ID
			READ_STATE_MSGID = 0,

			// 消息的长度65535以内
			READ_STATE_MSGLEN = 1,

			// 当上面的消息长度都无法到达要求时使用扩展长度
			// uint32
			READ_STATE_MSGLEN_EX = 2,

			// 消息的内容
			READ_STATE_BODY = 3
		}
		
		private MessageID msgid = 0;
		private MessageLength msglen = 0;
		private MessageLengthEx expectSize = 2;
		private READ_STATE state = READ_STATE.READ_STATE_MSGID;
		private MemoryStream stream = new MemoryStream();
		
		public MessageReader()
		{
		}
		
		public void process(byte[] datas, MessageLengthEx length)
		{
			MessageLengthEx totallen = 0;
			
			while(length > 0 && expectSize > 0)
			{
				if(state == READ_STATE.READ_STATE_MSGID)
				{
					if(length >= expectSize)
					{
						Array.Copy(datas, totallen, stream.data(), stream.wpos, expectSize);
						totallen += expectSize;
						stream.wpos += (int)expectSize;
						length -= expectSize;
						msgid = stream.readUint16();
						stream.clear();

						Message msg = Message.clientMessages[msgid];

						if(msg.msglen == -1)
						{
							state = READ_STATE.READ_STATE_MSGLEN;
							expectSize = 2;
						}
						else
						{
							expectSize = (MessageLengthEx)msg.msglen;
							state = READ_STATE.READ_STATE_BODY;
						}
					}
					else
					{
						Array.Copy(datas, totallen, stream.data(), stream.wpos, length);
						stream.wpos += (int)length;
						expectSize -= length;
						break;
					}
				}
				else if(state == READ_STATE.READ_STATE_MSGLEN)
				{
					if(length >= expectSize)
					{
						Array.Copy(datas, totallen, stream.data(), stream.wpos, expectSize);
						totallen += expectSize;
						stream.wpos += (int)expectSize;
						length -= expectSize;
						
						msglen = stream.readUint16();
						stream.clear();
						
						// 长度扩展
						if(msglen >= 65535)
						{
							state = READ_STATE.READ_STATE_MSGLEN_EX;
							expectSize = 4;
						}
						else
						{
							state = READ_STATE.READ_STATE_BODY;
							expectSize = msglen;
						}
					}
					else
					{
						Array.Copy(datas, totallen, stream.data(), stream.wpos, length);
						stream.wpos += (int)length;
						expectSize -= length;
						break;
					}
				}
				else if(state == READ_STATE.READ_STATE_MSGLEN_EX)
				{
					if(length >= expectSize)
					{
						Array.Copy(datas, totallen, stream.data(), stream.wpos, expectSize);
						totallen += expectSize;
						stream.wpos += (int)expectSize;
						length -= expectSize;
						
						expectSize = stream.readUint32();
						stream.clear();
						
						state = READ_STATE.READ_STATE_BODY;
					}
					else
					{
						Array.Copy(datas, totallen, stream.data(), stream.wpos, length);
						stream.wpos += (int)length;
						expectSize -= length;
						break;
					}
				}
				else if(state == READ_STATE.READ_STATE_BODY)
				{
					if(length >= expectSize)
					{
						stream.append (datas, totallen, expectSize);
						totallen += expectSize;
						length -= expectSize;

						Message msg = Message.clientMessages[msgid];

						msg.handleMessage(stream);
						stream.clear();
						
						state = READ_STATE.READ_STATE_MSGID;
						expectSize = 2;
					}
					else
					{
						stream.append (datas, totallen, length);
						expectSize -= length;
						break;
					}
				}
			}
		}
    }
} 
