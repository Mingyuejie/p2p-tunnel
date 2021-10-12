/*
 * @Author: snltty
 * @Date: 2021-08-19 21:50:16
 * @LastEditors: snltty
 * @LastEditTime: 2021-10-12 16:30:59
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
        path: '/plugins.html',
        name: 'Pugins',
        component: () => import('../views/plugin/Index.vue'),
        redirect: { name: 'PluginSetting' },
        children: [
            {
                path: '/plugin-setting.html',
                name: 'PluginSetting',
                component: () => import('../views/plugin/Setting.vue'),
                meta: { name: '插件配置' }
            },
            {
                path: '/plugin-cmd.html',
                name: 'PluginCmd',
                component: () => import('../views/plugin/cmd/Index.vue'),
                meta: { name: '远程命令' }
            },
            {
                path: '/plugin-logger.html',
                name: 'PluginLogger',
                component: () => import('../views/plugin/Logger.vue'),
                meta: { name: '日志信息' }
            },
            {
                path: '/plugin-ftp.html',
                name: 'PluginFtp',
                component: () => import('../views/plugin/ftp/Index.vue'),
                meta: { name: '文件服务' }
            },
            {
                path: '/plugin-upnp.html',
                name: 'PluginUPNP',
                component: () => import('../views/plugin/UPNP.vue'),
                meta: { name: 'UPNP映射' }
            },
            {
                path: '/plugin-album.html',
                name: 'PluginAlbum',
                component: () => import('../views/plugin/album/Index.vue'),
                meta: { name: '图片相册' }
            },
            {
                path: '/plugin-tcp-forward.html',
                name: 'PluginTcpForward',
                component: () => import('../views/plugin/TcpForward.vue'),
                meta: { name: 'TCP转发' }
            },
            {
                path: '/plugin-wakeup.html',
                name: 'PluginWakeUp',
                component: () => import('../views/plugin/WakeUp.vue'),
                meta: { name: '幻数据包' }
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
                path: '/about-setting.html',
                name: 'AboutSetting',
                component: () => import('../views/about/Setting.vue')
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
                path: '/about-env.html',
                name: 'AboutEnv',
                component: () => import('../views/about/Env.vue')
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
            }
        ]
    }
]

const router = createRouter({
    history: createWebHashHistory(),
    routes
})

export default router
