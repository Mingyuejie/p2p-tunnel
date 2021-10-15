<!--
 * @Author: snltty
 * @Date: 2021-10-12 16:31:35
 * @LastEditors: snltty
 * @LastEditTime: 2021-10-16 01:10:39
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\about\server.md
-->
# 你有服务器，想自己发布服务端
1. server.service -> publish文件夹  或者自己发布项目
2. 修改或者使用默认的 appsettings.json 里对应的配置 
```
{
  "udp": 5410, //udp监听端口
  "tcp": 59410//tcp监听端口
  "tcpForward":false //是否启用服务端TCP转发 （共用服务器不开启,顶不住）
}
```

3. 运行 
```
方式1、运行 server.service.exe  
方式2、命令行 进入 server.service -> publish文件夹  dotnet server.service.dll运行
方式3、使用nssm.exe 将 server.service.dll注册为windows services，启动服务
```