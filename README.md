<!--
 * @Author: snltty
 * @Date: 2021-08-22 14:09:03
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-03 14:56:07
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3c:\Users\ASUS\Desktop\p2p-tunnel\README.md
-->
# 详细说明

<a href="http://snltty.gitee.io/p2p-tunnel/#/about-home.html" target="_blank">详细说明</a>

# p2p-tunnel

1. .NET5 Socket编程实现内网穿透
2. UDP,TCP打洞实现点对点直连
3. 访问内网web，内网桌面，及其它TCP上层协议服务
4. 服务端只承受 客户端注册，客户端信息的交换。不承受数据转发，几乎无压力

### 内网穿透相关
1. server.service 服务端，
2. client.service 客户端
3. client.web.vue3 客户端管理界面

### 其它
1. audio.test 音频测试
2. NSpeex 音频压缩
3. ozeki 降噪 回音消除
4. mstsc.manager windows远程桌面管理
5. rdp.desktop rdp 桌面共享 
6. rdp.viewer  rdp 桌面共享查看器


### 截图
#### 1. 注册
![注册](./screenshot/reg.jpg)


#### 2. 客户端列表
![客户端列表](./screenshot/clients.jpg)


#### 3. 转发配置
![转发配置](./screenshot/tcpforward.jpg)

#### 4. 访问对方网页
![访问网页](./master/screenshot/bweb.jpg)

#### 5. 访问对方桌面
![访问网页](./screenshot/bdesktop.jpg)