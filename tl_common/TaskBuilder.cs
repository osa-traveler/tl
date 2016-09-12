using System.Collections.Generic;
using System.Linq;

using tl_utl_dev = tl_common.UtilDev;


namespace tl_common
{

    public class TaskBuilder
    {
        public int mnTaskCounter;
        public TaskList mrTaskList;
        public List<tl_common.TaskCommand> mCommandList;

        public int getCurrentCommandNum() { return mCommandList.Count;  }


        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="tl"></param>
        public TaskBuilder( ref TaskList tl )
        {
            tl_utl_dev.print("[TaskBuilder::TaskBuilder]\n");

            mnTaskCounter = 0;
            mrTaskList = tl ;

            mCommandList = new List<TaskCommand>();

        }

        /// <summary>
        /// コマンドを保存しておく
        /// </summary>
        public void saveCommands()
        {
            tl_utl_dev.print( "[TaskBuilder::saveCommands]\n" );

            /// シリアライザ
            System.Runtime.Serialization.DataContractSerializer serializer = new System.Runtime.Serialization.DataContractSerializer( typeof( List<TaskCommand> ) );

            /// 保存
            {
                string szFilename = tl_utl_dev.getExePath() + "\\" + "hoge.xml";

                System.Xml.XmlWriterSettings settings = new System.Xml.XmlWriterSettings();
                settings.Encoding = new System.Text.UTF8Encoding( false );
                settings.Indent = true;
                System.Xml.XmlWriter xml = System.Xml.XmlWriter.Create( szFilename, settings );

                serializer.WriteObject( xml, mCommandList );

                xml.Close();


            }

        }

        /// <summary>
        /// コマンドを読み込む
        /// </summary>
        public void loadCommands()
        {
            /// 読み込みコマンド列。
            List<TaskCommand> tc_list;

            /// 読み込み
            {
                /// シリアライザ
                System.Runtime.Serialization.DataContractSerializer serializer = new System.Runtime.Serialization.DataContractSerializer( typeof( List<TaskCommand> ) );
                
                /// 読み込み
                string szFilename = tl_utl_dev.getExePath() + "\\" + "hoge.xml";
                System.Xml.XmlReader xml = System.Xml.XmlReader.Create( szFilename );
                tc_list = ( List<TaskCommand> )serializer.ReadObject( xml );
                xml.Close();
            }

            /// 一旦リストを消して。
            mrTaskList.mTaskCollection.Clear();
            mrTaskList.mTaskCommentCollection.Clear();

            /// コマンドを適用していく
            foreach( TaskCommand tc in tc_list )
            {
                applyCommand( tc, null, null );

            }


        }

        /// <summary>
        /// バイナリで保存
        /// </summary>
        public void saveCommandsToBinaly()
        {
            tl_utl_dev.print( "[TaskBuilder::saveCommandsToBinaly]\n" );


            /// xmlを書き込むファイルストリーム
            string szFilename = tl_utl_dev.getExePath() + "\\" + "hoge.bin";
            System.IO.FileStream fs = new System.IO.FileStream( szFilename, System.IO.FileMode.Create );
            //System.IO.MemoryStream fs = new System.IO.MemoryStream();// メモリに書き込みたいときはこちら。

            /// 圧縮するストリーム
            System.IO.Compression.GZipStream ds = new System.IO.Compression.GZipStream( fs, System.IO.Compression.CompressionMode.Compress, true );

            /// シリアライザ
            System.Runtime.Serialization.DataContractSerializer serializer = new System.Runtime.Serialization.DataContractSerializer( typeof( List<TaskCommand> ) );

            /// メモリストリームに書き込む
            {

                System.Xml.XmlWriterSettings settings = new System.Xml.XmlWriterSettings();
                settings.Encoding = new System.Text.UTF8Encoding( false );
                settings.Indent = true;
                System.Xml.XmlWriter xml = System.Xml.XmlWriter.Create( ds, settings );

                serializer.WriteObject( xml, mCommandList );

                xml.Close();

            }

            // closeした時点でfsに書き込まれる
            ds.Close();

            fs.Close();

        }

        /// <summary>
        /// バイナリから読み込み
        /// </summary>
        public void loadCommandsFromBinaly()
        {
            tl_utl_dev.print( "[TaskBuilder::loadCommandsFromBinaly]\n" );

            /// 読み込みコマンド列。
            List<TaskCommand> tc_list;

            /// binを読み込みファイルストリーム
            string szFilename = tl_utl_dev.getExePath() + "/" + "hoge.bin";
            tl_utl_dev.print("[TaskBuilder::loadCommandsFromBinaly] "+ szFilename+"\n");
            /// ファイルがないときは初回起動の時とか
            if ( !System.IO.File.Exists( szFilename  ) )
            {
                tl_utl_dev.print("[TaskBuilder::loadCommandsFromBinaly] file not found\n");
                mrTaskList.mTaskCollection.Clear();
                return;
            }

            System.IO.FileStream fs = new System.IO.FileStream( szFilename, System.IO.FileMode.Open );
            //System.IO.MemoryStream fs = new System.IO.MemoryStream();// メモリに書き込みたいときはこちら。

            /// 解凍するストリーム
            System.IO.Compression.GZipStream ds = new System.IO.Compression.GZipStream( fs, System.IO.Compression.CompressionMode.Decompress, true );

            /// 読み込み
            {
                /// シリアライザ
                System.Runtime.Serialization.DataContractSerializer serializer = new System.Runtime.Serialization.DataContractSerializer( typeof( List<TaskCommand> ) );

                /// 読み込み
                System.Xml.XmlReader xml = System.Xml.XmlReader.Create( ds );
                tc_list = ( List<TaskCommand> )serializer.ReadObject( xml );
                xml.Close();
            }

            ds.Close();
            fs.Close();

            /// 一旦リストを消して。
            mrTaskList.mTaskCollection.Clear();
            mrTaskList.mTaskCommentCollection.Clear();

            /// コマンドを適用していく
            foreach ( TaskCommand tc in tc_list )
            {
                applyCommand( tc, null, null );

            }


        }


        /// <summary>
        /// コマンドを適用
        /// </summary>
        /// <param name="tc"></param>
        /// <returns></returns>
        public delegate void OnApplyCommandAddCommend( TaskComment taskcommand );
        public delegate void OnApplyCommand(tl_common.TaskCommand tc, tl_common.Task task);
        public bool applyCommand( tl_common.TaskCommand tc, OnApplyCommand oac,OnApplyCommandAddCommend acac )
        {
            // todo : ここでコマンドが順番通り来ているかを確認すること。

            switch( tc.meKind )
            {
                case TaskCommand.Kind.cCreateTask :
                    {
                        Task t = new Task();
                        t.From  = tc.mArg[ "From" ];
                        t.To    = tc.mArg[ "To" ];
                        t.Title = tc.mArg[ "Title" ];
                        t.ID = mnTaskCounter;
                        t.State = tc.mArg["State"];
                        t.LastUpdate = tc.mLastUpdate;
                        mrTaskList.mTaskCollection.Add( t );

                        if( null != tc.mArg["Comment"] )
                        {
                            TaskComment taskcomment = new TaskComment();
                            taskcomment.ParentId = t.ID;
                            taskcomment.Id = 0;
                            taskcomment.Date = tc.mLastUpdate;
                            taskcomment.Speaker = tc.mArg["Speaker"];
                            taskcomment.Comment = tc.mArg["Comment"];
                            mrTaskList.mTaskCommentCollection.Add( taskcomment );
                        }

                        if(null!= oac)
                        {
                            oac(tc,t);
                        }

                        // 一意な番号を割り当てる
                        ++mnTaskCounter;

                        break;
                    }
                case TaskCommand.Kind.cUpdateTask:
                    {
                        foreach ( Task t in mrTaskList.mTaskCollection.Where( ( p ) => { return p.ID == tc.mnTaskId; } ) )
                        {
                            t.From = tc.mArg[ "From" ];
                            t.To = tc.mArg[ "To" ];
                            t.Title = tc.mArg[ "Title" ];
                            t.LastUpdate = tc.mLastUpdate;
                            if ( null != tc.mArg[ "Comment" ] && "" != tc.mArg[ "Comment" ] )
                            {
                                TaskComment taskcomment = new TaskComment();
                                taskcomment.ParentId = t.ID;
                                taskcomment.Id = mrTaskList.mTaskCommentCollection.Count ;
                                taskcomment.Date = tc.mLastUpdate;
                                taskcomment.Comment = tc.mArg[ "Comment" ];
                                mrTaskList.mTaskCommentCollection.Add( taskcomment );

                                if( null != acac  )
                                {
                                    acac( taskcomment  );
                                }
                            }
                            if (null != oac)
                            {
                                oac(tc,t);
                            }

                        }

                        break;
                    }
                case TaskCommand.Kind.cAddComment:
                    {
                        foreach (Task t in mrTaskList.mTaskCollection.Where((p) => { return p.ID == tc.mnTaskId; }))
                        {
                            if (null != tc.mArg["Comment"] && "" != tc.mArg["Comment"])
                            {
                                TaskComment taskcomment = new TaskComment();
                                taskcomment.ParentId = t.ID;
                                taskcomment.Id = mrTaskList.mTaskCommentCollection.Count;
                                taskcomment.Date = tc.mLastUpdate;
                                taskcomment.Speaker = tc.mArg["Speaker"];
                                taskcomment.Comment = tc.mArg["Comment"];
                                mrTaskList.mTaskCommentCollection.Add(taskcomment);

                                if (null != acac)
                                {
                                    acac(taskcomment);
                                }
                            }
                        }

                        break;            
                    }
                case TaskCommand.Kind.cChangeState:
                    {
                        foreach (Task t in mrTaskList.mTaskCollection.Where((p) => { return p.ID == tc.mnTaskId; }))
                        {
                            t.State = tc.mArg["State"];
                            t.LastUpdate = tc.mLastUpdate;

                            /// コメントも追加してみる
                            TaskComment taskcomment = new TaskComment();
                            taskcomment.ParentId = t.ID;
                            taskcomment.Id = mrTaskList.mTaskCommentCollection.Count;
                            taskcomment.Date = tc.mLastUpdate;
                            taskcomment.Speaker = tc.mArg["Speaker"];
                            taskcomment.Comment = "tl_sys_changestate:" +taskcomment.Speaker+"さんが状態を「<b>" + tc.mArg["State"] + "</b>」に変えました";
                            mrTaskList.mTaskCommentCollection.Add(taskcomment);

                            if (null != acac)
                            {
                                acac(taskcomment);
                            }
                        }

                        break;
                    }
                default:
                    {

                        return false;
                    }


            }

            // 受けたコマンドを保存しておく
            mCommandList.Add( tc );

            return true;

        }


    }

}



// end of file
