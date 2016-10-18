using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace ZuiYouNameOrganizer
{
    class Program
    {
        static void Main(string[] args)
        {
            Program p = new Program();
        }

        private string _mid = "";
        private List<User> _users = new List<User>();

        public Program()
        {
            bool firstRun = !File.Exists("config.json");
            if (!firstRun)
            {
                try
                {
                    string configString = File.ReadAllText("config.json");
                    JObject config = JObject.Parse(configString);
                    if (config["id"] != null)
                        _mid = config["id"].ToString();
                }
                catch (Exception)
                {
                    firstRun = true;
                }
            }

            if (firstRun)
                Console.WriteLine("首次运行需获取个人ID，请按说明操作。\n");
            if (_mid == "")
            {
                GetOwnerID();
                Console.WriteLine();
            }

            if (_mid == "")
                return;

            string option;
            int optionNum;

            ShowMenu();
            Console.WriteLine();
            while (true)
            {
                do
                {
                    option = "";
                    optionNum = -1;
                    Console.Write("请选择：");
                    option = Console.ReadLine().Trim();
                    if (option == "")
                        option = "6";
                    else if (option.ToLower() == "exit" || option.ToLower() == "quit")
                        option = "7";
                } while (!int.TryParse(option, out optionNum) || optionNum < 1 || optionNum > 7);

                Console.WriteLine();
                switch (optionNum)
                {
                    case 1:
                        UpdateNameList();
                        break;
                    case 2:
                        UpdateNameList(true);
                        break;
                    case 3:
                        PrintFormerNameList();
                        break;
                    case 4:
                        PrintFollowList();
                        break;
                    case 5:
                        GetOwnerID();
                        break;
                    case 6:
                        ShowMenu();
                        break;
                    case 7:
                        return;
                    default:
                        break;
                }
                Console.WriteLine();
            }
        }

        private void ShowMenu()
        {
            Console.WriteLine("1. 更新关注人昵称存档");
            Console.WriteLine("2. 使用个人资料更新存档（确保最新，但很慢）");
            Console.WriteLine("3. 曾用名列表");
            Console.WriteLine("4. 关注列表");
            Console.WriteLine("5. 重设个人ID");
            Console.WriteLine("6. 菜单");
            Console.WriteLine("7. 结束");
        }

        private void GetOwnerID()
        {
            Console.WriteLine("获取方法");
            UtilHelper.PrintGapLine();

            Console.WriteLine("首先打开最右，找到一篇你发表的主题帖，分享至任意APP。从分享至的APP里复制该帖的链接，并截取id=后的数字粘贴至此。");
            Console.WriteLine();

            Console.WriteLine("请输入帖子ID，留空退出。");
            string pidString = "";
            int pid = -1;

            do
            {
                Console.Write("帖子ID：");
                pidString = Console.ReadLine().Trim();
                if (pidString == "")
                    return;
            } while (!int.TryParse(pidString, out pid));

            string url = "http://tbapi.ixiaochuan.cn/post/detail?sign=" + UtilHelper.GetTimeMD5();
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);

            req.ContentType = "application/json";
            req.Accept = "*/*";
            req.Method = "POST";
            req.Host = "tbapi.ixiaochuan.cn";
            req.UserAgent = "tieba/3.0.2 (iPhone; iOS 10.0.2; Scale/2.00)";

            string data = "{\"pid\":" + pid + "}";
            byte[] mybyte = Encoding.Default.GetBytes(data);
            req.ContentLength = mybyte.Length;
            using (Stream stream = req.GetRequestStream())
                stream.Write(mybyte, 0, mybyte.Length);

            HttpWebResponse res = (HttpWebResponse)req.GetResponse();
            StreamReader reader = new StreamReader(res.GetResponseStream(), Encoding.UTF8);
            string dat = reader.ReadToEnd();

            res.Close();
            reader.Close();

            JObject j = JObject.Parse(dat);
            string ret = j["ret"].ToString();

            if (ret.Trim() != "1")
            {
                Console.WriteLine(j["msg"].ToString());
                Console.ReadKey();
                return;
            }

            JObject jData = (JObject)j["data"];
            JObject jPost = (JObject)jData["post"];
            JObject jMember = (JObject)jPost["member"];

            string memberID = jMember["id"].ToString();
            string memberName = jMember["name"].ToString();

            JObject owner = new JObject();
            owner["id"] = memberID;
            owner["name"] = memberName;

            File.WriteAllText("config.json", owner.ToString(Newtonsoft.Json.Formatting.Indented));
            _mid = memberID;

            Console.WriteLine("设置个人ID成功：" + memberName + "。");
        }

        private void PrintFollowList()
        {
            if (_users.Count == 0)
                GetAllFollowing();

            Console.WriteLine("关注列表");
            UtilHelper.PrintGapLine();

            foreach (User user in _users)
                Console.WriteLine(user.Username);

            UtilHelper.PrintGapLine();
        }

        private void PrintFormerNameList()
        {
            Console.WriteLine("曾用名列表");
            UtilHelper.PrintGapLine();

            JObject nameList = new JObject();
            if (File.Exists("names.json"))
                nameList = JObject.Parse(File.ReadAllText("names.json"));

            foreach (var user in nameList)
            {
                string id = user.Key;
                JArray names = (JArray)user.Value["names"];
                if (names.Count > 1)
                {
                    string currentName = names.Last.ToString();
                    names.RemoveAt(names.Count - 1);
                    string[] formerNames = names.Select(x => (string)x).ToArray();
                    string formerNamesLine = string.Join("→", formerNames);
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.Write(currentName);
                    Console.ResetColor();
                    Console.Write("：" + formerNamesLine + "\n");
                }
            }

            UtilHelper.PrintGapLine();
        }

        private void UpdateNameList()
        {
            UpdateNameList(false);
        }

        private void UpdateNameList(bool force)
        {
            if (force)
                GetAllFollowing(force);
            else if (_users.Count == 0)
                GetAllFollowing();

            JObject nameList = new JObject();
            if (File.Exists("names.json"))
                nameList = JObject.Parse(File.ReadAllText("names.json"));

            int countAdd = 0;
            int countUpdate = 0;
            int countDelete = 0;

            List<Tuple<string, string>> updated = new List<Tuple<string, string>>();

            foreach (User user in _users)
            {
                if (nameList[user.ID] != null)
                {
                    JArray names = (JArray)nameList[user.ID]["names"];
                    string lastName = names.Last.ToString();
                    if (lastName != user.Username)
                    {
                        names.Add(user.Username);
                        countUpdate++;
                        updated.Add(new Tuple<string, string>(lastName, user.Username));
                    }
                }
                else
                {
                    nameList[user.ID] = new JObject();
                    nameList[user.ID]["names"] = new JArray();
                    JArray names = (JArray)nameList[user.ID]["names"];
                    names.Add(user.Username);
                    countAdd++;
                }
            }

            List<string> toRemove = new List<string>();

            foreach (var user in nameList)
            {
                string id = user.Key;
                var match = _users.Where(x => x.ID == id);
                if (match.Count() == 0)
                    toRemove.Add(id);
            }

            foreach (string id in toRemove)
            {
                nameList.Remove(id);
                countDelete++;
            }

            File.WriteAllText("names.json", nameList.ToString(Newtonsoft.Json.Formatting.Indented));

            UtilHelper.PrintGapLine();
            Console.WriteLine($"更新成功！新增{countAdd}，更新{countUpdate}，删除{countDelete}。");
            UtilHelper.PrintGapLine();
            foreach (Tuple<string, string> update in updated)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.Write(update.Item1);
                Console.ResetColor();
                Console.Write(" 更名为 ");
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.Write(update.Item2 + "\n");
                Console.ResetColor();
            }
            if (countUpdate > 0)
                UtilHelper.PrintGapLine();
        }

        private void GetAllFollowing()
        {
            GetAllFollowing(false);
        }

        private void GetAllFollowing(bool force)
        {
            List<User> newUsers = new List<User>();
            Console.Write("正在获取关注列表");
            GetFollowing(newUsers, "0", force);
            Console.Write("\n\n");
            _users = newUsers;
        }

        private void GetFollowing(List<User> newUsers, string offset, bool force)
        {
            Console.Write(".");

            string url = "http://tbapi.ixiaochuan.cn/attention/user_atts?sign=" + UtilHelper.GetTimeMD5();
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);

            req.ContentType = "application/json";
            req.Accept = "*/*";
            req.Method = "POST";
            req.Host = "tbapi.ixiaochuan.cn";
            req.UserAgent = "tieba/3.0.2 (iPhone; iOS 10.0.2; Scale/2.00)";

            string data = "{\"mid\":" + _mid + ", \"offset\":" + offset + ", \"h_ts\":" + UtilHelper.GetTimeStamp() + "}";
            byte[] mybyte = Encoding.Default.GetBytes(data);
            req.ContentLength = mybyte.Length;
            using (Stream stream = req.GetRequestStream())
                stream.Write(mybyte, 0, mybyte.Length);

            HttpWebResponse res = (HttpWebResponse)req.GetResponse();
            StreamReader reader = new StreamReader(res.GetResponseStream(), Encoding.UTF8);
            string dat = reader.ReadToEnd();

            res.Close();
            reader.Close();
            
            JObject j = JObject.Parse(dat);
            JObject jData = (JObject)j["data"];
            int more = int.Parse(jData.GetValue("more").ToString());
            int next_offset = int.Parse(jData.GetValue("offset").ToString());

            JArray list = (JArray)jData["list"];
            foreach (JObject user in list)
            {
                string uid = user["id"].ToString();
                string uname = user["name"].ToString();
                if (force)
                    uname = ForceGetFromProfile(uid);
                User u = new User(uname, uid);
                newUsers.Add(u);
            }

            Thread.Sleep(200);

            if (more == 1)
                GetFollowing(newUsers, next_offset.ToString(), force);
        }

        private string ForceGetFromProfile(string id)
        {
            string url = "http://tbapi.ixiaochuan.cn/user/profile?sign=" + UtilHelper.GetTimeMD5();
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);

            req.ContentType = "application/json";
            req.Accept = "*/*";
            req.Method = "POST";
            req.Host = "tbapi.ixiaochuan.cn";
            req.UserAgent = "tieba/3.0.2 (iPhone; iOS 10.0.2; Scale/2.00)";

            string data = "{\"mid\":" + id + ", \"h_ts\":" + UtilHelper.GetTimeStamp() + "}";
            byte[] mybyte = Encoding.Default.GetBytes(data);
            req.ContentLength = mybyte.Length;
            using (Stream stream = req.GetRequestStream())
                stream.Write(mybyte, 0, mybyte.Length);

            HttpWebResponse res = (HttpWebResponse)req.GetResponse();
            StreamReader reader = new StreamReader(res.GetResponseStream(), Encoding.UTF8);
            string dat = reader.ReadToEnd();

            res.Close();
            reader.Close();

            JObject j = JObject.Parse(dat);
            JObject jData = (JObject)j["data"];
            JObject jInfo = (JObject)jData["member_info"];

            return jInfo["name"].ToString();
        }
    }
}
