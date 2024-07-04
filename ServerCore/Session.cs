using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore
{
    public abstract class PacketSession : Session
    {
        public static readonly int HeaderSize = 2;
        public sealed override int OnRecv(ArraySegment<byte> buffer)
        {
            int processLen = 0;

            while (true)
            {
                if(buffer.Count < HeaderSize) { break; }

                // 페킷이 완전체로 도착했는지 확인
                ushort dataSize = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
                if(buffer.Count < dataSize) { break; }

                // 여기까지 통과했으면 패킷 조립 가능
                OnRecvPacket(new ArraySegment<byte>(buffer.Array, buffer.Offset, dataSize));

                processLen += dataSize;
                buffer = new ArraySegment<byte>(buffer.Array, buffer.Offset+ dataSize, buffer.Count - dataSize);
            }

            return processLen;
        }

        public abstract void OnRecvPacket(ArraySegment<byte> buffer);
    }
    public abstract class Session
    {
        Socket _socket;
        int _disconnected = 0;

        RecvBuffer _recvBuffer = new RecvBuffer(65535);

        object _lock = new object();

        Queue<ArraySegment<byte>> _sendQueue = new Queue<ArraySegment<byte>>();
        List<ArraySegment<byte>> _penddingList = new List<ArraySegment<byte>>();
        SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();
        SocketAsyncEventArgs _recvArgs = new SocketAsyncEventArgs();

        public abstract void OnConnected(EndPoint endPoint);
        public abstract int OnRecv(ArraySegment<byte> buffer);
        public abstract void OnSend(int numOfByte);
        public abstract void OnDisconnected(EndPoint endPoint);
        
        void Clear()
        {
            lock (_lock)
            {
                _sendQueue.Clear();
                _penddingList.Clear();
            }
        
        }

        public void Start(Socket socket)
        {
            _socket = socket;

            _recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted);
            _sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);
            RegisterRecv();
        }
        public void Send(List<ArraySegment<byte>> sendbuffer)
        {
            if (sendbuffer.Count == 0) { return; }

            lock (_lock)
            {
                foreach(ArraySegment<byte> buffer in sendbuffer)
                {
                    _sendQueue.Enqueue(buffer);
                }
                
                if (_penddingList.Count == 0)
                {
                    RegisterSend();
                }
            }
        }

        public void Send(ArraySegment<byte> sendbuffer)
        {
            lock (_lock) 
            {
                _sendQueue.Enqueue(sendbuffer);
                if (_penddingList.Count == 0)
                {
                    RegisterSend();
                }
            }            
        }

        public void Disconnect()
        {
            if(Interlocked.Exchange(ref _disconnected, 1)== 1)
            {
                return;
            }

            OnDisconnected(_socket.RemoteEndPoint);
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
            Clear();
        }


        #region 네트워크 통신

        void RegisterSend()
        {            
            if(_disconnected == 1) { return; }

            while (_sendQueue.Count > 0)
            {
                ArraySegment<byte> buffer = _sendQueue.Dequeue();
                _penddingList.Add(buffer);                
            }

            _sendArgs.BufferList = _penddingList;

            try
            {
                bool pending = _socket.SendAsync(_sendArgs);
                if (pending == false)
                {
                    OnSendCompleted(null, _sendArgs);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"RegisterSend Faild {e.ToString()}"); 
            }

           
        }

        void OnSendCompleted(object sender, SocketAsyncEventArgs args)
        {
            lock (_lock)
            {
                if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
                {
                    try
                    {
                        _sendArgs.BufferList = null;
                        _penddingList.Clear();

                        OnSend(_sendArgs.BytesTransferred);

                        if (_sendQueue.Count > 0)
                        {
                            RegisterSend();
                        }                       
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"OnSendCompleted Failed {e}");
                    }
                }
                else
                {
                    Disconnect();
                }
            }
        }

        void RegisterRecv()
        {
            if (_disconnected == 1) { return; }

            _recvBuffer.Clean();
            ArraySegment<byte> segment = _recvBuffer.WriteSegment;
            _recvArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);
            
            try
            {
                bool pending = _socket.ReceiveAsync(_recvArgs);
                if (pending == false)
                {
                    OnRecvCompleted(null, _recvArgs);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"RegisterRecv Faild {e.ToString()}");
            }
        }

        void OnRecvCompleted(object sender, SocketAsyncEventArgs args) 
        {
            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success) 
            {
                try
                {
                    // Write 커서 이동
                    if(_recvBuffer.OnWrite(args.BytesTransferred)==false)
                    {
                        Disconnect();
                        return;
                    }

                    // 컨텐츠 쪽으로 데이털르 넘겨 주고 얼마나 처리했는지 받는다.
                    int processLen = OnRecv(_recvBuffer.ReadSegment);
                    if (processLen < 0 || _recvBuffer.DataSize < processLen)
                    {
                        Disconnect();
                        return;
                    }

                    // Read 커서 이동
                    if(_recvBuffer.OnRead(processLen) == false)
                    {
                        Disconnect();
                        return;
                    }

                    RegisterRecv();
                }
                catch (Exception e) 
                {
                    Console.WriteLine($"OnRecvCompleted Failed {e}");
                }                
            }
            else
            {
                Disconnect();
            }
        }
        #endregion
    }
}
