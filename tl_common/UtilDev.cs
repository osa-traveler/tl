using System;

namespace tl_common
{
    public class UtilDev
    {
        public delegate void OnPrint( string s );

        public static OnPrint mOnPrint = null ;
        public static OnPrint mOnShowOkMessage = null;

        public static void print( string sz )
        {
            if( null == mOnPrint )
            {
                Console.Write( sz );
            }
            else
            {
                mOnPrint( sz );
            }
        }

        public static void showOkMessage( string sz )
        {
            if ( null == mOnShowOkMessage )
            {
                Console.Write( sz );
            }
            else
            {
                mOnShowOkMessage( sz );
            }
        }

        public static string getExePath()
        {
            return System.IO.Path.GetDirectoryName( System.Reflection.Assembly.GetExecutingAssembly().Location );
        }

        public static byte[] doSerialize( object obj, Type type )
        {
            byte[] result = null;

            System.IO.MemoryStream ms_tc = new System.IO.MemoryStream();// メモリに書き込みたいときはこちら。

            /// 圧縮するストリーム
            System.IO.Compression.GZipStream ds_tc = new System.IO.Compression.GZipStream(ms_tc, System.IO.Compression.CompressionMode.Compress, true);

#if false
            System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(type, @"http://schemas.datacontract.org/2004/07/tl_common");
            serializer.Serialize(ds_tc, obj);

#else
            /// シリアライザ
            System.Runtime.Serialization.DataContractSerializer serializer_tc = new System.Runtime.Serialization.DataContractSerializer(type);

            /// メモリストリームに書き込む
            {

                System.Xml.XmlWriterSettings settings = new System.Xml.XmlWriterSettings();
                settings.Encoding = new System.Text.UTF8Encoding(false);
                settings.Indent = true;
                System.Xml.XmlWriter xml = System.Xml.XmlWriter.Create(ds_tc, settings);

                serializer_tc.WriteObject(xml, obj);

                xml.Close();

            }
#endif

            // closeした時点でfsに書き込まれる
            ds_tc.Close();

            // 書き込み
            //ms.Write(ms_tc.GetBuffer(), 0, ms_tc.GetBuffer().Length);
            //tl_utl_dev.print( "[SendData::serialize] rrrrrr " + ms_tc.GetBuffer().Length + " / " + ms.GetBuffer().Length+ "\n" );
            result = ms_tc.ToArray();

            ms_tc.Close();

            return result;

        }

        public static object doDeserialize(byte[]bin, Type type)
        {
            object obj = null;
            {
                /// binを読み込みファイルストリーム
                System.IO.MemoryStream ms_tc = new System.IO.MemoryStream(bin, 0, bin.Length);// メモリに書き込みたいときはこちら。

                /// 解凍するストリーム
                System.IO.Compression.GZipStream ds_tc = new System.IO.Compression.GZipStream(ms_tc, System.IO.Compression.CompressionMode.Decompress, true);

                /// 読み込み
                {

#if false
                    System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(type);

                    obj = serializer.Deserialize(ds_tc);

#else
                    /// シリアライザ
                    System.Runtime.Serialization.DataContractSerializer serializer_tc = new System.Runtime.Serialization.DataContractSerializer(type);

                    /// 読み込み
                    System.Xml.XmlReader xml = System.Xml.XmlReader.Create(ds_tc);

                    obj = serializer_tc.ReadObject(xml);

                    xml.Close();
#endif
                }

                ds_tc.Close();
                ms_tc.Close();

            }
            return obj;
        }


    }

}



// end of file
