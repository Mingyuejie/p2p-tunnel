/*
 * @Author: snltty
 * @Date: 2021-08-19 21:50:16
 * @LastEditors: snltty
 * @LastEditTime: 2022-02-11 18:00:26
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\router\index.js
 */
import { createRouter, createWebHashHistory } from 'vue-router'

const routes = [
    {
        path: '/',
        name: 'Home',
        component: () => import('../views/Home.vue')
    },
    {
        path: '/register.html',
        name: 'Register',
        component: () => import('../views/Register.vue')
    },

    {
        path: '/services.html',
        name: 'Services',
        component: () => import('../views/service/Index.vue'),
        redirect: { name: 'ServiceConfigure' },
        children: [
            {
                path: '/service-configure.html',
                name: 'ServiceConfigure',
                component: () => import('../views/service/configure/Configure.vue'),
                meta: { name: '插件配置' }
            },
            {
                path: '/service-cmd.html',
                name: 'ServiceCmd',
                component: () => import('../views/service/cmd/Index.vue'),
                meta: { name: '远程命令' }
            },
            {
                path: '/service-logger.html',
                name: 'ServiceLogger',
                component: () => import('../views/service/Logger.vue'),
                meta: { name: '日志信息' }
            },
            {
                path: '/service-ftp.html',
                name: 'ServiceFtp',
                component: () => import('../views/service/ftp/Index.vue'),
                meta: { name: '文件服务' }
            },
            {
                path: '/service-tcp-forward.html',
                name: 'ServiceTcpForward',
                component: () => import('../views/service/TcpForward.vue'),
                meta: { name: 'TCP转发' }
            },
            {
                path: '/service-upnp.html',
                name: 'ServiceUPNP',
                component: () => import('../views/service/UPNP.vue'),
                meta: { name: 'UPNP映射' }
            },
            {
                path: '/service-wakeup.html',
                name: 'ServiceWakeUp',
                component: () => import('../views/service/WakeUp.vue'),
                meta: { name: '幻数据包' }
            },
            {
                path: '/service-ddns.html',
                name: 'ServiceDdns',
                component: () => import('../views/service/ddns/Index.vue'),
                meta: { name: '域名解析' }
            },
        ]
    },
    {
        path: '/about.html',
        name: 'About',
        component: () => import('../views/about/Index.vue'),
        redirect: { name: 'AboutHome' },
        children: [
            {
                path: '/about-home.html',
                name: 'AboutHome',
                component: () => import('../views/about/Home.vue')
            },
            {
                path: '/about-runtime.html',
                name: 'AboutRuntime',
                component: () => import('../views/about/Runtime.vue')
            },
            {
                path: '/about-server.html',
                name: 'AboutServer',
                component: () => import('../views/about/Server.vue')
            },
            {
                path: '/about-client.html',
                name: 'AboutClient',
                component: () => import('../views/about/Client.vue')
            },
            {
                path: '/about-server-tcp-forward.html',
                name: 'ServerTcpforward',
                component: () => import('../views/about/ServerTcpforward.vue')
            },
            {
                path: '/about-use.html',
                name: 'AboutUse',
                component: () => import('../views/about/Use.vue')
            },
            {
                path: '/about-publish.html',
                name: 'AboutPublish',
                component: () => import('../views/about/Publish.vue')
            },
            {
                path: '/about-winservice.html',
                name: 'AboutWinService',
                component: () => import('../views/about/WinService.vue')
            },
            {
                path: '/about-ddns.html',
                name: 'AboutDdns',
                component: () => import('../views/about/Ddns.vue')
            }
        ]
    }
]

const router = createRouter({
    history: createWebHashHistory(),
    routes
})

export default router
