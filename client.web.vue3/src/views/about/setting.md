<!--
 * @Author: snltty
 * @Date: 2021-09-03 14:39:29
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-03 15:02:44
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\about\setting.md
-->
## 1、安装.NET5 环境
1. windows 64位 <a href="https://dotnet.microsoft.com/download/dotnet/thank-you/sdk-5.0.400-windows-x64-installer" target="_blank">https://dotnet.microsoft.com/download/dotnet/thank-you/sdk-5.0.400-windows-x64-installer</a>
2. windows 32位 <a href="https://dotnet.microsoft.com/download/dotnet/thank-you/sdk-5.0.400-windows-x86-installer" target="_blank">https://dotnet.microsoft.com/download/dotnet/thank-you/sdk-5.0.400-windows-x86-installer</a>
3. 其它版本  <a href="https://dotnet.microsoft.com/download/dotnet/5.0" target="_blank">https://dotnet.microsoft.com/download/dotnet/5.0</a>

## 2、你有服务器，想自己发布服务端
1. server.service -> publish文件夹  或者自己发布项目
2. 修改或者使用默认的 appsettings.json 里对应的配置 
```
{
  "udp": 5410, //udp监听端口
  "tcp": 59410//tcp监听端口
}
```
3. 运行 
```
1、运行 server.service.exe  
2、命令行 进入 server.service -> publish文件夹  dotnet server.service.dll运行
3、使用nssm.exe 将 server.service.dll注册为windows services，启动服务
```
4. 开始使用客户端

## 3、使用客户端
1. client.service -> publish文件夹   或者自己发布 项目
2. 修改或者使用默认的  appsettings.json 里对应的配置 【没啥特殊需求的不改就行】
```
{
  //web服务，浏览器打开  127.0.0.1:5410  使用web端管理界面
  "web": {
    "Ip": "127.0.0.1",
    "Port": 5410,
    "Path": "./web"
  },
  //web管理端和客户端的通信 
  //如果你修改了这个配置 ，则需要自己修改配置发布一遍 web管理端，然后发布后的代码放到 “web”配置的path目录下
  "websocket": {
    "Ip": "127.0.0.1",
    "Port": 59410
  },
  //客户端
  "client": {
    "GroupId": "", //分组id
    "Name": "A客户端", //客户端名
    "AutoReg": false //启动服务自动注册到服务器
  },
  //服务器，与服务端对应
  "server": {
    "Ip": "120.79.205.184", //地址
    "Port": 5410,  //udp端口
    "TcpPort": 59410 //tcp端口 
  }
}
```
3. 运行 
```
1、运行 client.service.exe  
2、命令行 进入 client.service -> publish文件夹  dotnet client.service.dll运行
3、使用nssm.exe 将 client.service.dll注册为windows services，启动服务
```
4. 浏览器打开  配置文件里“web”配置项配置的地址，开始使用吧


## 4、如果你想自己修改 web管理端，或者修改可 客户端的 websocket配置
1. 项目 client.web.vue3  使用的是 vue3+element-plus
2. 修改通信地址
```
src  apis  request.js 里
new WebSocket('ws://127.0.0.1:59410');
与客户端【websocket】配置对应
```
3. npm run serve 运行调试，  npm run build 发布