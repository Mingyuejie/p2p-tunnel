<!--
 * @Author: snltty
 * @Date: 2021-09-03 14:39:29
 * @LastEditors: snltty
 * @LastEditTime: 2021-10-16 00:57:09
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\about\client.md
-->
## 使用客户端
1. client.service -> publish文件夹   或者自己发布 项目
2. 修改或者使用默认的  appsettings.json 里对应的配置 【没啥特殊需求的不改就行】
```
{
    //web管理端
  "web": {
    "Port": 5410,
    "Root": "./public/web",
    "UseIpv6": false // 使用ipv6
  },
  //web管理与客户端的通信
  "websocket": {
    "Port": 59410,
    "UseIpv6": false // 使用ipv6
  },
  //客户端
  "client": {
    "GroupId": "", //分组编号
    "Name": "A客户端",
    "AutoReg": false, //自动注册
    "UseMac": false, //上报 mac
    "UseIpv6": false // 使用ipv6
  },
  //信令服务器
  "server": {
    "Ip": "p2p.snltty.com",
    "Port": 5410,
    "TcpPort": 59410
  }
}
```
3. 运行 
```
方式1、运行 client.service.exe  
方式2、命令行 进入 client.service -> publish文件夹  dotnet client.service.dll运行
方式3、使用nssm.exe 将 client.service.dll注册为windows services，启动服务
```
4. 浏览器打开  配置文件里“web”配置项配置的地址，开始使用吧


## 如果你想自己修改 web管理端，或者修改客户端的 websocket配置
1. 项目 client.web.vue3  使用的是 vue3+element-plus
2. 修改通信地址
```
src  apis  request.js 里
new WebSocket('ws://127.0.0.1:59410');
与客户端【websocket】配置对应
```
3. npm run serve 运行调试，  npm run build 发布