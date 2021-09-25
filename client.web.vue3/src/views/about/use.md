<!--
 * @Author: snltty
 * @Date: 2021-09-11 22:51:06
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-25 23:28:24
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\about\use.md
-->
## 打洞直连
1. 打洞直连，借由服务器作为中介，客户端注册自身信息
2. 两客户端以相同 **【分组编号】** 注册后，会自动尝试进行连接对方
3. 连接成功后，可客户端之间直接发送数据，不经过服务器参与。
4. 连接成功后，方可使用 **【TCP转发】** 等网络通信功能 

## 基本流程

<img src="./imgs/liucheng.png">