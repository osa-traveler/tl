using System.Collections.Generic;
using System.Collections.ObjectModel;

using tl_utl_dev = tl_common.UtilDev;

namespace tl_common
{
    /// <summary>
    /// タスク
    /// </summary>
    public class Task : System.ComponentModel.INotifyPropertyChanged
    {
        private string mTitle;
        private string mFrom;
        private string mTo;
        private string mCc;
        private string mState;
        private string mLastUpdate;

        public int ID { get; set; }
        public string From { get { return mFrom; } set { mFrom = value; OnPropertyChanged( "From" ); } }
        public string To { get { return mTo; } set { mTo = value; OnPropertyChanged( "To" ); } }
        public string Cc { get { return mCc; } set { mCc = value; OnPropertyChanged( "Cc" ); } }
        public string State { get { return mState; } set { mState = value; OnPropertyChanged( "State" ); OnPropertyChanged("StateColor"); } }
        public string LastUpdate { get { return mLastUpdate; } set { mLastUpdate = value; OnPropertyChanged( "LastUpdate" ); } }
        public string Title { get { return mTitle; } set { mTitle = value; OnPropertyChanged( "Title" ); } }
        public string StateColor
        {
            get
            {
                if(null!= sTeamInfo)
                {
                    return sTeamInfo.getStateColor(State);
                }
                return "Ivory";
            }
        }

        public static tl_common.TlTeamInfo sTeamInfo = null;

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged( string name )
        {
            if ( PropertyChanged != null )
            {
                PropertyChanged( this, new System.ComponentModel.PropertyChangedEventArgs( name ) );
            }
        }

    }

    /// <summary>
    /// コメント
    /// </summary>
    public class TaskComment
    {
        public int ParentId { get; set; }
        public int Id { get; set; }
        public string Date { get; set; }
        public string Speaker { get; set; }
        public string Comment { get; set; }
    }


    /// <summary>
    /// タスクのリスト
    /// </summary>
    public class TaskList
    {
        public ObservableCollection<Task> mTaskCollection { set; get; }
        public List<TaskComment> mTaskCommentCollection { set; get; }

        public TaskList()
        {
            tl_utl_dev.print("[TaskList::TaskList]\n");

            mTaskCollection = new ObservableCollection<Task>();

            mTaskCommentCollection = new List<TaskComment>();




        }








    }



}



// end of file
