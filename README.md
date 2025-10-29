# TS3AudioBot-Plugin-Netease-QQ
> 基于Splamy/TS3AudioBot项目 https://github.com/Splamy/TS3AudioBot   
> 基于网易云音乐API项目 https://github.com/Binaryify/neteasecloudmusicapi   
> 网易云音乐API文档 https://binaryify.github.io/NeteaseCloudMusicApi/#/   
> 基于QQ音乐API项目 https://github.com/jsososo/QQMusicApi   
> QQ音乐API文档 https://qq-api-soso.vercel.app/#/   

[![Auto Release](https://github.com/HuxiaoRoar/TS3AudioBot-Plugin-Netease-QQ/actions/workflows/dotnet-release.yml/badge.svg)](https://github.com/HuxiaoRoar/TS3AudioBot-Plugin-Netease-QQ/actions/workflows/dotnet-release.yml)
[![License](https://img.shields.io/badge/license-OSL3.0-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-3.1-blue.svg)](https://dotnet.microsoft.com/download/dotnet/3.1)

参考了[ZHANGTIANYAO1](https://github.com/ZHANGTIANYAO1)的[TS3AudioBot-NetEaseCloudmusic-plugin](https://github.com/ZHANGTIANYAO1/TS3AudioBot-NetEaseCloudmusic-plugin)项目   
参考了[FiveHair](https://github.com/FiveHair)的[TS3AudioBot-NetEaseCloudmusic-plugin-UNM](https://github.com/FiveHair/TS3AudioBot-NetEaseCloudmusic-plugin-UNM)项目   
参考了[方块君](https://github.com/577fkj)的[TS3AudioBot-CloudMusic-plugin](https://github.com/577fkj/TS3AudioBot-CloudMusic-plugin)

TeamSpeak3音乐机器人插件，实现在语音频道中播放网络QQ音乐和网易云音乐。   
需要自行部署1. TS3AudioBot  2.网易云API 3.QQ音乐API （API也可只部署其中一个）   
测试过在Docker环境下能正常使用，Windows和Linux同样支持，推荐使用docker。   

## 功能简介

- QQ音乐、网易云音乐cookie登录，扫码登录
- 歌曲列表管理。
- 歌曲点播、添加进队列、下一首播放。
- 歌单点播、添加。
- 专辑点播。
- 6种播放模式切换。
- 网易云私人FM模式。
- QQ音乐FM模式、FM模式切换。
- 歌词功能。
- 调整播放进度，音量。
- 频道无人30s自动停止播放。
- 登录状态校验。

## 安装方法
您需要部署TS3AudioBot，网易云API，QQ音乐API。

### 方法一：docker

[详见视频](https://www.bilibili.com/video/BV1sHySBtE31/)

1. **安装 Docker**

   请参考 [Docker 官方文档](https://docs.docker.com/get-docker/) 安装 Docker。

2. **拉取镜像、下载插件**

- [TS3AudioBot_docker](https://github.com/getdrunkonmovies-com/TS3AudioBot_docker)
- [网易云API Docker安装](https://binaryify.github.io/NeteaseCloudMusicApi/#/?id=docker-%e5%ae%b9%e5%99%a8%e8%bf%90%e8%a1%8c)
- [QQMusicApi_QR](https://github.com/RayQuantum/QQMusicApi_QR)
- [TS3AudioBot-Plugin-Netease-QQ](https://github.com/HuxiaoRoar/TS3AudioBot-Plugin-Netease-QQ/releases/)

3. **配置插件**

- 将`TS3AudioBot-Plugin-Netease-QQ.dll`文件以及配置文件`netease_qq_config.ini`复制到TS3AudioBot/plugins下，如果没有请先自行创建插件文件夹，文件的目录应该如下：
    ```
    - Bots
      - default
        - bot.toml
    - logs
    - plugins
      - TS3AudioBot-Plugin-Netease-QQ.dll
      - netease_qq_config.ini
    - NLog.config
    - rights.toml
    - ts3audiobot.db
    - ts3audiobot.toml
    ```

- 编辑文件`netease_qq_config.ini`，默认的配置文件如下

    ```ini
    [netease]
    neteaseAPI = http://127.0.0.1:3000
    cookies = ""

    [qq]
    qqAPI = http://127.0.0.1:3300
    cookies = ""
    qqfm = "99"
    ```

  在neteaseAPI和qqAPI处分别填入自己的api接口地址。随后将自己的QQ和网易云（可选）的cookies填入，保存。

4. **权限配置**

- 打开根目录下的`rights.toml` 修改权限
- 本插件可用全部命令如下

   ```toml
   # wq
   "cmd.wq.play",
   "cmd.wq.next",
   "cmd.wq.pre",
   "cmd.wq.seek",
   "cmd.wq.mv",
   "cmd.wq.mode",
   "cmd.wq.ls",
   "cmd.wq.go",
   "cmd.wq.rm",
   "cmd.wq.ls.p",
   "cmd.wq.clear",
   "cmd.wq.lyric",
   "cmd.wq.status",
   # wyy
   "cmd.wyy.login",
   "cmd.wyy.play",
   "cmd.wyy.insert",
   "cmd.wyy.add",
   "cmd.wyy.gd",
   "cmd.wyy.agd",
   "cmd.wyy.fm",
   "cmd.wyy.zj",
   # qq
   "cmd.qq.login",
   "cmd.qq.cookie",
   "cmd.qq.play",
   "cmd.qq.insert",
   "cmd.qq.add",
   "cmd.qq.gd",
   "cmd.qq.agd",   
   "cmd.qq.zj",
   "cmd.qq.fm",
   "cmd.qq.fmls",
   "cmd.qq.load",
   # Play controls
   "cmd.pause",
   "cmd.stop",
   "cmd.seek",
   "cmd.volume"   
   ```

- 管理员权限配置部分：

   在`useruid`一项，填写管理员的UID。UID在频道-客户端列表-自己昵称旁下拉查看。

   ```toml
   # Admin rule
   [[rule]]
      # Set your admin Group Ids here, ex: [ 13, 42 ]
      groupid = []
      # And/Or your admin Client Uids here
      useruid = ["此处填入你自己的UID"]
      # By default treat requests from localhost as admin
      ip = [ "127.0.0.1", "::1" ]
      "+" = "*"
   ```

- 普通用户权限配置部分：

  `useruid` 填写想赋予权限的用户UID，并将允许使用的指令复制进去。

   ```toml
   # Playing rights
   [[rule]]
   # Set Group Ids you want to allow here, ex: [ 13, 42 ]
   groupid = []
   # And/Or Client Uids here, ex [ "uA0U7t4PBxdJ5TLnarsOHQh4/tY=", "8CnUQzwT/d9nHNeUaed0RPsDxxk=" ]
   useruid = ["此处填入赋权用户的UID"]
   # Or remove groupid and useruid to allow for everyone

   "+" = [
   # Basic stuff
   "cmd.help.*",
   "cmd.pm",
   "cmd.subscribe",     
   ......
   "cmd.bot.use",
   "cmd.rights.can",

   #此处添加Bilibili插件权限
   "cmd.wq.play",
   "cmd.wq.next",
   ......
   ]

   ```

- 如果是私人频道，可以直接删除`groupid`和`useruid` ，权限填写通配符，给所有人所有权限

   ```toml
   # Admin rule
   [[rule]]
      # Set your admin Group Ids here, ex: [ 13, 42 ]    
      # And/Or your admin Client Uids here    
      # By default treat requests from localhost as admin
      ip = [ "127.0.0.1", "::1" ]
      "+" = "*"
   ```

5. **部署ts3audiobot容器**

    启动 TS3AudioBot 容器，初始化配置，设置服务器地址，频道，密码等。
    如果没有自动初始化，则去目录内的bots/default文件夹内创建bot.toml，总之，确保此时机器人能成功连接到你的TS3服务器。

    ```toml
    #Starts the instance when the TS3AudioBot is launched.
    run = true

    [bot]

    [commands]

    [commands.alias]
    default = "!play <url>"
    yt = "!search from youtube (!param 0)"

    [connect]
    #The server password. Leave empty for none.
    server_password = { pw = "服务器密码" }
    #The default channel password. Leave empty for none.
    channel_password = {  }
    #Overrides the displayed version for the ts3 client. Leave empty for default.
    client_version = {  }
    #The address, ip or nickname (and port; default: 9987) of the TeamSpeak3 server
    address = "服务器IP,示例127.0.0.1:9987"
    #Client nickname when connecting.
    name = "机器人名字"
    #Default channel when connecting. Use a channel path or "/<id>".
    #Examples: "Home/Lobby", "/5", "Home/Afk \\/ Not Here".
    channel = "<starting channel name>"

    [connect.identity]
    #||| DO NOT MAKE THIS KEY PUBLIC ||| The client identity. You can import a teamspeak3 identity here too.
    key = "<需要修改teamspeak 3 identity>"
    #The client identity offset determining the security level.
    offset = 28

    [reconnect]

    [audio]
    #When a new song starts the volume will be trimmed to between min and max.
    #When the current volume already is between min and max nothing will happen.
    #To completely or partially disable this feature, set min to 0 and/or max to 100.
    volume = {  }

    [playlists]

    [history]

    [events]

    ```

6. **部署api容器**

- 启动网易云API容器，初始化配置，登录网站<http://127.0.0.1:3000>，看到有网易云api的提示即安装完成。
- 启动QQ音乐API容器，初始化配置，登录网站<http://127.0.0.1:3300>，看到有QQ音乐api的提示即安装完成。
- 备注：

  原项目：<https://github.com/jsososo/QQMusicApi> 分支项目：<https://github.com/yunxiangjun/QQMusicApi/tree/master> 为了支持扫码登录和cookie保存，这里[我](https://github.com/RayQuantum/)自己修改了一下分支项目的代码，之后的插件都需要使用[我](https://github.com/RayQuantum/)修改过的QQ音乐API才能正常使用。具体部署项目如下，QQMusicApi_QR，具体部署方法和原版一致。

7. **加载插件**

    在频道中，输入`!plugin list`，你会看到输出`#0|RDY|Netease_QQ_plugin (BotPlugin)`，随后输入`!plugin load 0`（具体序号看自己的情况），成功加载会有提示。
8. **添加频道描述（可选）**

     复制频道描述，粘贴到频道说明中。

### 方法二：win安装

1. **下载软件、API、插件，并解压**

- [TS3AudioBot](https://github.com/Splamy/TS3AudioBot)
- [QQMusicApi_QR](https://github.com/RayQuantum/QQMusicApi_QR/releases/tag/v1.0.0)
- [网易云API](https://binaryify.github.io/NeteaseCloudMusicApi/#/README)原版寄了，找找fork吧。比如这个[api-enhanced](https://github.com/NeteaseCloudMusicApiEnhanced/api-enhanced)，还有这个[网易云API](https://github.com/577fkj/NeteaseCloudMusicApi)都行。
- [TS3AudioBot-Plugin-Netease-QQ](https://github.com/HuxiaoRoar/TS3AudioBot-Plugin-Netease-QQ/releases/)

2. **插件配置**

- 将`TS3AudioBot-Plugin-Netease-QQ.dll`文件以及配置文件`netease_qq_config.ini`复制到TS3AudioBot/plugins下，如果没有请先自行创建插件文件夹，文件的目录应该如下：
    ```ini
    - Bots
      - default
        - bot.toml
    - logs
    - plugins
      - TS3AudioBot-Plugin-Netease-QQ.dll
      - netease_qq_config.ini
    - NLog.config
    - TS3AudioBot.exe
    - rights.toml
    - ts3audiobot.db
    - ts3audiobot.toml
    ```

- 编辑文件`netease_qq_config.ini`同上

3. **权限配置** 同上

5. **启动软件**
- 在**TS3AudioBot**运行`TS3AudioBot.exe`，初始化配置，设置服务器地址，频道，密码等。

6. **部署api容器**

- 启动网易云API，初始化配置，登录网站<http://127.0.0.1:3000>，看到有网易云api的提示即安装完成。
- 启动QQ音乐API，初始化配置，登录网站<http://127.0.0.1:3300>，看到有QQ音乐api的提示即安装完成。
- 这部分具体查看各api的说明文档。
- 运行方法：
  ```
  网易
  npm i
  node app.js

  qq音乐
  node ./bin/www
  ```

7. **加载插件** 同上

8. **添加频道描述（可选）** 同上


## 频道描述（复制到频道说明）

```txt
[COLOR=#66ccff]频道输入或者私聊机器人，指令如下(把[xxx]替换成对应信息，最终结果不含中括号)[/COLOR]
-------------------------------------------
常用操作
获取token：!api token
插件列表：!plugins list
插件加载：!plugins load 数字序号
插件卸载：!plugins unload 数字序号
-------------------------------------------
基本操作
1.播放音乐(默认网易云)
[COLOR=#66ccff]!wq play [音乐id或者名称][/COLOR]
2.暂停音乐(或继续)
[COLOR=#66ccff]!pause[/COLOR]
3.修改音量
[COLOR=#66ccff]!volume [0-100的音量][/COLOR]
4.下一首
[COLOR=#66ccff]!wq next/!next[/COLOR]
5.上一首
[COLOR=#66ccff]!wq pre[/COLOR]
6.修改播放进度
[COLOR=#66ccff]!wq seek [进度(s)][/COLOR]
7.改变播放模式
[1=顺序播放 2=单曲循环 3=顺序循环 4=随机播放]
[5=顺序销毁 6=随机销毁(销毁为播完即从队列删除)]
[COLOR=#66ccff]!wq mode [模式号][/COLOR]
8.列出歌曲列表
[COLOR=#66ccff]!wq ls[/COLOR]
9.跳转到歌单中的歌曲
[COLOR=#66ccff]!wq go [音乐索引][/COLOR]
10.使用本插件播放当前音乐
[COLOR=#66ccff]!wq go [/COLOR]
11.从列表移除歌曲
[COLOR=#66ccff]!wq rm [音乐索引][/COLOR]
12.移动歌曲
[COLOR=#66ccff]!wq mv [需要移动的音乐] [目标位置][/COLOR]
13.列出歌曲列表第N页
[COLOR=#66ccff]!wq ls p [第N页][/COLOR]
14.清空歌曲列表
[COLOR=#66ccff]!wq clear[/COLOR]
15.启用歌词功能-默认关闭
[COLOR=#66ccff]!wq lyric[/COLOR]
16.查看当前状态
[COLOR=#66ccff]!wq status[/COLOR]
-------------------------------------------
网易云：
1.登录网易云账号(输入后通过机器人头像扫码登录)
[COLOR=#f02001]!wyy login[/COLOR]
2.立即播放网易云音乐
[COLOR=#f02001]!wyy play [音乐id或者名称][/COLOR]
3.添加音乐到下一首
[COLOR=#f02001]!wyy insert [音乐id或者名称][/COLOR]
4.添加音乐到列表末尾
[COLOR=#f02001]!wyy add [音乐id或者名称][/COLOR]
5.播放网易云音乐歌单
[COLOR=#f02001]!wyy gd [歌单id或者名称][/COLOR]
6.添加网易云音乐歌单到列表末尾
[COLOR=#f02001]!wyy agd [歌单id或者名称][/COLOR]
7.进入网易云FM模式(在FM模式中，下一首则为next)
[COLOR=#f02001]!wyy fm[/COLOR]
8.播放网易云音乐专辑
[COLOR=#f02001]!wyy zj [专辑id或者名称][/COLOR]
-------------------------------------------
QQ音乐
1.登录qq账号(使用QQ扫码登录)
[COLOR=#0DBD72]!qq login[/COLOR]
2.使用cookie登录qq账号(使用cookie登录)
[COLOR=#0DBD72]!qq cookie [填入cookie][/COLOR]
3.立即播放QQ音乐
[COLOR=#0DBD72]!qq play [音乐id或者名称][/COLOR]
4.添加音乐到下一首
[COLOR=#0DBD72]!qq insert [音乐id或者名称][/COLOR]
5.添加音乐到下一首
[COLOR=#0DBD72]!qq add [音乐id或者名称][/COLOR]
6.播放QQ音乐歌单
[COLOR=#0DBD72]!qq gd [歌单id或者名称][/COLOR]
7.添加QQ音乐歌单到列表末尾
[COLOR=#0DBD72]!qq agd [歌单id或者名称][/COLOR]
8.进入QQ音乐FM模式(在FM模式中，下一首则为next)
[COLOR=#0DBD72]!qq fm[/COLOR]
9.播放QQ音乐专辑
[COLOR=#0DBD72]!qq zj [专辑id或者名称][/COLOR]
10.加载本地的QQ的cookie
[COLOR=#0DBD72]!qq load[/COLOR]

-------------------------------------------
需要注意的是如果歌单歌曲过多需要时间加载，请耐心等待
以下例子加粗的就是音乐或者歌单id
网易云:
单曲: https://music.163.com/#/song?id=[B]254548[/B]
歌单: https://music.163.com/#/playlist?id=[B]545016340[/B]
专辑: https://music.163.com/#/album?id=[B]157289084[/B]
QQ音乐:
单曲mid: https://y.qq.com/n/ryqq/songDetail/[B]004Z8Ihr0JIu5s[/B]
单曲数字id: https://y.qq.com/n/ryqq/songDetail/[B]102065756[/B]?songtype=0
QQ歌单: https://y.qq.com/n/ryqq/playlist/[B]3805603854[/B]
专辑: https://y.qq.com/n/ryqq/albumDetail/[B]002koqNW00oOtF[/B]
客户端分享短链： [B]https://c6.y.qq.com/base/fcgi-bin/u?__=[I]7h5BKAiPjwKA[/I][/B]（输入完整短链或短链id均可）

```

## 使用教程

### 用户登录

#### 扫码登录

```txt
!wyy login
!qq login
```

- 机器人会通过头像发送二维码，使用APP扫码即可登录。

#### Cookie 登录

```txt
!qq cookie [填入cookie]
```

- 使用已获取的Cookie信息进行登录。

#### 加载本地Cookie

```txt
!qq load
```

- 加载保存在本地的QQ音乐Cookie。

#### 状态校验

```txt
!wq status
```

- 查看当前用户的登录状态
- 显示用户名和会员详情
- 显示代理服务器状态

### 音频播放、添加

#### 一、单曲播放

1. **基础播放命令**

```txt
!wq play [音乐id或者名称]      （默认网易云，等价于!wyy play）
!wyy play [音乐id或者名称]      例如：!wyy play 270474713
!qq play [音乐id或者名称]       例如：!qq play 004Z8Ihr0JIu5s 或 !qq play https://c6.y.qq.com/base/fcgi-bin/u?__=CuU2m4lL02Xv
```

- 播放音频
- 网易云音乐链接为`https://music.163.com/song?id=270474713`，id为270474713，唯一标识。
- QQ音乐的id比较复杂。
  - 网页直接打开的链接为`https://y.qq.com/n/ryqq/songDetail/004Z8Ihr0JIu5s`
  - 客户端的分享短链为`https://c6.y.qq.com/base/fcgi-bin/u?__=CuU2m4lL02Xv`
  - 打开这个短链，网页链接又变成了`https://y.qq.com/n/ryqq/songDetail/102065756`
  - 所以QQ音乐的id可以是`004Z8Ihr0JIu5s`，也可以是`102065756`，还可以直接使用分享短链`https://c6.y.qq.com/base/fcgi-bin/u?__=CuU2m4lL02Xv`，或者短链的id`CuU2m4lL02Xv`，四种方式均可。
- 也可以直接搜歌名+歌手，如果出现歧义，如歌名是纯数字，可以用方括号括起来，如`!wq play 【1234】`，则会优先搜索歌名为1234的歌曲。

2. **添加到下一首**

```txt
!wyy insert [音乐id或者名称]     例如：!wyy insert 270474713
!qq insert [音乐id或者名称]      例如：!qq insert 004Z8Ihr0JIu5s
```

- 将指定歌曲添加到下一首播放
- id规则同上

3. **添加到列表末尾**

```txt
!wyy add [音乐id或者名称]        例如：!wyy add 270474713
!qq add [音乐id或者名称]         例如：!qq add 004Z8Ihr0JIu5s
```

- 将指定歌曲添加到播放队列，播放完当前歌曲后依次播放
- id规则同上

#### 二、批量播放

1. **播放歌单**

```txt
!wyy gd [歌单id或者名称]         例如：!wyy gd 545016340
!qq gd [歌单id或者名称]          例如：!qq gd 3805603854
```

- 播放指定的歌单
- 该命令会清空当前播放队列
- 网易云音乐歌单链接为`https://music.163.com/playlist?id=545016340`，id为545016340，唯一标识。
- QQ音乐歌单链接为`https://y.qq.com/n/ryqq/playlist/3805603854`，id为3805603854，唯一标识。分享短链也可用，完整`https://c6.y.qq.com/base/fcgi-bin/u?__=ydKD1qUQ0Sd7`和短链ID`ydKD1qUQ0Sd7`均可。

2. **添加歌单到列表**

```txt
!wyy agd [歌单id或者名称]        例如：!wyy agd 545016340
!qq agd [歌单id或者名称]         例如：!qq agd 3805603854
```

- 将指定歌单添加到播放队列的末尾
- 该命令不会清空当前播放队列。
- id规则同上

3. **播放专辑**

```txt
!wyy zj [专辑id或者名称]         例如：!wyy zj 157289084
!qq zj [专辑id或者名称]          例如：!qq zj 002koqNW00oOtF
```

- 播放指定的专辑
- 该命令会清空当前播放队列。
- 网易云音乐专辑链接为`https://music.163.com/album?id=157289084`，id为157289084，唯一标识。
- QQ音乐专辑链接为`https://y.qq.com/n/ryqq/albumDetail/000f01724fd7TH`，id为000f01724fd7TH，唯一标识。分享短链也可用，完整`https://c6.y.qq.com/base/fcgi-bin/u?__=NkiumYqV0S7A`和短链ID`NkiumYqV0S7A`均可。


### FM 模式

#### 1. **FM播放**

```TXT
!wyy fm
!qq fm
!qq fm [频道号]                 例如：!qq fm 99
```

- 进入网易云音乐、QQ音乐的私人FM模式。
- 在该模式下，使用 !wq next 或 !next 切歌。
- qq fm后面可以加频道号，默认99，频道号列表见下方`!qq fmls`命令。
- 该命令会清空当前播放队列。

#### 2. **查看QQ音乐 FM频道**

```TXT
!qq fmls
```

- 查看QQ音乐FM频道列表和频道号
- 99为私人频道推荐，需要登录

### 队列管理

#### 1. **查看当前播放队列**

```txt

!wq ls
!wq ls p [第N页]                例如：!wq ls p 2
```

- 显示当前播放队列中的歌曲信息
- 可跳转指定页数。

#### 2. **清空队列**

```txt
!wq clear
```

- 暂停播放并清空当前播放队列。

#### 3. **队列控制**

```txt
!wq next           # 播放下一首
!wq pre            # 播放上一首
!wq go             # 使用本插件播放当前音乐
!wq go [音乐索引]   # 跳转队列中的指定歌曲                            例如：!wq go 3
!wq rm [音乐索引]   # 移除队列中的指定歌曲                            例如：!wq rm 3
!wq mv [需要移动的音乐索引] [目标位置] # 将队列中的一首歌移动到另一位置  例如：!wq mv 3 1
```

- 音乐索引可通过`!wq ls`查看
- !wq go 不加参数，可以激活本插件，并播放最后放过的音乐，适用于多插件切换情况。

#### 4. **切换播放模式**

```txt
!wq mode [模式号] # 切换播放模式，1=顺序播放 2=单曲循环 3=顺序循环 4=随机播放 5=顺序销毁 6=随机销毁
```

- 切换播放模式
- 销毁模式为播放完即从队列删除

### 其他功能

#### 歌词功能

```txt
!wq lyric
```

- 启用或关闭歌词显示功能（默认为关闭）。

#### 调整播放进度

```txt
!wq seek [进度(s)]
```

快进或后退到指定的时间点（单位：秒）。

## 📋 完整命令列表

| 命令                 | 参数                | 功能描述                   | 示例                      |
| :------------------- | :------------------ | :------------------------- | :------------------------ |
| **通用命令**         |                     |                            |                           |
| `!wq play`           | [音乐id或名称]      | 播放音乐（默认为网易云）   | `!wq play 稻香`           |
| `!wq next` / `!next` | 无                  | 播放下一首                 | `!wq next`                |
| `!wq pre`            | 无                  | 播放上一首                 | `!wq pre`                 |
| `!wq seek`           | [进度(s)]           | 修改播放进度               | `!wq seek 60`             |
| `!wq mv`             | [原位置] [目标位置] | 移动歌曲到指定位置         | `!wq mv 3 1`              |
| `!wq mode`           | [模式号]            | 切换播放模式 (1-6)         | `!wq mode 4`              |
| `!wq ls`             | 无 或 p [页码]      | 列出歌曲列表或指定页       | `!wq ls p 2`              |
| `!wq go`             | 无                  | 使用本插件播放当前音乐     | `!wq go `                 |
| `!wq go`             | [音乐索引]          | 跳转到列表指定歌曲         | `!wq go 5`                |
| `!wq rm`             | [音乐索引]          | 从列表移除指定歌曲         | `!wq rm 3`                |
| `!wq clear`          | 无                  | 清空歌曲列表               | `!wq clear`               |
| `!wq lyric`          | 无                  | 启用/关闭歌词功能          | `!wq lyric`               |
| `!wq status`         | 无                  | 查看当前登录和播放状态     | `!wq status`              |
| **网易云音乐**       |                     |                            |                           |
| `!wyy login`         | 无                  | 扫码登录网易云账号         | `!wyy login`              |
| `!wyy play`          | [音乐id或名称]      | 立即播放网易云音乐         | `!wyy play 254548`        |
| `!wyy insert`        | [音乐id或名称]      | 添加音乐到下一首播放       | `!wyy insert 七里香`      |
| `!wyy add`           | [音乐id或名称]      | 添加音乐到列表末尾         | `!wyy add 254548`         |
| `!wyy gd`            | [歌单id或名称]      | 播放网易云音乐歌单         | `!wyy gd 545016340`       |
| `!wyy agd`           | [歌单id或名称]      | 添加网易云歌单到列表       | `!wyy agd 545016340`      |
| `!wyy fm`            | 无                  | 进入网易云私人FM模式       | `!wyy fm`                 |
| `!wyy zj`            | [专辑id或名称]      | 播放网易云音乐专辑         | `!wyy zj 157289084`       |
| **QQ音乐**           |                     |                            |                           |
| `!qq login`          | 无                  | 扫码登录QQ音乐账号         | `!qq login`               |
| `!qq cookie`         | [Cookie字符串]      | 使用Cookie登录QQ音乐       | `!qq cookie "uin=xxx;"`   |
| `!qq play`           | [音乐id或名称]      | 立即播放QQ音乐             | `!qq play 004Z8Ihr0JIu5s` |
| `!qq insert`         | [音乐id或名称]      | 添加音乐到下一首播放       | `!qq insert 夜曲`         |
| `!qq add`            | [音乐id或名称]      | 添加音乐到列表末尾         | `!qq add 102065756`       |
| `!qq gd`             | [歌单id或名称]      | 播放QQ音乐歌单             | `!qq gd 3805603854`       |
| `!qq agd`            | [歌单id或名称]      | 添加QQ音乐歌单到列表       | `!qq agd 3805603854`      |
| `!qq fm`             | 无                  | 进入QQ音乐FM模式           | `!qq fm`                  |
| `!qq fm`             | [频道号]            | 进入指定频道的QQ音乐FM模式 | `!qq fm 99`               |
| `!qq fmls`           | 无                  | 查看QQ音乐FM频道列表       | `!qq fmls`                |
| `!qq zj`             | [专辑id或名称]      | 播放QQ音乐专辑             | `!qq zj 002koqNW00oOtF`   |
| `!qq load`           | 无                  | 加载本地保存的QQ音乐Cookie | `!qq load`                |
| **其他通用命令**     |                     |                            |                           |
| `!pause`             | 无                  | 暂停或继续播放             | `!pause`                  |
| `!stop`              | 无                  | 停止播放                   | `!stop`                   |
| `!volume`            | [0-100]             | 修改音量                   | `!volume 50`              |

## 已知问题

1. QQ音乐有的QQ音乐无法播放，具体报错：URL无法播放-http/isure.stream.qqmusic.qq.com/xxxxxxxxxxxxxxxxx.m4a?guid=xxxxxxxxxxxxxx&vkey=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx&uin=xxxxxxx&fromtag=xxxxx。已经尝试修复，若出现建议重启QQ音乐API。
2. 使用！wq next会连续跳两次，解决方法：使用！next
3. qq 的 cookie有效期很短，3天左右就会失效，需要重新登录获取新的cookie。

## 🙏 致谢

感谢以下项目和开发者：

- [TS3AudioBot-BiliBiliPlugin](https://github.com/xxmod/TS3AudioBot-BiliBiliPlugin) - 提供插件开发参考
- [TS3AudioBot-NetEaseCloudmusic-plugin](https://github.com/ZHANGTIANYAO1/TS3AudioBot-NetEaseCloudmusic-plugin) - 提供插件开发参考
- [TS3AudioBot-CloudMusic-plugin](https://github.com/577fkj/TS3AudioBot-CloudMusic-plugin) - 提供插件开发参考
- [TS3AudioBot-Plugin-Netease-QQ](https://github.com/RayQuantum/TS3AudioBot-Plugin-Netease-QQ) - 提供插件开发参考
- [`Splamy/TS3AudioBot`](https://github.com/Splamy/TS3AudioBot) - 优秀的 TeamSpeak 音频机器人框架
- [`neteasecloudmusicapi`](https://github.com/Binaryify/neteasecloudmusicapi) - 网易云音乐API项目
- [`QQMusicApi`](https://github.com/jsososo/QQMusicApi) - QQ音乐API项目
