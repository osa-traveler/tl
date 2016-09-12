using System.Collections.Generic;


namespace tl_common
{
    /// <summary>
    /// コマンド
    /// </summary>
    public class TaskCommand
    {
        public enum Kind
        {
            cCreateTask,
            cUpdateTask,
            cAddComment,
            cChangeState,
            cLast
        };

        public int mnId; // コマンドID
        public int mnTaskId;
        public Kind   meKind;
        public string mLastUpdate;
        public Dictionary<string, string> mArg;


        public TaskCommand()
        {
            mnId = -1;
            mnTaskId = -1;
            meKind = Kind.cLast;
            mLastUpdate = "";
            mArg = new Dictionary<string, string>();
        }

        public TaskCommand( Kind eKind )
        {
            mnId = -1;
            mnTaskId = -1;
            meKind = eKind;
            mLastUpdate = "";
            mArg = new Dictionary<string, string>();

        }




    }

    /// <summary>
    /// コマンドを発行する
    /// </summary>
    public class TaskCommander
    {

        public TaskCommander()
        {
        }


        public TaskCommand createTask( string szSpeaker, string szFrom, string szTo, string szTitle, string szComment )
        {
            TaskCommand tc = new TaskCommand( TaskCommand.Kind.cCreateTask ) ;
            tc.mArg.Add( "From" , szFrom ) ;
            tc.mArg.Add( "To"   , szTo );
            tc.mArg.Add( "Title", szTitle );
            tc.mArg.Add( "State", "新規");
            tc.mArg.Add("Speaker", szSpeaker);
            tc.mArg.Add( "Comment", szComment );

            return tc;

        }

        public TaskCommand updateTask( int nTaskId, string szFrom, string szTo, string szTitle, string szComment )
        {
            TaskCommand tc = new TaskCommand( TaskCommand.Kind.cUpdateTask );
            tc.mnTaskId = nTaskId;
            tc.mArg.Add( "From", szFrom );
            tc.mArg.Add( "To", szTo );
            tc.mArg.Add( "Title", szTitle );
            tc.mArg.Add( "Comment", szComment );

            return tc;

        }

        public TaskCommand addComment(int nTaskId, string szSpeaker, string szComment)
        {
            TaskCommand tc = new TaskCommand(TaskCommand.Kind.cAddComment);
            tc.mnTaskId = nTaskId;
            tc.mArg.Add("Speaker", szSpeaker);
            tc.mArg.Add("Comment", szComment);

            return tc;
        }

        public TaskCommand changeState(int nTaskId, string szSpeaker, string szState)
        {
            TaskCommand tc = new TaskCommand(TaskCommand.Kind.cChangeState);
            tc.mnTaskId = nTaskId;
            tc.mArg.Add("Speaker", szSpeaker);
            tc.mArg.Add("State", szState);

            return tc;
        }

    }


}



// end of file
