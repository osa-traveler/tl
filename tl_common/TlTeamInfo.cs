using System;
using System.Collections.Generic;

using tl_utl_dev = tl_common.UtilDev;


namespace tl_common
{
    public class TlTeamInfo
    {
        [Serializable()]
        ///[System.Xml.Serialization.XmlRoot("TlTeamInfo.Member")]
        public class Member
        {
            public string PCLogin { get; set; }
            public string Name { get; set; }
        }
        [Serializable()]
        //[System.Xml.Serialization.XmlRoot("TlTeamInfo.State")]
        public class State
        {
            public string Name { get; set; }
            public string Backcolor { get; set; }
        }

        [Serializable()]
        [System.Xml.Serialization.XmlRoot("TlTeamInfo.Bin")]
        public class Bin
        {

            public Bin()
            {
                mMemberList = new List<Member>();
                mStateList = new List<State>();
            }
            public void print()
            {
                foreach (Member member in mMemberList)
                {
                    tl_utl_dev.print("  " + member.PCLogin + "," + member.Name + "\n");
                }
            }

            public List<Member> mMemberList;
            public List<State> mStateList;

        }

        public Bin mBin;
        TlSetting mrSetting;

        public TlTeamInfo(TlSetting setting)
        {
            mBin = new Bin();
            mrSetting = setting; 
        }

        public void print()
        {
            tl_utl_dev.print("[TlTeamInfo::print]\n");

            mBin.print();

        }

        public void addMember( string pclogin, string name )
        {
            Member member = new Member();
            member.PCLogin = pclogin;
            member.Name = name;
            mBin.mMemberList.Add(member);
        }
        public void addState(string name,string backcolor)
        {
            State state = new State();
            state.Name = name;
            state.Backcolor = backcolor;
            mBin.mStateList.Add(state);
        }
        public string getMyName()
        {
            Member m = mBin.mMemberList.Find((member) => { return member.PCLogin == mrSetting.UserName; });
            if( null == m )
            {
                return mrSetting.UserName;
            }
            return m.Name;
        }
        public string getFaceImg( string name )
        {
            Member m = mBin.mMemberList.Find((member) => { return member.Name == name; });
            if (null == m)
            {
                return mrSetting.getFaceImgDir() + "";
            }
            return mrSetting.getFaceImgDir() + m.PCLogin +".face.jpg";
        }
        public string getStateColor(string statename)
        {
            State state = mBin.mStateList.Find((s) => { return s.Name == statename; });
            if (null == state)
            {
                return "Ivory";
            }
            return state.Backcolor;
        }

        public void save()
        {

            /// 保存
            try
            {
                string szFilename = mrSetting.getTeamInfoPath();
                System.IO.StreamWriter sw = new System.IO.StreamWriter(szFilename, false, new System.Text.UTF8Encoding(true));

                System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(tl_common.TlTeamInfo.Bin), @"http://schemas.datacontract.org/2004/07/tl_common");
                serializer.Serialize(sw,mBin);
                sw.Close();

                /*
                /// シリアライザ
                System.Runtime.Serialization.DataContractSerializer serializer = new System.Runtime.Serialization.DataContractSerializer(typeof(tl_common.TlTeamInfo.Bin));

                string szFilename = mrSetting.getTeamInfoPath();

                System.Xml.XmlWriterSettings settings = new System.Xml.XmlWriterSettings();
                settings.Encoding = new System.Text.UTF8Encoding(false);
                settings.Indent = true;
                System.Xml.XmlWriter xml = System.Xml.XmlWriter.Create(szFilename, settings);

                serializer.WriteObject(xml, mBin);

                xml.Close();
                */

            }
            catch ( Exception e)
            {
                tl_utl_dev.print("[TlTeamInfo::save] teaminfo.xml save fail.."+e.Message+"\n");
            }

        }

        public void load()
        {

            /// 読み込み
            try
            {
                System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(tl_common.TlTeamInfo.Bin));

                string szFilename = mrSetting.getTeamInfoPath();
                System.IO.StreamReader sr = new System.IO.StreamReader(szFilename, new System.Text.UTF8Encoding(true));
                mBin = (tl_common.TlTeamInfo.Bin)serializer.Deserialize(sr);
                sr.Close();


                /*
                /// シリアライザ
                System.Runtime.Serialization.DataContractSerializer serializer = new System.Runtime.Serialization.DataContractSerializer(typeof(Bin));

                /// 読み込み
                string szFilename = mrSetting.getTeamInfoPath();
                System.Xml.XmlReader xml = System.Xml.XmlReader.Create(szFilename);
                mBin = (Bin)serializer.ReadObject(xml);
                xml.Close();*/
            }
            catch ( Exception e)
            {
                tl_utl_dev.print("[TlTeamInfo::load] teaminfo.xml load fail.."+e.Message+"\n");
            }


        }

    }

}



// end of file
