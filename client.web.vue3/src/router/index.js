/*
 * @Author: snltty
 * @Date: 2021-08-19 21:50:16
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-11 22:51:21
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
        path: '/upnp.html',
        name: 'UPNP',
        component: () => import('../views/UPNP.vue')
    },
    {
        path: '/tcp-forward.html',
        name: 'TcpForward',
        component: () => import('../views/TcpForward.vue')
    },
    {
        path: '/fileserver.html',
        name: 'FileServer',
        component: () => import('../views/fileserver/Index.vue')
    },
    {
        path: '/wakeup.html',
        name: 'WakeUp',
        component: () => import('../views/WakeUp.vue')
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
