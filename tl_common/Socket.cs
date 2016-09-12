using System;
using System.Collections.Generic;

using tl_utl_dev = tl_common.UtilDev;


namespace tl_common
{

    /// 非同期データ受信のための状態オブジェクト
    public class AsyncStateObject
    {
        public System.Net.Sockets.Socket mSocket;
        public byte[] ReceiveBuffer;
        public System.IO.MemoryStream ReceivedData;

        public AsyncStateObject()
        {
            this.ReceiveBuffer = new byte[ 1024 ];
            this.ReceivedData = new System.IO.MemoryStream();
        }


    }

    /// <summary>
    /// ソケット基底
    /// </summary>
    public class SocketBase
    {
        public SocketBase()
        {

        }

        // 継承先に任せる系
        public virtual void onSocketException( AsyncStateObject so ) { }
        public virtual void OnReceiveData( System.Net.Sockets.Socket socket, System.IO.MemoryStream receivedData ) { }
        public virtual bool tryOnReceiveData( System.Net.Sockets.Socket socket, System.IO.MemoryStream receivedData ) { return false; }


        // データ受信スタート
        public void StartReceive( System.Net.Sockets.Socket socket, AsyncStateObject aso )
        {
            tl_utl_dev.print( "[SocketBase::StartReceive]\n" );

            //非同期受信を開始
            System.IAsyncResult ar = socket.BeginReceive
            (
                aso.ReceiveBuffer,
                0,
                aso.ReceiveBuffer.Length,
                System.Net.Sockets.SocketFlags.None,
                new System.AsyncCallback( ReceiveDataCallback ),
                aso
            );

        }

        private void finalizeWith_( AsyncStateObject so  )
        {
            onSocketException( so );
            so.mSocket.Shutdown( System.Net.Sockets.SocketShutdown.Both );
            so.mSocket.Close();
        }


        // BeginReceiveのコールバック
        private void ReceiveDataCallback( System.IAsyncResult ar )
        {
            tl_utl_dev.print( "[SocketBase::ReceiveDataCallback]\n" );

            // 状態オブジェクトの取得
            AsyncStateObject so = ( AsyncStateObject )ar.AsyncState;

            // 読み込んだ長さを取得
            int len = 0;
            try
            {
                len = so.mSocket.EndReceive( ar );
            }
            catch ( System.Net.Sockets.SocketException e )
            {
                // クライアントが切断した
                tl_utl_dev.print( "[System.Net.Sockets.SocketException] " + e.ErrorCode.ToString() + " exception\n" );
                finalizeWith_( so ) ;
                return;
            }
            catch ( System.ArgumentException )
            {
                // BeginReceive  メソッドへの呼び出しで asyncResult が返されませんでした。
                tl_utl_dev.print( "[System.ArgumentException] exception\n" );
                finalizeWith_( so );
                return;
            }
            catch ( System.ObjectDisposedException )
            {
                // Socket  は閉じられています。
                tl_utl_dev.print( "[System.ObjectDisposedException] exception\n" );
                finalizeWith_( so );
                return;
            }
            catch
            {
                // 謎
                tl_utl_dev.print( "[System.ObjectDisposedException] unknown exception\n" );
                finalizeWith_( so );
                return;

            }

            //切断されたか調べる
            if ( len <= 0 )
            {
                tl_utl_dev.print( "切断されました。\n" );
                finalizeWith_( so );
                return;
            }

            try
            {
                // 受信したデータを蓄積する
                so.ReceivedData.Write( so.ReceiveBuffer, 0, len );
                if ( so.mSocket.Available == 0 )
                {
                    if( tryOnReceiveData( so.mSocket, so.ReceivedData ) )
                    {
                        OnReceiveData( so.mSocket, so.ReceivedData );

                        so.ReceivedData.Close();

                        so.ReceivedData = new System.IO.MemoryStream();
                    }

                    

                }

                // 再び受信開始
                so.mSocket.BeginReceive
                (
                    so.ReceiveBuffer,
                    0,
                    so.ReceiveBuffer.Length,
                    System.Net.Sockets.SocketFlags.None,
                    new System.AsyncCallback( ReceiveDataCallback ),
                    so
                );

            }
            catch( Exception e )
            {
                tl_utl_dev.print( "[SocketBase::RecieveDataCallback] " + e.Message + " exception\n" );
            }


        }

        public static int Send( System.Net.Sockets.Socket socket, byte[] data )
        {
            if ( null == data )
            {
                tl_utl_dev.print( "[SocketBase::Send] data is null\n" );
                return -1;
            }

            try
            {
                int nSize = socket.Send( data );
                if ( -1 == nSize )
                {
                    tl_utl_dev.print( "[SocketBase::Send] fail\n" );
                }
                tl_utl_dev.print( "[SocketBase::Send] done : " + nSize.ToString()+ "\n" );
                return nSize;
            }
            catch ( System.Exception e )
            {
                tl_utl_dev.print( "[SocketBase::Send] fail(catch) : " + e.Message + "\n" );
                return -1;
            }

        }

        public static int Send( SocketServer.ClientInfo ci, byte[] data )
        {
            try
            {
                int nSize = Send( ci.mClient, data );
                if ( -1 == nSize )
                {
                    tl_utl_dev.print( "[SocketBase::Send2] fail to : " + ci.mUserName + "\n" );
                }

                return nSize;
            }
            catch
            {

            }

            return -1;
        }



    }


    /// <summary>
    /// サーバ
    /// </summary>
    public class SocketServer : SocketBase
    {
        public System.Net.Sockets.Socket mServer;
        public List<ClientInfo> mClients;

        public class ClientInfo
        {
            public System.Net.Sockets.Socket mClient;
            public AsyncStateObject mReceiveObject;
            public string mUserName;
            public int mnId;

            public ClientInfo( System.Net.Sockets.Socket ci )
            {
                mClient = ci;
                mReceiveObject = new AsyncStateObject();
                mUserName = "";
                mnId = -1;
            }

        }

        public SocketServer( System.Net.IPEndPoint endpoint )
        {
            mClients = new List<ClientInfo>();

            mServer = new System.Net.Sockets.Socket
                          (
                              System.Net.Sockets.AddressFamily.InterNetwork,
                              System.Net.Sockets.SocketType.Stream,
                              System.Net.Sockets.ProtocolType.Tcp
                          );
            mServer.SetSocketOption( System.Net.Sockets.SocketOptionLevel.Socket, System.Net.Sockets.SocketOptionName.KeepAlive, true );

            mServer.Bind( endpoint );
            mServer.Listen( 100 );
            StartAccept();

        }

        public void StartAccept()
        {
            tl_utl_dev.print( "[SocketServer::StartAccept]\n" );
            mServer.BeginAccept( new System.AsyncCallback( AcceptCallback ), mServer );
        }

        public void AcceptCallback( System.IAsyncResult ar )
        {
            tl_utl_dev.print( "[SocketServer::AcceptCallback] \n" );

            System.Net.Sockets.Socket client_tmp = null;

            //接続要求を受け入れる
            try
            {
                //クライアントSocketの取得
                client_tmp = mServer.EndAccept( ar );
            }
            catch
            {
                tl_utl_dev.print( "閉じました。" );
                return;
            }


            // リストに追加
            ClientInfo ci = addClient( client_tmp );

            // 受信開始
            startReceive( client_tmp, ci.mReceiveObject );

            // 接続待機も開始
            StartAccept();

            // リクエストがあったコールバック
            OnRequestConnectFromClient( ci );

        }

        private void startReceive( System.Net.Sockets.Socket client, AsyncStateObject aso )
        {
            aso.mSocket = client;
            StartReceive( client, aso );
        }

        /// <summary>
        /// clientが切断した
        /// </summary>
        /// <param name="so"></param>
        public override void onSocketException( AsyncStateObject so )
        {
            tl_utl_dev.print( "[SocketServer::onSocketException]\n" );

            removeClient( so.mSocket );
        }

        private ClientInfo addClient( System.Net.Sockets.Socket client )
        {
            ClientInfo ci = new ClientInfo( client );
            mClients.Add( ci );
            return ci;
        }

        void removeClient( System.Net.Sockets.Socket client )
        {
            /// リストから該当するクライアントを除く
            lock( mClients )
            {
                ClientInfo ci = mClients.Find
                                (
                                    delegate ( ClientInfo tmp )
                                    {
                                        if ( tmp.mClient == client )
                                        {
                                            return true;
                                        }
                                        return false;
                                    }
                                );
                if ( null != ci )
                {
                    OnClientLeave( ci );
                    try
                    {
                        if( !mClients.Remove( ci ) )
                        {
                            tl_utl_dev.print( "[SocketServer::removeClient] fail to remove 1\n" );
                        }
                    }
                    catch
                    {
                        tl_utl_dev.print( "[SocketServer::removeClient] fail to remove 2\n" );
                    }
                }
            }
        }

        public ClientInfo findClient( int nClientId )
        {
            return mClients.Find
                            (
                                delegate( ClientInfo ci )
                                {
                                    if( ci.mnId == nClientId )
                                    {
                                        return true ;
                                    }
                                    return false;
                                }
                            );
        }
        public ClientInfo findClient( string name )
        {
            return mClients.Find
                            (
                                delegate ( ClientInfo ci )
                                {
                                    if ( ci.mUserName == name )
                                    {
                                        return true;
                                    }
                                    return false;
                                }
                            );
        }

        // 継承先で処理する
        public virtual void OnRequestConnectFromClient( SocketServer.ClientInfo ci ) { }
        public virtual void OnClientLeave( SocketServer.ClientInfo ci ) { }



    }

    /// <summary>
    /// クライアント
    /// </summary>
    public class SocketClient : SocketBase
    {
        public System.Net.Sockets.Socket mClient;
        public AsyncStateObject mReceiveObject;

        bool mbDisconnect;

        public SocketClient()
        {
            mReceiveObject = new AsyncStateObject();

            mClient = new System.Net.Sockets.Socket
                      (
                          System.Net.Sockets.AddressFamily.InterNetwork,
                          System.Net.Sockets.SocketType.Stream,
                          System.Net.Sockets.ProtocolType.Tcp
                      );
            mReceiveObject.mSocket = mClient;
            mClient.SetSocketOption( System.Net.Sockets.SocketOptionLevel.Socket, System.Net.Sockets.SocketOptionName.KeepAlive, true );

            mbDisconnect = false;

        }

        /// <summary>
        /// サーバへ接続
        /// </summary>
        /// <returns></returns>
        public bool connectToServer( System.Net.IPEndPoint endpoint )
        {
            try
            {
                mClient.Connect( endpoint );
                startReceive( mReceiveObject );
            }
            catch
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 切断
        /// </summary>
        /// <returns></returns>
        public bool disconnect()
        {
            try
            {
                mbDisconnect = true;
                mClient.Disconnect( false );
            }
            catch
            {
                return false;
            }


            return true;
        }

        /// <summary>
        /// 受信開始
        /// </summary>
        /// <param name="aso"></param>
        private void startReceive( AsyncStateObject aso )
        {
            StartReceive( mClient, aso );
        }

        /// <summary>
        /// サーバ落ちたくさい
        /// </summary>
        /// <param name="so"></param>
        public override void onSocketException( AsyncStateObject so )
        {
            tl_utl_dev.print( "server may shutdown\n" );

            if ( mbDisconnect )
            {
                return;
            }
            OnClientDisconnect();
        
            
        }

        // 継承先で処理する
        public virtual void OnClientDisconnect() { }



    }



}



// end of file
