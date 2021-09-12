<!--
 * @Author: snltty
 * @Date: 2021-09-07 00:37:53
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-12 21:07:36
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\about\home.md
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
6. rdpViewer  rdp 桌面共享查看器

### 一些特殊的地方
1. 如果你使用<font color="red">虚拟机</font>，请将<font color="red">虚拟机</font>网络设置为 **【桥接模式】**，虚拟机使用物理网络
2. 源码都是开源的，默认提供的服务器会对 **【分组编号】** 进行加密，也尽可能的做好安全防范，但是无法保证绝对安全，如果有条件，非常推荐自己部署服务器