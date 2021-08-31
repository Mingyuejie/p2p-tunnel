<!--
 * @Author: snltty
 * @Date: 2021-08-22 14:09:03
 * @LastEditors: snltty
 * @LastEditTime: 2021-08-22 14:14:39
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3d:\Desktop\p2p-tunnel\README.md
-->
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

```
我也不知道成功率大概有多少，想试试的同学，配置及更详细说明在 wiki 
```

| A端 | B端 | 平台 | 可用 |
| :----:| :----: | :----: |:----: |
| 三级网关（电信网） | 二级网关(TP-LINK WDR5620),三级网关(天翼网关 EchoLife HS8145C5->TP-LINK WDR5620) | win10 |√ |


### 截图
#### 1. 注册
![注册](https://gitee.com/snltty/p2p-tunnel/raw/master/screenshot/reg.jpg)


#### 2. 客户端列表
![客户端列表](https://gitee.com/snltty/p2p-tunnel/raw/master/screenshot/clients.jpg)


#### 3. 转发配置
![转发配置](https://gitee.com/snltty/p2p-tunnel/raw/master/screenshot/tcpforward.jpg)

#### 4. 访问对方网页
![访问网页](https://gitee.com/snltty/p2p-tunnel/raw/master/screenshot/bweb.jpg)

#### 5. 访问对方桌面
![访问网页](https://gitee.com/snltty/p2p-tunnel/raw/master/screenshot/bdesktop.jpg)