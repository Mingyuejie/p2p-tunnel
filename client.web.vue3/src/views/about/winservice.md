<!--
 * @Author: snltty
 * @Date: 2021-09-04 00:02:43
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-04 00:17:14
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\about\winservice.md
-->
# 注册为 windows 服务
1. 打开 **nssm安装服务.bat**   或者 命令行进入程序目录运行  nssm install ，出现以下界面

<img src="./imgs/nssm.png">

2. win+r 输入 services.msc 确定 进入服务列表 找到服务  右键启动

# 删除服务

win+r 输入cmd 确定 进入命令行
```
nssm remove client.service
```
