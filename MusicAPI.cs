using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection.Metadata;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using TS3AudioBot;
using TS3AudioBot.Playlists;
using TSLib;
using TSLib.Helper;
using static System.Net.Mime.MediaTypeNames;


namespace MusicAPI
{
    public class MusicAPI
    {
        // 0位为网易云音乐 1位为QQ音乐
        public List<string> cookies = new List<string> { "", "" };
        public List<string> addresss = new List<string> { "", "" };
        public string netease_login_key;
        public string ptqrtoken;
        public string qrsig;

        // 报错信息文本
        private static string error_http_error = "HTTP请求发生错误";
        private static string error_json_error = "JSON解析错误";
        
        // 重试参数
        private static int maxtry = 3;
        private static int trydelay = 1500;
        
        // 日志记录
        private void LogError(string message, Exception ex = null)
        {
            Console.WriteLine($"[MusicAPI Error] {message}");
            if (ex != null)
            {
                Console.WriteLine($"[MusicAPI Exception] {ex.Message}");
                Console.WriteLine($"[MusicAPI StackTrace] {ex.StackTrace}");
            }
        }

        //--------------------------参数设置--------------------------
        public void SetCookies(string cookies, int type_music)
        {
            // 设置cookies，type_music为选择是哪一个
            this.cookies[type_music] = cookies;
        }
        public void SetAddress(string address, int type_music)
        {
            this.addresss[type_music] = address;
        }
        //--------------------------登录--------------------------
        public async Task<Stream> GetNeteaseLoginImage()
        {
            // 获取登录key
            string login_key_url = $"{addresss[0]}/login/qr/key?timestamp={GetTimeStamp()}";
            string Json_get = await HttpGetAsync(login_key_url);
            if (Json_get == null)
            {
                return null;
            }
            dynamic data = JsonConvert.DeserializeObject<dynamic>(Json_get);
            netease_login_key = data.data.unikey;
            // 获取登录二维码
            string image_url = $"{addresss[0]}/login/qr/create?key={netease_login_key}&qrimg=true&timestamp={GetTimeStamp()}";
            Json_get = await HttpGetAsync(image_url);
            if (Json_get == null)
            {
                return null;
            }
            data = JsonConvert.DeserializeObject<dynamic>(Json_get);
            string img_string = data.data.qrimg;
            // string转stream
            string[] img = img_string.Split(",");
            byte[] bytes = Convert.FromBase64String(img[1]);
            Stream stream = new MemoryStream(bytes);

           return stream;
        }
        public async Task<Dictionary<string,string>> CheckNeteaseLoginStatus()
        {
            // 检查登录情况
            string check_url = $"{addresss[0]}/login/qr/check?key={netease_login_key}&timestamp={GetTimeStamp()}";
            string Json_get = await HttpGetAsync(check_url);
            Dictionary<string,string> re = new Dictionary<string,string>();
            if (Json_get == null)
            {
                return null;
            }
            dynamic data = JsonConvert.DeserializeObject<dynamic>(Json_get);
            int code = data.code;
            string message = data.message;
            string cookie = data.cookie;
            re.Add("code",code.ToString());
            re.Add("message", message);
            re.Add("cookie", cookie);
            return re;
        }
        
        public async Task<Stream> GetQQLoginImage()
        {
            // 获取登录二维码
            string login_key_url = $"{addresss[1]}/user/getLoginQr/qq";
            string Json_get;
            try
            {
                Json_get = await HttpGetAsync(login_key_url);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"访问QQ音乐二维码登录api失败, 检查是否部署支持扫码登录的API-msg:{e.Message}-{login_key_url}");
            }
            if (Json_get == null)
            {
                return null;
            }
            dynamic data = JsonConvert.DeserializeObject<dynamic>(Json_get);
            string image;
            image = data.img;
            ptqrtoken = data.ptqrtoken;
            qrsig = data.qrsig;
            
            // string转stream
            string[] img = image.Split(",");
            byte[] bytes = Convert.FromBase64String(img[1]);
            Stream stream = new MemoryStream(bytes);

            return stream;
        }
        public async Task<Dictionary<string,string>> CheckQQLoginStatus()
        {
            // 检查登录情况
            string check_url = $"{addresss[1]}/user/checkLoginQr/qq";
            string content = String.Format("{{\"ptqrtoken\" : \"{0}\",\"qrsig\": \"{1}\"}}", ptqrtoken, qrsig);
            string res = await HttpPostAsync(check_url, content);
            Dictionary<string, string> re = new Dictionary<string, string>();
            if (res == null)return null;
            dynamic data = JsonConvert.DeserializeObject<dynamic> (res);

            bool isOK = data.isOk;
            int result = data.result;
            string message,uin, cookie;
            re.Add("isOK", isOK.ToString());
            if (isOK && result == 100)
            {
                message = data.message;
                uin = data.uin;

                var cookieBuilder = new StringBuilder();
                foreach (var item in data.cookie.Properties())
                {
                    cookieBuilder.Append($"{item.Name}={item.Value}; ");
                }
                cookie = cookieBuilder.ToString().TrimEnd(' ', ';');
                re.Add("message",message);
                re.Add("uin", uin);
                re.Add("cookie",cookie);
            }
            else
            {
                message = data.errMsg;
                re.Add("message", message);
            }

            return re;
        }
        public async Task<string> SetQQLoginCookies(string cookies)
        {
            // 设置QQ音乐的登录cookies
            string set_url = $"{addresss[1]}/user/setCookie";
            string content = @"{""data"": """ + cookies + @"""}";
            // string content = @"{""data"": ""Your actual JSON data here""}";
            string res = await HttpPostAsync(set_url, content);
            if (res == null)
            {
                return "Post返回数据为空";
            }
            dynamic data = JsonConvert.DeserializeObject<dynamic>(res);
            if(data.result == 100)
            {
                // 设置cookie完成
                string qqValue = ExtractValueFromCookie(cookies, "uin");
                // 设置cookie的QQ号
                string set_qq_url = $"{addresss[1]}/user/getCookie?id={qqValue}";
                await Task.Delay(500);
                res = await HttpGetAsync(set_qq_url);
                if (res == null)
                {
                    return "QQ:"+qqValue+"Get返回数据为空,检查格式是否正确";
                }
                data = JsonConvert.DeserializeObject<dynamic>(res);
                if(data.result == 100)
                {
                    // 设置QQ号完成
                    return "true";
                }
                else
                {
                    return "QQ:" + qqValue + "QQ号设置失败";
                }
            }
            else
            {
                return "cookie设置失败" ;
            }
        }
        public async Task<string> CheckQQCookie()
        {
            // 获取cookie,已失效
            string get_cookie_url = $"{addresss[1]}/user/cookie";
            string res = await HttpGetAsync(get_cookie_url);
            if(res == null)
                {
                return null;
            }
            JObject data = JObject.Parse(res);
            if (data["result"].ToString() =="100")
            {
                // 遍历全部"data"下的键值对
                JObject dataOjbect = (JObject)data["data"];
                StringBuilder cookie_string_builder = new StringBuilder();
                foreach (var cookie in dataOjbect)
                {
                    string key = cookie.Key;
                    JToken value = cookie.Value;
                    // 以key=value;形式保存
                    cookie_string_builder.AppendFormat("{0}={1};",cookie.Key,cookie.Value);
                }
                return cookie_string_builder.ToString();
            }
            else 
            {
                return null;
            }
        }

        //--------------------------获取用户状态--------------------------
        public async Task<UserInfo> GetNeteaseUserInfo()
        {
            // 检查是否有Cookie
            if (string.IsNullOrEmpty(cookies[0]))
            {
                return null;
            }

            string statusJsonGet = "";
            try
            {
                // --- 第一步: 调用 /login/status (已移除 timestamp) ---
                string statusUrl = $"{addresss[0]}/login/status";
                statusJsonGet = await HttpGetWithCookiesAsync(statusUrl, 0);

                if (string.IsNullOrEmpty(statusJsonGet))
                {
                    return null; // API没返回任何东西，视为未登录
                }

                var statusData = JObject.Parse(statusJsonGet);
                var profile = statusData["data"]?["profile"];

                if (profile == null || profile.Type == JTokenType.Null || profile["userId"] == null || (long)profile["userId"] == 0)
                {
                    return null; // 未登录
                }

                // --- 第二步: 安全地获取用户信息和VIP详情 ---
                string nickname = profile["nickname"]?.ToString() ?? "未知用户";
                long userId = (long)profile["userId"];

                // --- 第三步: 请求并安全地解析VIP信息 ---
                string extraInfo = "非会员"; // 默认值为非会员
                string vipInfoUrl = $"{addresss[0]}/vip/info";
                string vipJsonGet = await HttpGetWithCookiesAsync(vipInfoUrl, 0);

                if (!string.IsNullOrEmpty(vipJsonGet))
                {
                    var vipData = JObject.Parse(vipJsonGet);
                    if (vipData["code"]?.Value<int>() == 200 && vipData["data"] != null)
                    {
                        var vipDetails = vipData["data"];
                        long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        string tempVipInfo = "";

                        // 判断层级：黑胶 SVIP > 黑胶 VIP > 音乐包
                        if (vipDetails["redplus"]?["expireTime"]?.Value<long>() > currentTime)
                        {
                            long expireTime = (long)vipDetails["redplus"]["expireTime"];
                            DateTime expireDateTime = DateTimeOffset.FromUnixTimeMilliseconds(expireTime).LocalDateTime;
                            tempVipInfo = $"黑胶SVIP Lv.{vipDetails["redVipLevel"]} (到期: {expireDateTime:yyyy-MM-dd})";
                        }
                        else if (vipDetails["associator"]?["expireTime"]?.Value<long>() > currentTime)
                        {
                            long expireTime = (long)vipDetails["associator"]["expireTime"];
                            DateTime expireDateTime = DateTimeOffset.FromUnixTimeMilliseconds(expireTime).LocalDateTime;
                            tempVipInfo = $"黑胶VIP Lv.{vipDetails["associator"]["vipLevel"]} (到期: {expireDateTime:yyyy-MM-dd})";
                        }
                        else if (vipDetails["musicPackage"]?["expireTime"]?.Value<long>() > currentTime)
                        {
                            long expireTime = (long)vipDetails["musicPackage"]["expireTime"];
                            DateTime expireDateTime = DateTimeOffset.FromUnixTimeMilliseconds(expireTime).LocalDateTime;
                            tempVipInfo = $"畅听会员 (到期: {expireDateTime:yyyy-MM-dd})";
                        }

                        // 如果有任何有效的会员信息，再判断是否为年费
                        if (!string.IsNullOrEmpty(tempVipInfo))
                        {
                            // 根据您的要求，只在是年费时显示“年费”
                            if (vipDetails["redVipAnnualCount"]?.Value<int>() == 1)
                            {
                                extraInfo = "年费" + tempVipInfo;
                            }
                            else
                            {
                                extraInfo = tempVipInfo;
                            }
                        }
                    }
                }

                return new UserInfo
                {
                    Name = nickname,
                    Url = $"https://music.163.com/#/user/home?id={userId}",
                    Extra = extraInfo
                };
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"解析网易云用户信息时出错: {e.Message} - 返回内容: {statusJsonGet}", e);
            }
        }
        public async Task<UserInfo> GetQQUserInfo()
        {
            // 检查是否有QQ音乐的Cookie
            if (string.IsNullOrEmpty(cookies[1]))
            {
                return null;
            }

            string json_get = "";
            try
            {
                // 从已保存的Cookie字符串中提取uin(QQ号)
                string uin = ExtractValueFromCookie(cookies[1], "uin");
                if (string.IsNullOrEmpty(uin))
                {
                    return null;
                }

                // 使用正确的API地址和uin参数构建URL
                string status_url = $"{addresss[1]}/user/detail?id={uin}";

                // 发送带Cookie的HTTP请求
                json_get = await HttpGetWithCookiesAsync(status_url, 1);
                if (string.IsNullOrEmpty(json_get))
                {
                    return null;
                }

                var json = JObject.Parse(json_get);
                if (json["code"]?.Value<int>() == 0 && json["data"]?["creator"] != null)
                {
                    var creator = json["data"]["creator"];
                    string nickname = creator["nick"]?.ToString() ?? "未知用户";
                    string encrypt_uin = creator["encrypt_uin"]?.ToString();

                    // --- 最终版会员层级判断逻辑 ---
                    string extraInfo = "非会员"; // 默认值为非会员

                    var userInfoUI = creator["userInfoUI"];
                    var iconList = userInfoUI?["iconlist"] as JArray;

                    // 定义会员等级，数字越大，等级越高
                    int membershipRank = 0; // 0: 非会员, 1: 音乐包, 2: 豪华绿钻, 3: 超级会员
                    string membershipLevel = "";
                    bool isAnnual = false;

                    // 1. 最高优先级：判断是否为超级会员 (根据您的发现)
                    string lightColor = userInfoUI?["nickname"]?["lightColor"]?.ToString();
                    if (!string.IsNullOrEmpty(lightColor))
                    {
                        membershipRank = 3;
                    }

                    // 2. 遍历iconlist，寻找会员标识
                    if (iconList != null && iconList.Count > 0)
                    {
                        var regex = new Regex(@"(n?)(svip|sui)(\d+)\.png");

                        foreach (var iconToken in iconList)
                        {
                            string srcUrl = iconToken["srcUrl"]?.ToString();
                            if (string.IsNullOrEmpty(srcUrl)) continue;

                            Match match = regex.Match(srcUrl);
                            if (match.Success)
                            {
                                string type = match.Groups[2].Value;
                                string level = match.Groups[3].Value;
                                bool currentIsAnnual = (match.Groups[1].Value == "n");

                                // 如果已经是超级会员，只需更新等级和年费信息
                                if (membershipRank == 3 && type == "svip")
                                {
                                    membershipLevel = level;
                                    isAnnual = currentIsAnnual;
                                }
                                // 判断豪华绿钻
                                else if (type == "svip" && membershipRank < 2)
                                {
                                    membershipRank = 2;
                                    membershipLevel = level;
                                    isAnnual = currentIsAnnual;
                                }
                                // 判断音乐包
                                else if (type == "sui" && membershipRank < 1)
                                {
                                    membershipRank = 1;
                                    membershipLevel = level;
                                    isAnnual = currentIsAnnual;
                                }
                            }
                        }
                    }

                    // 3. 根据最终确定的最高等级来格式化输出
                    if (membershipRank > 0)
                    {
                        string membershipName = "未知会员";
                        if (membershipRank == 3) membershipName = "超级会员";
                        else if (membershipRank == 2) membershipName = "豪华绿钻";
                        else if (membershipRank == 1) membershipName = "音乐包";

                        string annualPrefix = isAnnual ? "年费" : "";
                        extraInfo = $"{annualPrefix}{membershipName} Lv.{membershipLevel}";
                    }

                    return new UserInfo
                    {
                        Name = nickname,
                        Url = $"https://y.qq.com/portal/profile.html?uin={encrypt_uin}",
                        Extra = extraInfo
                    };
                }

                return null;
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Netease QQ Plugin Critical Error] GetQQUserInfo failed: {e.Message} - Response: {json_get}");
                return null;
            }
        }

        // 添加这两个方法以便在主插件文件中获取API地址
        public string GetNeteaseApiServerUrl() => addresss[0];
        public string GetQQApiServerUrl() => addresss[1];

        //--------------------------获取歌曲--------------------------  
        public async Task<string> GetSongUrl(string Song_id, int type_music, string mediamid = "")
        {// 仅QQ音乐会需要mediamid
            // 获取歌曲URL
            if (type_music == 0)
            {
                for (int trytime = 0; trytime < maxtry; trytime++)
                {
                    // 网易云音乐
                    string url = $"{addresss[type_music]}/song/url/v1?id={Song_id}&level=exhigh";
                    if (cookies[type_music] != "")
                    {
                        url += $"&cookie={cookies[type_music]}";
                    }
                    string Json_get;
                    try
                    {// http get错误
                        Json_get = await HttpGetAsync(url);
                    }
                    catch (Exception e)
                    {
                        throw new InvalidOperationException($"{error_http_error}-msg:{e.Message}-{url}");
                    }
                    var jsonObject = JObject.Parse(Json_get);
                    int code = jsonObject["code"]?.Value<int>() ?? 0;
                    if (code != 200)
                    {// code不为200
                        string errorMsg = jsonObject["message"]?.Value<string>() ?? "Unknown error";
                        throw new InvalidOperationException($"{error_http_error}-code:{code}: {errorMsg}-{url}");
                    }
                    // 解析数据
                    try
                    {
                        string Songurl = jsonObject["data"][0]["url"].ToString();
                        return Songurl;
                    }
                    catch (Exception)
                    {// JSON解析错误
                        if (trytime == maxtry - 1) { throw new ArgumentException($"{error_json_error}-{Json_get}"); }
                        await Task.Delay(trydelay);
                    }
                }
                return null;
            }
            else if (type_music == 1)
            {
                for (int trytime = 0; trytime < maxtry; trytime++)
                {
                    // QQ音乐
                    string url = $"{addresss[type_music]}/song/url?id={Song_id}&mediaId={mediamid}";
                    string Json_get;
                    try
                    {// http get
                        Json_get = await HttpGetWithCookiesAsync(url, type_music);
                    }
                    catch (Exception e)
                    {
                        throw new InvalidOperationException($"{error_http_error}-msg:{e.Message}-{url}");
                    }
                    var jsonObject = JObject.Parse(Json_get);
                    // 解析JSON
                    try
                    {
                        string Songurl = "";
                        Songurl = jsonObject["data"].ToString();
                        return Songurl;
                    }
                    catch (Exception)
                    {
                        if (trytime == maxtry - 1) { throw new ArgumentException($"{error_json_error}-{Json_get}"); }
                        await Task.Delay(trydelay);
                    }
                }
                return null;
            }
            return null;
        }
        public async Task<Dictionary<string, string>> GetSongDetail(string Song_id, int type_music)
        {
            // 获取歌曲详细信息 返回值包括 "name":<歌曲名>, "picurl":<封面url>
            if (type_music == 0)
            {
                for (int trytime = 0; trytime < maxtry; trytime++)
                {
                    // 网易云音乐
                    string url = $"{addresss[type_music]}/song/detail?ids={Song_id}";
                    string Json_get;
                    try
                    {// http get错误
                        Json_get = await HttpGetAsync(url);
                    }
                    catch (Exception e)
                    {
                        throw new InvalidOperationException($"{error_http_error}-msg:{e.Message}-{url}");
                    }
                    var jsonObject = JObject.Parse(Json_get);
                    int code = jsonObject["code"]?.Value<int>() ?? 0;
                    if (code != 200)
                    {// code不为200
                        string errorMsg = jsonObject["message"]?.Value<string>() ?? "Unknown error";
                        throw new InvalidOperationException($"{error_http_error}-code:{code}: {errorMsg}-{url}");
                    }
                    // 解析数据
                    Dictionary<string, string> re = new Dictionary<string, string>();
                    try
                    {
                        string name = jsonObject["songs"][0]["name"].ToString();
                        string author = jsonObject["songs"][0]["ar"][0]["name"].ToString();
                        string picurl = jsonObject["songs"][0]["al"]["picUrl"].ToString();
                        re.Add("name", name);
                        re.Add("author", author);
                        re.Add("picurl", picurl);
                        return re;
                    }
                    catch (Exception)
                    {// JSON解析错误
                        if(trytime == maxtry - 1) { throw new ArgumentException($"{error_json_error}-{Json_get}"); }
                        await Task.Delay(trydelay);
                    }
                }
                return null;
            }
            else if (type_music == 1)
            {
                for (int trytime = 0; trytime < maxtry; trytime++)
                {
                    string url = $"{addresss[1]}/song?songmid={Song_id}";
                    string Json_get;
                    try
                    {// http get错误
                        Json_get = await HttpGetWithCookiesAsync(url, type_music);
                    }
                    catch (Exception e)
                    {
                        throw new InvalidOperationException($"{error_http_error}-msg:{e.Message}-{url}");
                    }
                    var jsonObject = JObject.Parse(Json_get);
                    Dictionary<string, string> re = new Dictionary<string, string>();
                    // 解析JSON
                    try
                    {
                        string name = jsonObject["data"]["track_info"]["name"].ToString();
                        string author = jsonObject["data"]["track_info"]["singer"][0]["name"].ToString();
                        string pmid = jsonObject["data"]["track_info"]["album"]["pmid"].ToString();
                        string picurl = $"https://y.gtimg.cn/music/photo_new/T002R300x300M000{pmid}.jpg";
                        re.Add("name", name);
                        re.Add("author", author);
                        re.Add("picurl", picurl);
                        // 新增media_mid，用于url申请，可能会防止空url的出现
                        string mediamid = jsonObject["data"]?["track_info"]?["file"]?["media_mid"]?.ToString() ?? Song_id;
                        re.Add("mediamid",mediamid);
                        return re;
                    }
                    catch (Exception)
                    {
                        if(trytime == maxtry -1) { throw new ArgumentException($"{error_json_error}-{Json_get}"); }
                        await Task.Delay(trydelay);
                    }
                }
                return null;
            }
            else
            {
                return null;
            }
        }
        public async Task<Dictionary<string, string>> SearchSong(string Song_name, int type_music)
        {
            // 查找歌曲, 返回类型为id或者错误 0
            Dictionary<string, string> re = new Dictionary<string, string>();
            if (type_music == 0)
            {
                for(int trytime = 0; trytime < maxtry; trytime++)
                {
                    // 网易云音乐
                    string url = $"{addresss[type_music]}/search?keywords={Song_name}&limit=1";
                    string Json_get;
                    try
                    {
                        Json_get = await HttpGetAsync(url);
                    }
                    catch (Exception e)
                    {
                        throw new InvalidOperationException($"{error_http_error}-msg:{e.Message}-{url}");
                    }
                    var jsonObject = JObject.Parse(Json_get);
                    int code = jsonObject["code"]?.Value<int>() ?? 0;
                    if (code != 200)
                    {// code不为200
                        string errorMsg = jsonObject["message"]?.Value<string>() ?? "Unknown error";
                        throw new InvalidOperationException($"{error_http_error}-code:{code}: {errorMsg}-{url}");
                    }
                    try
                    {
                        string id_get = jsonObject["result"]["songs"][0]["id"].ToString();
                        string songname = jsonObject["result"]["songs"][0]["name"].ToString();
                        string authorname = jsonObject["result"]["songs"][0]["artists"][0]["name"].ToString();
                        re.Add("id",id_get);
                        re.Add("name",songname);
                        re.Add("author", authorname);
                        return re;
                    }
                    catch (Exception)
                    {
                        if (trytime == maxtry - 1) { throw new ArgumentException($"{error_json_error}-{Json_get}"); }
                        await Task.Delay(trydelay);
                    }
                }
                return null;
            }
            else if (type_music == 1)
            {
                for (int trytime = 0; trytime < maxtry; trytime++)
                {
                    // QQ音乐
                    string url = $"{addresss[type_music]}/search?key={Song_name}&pageSize=1";
                    string Json_get;
                    try
                    {
                        Json_get = await HttpGetWithCookiesAsync(url,type_music);
                    }
                    catch (Exception e)
                    {
                        throw new InvalidOperationException($"{error_http_error}-msg:{e.Message}-{url}");
                    }
                    var jsonObject = JObject.Parse(Json_get);
                    try
                    {
                        string id_get = jsonObject["data"]["list"][0]["songmid"].ToString();
                        string songname = jsonObject["data"]["list"][0]["songname"].ToString();
                        string authorname = jsonObject["data"]["list"][0]["singer"][0]["name"].ToString();
                        re.Add("id", id_get);
                        re.Add("name", songname);
                        re.Add("author", authorname);
                        return re;
                    }
                    catch (Exception)
                    {
                        if (trytime == maxtry - 1) { throw new ArgumentException($"{error_json_error}-{Json_get}"); }
                        await Task.Delay(trydelay);
                    }
                }
            }
            return null;
        }
        public async Task<List<Dictionary<TimeSpan, Tuple<string, string>>>> GetSongLyric(string Song_id, int type_music)
        {
            List<Dictionary<TimeSpan, Tuple<string, string>>> lyric = new List<Dictionary<TimeSpan, Tuple<string, string>>>();
            if (type_music == 0)
            {
                for (int trytime = 0; trytime < maxtry; trytime++)
                {
                    // 网易云音乐
                    string url = $"{addresss[type_music]}/lyric?id={Song_id}";
                    string Json_get;
                    try
                    {// http get错误
                        Json_get = await HttpGetAsync(url);
                    }
                    catch (Exception e)
                    {
                        throw new InvalidOperationException($"{error_http_error}-msg:{e.Message}-{url}");
                    }
                    var jsonObject = JObject.Parse(Json_get);
                    int code = jsonObject["code"]?.Value<int>() ?? 0;
                    if (code != 200)
                    {// code不为200
                        string errorMsg = jsonObject["message"]?.Value<string>() ?? "Unknown error";
                        throw new InvalidOperationException($"{error_http_error}-code:{code}: {errorMsg}-{url}");
                    }
                    // 解析数据
                    try
                    {
                        // 解析主歌词和翻译歌词
                        var mainLyricData = jsonObject["lrc"]?["lyric"]?.ToString() ?? "";
                        var transLyricData = jsonObject["tlyric"]?["lyric"]?.ToString() ?? "";
                        
                        // 解析主歌词
                        Dictionary<TimeSpan, string> mainLyrics = new Dictionary<TimeSpan, string>();
                        if (!string.IsNullOrEmpty(mainLyricData))
                        {
                            string[] mainLines = mainLyricData.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var line in mainLines)
                            {
                                int closingBracketIndex = line.IndexOf(']');
                                if (closingBracketIndex <= 0) continue;

                                string timePart = line.Substring(1, closingBracketIndex - 1);
                                string lyricSingle = line.Substring(closingBracketIndex + 1).Trim();

                                if (string.IsNullOrWhiteSpace(lyricSingle)) continue;
                                
                                string[] timeFormats = new string[]
                                {
                                    @"mm\:ss\.fff", // [11.11.111]
                                    @"mm\:ss\.ff",  // [11.11.11]
                                    @"mm\:ss\.f"    // [11.11.1]
                                };
                                if (TimeSpan.TryParseExact(timePart, timeFormats, CultureInfo.InvariantCulture, out TimeSpan timestamp))
                                {
                                    mainLyrics[timestamp] = lyricSingle;
                                }
                            }
                        }
                        
                        // 解析翻译歌词
                        Dictionary<TimeSpan, string> transLyrics = new Dictionary<TimeSpan, string>();
                        if (!string.IsNullOrEmpty(transLyricData))
                        {
                            string[] transLines = transLyricData.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var line in transLines)
                            {
                                int closingBracketIndex = line.IndexOf(']');
                                if (closingBracketIndex <= 0) continue;

                                string timePart = line.Substring(1, closingBracketIndex - 1);
                                string lyricSingle = line.Substring(closingBracketIndex + 1).Trim();

                                if (string.IsNullOrWhiteSpace(lyricSingle)) continue;
                                
                                string[] timeFormats = new string[]
                                {
                                    @"mm\:ss\.fff", // [11.11.111]
                                    @"mm\:ss\.ff",  // [11.11.11]
                                    @"mm\:ss\.f"    // [11.11.1]
                                };
                                if (TimeSpan.TryParseExact(timePart, timeFormats, CultureInfo.InvariantCulture, out TimeSpan timestamp))
                                {
                                    transLyrics[timestamp] = lyricSingle;
                                }
                            }
                        }
                        
                        // 合并主歌词和翻译歌词
                        foreach (var mainLyric in mainLyrics)
                        {
                            TimeSpan timestamp = mainLyric.Key;
                            string mainText = mainLyric.Value;
                            string transText = transLyrics.ContainsKey(timestamp) ? transLyrics[timestamp] : "";
                            
                            var lyricEntry = new Dictionary<TimeSpan, Tuple<string, string>>();
                            lyricEntry.Add(timestamp, new Tuple<string, string>(mainText, transText));
                            lyric.Add(lyricEntry);
                        }
                        
                        return lyric;
                    }
                    catch (Exception)
                    {// JSON解析错误
                        if (trytime == maxtry - 1) { throw new ArgumentException($"{error_json_error}-{Json_get}"); }
                        await Task.Delay(trydelay);
                    }
                }
                return null;
            }
            else if (type_music == 1)
            {

                for (int trytime = 0; trytime < maxtry; trytime++)
                {
                    // QQ音乐
                    string url = $"{addresss[type_music]}/lyric?songmid={Song_id}";
                    string Json_get;
                    try
                    {// http get错误
                        Json_get = await HttpGetAsync(url);
                    }
                    catch (Exception e)
                    {
                        throw new InvalidOperationException($"{error_http_error}-msg:{e.Message}-{url}");
                    }
                    var jsonObject = JObject.Parse(Json_get);
                    // 解析数据
                    try
                    {
                        // 解析主歌词和翻译歌词
                        string mainLyricData = jsonObject["data"]["lyric"].ToString();
                        string transLyricData = "";
                        var dataObject = jsonObject["data"] as JObject;
                        if (dataObject != null && dataObject.ContainsKey("trans"))
                        {
                            transLyricData = dataObject["trans"].ToString();
                        }
                        
                        // 解析主歌词
                        Dictionary<TimeSpan, string> mainLyrics = new Dictionary<TimeSpan, string>();
                        string[] mainLines = mainLyricData.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string line in mainLines)
                        {
                            Match match = Regex.Match(line, @"^\[(\d{2}:\d{2}\.\d{1,3})\](.*)$");
                            if (match.Success)
                            {
                                string timeStr = match.Groups[1].Value;
                                string lyric_single = match.Groups[2].Value.Trim();
                                string[] timeFormats = new string[]
                                {
                                    @"mm\:ss\.fff", // [11.11.111]
                                    @"mm\:ss\.ff",  // [11.11.11]
                                    @"mm\:ss\.f"    // [11.11.1]
                                };
                                if (TimeSpan.TryParseExact(timeStr, timeFormats, CultureInfo.InvariantCulture, out TimeSpan time))
                                {
                                    mainLyrics[time] = lyric_single;
                                }
                            }
                        }
                        
                        // 解析翻译歌词
                        Dictionary<TimeSpan, string> transLyrics = new Dictionary<TimeSpan, string>();
                        if (!string.IsNullOrEmpty(transLyricData))
                        {
                            string[] transLines = transLyricData.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (string line in transLines)
                            {
                                Match match = Regex.Match(line, @"^\[(\d{2}:\d{2}\.\d{1,3})\](.*)$");
                                if (match.Success)
                                {
                                    string timeStr = match.Groups[1].Value;
                                    string lyric_single = match.Groups[2].Value.Trim();
                                    string[] timeFormats = new string[]
                                    {
                                        @"mm\:ss\.fff", // [11.11.111]
                                        @"mm\:ss\.ff",  // [11.11.11]
                                        @"mm\:ss\.f"    // [11.11.1]
                                    };
                                    if (TimeSpan.TryParseExact(timeStr, timeFormats, CultureInfo.InvariantCulture, out TimeSpan time))
                                    {
                                        transLyrics[time] = lyric_single;
                                    }
                                }
                            }
                        }
                        
                        // 合并主歌词和翻译歌词
                        foreach (var mainLyric in mainLyrics)
                        {
                            TimeSpan timestamp = mainLyric.Key;
                            string mainText = mainLyric.Value;
                            string transText = transLyrics.ContainsKey(timestamp) ? transLyrics[timestamp] : "";
                            
                            var lyricEntry = new Dictionary<TimeSpan, Tuple<string, string>>();
                            lyricEntry.Add(timestamp, new Tuple<string, string>(mainText, transText));
                            lyric.Add(lyricEntry);
                        }
                        
                        return lyric;
                    }
                    catch (Exception)
                    {// JSON解析错误
                        if (trytime == maxtry - 1) { throw new ArgumentException($"{error_json_error}-{Json_get}"); }
                        await Task.Delay(trydelay);
                    }
                }
                return null;
            }
             return null;
        }
        // 使用新的API将纯数字ID (songid) 转换为 mid
        public async Task<string> GetMidFromIdApi(string song_id)
        {
            // 使用新的API将纯数字ID (songid) 转换为 mid，并直接请求json格式
            string url = $"https://c.y.qq.com/v8/fcg-bin/fcg_play_single_song.fcg?songid={song_id}&tpl=yqq_song_detail&format=json&outCharset=utf-8&callback=getOneSongInfoCallback";
            string json_get;
            try
            {
                json_get = await HttpGetAsync(url);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"通过API转换songid失败: {e.Message}");
            }

            if (string.IsNullOrEmpty(json_get))
            {
                throw new Exception("通过API转换songid时，未收到任何返回内容");
            }

            // --- 解析JSON并提取mid ---
            try
            {
                var jsonObject = JObject.Parse(json_get);
                if (jsonObject["code"]?.Value<int>() == 0 && jsonObject["data"] != null && jsonObject["data"].HasValues)
                {
                    // 从 data 数组的第一个元素中提取 mid
                    string mid = jsonObject["data"][0]["mid"]?.ToString();
                    if (!string.IsNullOrEmpty(mid))
                    {
                        return mid;
                    }
                }
                string errorMsg = jsonObject["message"]?.ToString() ?? "未知错误或数据为空";
                throw new Exception($"API返回错误: {errorMsg}");
            }
            catch (Exception ex)
            {
                throw new Exception($"解析单曲API返回的JSON时出错: {ex.Message} - 原始返回: {json_get}");
            }
        }
        // 专辑
        public async Task<string> GetAlbumMidFromIdApi(string album_id)
        {
            // 使用新的API将纯数字专辑ID (albumid) 转换为 album_mid，并直接请求json格式
            string url = $"https://c.y.qq.com/v8/fcg-bin/musicmall.fcg?albumid={album_id}&format=json&inCharset=utf-8&outCharset=utf-8&cmd=get_album_buy_page";
            string json_get;
            try
            {
                Console.WriteLine($"AAAAA{url}");
                json_get = await HttpGetAsync(url);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"通过API转换albumid失败: {e.Message}");
            }

            if (string.IsNullOrEmpty(json_get))
            {
                throw new Exception("通过API转换albumid时，未收到任何返回内容");
            }

            // --- 解析JSON并提取album_mid ---
            try
            {
                var jsonObject = JObject.Parse(json_get);
                if (jsonObject["code"]?.Value<int>() == 0 && jsonObject["data"] != null)
                {
                    string mid = jsonObject["data"]["album_mid"]?.ToString();
                    if (!string.IsNullOrEmpty(mid))
                    {
                        return mid;
                    }
                }
                string errorMsg = jsonObject["message"]?.ToString() ?? "未知错误";
                throw new Exception($"API返回错误: {errorMsg}");
            }
            catch (Exception ex)
            {
                throw new Exception($"解析专辑API返回的JSON时出错: {ex.Message} - 原始返回: {json_get}");
            }
        }

        public async Task<Tuple<string, string>> ResolveQQMusicLinkAndGetId(string url)
        {
            // 返回值: <ID, Type> Type可以是 "song", "album", "playlist"
            using (var handler = new HttpClientHandler { AllowAutoRedirect = true })
            using (var client = new HttpClient(handler))
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

                try
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    Uri finalUri = response.RequestMessage.RequestUri;
                    string finalUrl = finalUri.AbsoluteUri;
                   

                    // --- 核心修复与智能识别逻辑 ---

                    // **情况1: 跳转到了中间分享页 (i.y.qq.com)**
                    if (finalUrl.Contains("i.y.qq.com/n2/m/share/details/"))
                    {
                        var queryParams = finalUri.Query.TrimStart('?').Split('&');

                        // **专辑中间页 (album.html)**
                        if (finalUrl.Contains("album.html"))
                        {
                            string numericAlbumId = queryParams.FirstOrDefault(p => p.StartsWith("albumId="))?.Split('=')[1];
                            if (!string.IsNullOrEmpty(numericAlbumId))
                            {
                                // 获取到数字ID后，立即调用新API进行转换
                                string albumMid = await GetAlbumMidFromIdApi(numericAlbumId);
                                return Tuple.Create(albumMid, "album");
                            }
                        }
                        // **歌单中间页 (taoge.html)**
                        else if (finalUrl.Contains("taoge.html"))
                        {
                            string playlistId = queryParams.FirstOrDefault(p => p.StartsWith("id="))?.Split('=')[1];
                            if (!string.IsNullOrEmpty(playlistId))
                            {
                                return Tuple.Create(playlistId, "playlist");
                            }
                        }
                        throw new Exception("在中间跳转页面中未能解析出ID");
                    }
                    // **情况2: 直接跳转到了最终的PC页面**
                    else if (finalUrl.Contains("/n/ryqq/"))
                    {
                        // **核心修正：使用正则表达式精确匹配**
                        // 匹配 /n/ryqq/ 后面跟着的 类型 和 ID
                        string pattern = @"/n/ryqq/(songDetail|albumDetail|playlist)/([^/?#]+)";
                        Match match = Regex.Match(finalUrl, pattern);

                        if (match.Success)
                        {
                            string type_str = match.Groups[1].Value; // "songDetail", "albumDetail", "playlist"
                            string id = match.Groups[2].Value;       // 捕获到的ID
                            string type = "";

                            if (type_str == "songDetail") type = "song";
                            else if (type_str == "albumDetail") type = "album";
                            else if (type_str == "playlist") type = "playlist";

                            if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(type))
                            {
                                return Tuple.Create(id, type);
                            }
                        }
                        throw new Exception("在最终跳转链接中未能解析出ID或类型");
                    }
                    // **兜底/错误处理**
                    else
                    {
                        throw new Exception($"链接跳转到了未知的页面类型: '{finalUrl}'");
                    }
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException($"解析分享链接时出错: {e.Message}");
                }
            }
        }
        //-------------------------获取专辑--------------------------
        public async Task<string> SearchAlbum(string keyword, int type_music)
        {
            // 根据关键词搜索专辑，返回第一个匹配的专辑ID
            if (type_music == 0) // 网易云音乐
            {
                string url = $"{addresss[0]}/search?keywords={keyword}&type=10&limit=1";
                if (cookies[type_music] != "")
                {
                    url += $"&cookie={cookies[type_music]}";
                }
                try
                {
                    string json_get = await HttpGetAsync(url);
                    var jsonObject = JObject.Parse(json_get);
                    if (jsonObject["code"]?.Value<int>() == 200 && jsonObject["result"]?["albums"] != null)
                    {
                        return jsonObject["result"]["albums"][0]["id"].ToString();
                    }
                    return null;
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException($"搜索网易云专辑失败: {e.Message}");
                }
            }
            else if (type_music == 1) // QQ音乐
            {   

                string url = $"{addresss[1]}/search?key={keyword}&t=8&pageSize=1";
                
                try
                {
                    string json_get = await HttpGetWithCookiesAsync(url, 1);
                    var jsonObject = JObject.Parse(json_get);
                    if (jsonObject["data"]?["list"] != null)
                    {
                        return jsonObject["data"]["list"][0]["albumMID"].ToString();
                    }
                    return null;
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException($"搜索QQ音乐专辑失败: {e.Message}");
                }
            }
            return null;
        }

        public async Task<Tuple<string, List<Dictionary<string, string>>>> GetAlbumDetail(string album_id, int type_music)
        {
            // 获取专辑详情，返回一个包含专辑名和歌曲列表的元组
            List<Dictionary<string, string>> songList = new List<Dictionary<string, string>>();
            string albumName = "未知专辑";

            if (type_music == 0) // 网易云音乐
            {
                string url = $"{addresss[0]}/album?id={album_id}";
                try
                {
                    string json_get = await HttpGetAsync(url);
                    var jsonObject = JObject.Parse(json_get);
                    if (jsonObject["code"]?.Value<int>() == 200)
                    {
                        albumName = jsonObject["album"]?["name"]?.ToString() ?? albumName;
                        var songs = jsonObject["songs"] as JArray;
                        if (songs != null)
                        {
                            foreach (var song in songs)
                            {
                                songList.Add(new Dictionary<string, string> {
                            { "id", song["id"].ToString() },
                            { "name", song["name"].ToString() },
                            { "author", song["ar"][0]["name"].ToString() }
                        });
                            }
                        }
                    }
                    return Tuple.Create(albumName, songList);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException($"获取网易云专辑详情失败: {e.Message}");
                }
            }
            else if (type_music == 1) // QQ音乐
            {
                string url = $"{addresss[1]}/album/songs?albummid={album_id}";
                try
                {
                    string json_get = await HttpGetAsync(url);
                    var jsonObject = JObject.Parse(json_get);
                    if (jsonObject["data"] != null)
                    {
                        albumName = jsonObject["data"]?["list"][0]?["album"]?["name"]?.ToString() ?? albumName;
                        var songs = jsonObject["data"]?["list"] as JArray;
                        if (songs != null)
                        {
                            foreach (var song in songs)
                            {
                                songList.Add(new Dictionary<string, string> {
                            { "id", song["mid"].ToString() },
                            { "name", song["name"].ToString() },
                            { "author", song["singer"][0]["name"].ToString() }
                        });
                            }
                        }
                    }
                    return Tuple.Create(albumName, songList);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException($"获取QQ音乐专辑详情失败: {e.Message}");
                }
            }
            return null;
        }
        //-------------------------获取歌单--------------------------
        public async Task<long> SearchPlayList(string PlayList_name, int type_music)
        {
            if (type_music == 0) {
                for(int trytime = 0; trytime< maxtry; trytime++)
                {
                    // 网易云音乐歌单搜索
                    string url = $"{addresss[type_music]}/search?keywords={PlayList_name}&limit=1&type=1000";
                    string Json_get;
                    try
                    {// http get错误
                        Json_get = await HttpGetAsync(url);
                    }
                    catch (Exception e)
                    {
                        throw new InvalidOperationException($"{error_http_error}-msg:{e.Message}-{url}");
                    }
                    var jsonObject = JObject.Parse(Json_get);
                    int code = jsonObject["code"]?.Value<int>() ?? 0;
                    if (code != 200)
                    {// code不为200
                        string errorMsg = jsonObject["message"]?.Value<string>() ?? "Unknown error";
                        throw new InvalidOperationException($"{error_http_error}-code:{code}: {errorMsg}-{url}");
                    }
                    try
                    {// 解析数据
                        string id_get = jsonObject["result"]["playlists"][0]["id"].ToString();
                        long id = long.Parse(id_get);
                        return id;
                    }
                    catch (Exception)
                    {
                        if (trytime == maxtry - 1) { throw new ArgumentException($"{error_json_error}-{Json_get}"); }
                        await Task.Delay(trydelay);
                    }
                }
                return 0;
            }
            else if(type_music == 1)
            {
                for (int trytime = 0; trytime < maxtry; trytime++)
                {
                    // QQ音乐
                    string url = $"{addresss[1]}/search?key={PlayList_name}&t=2";
                    string Json_get;
                    try
                    {
                        Json_get = await HttpGetWithCookiesAsync(url, type_music);
                    }
                    catch (Exception e)
                    {
                        throw new InvalidOperationException($"{error_http_error}-msg:{e.Message}-{url}");
                    }
                    var jsonObject = JObject.Parse(Json_get);
                    try
                    {
                        string id_get = jsonObject["data"]["list"][0]["dissid"].ToString();
                        long id = long.Parse (id_get);
                        return id;
                    }
                    catch (Exception)
                    {
                        if (trytime == maxtry - 1) { throw new ArgumentException($"{error_json_error}-{Json_get}"); }
                        await Task.Delay(trydelay);
                    }
                }
                return 0;
            }
            return 0;
        }
        public async Task<List<Dictionary<string,string>>> GetPlayListDetail(string PlayList_id, int type_music)
        {
            // 获取歌单 返回数据结构[ {id:id,name:name,author},{id:id,name:name,author}]
            List< Dictionary<string, string> > re = new List < Dictionary<string, string> >();
            if (type_music == 0)
            {
                string url = $"{addresss[type_music]}/playlist/detail?id={PlayList_id}";
                if (cookies[type_music] != "")
                {
                    url += $"&cookie={cookies[type_music]}";
                }
                string Json_get = await HttpGetAsync(url);
                if (Json_get == null)
                {
                    return null;
                }
                var jsonObject = JObject.Parse(Json_get);
                // 歌单信息获取完成
                int code = (int)jsonObject["code"];
                int song_count = (int)jsonObject["playlist"]["trackCount"];

                if (code == 200)
                {
                    for (int i = 0; i < song_count / 50 + 1; i++)
                    {
                        // 每次循环获得50首，若剩余小于50则取剩余数
                        for (int j = 0; j < ((song_count - i * 50) < 50 ? song_count - i * 50 : 50); j++)
                        {
                            // 获取i*50到i*50+50之间
                            string tracks_url = $"{addresss[type_music]}/playlist/track/all?id={PlayList_id}&limit=50&offset={i * 50}";
                            if (cookies[type_music] != "")
                            {
                                tracks_url += $"&cookie={cookies[type_music]}";
                            }
                            string tracks_Json_get = await HttpGetAsync(tracks_url);

                            if (tracks_Json_get == null)
                            {
                                return null;
                            }
                            dynamic tracks_data = JsonConvert.DeserializeObject<dynamic>(tracks_Json_get);
                            // i*50到i*50+50获取完成
                            long song_id = tracks_data.songs[j].id;
                            string song_name = tracks_data.songs[j].name;
                            string song_author = tracks_data.songs[j].ar[0].name;

                            re.Add(new Dictionary<string, string> { { "id", song_id.ToString() }, { "name", song_name }, { "author", song_author } });
                        }
                    }
                    return re;
                }
            }
            else if (type_music == 1)
            {
                for(int trytime = 0; trytime <maxtry;  trytime++)
                {
                    // QQ音乐
                    string url = $"{addresss[1]}/songlist?id={PlayList_id}";
                    string Json_get;
                    try
                    {// http get错误
                        Json_get = await HttpGetAsync(url);
                    }
                    catch (Exception e)
                    {
                        throw new InvalidOperationException($"{error_http_error}-msg:{e.Message}-{url}");
                    }
                    var jsonObject = JObject.Parse(Json_get);
                    try
                    {// 解析json
                        int song_count = (int)jsonObject["data"]["songnum"];
                        for (int i = 0; i< song_count; i++)
                        {
                            string song_mid = jsonObject["data"]["songlist"][i]["songmid"].ToString();
                            string song_name = jsonObject["data"]["songlist"][i]["songname"].ToString();
                            string song_author = jsonObject["data"]["songlist"][i]["singer"][0]["name"].ToString();
                            re.Add(new Dictionary<string, string> { { "id", song_mid }, { "name", song_name }, { "author", song_author } });
                        }
                        return re;
                    }
                    catch (Exception)
                    {
                        if (trytime == maxtry - 1) { throw new ArgumentException($"{error_json_error}-{Json_get}"); }
                        await Task.Delay(trydelay);
                    }
                }
                return null;
            }
            return null;
        }
        //-------------------------网易 FM--------------------------
        public async Task<string> GetFMSongId()
        {
            string url = $"{addresss[0]}/personal_fm/mode?mode=DEFAULT&timestamp={GetTimeStamp()}";
            if (cookies[0] != "")
            {
                url += $"&cookie={cookies[0]}";
            }
            for(int trytime = 0; trytime < maxtry; trytime++)
            {
                string Json_get;
                try
                {// http get错误
                    Json_get = await HttpGetAsync(url);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException($"{error_http_error}-msg:{e.Message}-{url}");
                }
                var jsonObject = JObject.Parse(Json_get);
                int code = jsonObject["code"]?.Value<int>() ?? 0;
                if (code != 200)
                {// code不为200
                    string errorMsg = jsonObject["message"]?.Value<string>() ?? "Unknown error";
                    throw new InvalidOperationException($"{error_http_error}-code:{code}: {errorMsg}-{url}");
                }
                string id=null;
                try
                {
                    id = jsonObject["data"][0]["id"].ToString();
                    return id;
                }
                catch (Exception)
                {
                    if (trytime == maxtry - 1) { throw new ArgumentException($"{error_json_error}-{Json_get}"); }
                    await Task.Delay(800);
                }
            }
            return null ;
           
        }
        //--------------------------QQ FM--------------------------
        public async Task<List<Dictionary<string, string>>> GetQQFMSongs(string radioId)
        {
            // 根据电台ID获取QQ电台的歌曲列表
            List<Dictionary<string, string>> songList = new List<Dictionary<string, string>>();
            string url = $"{addresss[1]}/radio?id={radioId}";

            try
            {
                string json_get = await HttpGetWithCookiesAsync(url, 1);
                if (string.IsNullOrEmpty(json_get))
                {
                    return songList; // 返回空列表
                }

                var jsonObject = JObject.Parse(json_get);
                if (jsonObject["data"]?["tracks"] is JArray songs)
                {
                    foreach (var song in songs)
                    {
                        // 从JSON中提取我们需要的歌曲信息
                        songList.Add(new Dictionary<string, string> {
                    { "id", song["mid"].ToString() },
                    { "name", song["name"].ToString() },
                    { "author", song["singer"][0]["name"].ToString() }
                });
                    }
                }
                return songList;
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"获取QQ电台歌曲失败: {e.Message}");
            }
        }
        //--------------------------HTTP相关--------------------------
        public static async Task<string> HttpGetAsync(string url)
        {
            using HttpClient client = new HttpClient();
            try
            {
                // 发送GET请求
                HttpResponseMessage res = await client.GetAsync(url);
                // 确保HTTP成功状态值
                res.EnsureSuccessStatusCode();
                // 读取响应内容为字符串
                string responseBody = await res.Content.ReadAsStringAsync();
                return responseBody;
            }
            catch (HttpRequestException e)
            {
                throw e;
            }
        }
        public async Task<string> HttpGetWithCookiesAsync(string url, int type_music)
        {
            using (var handler = new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = new CookieContainer()
            })
            {
                // 从类成员获取cookie字符串
                string cookieString = this.cookies[type_music];

                if (!string.IsNullOrEmpty(cookieString))
                {
                    // 分割cookie字符串为键值对
                    string[] cookiePairs = cookieString.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var pair in cookiePairs)
                    {
                        int equalsIndex = pair.IndexOf('=');
                        if (equalsIndex > 0)
                        {
                            string key = pair.Substring(0, equalsIndex).Trim();
                            string value = pair.Substring(equalsIndex + 1).Trim();
                            try
                            {
                                // 将cookie添加到CookieContainer
                                handler.CookieContainer.Add(new Uri(url), new Cookie(key, value));
                            }
                            catch (UriFormatException)
                            {
                                Console.WriteLine("Invalid URL format for cookie domain");
                            }
                        }
                    }
                }

                using (var client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

                    try
                    {
                        HttpResponseMessage response = await client.GetAsync(url);
                        response.EnsureSuccessStatusCode();
                        var cookies = handler.CookieContainer.GetCookies(new Uri(url));
                        // Console.WriteLine($"Sent cookies: {string.Join(", ", cookies.Cast<Cookie>().Select(c => $"{c.Name}={c.Value}"))}");
                        return await response.Content.ReadAsStringAsync();
                    }
                    catch (HttpRequestException e)
                    {
                        throw e;
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
            }
        }



        public static async Task<string> HttpPostAsync(string url, string jsonData)
        {
            using (var client = new HttpClient())
            {
                HttpContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                try
                {
                    // 发起POST请求
                    HttpResponseMessage response = await client.PostAsync(url, content);
                    // 确保请求成功
                    response.EnsureSuccessStatusCode();
                    // 读取响应内容
                    string responseBody = await response.Content.ReadAsStringAsync();
                    return responseBody;
                }
                catch (HttpRequestException e)
                {
                    // 异常处理
                    throw e;
                }
            }
        }
        public async Task<byte[]> HttpGetImage(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return null;
            }
            
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";

                // 关键：在这里添加 User-Agent，伪装成浏览器
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Safari/537.36";

                using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
                using (Stream stream = response.GetResponseStream())
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    if (stream == null)
                    {
                        return null;
                    }
                    await stream.CopyToAsync(memoryStream);
                    return memoryStream.ToArray();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] HttpGetImage 失败: {ex.Message}, url: {url}");
                return null;
            }
        }
        //--------------------------其他功能性--------------------------
        public static string GetTimeStamp()
        {
            //获得时间戳
            TimeSpan ts = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds).ToString();
        }
        public static string ExtractValueFromCookie(string cookie, string fieldName)
        {
            string pattern = fieldName + @"=(\d+)";
            Match match = Regex.Match(cookie, pattern);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            else
            {
                return null;
            }
        }
    }
    public class UserInfo
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string Extra { get; set; }
    }
}

