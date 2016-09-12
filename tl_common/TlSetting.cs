#if DEBUG
#define RunOnTest
#endif


using tl_utl_dev = tl_common.UtilDev;

namespace tl_common
{
    public class TlSetting
    {
        // プロトコルバージョン
        public const string cProtocolVersion = "0.1";

        public string Team { get; set; }
        public string UserName { get; set; }
        private int mnPort;

        private string mDirectIp;

        public TlSetting( string szUsername, string szTeam, string szDirectIp = "" )
        {
            Team = szTeam;
            if ( "" == szTeam )
            {
                Team = "test";
            }

            tl_utl_dev.print( "[TlSetting::TlSetting] team : " + Team + "  username : " + szUsername + "\n" );


            mDirectIp = szDirectIp;

            mnPort = 0;

            UserName = szUsername;

        }

        public bool setup()
        {
            mnPort = 46500;//11240;


            return true;
        }

        public System.Net.IPEndPoint GetIpEndPoint()
        {
#if RunOnTest
            tl_utl_dev.print( "[TlSetting::GetIpEndPoint] local(127.0.0.1:"+getTlServerPort().ToString()+")\n" );
            System.Net.IPEndPoint endpoint = new System.Net.IPEndPoint( System.Net.IPAddress.Parse("127.0.0.1"), getTlServerPort() );
#else

            tl_utl_dev.print( "[TlSetting::GetIpEndPoint] tlserver\n" );
            //tl_utl_dev.print( "[TlSetting::GetIpEndPoint] local(127.0.0.1:"+getTlServerPort().ToString()+")\n" );
            System.Net.IPAddress ipAdd =System.Net.Dns.GetHostEntry("xxxx").AddressList[0];
            System.Net.IPEndPoint endpoint = new System.Net.IPEndPoint(ipAdd, getTlServerPort() );

#endif
            return endpoint;
        }

        private int getTlServerPort() { return mnPort ;  }


        public string getChatlineHtml() { return tl_utl_dev.getExePath() + @"/../../../html\index.html"; }
        public string getFaceImgDir() { return tl_utl_dev.getExePath() + @"/../../../html\img\"; }

        public string getTeamScriptFile() { return "tlscript." + Team + ".cs"; }
        public string getTeamScriptPath() { return tl_utl_dev.getExePath() + "/" + getTeamScriptFile();  }
        public string getTeamInfoFile() { return "tl.teaminfo." + Team + ".xml"; }
        public string getTeamInfoPath() { return tl_utl_dev.getExePath() + "/" + getTeamInfoFile(); }


    }

}


// end of file

