using System;
using System.Collections.Generic;
using System.Linq;

using tl_utl_dev = tl_common.UtilDev;

namespace tl_common
{
    public class TlServer : SocketServer
    {
        public enum ServerNotifyEvent
        {
            cLast
        };
        public delegate void OnNotifyEvent( ServerNotifyEvent eNotifyEvent, System.Collections.Generic.Dictionary<string, string> sz );

        public OnNotifyEvent mOnNotifyEvent;

        System.Threading.Thread mTaskCommandThread ;
        bool mbRunTaskCommandThread;

        tl_common.TaskList mTaskList;
        tl_common.TaskCommander mTaskCommander;
        tl_common.TaskBuilder mTaskBuilder;

        tl_common.TlTeamInfo mTeamInfo;

        private List<tl_common.TaskCommand> mTmpCommandList;
        private int mnTaskCommandCounter;
        private int mnUserCounter;

        public TlServer( TlSetting setting ) : base( setting.GetIpEndPoint() )
        {
            tl_utl_dev.print("[TlServer::TlServer]\n");

            mTmpCommandList = new List<TaskCommand>();
            mnTaskCommandCounter = 0;
            mnUserCounter = 0;

            // チーム情報
            mTeamInfo = new TlTeamInfo(setting);
#if true
            mTeamInfo.addMember("ieyasu","徳川家康");
            mTeamInfo.addMember("hideyoshi", "豊臣秀吉");
            mTeamInfo.addMember("nobunaga","織田信長");
            mTeamInfo.addState("新規", "Yellow");
            mTeamInfo.addState("見た", "White");
            mTeamInfo.addState("作業中", "Pink");
            mTeamInfo.addState("対処", "LightGreen");
            mTeamInfo.addState("確認済", "Gray");
            mTeamInfo.addState("中断", "LightGray");
            mTeamInfo.save();
#else
            mTeamInfo.load();
#endif
            mTeamInfo.print();



            // タスクリスト
            mTaskList = new tl_common.TaskList();
            mTaskBuilder = new tl_common.TaskBuilder( ref mTaskList );
            mTaskCommander = new tl_common.TaskCommander();

            /// ファイルを読む
            mTaskBuilder.loadCommandsFromBinaly();
            mnTaskCommandCounter = mTaskBuilder.mrTaskList.mTaskCollection.Count;
            tl_utl_dev.print( "[TlServer::TlServer] tasknum : " + mTaskBuilder.mnTaskCounter.ToString() + "  taskcommandnum : " + mnTaskCommandCounter.ToString()+ "\n" );

            mbRunTaskCommandThread = true;
            mTaskCommandThread = new System.Threading.Thread( new System.Threading.ThreadStart( TaskCommandThread ) );
            mTaskCommandThread.Start();

        }

        /// <summary>
        /// タスクコマンドを処理するスレッド
        /// </summary>
        private void TaskCommandThread()
        {
            while( mbRunTaskCommandThread )
            {
                System.Threading.Thread.Sleep(100);

                if( mTmpCommandList.Count > 0 )
                {
                    tl_utl_dev.print( "[TaskCommandThread::TaskCommandThread] " + mTmpCommandList.Count + "\n" );

                    lock( mTmpCommandList )
                    {
                        foreach( TaskCommand tc in mTmpCommandList )
                        {
                            // すべてのタスクコマンドに通しの番号を付ける
                            tc.mnId = mnTaskCommandCounter;
                            ++mnTaskCommandCounter;

                            // 最終更新日時をつける
                            System.DateTime dt = System.DateTime.Now;
                            tc.mLastUpdate = dt.ToString( "yyyy/MM/dd HH:mm" );

                            // 自分のタスクを構築
                            mTaskBuilder.applyCommand( tc, null, null );

                            // 全員に送る
                            foreach ( ClientInfo ci in mClients )
                            {
                                SendData sd = new SendData( SendCode.cSendTaskCommand, tc );
                                sd.mBin.mData[ 0 ] = "";        // ユーザ名
                                sd.mBin.mData[ 1 ] = "";  // 認証用のユーザ番号
                                SocketBase.Send( ci, sd.serialize() );
                            }

                            

                        }
                        /// 消す
                        mTmpCommandList.Clear();
                    }

                }

            }

            /// セーブ
            mTaskBuilder.saveCommands();
            mTaskBuilder.saveCommandsToBinaly();

        }

        public void stop()
        {
            tl_utl_dev.print( "[TlServer::stop]\n" );
            mbRunTaskCommandThread = false;
        }

        /// <summary>
        /// クライアントからの接続問いあわせ
        /// </summary>
        /// <param name="ci"></param>
        public override void OnRequestConnectFromClient( SocketServer.ClientInfo ci )
        {
            tl_utl_dev.print( "[TlServer::OnRequestConnectFromClient] userid : " + mnUserCounter.ToString() + "\n" );
            ci.mnId = mnUserCounter;
            ++mnUserCounter;

            SendData sd = new SendData( SendCode.cGetClientName );
            sd.mBin.mData[ 0 ] = TlSetting.cProtocolVersion; // プロトコルバージョンを送る
            sd.mBin.mData[ 1 ] = ci.mnId.ToString();         // 認証用のユーザ番号
            SocketBase.Send( ci, sd.serialize() );

        }

        /// <summary>
        /// クライアントが去った
        /// </summary>
        /// <param name="ci"></param>
        public override void OnClientLeave( SocketServer.ClientInfo ci )
        {
            tl_utl_dev.print( "[TlServer::OnClientLeave]\n" );

        }

        private void sendInitClient_(ClientInfo ci)
        {
            tl_utl_dev.print("[TlServer::sendInitClient_] ok : " + ci.mUserName + "\n");

            SendData sd = new SendData(SendCode.cInitClient);
            sd.mBin.mData[0] = ci.mUserName;        // ユーザ名
            sd.mBin.mData[1] = ci.mnId.ToString();  // 認証用のユーザ番号
            SocketBase.Send(ci, sd.serialize());

        }

        private void sendTeamInfo_(ClientInfo ci)
        {
            /// クライアントにチーム情報を返してあげる
            SendData sd = new SendData(SendCode.cSendTeamInfo);
            //sd.mBin.mData[ 0 ] = "";        // ユーザ名
            //sd.mBin.mData[ 1 ] = "";  // 認証用のユーザ番号
            sd.mBuffer = tl_utl_dev.doSerialize(mTeamInfo.mBin, typeof(TlTeamInfo.Bin));
            SocketBase.Send(ci, sd.serialize());

        }


        public override bool tryOnReceiveData( System.Net.Sockets.Socket socket, System.IO.MemoryStream receivedData )
        {
            byte[] data = receivedData.ToArray();

            /// 最初の４バイトにサイズが入ってる
            int nBufSize = BitConverter.ToInt32( data, 0 );

            /// それより大きかったら、少なくともOK
            if ( data.Length >= nBufSize )
            {
                return true;
            }

            return false;
        }

        public override void OnReceiveData( System.Net.Sockets.Socket socket, System.IO.MemoryStream receivedData )
        {

            byte[] data = receivedData.ToArray();
            int nRestSize = data.ToArray().Length;

            //tl_utl_dev.print( "[TlClient::OnReceiveData] datasize : " + nRestSize.ToString() + "\n" );

            while ( nRestSize > 0 )
            {

                SendData rd = new SendData( data );

                //tl_utl_dev.print( "[TlClient::OnReceiveData] sendcode : " + rd.meSendCode.ToString() + " size : " + rd.mnBufSize + "\n" );

                if ( null != rd && SendCode.cLast != rd.meSendCode )
                {
                    switch ( rd.meSendCode )
                    {
                        case SendCode.cSetClientName:
                            {
                                tl_utl_dev.print( "[TlServer::OnReceiveData] cSetClientName : " + rd.mBin.mData[ 0 ] + " id : " + rd.mBin.mData[ 1 ] + "\n" );

                                lock ( mClients )
                                {
                                    ClientInfo ci_check = findClient( rd.mBin.mData[ 0 ] );

                                    /// なんかすでにいる
                                    if ( null != ci_check )
                                    {
                                        tl_utl_dev.print( "[TlServer::OnReceiveData] cSetClientName : " + rd.mBin.mData[ 0 ] + " の強制退去\n" );
                                        // todo : 強制退去処理

                                    }

                                    ClientInfo ci = findClient( Int32.Parse( rd.mBin.mData[ 1 ] ) );
                                    if ( null != ci )
                                    {
                                        ci.mUserName = rd.mBin.mData[ 0 ];
                                        sendInitClient_( ci );
                                        sendTeamInfo_(ci);
                                    }
                                    else
                                    {
                                        tl_utl_dev.print( "[TlServer::OnReceiveData] cSetClientName : " + rd.mBin.mData[ 0 ] + " がいない・・\n" );

                                    }


                                }


                                break;
                            }
                        case SendCode.cSendTaskCommand:
                            {
                                tl_utl_dev.print( "[TlServer::OnReceiveData] cSendTaskCommand : from : " + rd.mBin.mData[ 0 ] + " size : " + rd.mnBufSize + " recvsize: " + receivedData.GetBuffer().Length + "\n" );

                                lock( mTmpCommandList )
                                {
                                    mTmpCommandList.Add( rd.mTaskCommands[ 0 ] );
                                }

                                break;
                            }
                        case SendCode.cRequestTaskCommand:
                            {
                                tl_utl_dev.print( "[TlServer::OnReceiveData] cRequestTaskCommand : from : " + rd.mBin.mData[ 0 ] + " commandnum : " + rd.mBin.mData[ 2 ] + "\n" );

                                int nClientCommandNum = Int32.Parse( rd.mBin.mData[ 2 ] );

                                if( mTaskBuilder.mCommandList.Count > nClientCommandNum )
                                {
                                    for( int nCommand = nClientCommandNum ; nCommand < mTaskBuilder.mCommandList.Count ; ++nCommand )
                                    {
                                        TaskCommand tc = mTaskBuilder.mCommandList.ElementAt( nCommand ) ;
                                        SendData sd = new SendData( SendCode.cSendTaskCommand, tc );
                                        //sd.mBin.mData[ 0 ] = "";        // ユーザ名
                                        //sd.mBin.mData[ 1 ] = "";  // 認証用のユーザ番号
                                        SocketBase.Send( socket, sd.serialize() );
                                    }

                                }

                                break;
                            }
                        case SendCode.cRequestTeamInfo:
                            {
                                tl_utl_dev.print("[TlServer::OnReceiveData] cRequestTeamInfo : from : " + rd.mBin.mData[0] + " size : " + rd.mnBufSize + " recvsize: " + receivedData.GetBuffer().Length + "\n");

                                /// クライアントにチーム情報を返してあげる
                                //SendData sd = new SendData(SendCode.cSendTeamInfo);
                                //sd.mBin.mData[ 0 ] = "";        // ユーザ名
                                //sd.mBin.mData[ 1 ] = "";  // 認証用のユーザ番号
                                //sd.mBuffer = tl_utl_dev.doSerialize( mTeamInfo.mBin, typeof(TlTeamInfo.Bin) );
                                //SocketBase.Send(socket, sd.serialize());
                                ClientInfo ci = findClient(Int32.Parse(rd.mBin.mData[1]));
                                sendTeamInfo_(ci);

                                break;
                            }
                    }
                }
                else
                {

                }

                nRestSize -= rd.mnBufSize;
                if ( nRestSize > 0 )
                {
                    byte[] data_tmp = new byte[ nRestSize ];
                    Array.Copy( data, rd.mnBufSize, data_tmp, 0, nRestSize );
                    data = data_tmp;
                }

            }

            //tl_utl_dev.print( "[TlClient::OnReceiveData] end\n" );


        }

    }


}


// end of file
