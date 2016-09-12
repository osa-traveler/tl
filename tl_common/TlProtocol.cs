using System;

using tl_utl_dev = tl_common.UtilDev;

namespace tl_common
{
    public enum SendCode
    {
        cGetClientName,   // クライアント名を教えよ( S → C )
        cSetClientName,   // クライアント名を教える( C → S )
        cInitClient,      // 接続許可( S → C )
        cSendTaskCommand, // タスクコマンド送信( S ←→ C )
        cRequestTaskCommand, // ローカルが今持ってるタスクコマンドの数を知らせる( C → S )
        cRequestTeamInfo, // チーム情報が欲しいなあ( C → S )
        cSendTeamInfo, // チーム情報あげる( S → C )

        cLast
    }

    /// <summary>
    /// 送信データ
    /// </summary>
    public class SendData
    {
        public SendCode meSendCode;
        public Bin mBin;

        public tl_common.TaskCommand[] mTaskCommands;
        public int mnBufSize;
        public int mnTaskCommandStartOffset;

        public byte[] mBuffer; // 自由に使えるバッファ


        [ Serializable()]
        public class Bin
        {
            public string[] mData;
            public Bin()
            {
                mData = new string[ 4 ];
            }
        }



        public SendData( SendCode eSendCode  )
        {
            meSendCode = eSendCode;
            mBin = new Bin();
            mnBufSize = 0;
            mnTaskCommandStartOffset = 0;
            mTaskCommands = null;
            mBuffer = null;
        }

        public SendData( SendCode eSendCode, tl_common.TaskCommand tc )
        {
            meSendCode = eSendCode;
            mBin = new Bin();
            mnBufSize = 0;
            mnTaskCommandStartOffset = 0;
            mTaskCommands = new TaskCommand[ 1 ];
            mTaskCommands[ 0 ] = tc;
            mBuffer = null;
        }

        public SendData(SendCode eSendCode, byte[] buf)
        {
            meSendCode = eSendCode;
            mBin = new Bin();
            mnBufSize = 0;
            mnTaskCommandStartOffset = 0;
            mTaskCommands = null;
            mBuffer = buf;
        }

        public SendData( byte[] bin )
        {
            meSendCode = SendCode.cLast;
            mTaskCommands = null;

            deserialize( bin );
        }

        public byte[] serialize()
        {
            try
            {
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                
                mnBufSize = 0;// ms_bin.GetBuffer().Length + 4 + 4;
                byte[] bytes_size = BitConverter.GetBytes( mnBufSize );

                mnTaskCommandStartOffset = 0;
                byte[] bytes_commandstartoffset = BitConverter.GetBytes( mnTaskCommandStartOffset );

                int nSendCode = ( int )meSendCode;
                byte[] bytes_sendcode = BitConverter.GetBytes( nSendCode );

                System.IO.MemoryStream ms = new System.IO.MemoryStream();

                // コード
                ms.Write( bytes_size, 0, bytes_size.Length );
                ms.Write( bytes_commandstartoffset, 0, bytes_commandstartoffset.Length );
                ms.Write( bytes_sendcode, 0, bytes_sendcode.Length );
                bf.Serialize( ms, mBin );

                // ここからがタスクコマンドのスタート位置
                mnTaskCommandStartOffset = (int)ms.Position;

                /// タスクコマンドがあるときはくっつける
                if ( null != mTaskCommands )
                {
                    System.IO.MemoryStream ms_tc = new System.IO.MemoryStream();// メモリに書き込みたいときはこちら。

                    /// 圧縮するストリーム
                    System.IO.Compression.GZipStream ds_tc = new System.IO.Compression.GZipStream( ms_tc, System.IO.Compression.CompressionMode.Compress, true );

                    /// シリアライザ
                    System.Runtime.Serialization.DataContractSerializer serializer_tc = new System.Runtime.Serialization.DataContractSerializer( typeof( TaskCommand[] ) );

                    /// メモリストリームに書き込む
                    {

                        System.Xml.XmlWriterSettings settings = new System.Xml.XmlWriterSettings();
                        settings.Encoding = new System.Text.UTF8Encoding( false );
                        settings.Indent = true;
                        System.Xml.XmlWriter xml = System.Xml.XmlWriter.Create( ds_tc, settings );

                        serializer_tc.WriteObject( xml, mTaskCommands );

                        xml.Close();

                    }

                    // closeした時点でfsに書き込まれる
                    ds_tc.Close();

                    // 書き込み
                    ms.Write( ms_tc.GetBuffer(), 0, ms_tc.GetBuffer().Length );
                    //tl_utl_dev.print( "[SendData::serialize] rrrrrr " + ms_tc.GetBuffer().Length + " / " + ms.GetBuffer().Length+ "\n" );

                    ms_tc.Close();

                }

                /// バッファがあるときはくっつける
                if (null != mBuffer)
                {
                    tl_utl_dev.print("[SendData::serialize] mBuffer " + mnTaskCommandStartOffset.ToString() + ", " + mBuffer.Length+"\n" );
                    ms.Write(mBuffer, 0, mBuffer.Length);
                }

                /// サイズを入れなおす
                { 
                    mnBufSize = ms.GetBuffer().Length;
                    byte[] tmp = BitConverter.GetBytes( mnBufSize );
                    int nCount = 0;
                    foreach (byte b in tmp)
                    {
                        ms.GetBuffer()[ nCount ] = b;
                        ++nCount;
                    }

                    byte[] tmp2 = BitConverter.GetBytes( mnTaskCommandStartOffset );
                    foreach ( byte b in tmp2 )
                    {
                        ms.GetBuffer()[ nCount ] = b;
                        ++nCount;
                    }


                }
                //tl_utl_dev.print( "[SendData::serialize] !!!!" + ms.GetBuffer().Length+"\n" );
                 
                return ms.GetBuffer();
            }
            catch
            {
                tl_utl_dev.print( "[SendData::serialize] fail..\n" );
                return null;
            }

        }

        public void deserialize( byte[] bin )
        {
            //tl_utl_dev.print("[SendData::deserialize] bin size : " + bin.Length.ToString()  + "\n");

            try
            {
                mnBufSize = BitConverter.ToInt32( bin, 0 );
                mnTaskCommandStartOffset = BitConverter.ToInt32( bin, 4 );
                meSendCode = ( SendCode )BitConverter.ToInt32( bin, 8 );

                System.IO.MemoryStream ms_bin = new System.IO.MemoryStream( bin, 12, mnTaskCommandStartOffset - 12 );
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                mBin = ( Bin )bf.Deserialize( ms_bin );

                if ( SendCode.cSendTaskCommand == meSendCode )
                {
                    /// binを読み込みファイルストリーム
                    System.IO.MemoryStream ms_tc = new System.IO.MemoryStream(bin, mnTaskCommandStartOffset, mnBufSize - mnTaskCommandStartOffset );// メモリに書き込みたいときはこちら。

                    /// 解凍するストリーム
                    System.IO.Compression.GZipStream ds_tc = new System.IO.Compression.GZipStream( ms_tc, System.IO.Compression.CompressionMode.Decompress, true );

                    /// 読み込み
                    {
                        /// シリアライザ
                        System.Runtime.Serialization.DataContractSerializer serializer_tc = new System.Runtime.Serialization.DataContractSerializer( typeof( TaskCommand[] ) );

                        /// 読み込み
                        System.Xml.XmlReader xml = System.Xml.XmlReader.Create( ds_tc );

                        mTaskCommands = ( TaskCommand[] )serializer_tc.ReadObject( xml );

                        xml.Close();
                    }

                    ds_tc.Close();
                    ms_tc.Close();

                }

                /// mBufferがあるひと
                if (SendCode.cSendTeamInfo == meSendCode)
                {
                    /// 今のところtaskcommandとmbufferは共存できない。もしするときは、mbuffer用のオフセットをつける
                    int nSize = mnBufSize - mnTaskCommandStartOffset;
                    //System.IO.MemoryStream ms_tc = new System.IO.MemoryStream(bin, mnTaskCommandStartOffset, nSize);// メモリに書き込みたいときはこちら。
                    //mBuffer = ms_tc.ToArray();
                    //tl_utl_dev.print("[SendData::serialize] buffer ! " + mnBufSize.ToString() +", " + nSize.ToString() + "("+ mnTaskCommandStartOffset.ToString()+ ")\n");
                    mBuffer = new byte[nSize];
                    // bin.CopyTo(mBuffer, mnTaskCommandStartOffset);
                    Array.Copy(bin, mnTaskCommandStartOffset,mBuffer,0, nSize);
                }


            }
            catch( Exception e)
            {
                tl_utl_dev.print( "[SendData::deserialize] fail.." + e.Message + "\n" );
                meSendCode = SendCode.cLast ;

            }


        }



    }




}


// end of file
