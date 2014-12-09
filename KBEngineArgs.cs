namespace KBEngine
{
	using System; 
	
	using MessageLength = System.UInt16;
	
	/*
		初始化KBEngine的参数类
	*/
    public class KBEngineArgs 
    {
    	// 登录ip和端口
		public string ip = "127.0.0.1";
		public UInt16 port = 20013;
		
		
		// 客户端类型
		// Reference: http://www.kbengine.org/docs/programming/clientsdkprogramming.html, client types
		public sbyte clientType = 5;
		
		
		// 持久化插件信息， 例如：从服务端导入的协议可以持久化到本地，下次登录版本不发生改变
		// 可以直接从本地加载来提供登录速度
		public string persistentDataPath = "";
		
		
		// Allow synchronization role position information to the server
		// 是否开启自动同步玩家信息到服务端，信息包括位置与方向
		// 非高实时类游戏不需要开放这个选项
		public bool syncPlayer = true;
		
		
		// 接收与发送缓冲大小
		public MessageLength SEND_BUFFER_MAX = NetworkInterface.TCP_PACKET_MAX;
		public MessageLength RECV_BUFFER_MAX = NetworkInterface.TCP_PACKET_MAX;
		
		// 只在多线程模式启用
		// 主循环tick间隔
		public int HZ_TICK = 100;
    }

} 
