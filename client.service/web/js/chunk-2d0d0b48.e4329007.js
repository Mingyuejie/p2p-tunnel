(window["webpackJsonp"]=window["webpackJsonp"]||[]).push([["chunk-2d0d0b48"],{"68b2":function(n,r,e){"use strict";e.r(r);var t=e("7a23");function s(n,r,e,s,o,c){var i=Object(t["resolveComponent"])("v-md-preview");return Object(t["openBlock"])(),Object(t["createElementBlock"])("div",null,[Object(t["createVNode"])(i,{text:s.md},null,8,["text"])])}var o='## 1、安装.NET5 环境\r\n1. windows 64位 https://dotnet.microsoft.com/download/dotnet/thank-you/sdk-5.0.400-windows-x64-installer\r\n2. windows 32位 https://dotnet.microsoft.com/download/dotnet/thank-you/sdk-5.0.400-windows-x86-installer\r\n3. 其它版本  https://dotnet.microsoft.com/download/dotnet/5.0\r\n\r\n## 2、你有服务器，想自己发布服务端\r\n1. server.service -> publish文件夹  或者自己发布项目\r\n2. 修改或者使用默认的 appsettings.json 里对应的配置 \r\n```\r\n{\r\n  "udp": 5410, //udp监听端口\r\n  "tcp": 59410//tcp监听端口\r\n}\r\n```\r\n3. 运行 \r\n```\r\n1、运行 server.service.exe  \r\n2、命令行 进入 server.service -> publish文件夹  dotnet server.service.dll运行\r\n3、使用nssm.exe 将 server.service.dll注册为windows services，启动服务\r\n```\r\n4. 开始使用客户端\r\n\r\n## 3、使用客户端\r\n1. client.service -> publish文件夹   或者自己发布 项目\r\n2. 修改或者使用默认的  appsettings.json 里对应的配置 【没啥特殊需求的不改就行】\r\n```\r\n{\r\n  //web服务，浏览器打开  127.0.0.1:5410  使用web端管理界面\r\n  "web": {\r\n    "Ip": "127.0.0.1",\r\n    "Port": 5410,\r\n    "Path": "./web"\r\n  },\r\n  //web管理端和客户端的通信 \r\n  //如果你修改了这个配置 ，则需要自己修改配置发布一遍 web管理端，然后发布后的代码放到 “web”配置的path目录下\r\n  "websocket": {\r\n    "Ip": "127.0.0.1",\r\n    "Port": 59410\r\n  },\r\n  //客户端\r\n  "client": {\r\n    "GroupId": "", //分组id\r\n    "Name": "A客户端", //客户端名\r\n    "AutoReg": false //启动服务自动注册到服务器\r\n  },\r\n  //服务器，与服务端对应\r\n  "server": {\r\n    "Ip": "120.79.205.184", //地址\r\n    "Port": 5410,  //udp端口\r\n    "TcpPort": 59410 //tcp端口 \r\n  }\r\n}\r\n```\r\n3. 运行 \r\n```\r\n1、运行 client.service.exe  \r\n2、命令行 进入 client.service -> publish文件夹  dotnet client.service.dll运行\r\n3、使用nssm.exe 将 client.service.dll注册为windows services，启动服务\r\n```\r\n4. 浏览器打开  配置文件里“web”配置项配置的地址，开始使用吧\r\n\r\n\r\n## 4、如果你想自己修改 web管理端，或者修改可 客户端的 websocket配置\r\n1. 项目 client.web.vue3  使用的是 vue3+element-plus\r\n2. 修改通信地址\r\n```\r\nsrc  apis  request.js 里\r\nnew WebSocket(\'ws://127.0.0.1:59410\');\r\n与客户端【websocket】配置对应\r\n```\r\n3. npm run serve 运行调试，  npm run build 发布',c={setup:function(){return{md:o}}};c.render=s;r["default"]=c}}]);