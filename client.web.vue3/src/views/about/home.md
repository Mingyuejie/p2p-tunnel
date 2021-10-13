<!--
 * @Author: snltty
 * @Date: 2021-09-07 00:37:53
 * @LastEditors: snltty
 * @LastEditTime: 2021-10-13 17:54:57
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\about\home.md
-->

# p2p-tunnel
1. .NET5 Socket编程实现内网穿透
2. UDP,TCP打洞实现点对点直连
3. 访问内网web，内网桌面，及其它TCP上层协议服务
4. 服务端只承受 客户端注册，客户端信息的交换。不承受数据转发，几乎无压力
5. 也带有 服务端转发 插件（需要自己部署服务器）

# <font color="red">注意事项</font>
1. <font color="red">服务器 或 内网电脑，暴露服务在公网时，请做好安全防范</font>
2. <font color="red">如 3389 桌面服务 不使用 administrator作为登录账号</font>
3. <font color="red">如 1433 数据库服务 不使用 sa 账号</font>
4. <font color="red">请使用复杂密码， 数字+字母+字符 交替</font>


# 方案
<table>
    <thead>
        <tr>
            <td>方案</td>
            <td>优点</td>
            <td>缺点</td>
            <td>说明</td>
        </tr>
    </thead>
    <tbody>
        <tr>
            <td>TCP打洞直连</td>
            <td>延迟低，速度可达110M/s，无服务器压力</td>
            <td>需网关支持</td>
            <td>默认方案</td>
        </tr>
        <tr>
            <td>服务器中转</td>
            <td>无需打洞，不需要额外条件</td>
            <td>延迟高，服务器压力较大，只在客户端可用</td>
            <td>当打洞失败时，消息将通过服务器中转</td>
        </tr>
        <tr>
            <td>服务器转发代理</td>
            <td>无需打洞，不需要额外条件，随处可用</td>
            <td>延迟高，服务器压力较大</td>
            <td>当需要随时随地访问内网时，可使用服务器转发代理</td>
        </tr>
    </tbody>
</table>

# 项目结构
1. p2p  打洞项目
    1. client 客户端公共内容
    2. client.service 客户端服务
    3. client.service.album 客户端服务的 图片相册插件
    4. client.service.ftp 客户端服务的  文件服务插件
    5. client.service.tcpforward 客户端服务的 tcp转发插件
    5. client.service.upnp 网关端口映射管理
    5. client.service.wakeup 幻数据包唤醒
    5. client.service.cmd p2p远程客户端命令
    5. client.service.serverTcpforward 服务端TCP转发(需要自己部署服务器)
    6. common 一些公共的功能
    7. server 服务器
    8. server.service 打洞服务器的服务端
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

# QQ群 
一起打造一个满意的打洞直连内网穿透工具
<img src="./imgs/qrcode.png" width="200" style="border:1px solid #ddd;">


# 一些特殊的地方
1. 如果你使用<font color="red">虚拟机</font>，请将<font color="red">虚拟机</font>网络设置为 **【桥接模式】**，虚拟机使用物理网络
2. 源码都是开源的，默认提供的服务器会对 **【分组编号】** 进行加密，也尽可能的做好安全防范，但是无法保证绝对安全，如果有条件，非常推荐自己部署服务器