using System;
using System.Collections.Generic;
using System.Linq;

using tl_utl_dev = tl_common.UtilDev;

namespace tl_common
{
    public class TlClient : SocketClient
    {
        public enum NotifyEvent
        {
            cOnDifferentVersion,
            cOnInitClient,
            cOnTaskCommand,
            cOnTeamInfo,
            cLast
        };
        public delegate void OnNotifyEvent( NotifyEvent eNotifyEvent, System.Collections.Generic.Dictionary<string,string> sz, TaskCommand[] tc,object data);

        public OnNotifyEvent mOnNotifyEvent;

        private string mUserName;
        private string mClientId;

        public TlClient( OnNotifyEvent ne, string username )
        {
            mOnNotifyEvent = ne;
            mUserName = username;

        }

        public bool connect( TlSetting setting  )
        {
            try
            {
                return connectToServer( setting.GetIpEndPoint() );
            }
            catch
            {
                return false;
            }
        }

        public bool sendTaskCommand( tl_common.TaskCommand tc )
        {
            SendData sd = new SendData( SendCode.cSendTaskCommand, tc );
            sd.mBin.mData[ 0 ] = mUserName;  // ユーザ名
            sd.mBin.mData[ 1 ] = mClientId;  // 認証用のユーザ番号
            SocketBase.Send( mClient, sd.serialize() );

            return true;
        }

        public bool sendRequestTaskCommand( int nCurrentTaskCommandNum )
        {
            SendData sd = new SendData( SendCode.cRequestTaskCommand );
            sd.mBin.mData[ 0 ] = mUserName;  // ユーザ名
            sd.mBin.mData[ 1 ] = mClientId;  // 認証用のユーザ番号
            sd.mBin.mData[ 2 ] = nCurrentTaskCommandNum.ToString();
            SocketBase.Send( mClient, sd.serialize() ) ;
            return true;
        }
        public bool sendRequestTeamInfo()
        {
            SendData sd = new SendData(SendCode.cRequestTeamInfo);
            sd.mBin.mData[0] = mUserName;  // ユーザ名
            sd.mBin.mData[1] = mClientId;  // 認証用のユーザ番号
            SocketBase.Send(mClient, sd.serialize());
            return true;
        }
        public override bool tryOnReceiveData( System.Net.Sockets.Socket socket, System.IO.MemoryStream receivedData )
        {
            byte[] data = receivedData.ToArray();

            /// 最初の４バイトにサイズが入ってる
            int nBufSize = BitConverter.ToInt32( data, 0 );

            /// それより大きかったら、少なくともOK
            if( data.Length >= nBufSize  )
            {
                return true ;
            }

            return false;
        }


        public override void OnReceiveData( System.Net.Sockets.Socket socket, System.IO.MemoryStream receivedData )
        {
            
            byte[] data = receivedData.ToArray();
            int nRestSize = data.ToArray().Length;

            //tl_utl_dev.print( "[TlClient::OnReceiveData] datasize : " + nRestSize.ToString() + "\n" );

            while( nRestSize > 0 )
            {

                SendData rd = new SendData( data );

                //tl_utl_dev.print( "[TlClient::OnReceiveData] sendcode : " + rd.meSendCode.ToString() + " size : " + rd.mnBufSize + "\n" );

                if ( null != rd && SendCode.cLast != rd.meSendCode )
                {
                    switch ( rd.meSendCode )
                    {
                        case SendCode.cGetClientName:
                            {
                                tl_utl_dev.print( "[TlClient::OnReceiveData] cGetClientName : " + rd.mBin.mData[ 0 ] + "\n" );

                                // バージョンチェック
                                if ( rd.mBin.mData[ 0 ] != TlSetting.cProtocolVersion )
                                {
                                    Dictionary<string, string> dic = new Dictionary<string, string>();
                                    dic[ "ServerProtocolVersion" ] = rd.mBin.mData[ 0 ];
                                    dic[ "ClientProtocolVersion" ] = TlSetting.cProtocolVersion;
                                    mOnNotifyEvent( NotifyEvent.cOnDifferentVersion, dic, null,null );
                                }
                                else
                                {
                                    SendData sd = new SendData( SendCode.cSetClientName );
                                    sd.mBin.mData[ 0 ] = mUserName;         // ユーザ名
                                    sd.mBin.mData[ 1 ] = rd.mBin.mData[ 1 ];  // 認証用のユーザ番号
                                    SocketBase.Send( mClient, sd.serialize() );

                                }




                                break;
                            }
                        case SendCode.cInitClient:
                            {
                                tl_utl_dev.print( "[TlClient::OnReceiveData] cInitClient : id : " + rd.mBin.mData[ 1 ] + "\n" );
                                mClientId = rd.mBin.mData[ 1 ];
                                mOnNotifyEvent( NotifyEvent.cOnInitClient, null, null, null);
                                break;
                            }
                        case SendCode.cSendTaskCommand:
                            {
                                tl_utl_dev.print( "[TlClient::OnReceiveData] cSendTaskCommand : from : " + rd.mBin.mData[ 0 ] + " size : " + rd.mnBufSize + " recvsize: " + receivedData.GetBuffer().Length + "\n" );
                                mOnNotifyEvent( NotifyEvent.cOnTaskCommand, null, rd.mTaskCommands, null);
                                break;
                            
                            }
                        case SendCode.cSendTeamInfo:
                            {
                                tl_utl_dev.print("[TlClient::OnReceiveData] cSendTeamInfo : id : " + rd.mBin.mData[1] + "\n");
                                if (null!= rd.mBuffer)
                                {
                                    //tl_utl_dev.print("[TlClient::OnReceiveData] cSendTeamInfo : size : " + rd.mBuffer.Length + "\n");
                                    //TlTeamInfo.Bin tibin = (TlTeamInfo.Bin)UtilDev.doDeserialize(rd.mBuffer,typeof(TlTeamInfo.Bin));
                                    //tibin.print();
                                    mOnNotifyEvent(NotifyEvent.cOnTeamInfo, null, null, UtilDev.doDeserialize(rd.mBuffer, typeof(tl_common.TlTeamInfo.Bin)));
                                }
                                //mClientId = rd.mBin.mData[1];
                                //mOnNotifyEvent(NotifyEvent.cOnInitClient, null, null);
                                break;
                            }

                    }
                }
                else
                {

                }

                nRestSize -= rd.mnBufSize;
                if( nRestSize > 0 )
                {
                    byte[] data_tmp = new byte[ nRestSize ];
                    Array.Copy( data,rd.mnBufSize,data_tmp,0,nRestSize);
                    data = data_tmp;
                }

            }

            //tl_utl_dev.print( "[TlClient::OnReceiveData] end\n" );


        }


    } // class

} // namespace


// end of file
