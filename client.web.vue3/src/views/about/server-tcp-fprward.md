<!--
 * @Author: snltty
 * @Date: 2021-10-12 16:31:35
 * @LastEditors: snltty
 * @LastEditTime: 2021-10-16 01:14:50
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\about\server-tcp-fprward.md
-->
# 服务器转发实现内网穿透
## 并不是所有时候都可以打洞成功，这时候就需要服务端转发实现内网穿透
--

## 服务端  server.service
```
{
  "udp": 5410,
  "tcp": 59410,
  "tcpForward": false //此配置项设置为true  重新运行
}
```
-- 

## 客户端  client.service.serverTcpforward 
配置文件  servertcpforward-appsettings.json
<font color="red">如果未开启自动注册(AutoReg)，则需要在**插件配置**页面，**启用插件(已启用需重新启用) 或者 保存配置**，才会注册转发</font>
```
{
    //web转发
    // 一个端口可对应多个host，同端口，不同host转发到内网不同的服务
  "Web": [
    {
      "Port": 8099, //服务器端口
      "Forwards": {
        "test1.snltty.com": {
          "TargetPort": 8099, //内网端口
          "TargetIp": "127.0.0.1" //内网服务地址
        }
      }
    }
  ],
  //tcp协议转发
  // 一个端口对应一个内网服务
  "Tunnel": [
    {
      "Port": 3388, //服务器端口
      "TargetPort": 3389, //内网端口
      "TargetIp": "127.0.0.1" //内网服务地址
    }
  ],
  "Enable": true,  //是否启用，当不启用时，表示不允许访问本地服务
  "AutoReg": true  //是否自动注册， 当开启时， “客户端注册”成功后，将自动注册转发
}

```