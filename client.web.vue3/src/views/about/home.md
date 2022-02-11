<!--
 * @Author: snltty
 * @Date: 2021-09-07 00:37:53
 * @LastEditors: snltty
 * @LastEditTime: 2022-02-11 18:07:12
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\about\home.md
-->

# p2p-tunnel
1. .NET6 Socket编程实现内网穿透
2. UDP,TCP打洞实现点对点直连
3. 访问内网web，内网桌面，及其它TCP上层协议服务
4. 服务端只承受 客户端注册，客户端信息的交换。不承受数据转发，几乎无压力
5. 也带有 服务端转发 插件（需要自己部署服务器）
6. 多平台域名解析服务，动态更新解析(当IP变化时自动更新),支持 **阿里云、腾讯云** , 适合有公网家庭宽带做服务器的需求
7. 文件服务 600MB/s+,(同机器下，硬盘速度受到了影响)

<img src="./imgs/file-speed.png" style="border:1px solid #ddd;max-width:50%">

8. 序列化->打包->粘包解析->解包->反序列化 整个流程时间

<img src="./imgs/speed.png" style="border:1px solid #ddd;max-width:50%">

# QQ群 
一起打造一个满意的打洞直连内网穿透工具
<img src="./imgs/qrcode.png" width="200" style="border:1px solid #ddd;">


# <font color="red">注意事项</font>
1. 服务器 或 内网电脑，暴露服务在公网时，请做好安全防范
2. 如 3389 桌面服务 不使用 administrator作为登录账号
3. 如 1433 数据库服务 不使用 sa 账号
4. 请使用复杂密码， 数字+字母+字符 交替

# 项目结构
1. p2p  打洞项目
    1. client 客户端公共内容
    2. client.service **打洞客户端**
    4. client.service.ftp 客户端服务的  文件服务插件
    5. client.service.tcpforward 客户端服务的 tcp转发插件
    6. client.service.upnp 网关端口映射管理 **(默认不启用)**
    7. client.service.wakeup 幻数据包唤醒 **(默认不启用)**
    8. client.service.cmd p2p远程客户端命令 **(默认不启用)**
    9. client.service.ddns 域名解析 **(默认不启用)**
    10. client.service.serverTcpforward 服务端TCP转发(需要自己部署服务器) **(默认不启用)**
    11. common 一些公共的功能
    12. server 服务器
    13. server.service **打洞服务端**
2. platform 跟平台有关的一些实验项目
    1. win  windows特有的
        1. remoteDektop 远程桌面相关
            1. mstsc.manager 远程桌面管理
            2. rdp.desktop rdp协议的桌面共享
            3. rdp.viewer rdp协议的桌面共享查看器
3. testing  尝试中的项目
    1. audio.test 音频测试
    2. NSpeex 音频压缩
    3. ozeki 降噪 回音消除
    4. mstsc.manager windows远程桌面管理
    5. rdp.desktop rdp 桌面共享 
    6. rdp.viewer  rdp 桌面共享查看器
4. client.web.vue3 客户端管理界面
