<!--
 * @Author: snltty
 * @Date: 2021-09-03 11:06:47
 * @LastEditors: snltty
 * @LastEditTime: 2021-10-25 17:20:47
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\about\Index.vue
-->
<template>
    <div class="absolute">
        <div class="menus absolute">
            <ul>
                <li>
                    <router-link :to="{name:'AboutHome'}">概览</router-link>
                </li>
                <li>
                    <router-link :to="{name:'AboutRuntime'}">1、运行环境</router-link>
                </li>
                <li>
                    <router-link :to="{name:'AboutServer'}">2、服务端</router-link>
                </li>
                <li>
                    <router-link :to="{name:'AboutClient'}">3、客户端</router-link>
                </li>
                <li>
                    <router-link :to="{name:'ServerTcpforward'}">服务器转发穿透</router-link>
                </li>
                <li>
                    <router-link :to="{name:'AboutDdns'}">域名解析插件</router-link>
                </li>
                <li>
                    <router-link :to="{name:'AboutPublish'}">自己发布代码</router-link>
                </li>
                <li>
                    <router-link :to="{name:'AboutWinService'}">注册windows服务</router-link>
                </li>
                <li>
                    <router-link :to="{name:'AboutUse'}">打洞大概原理流程</router-link>
                </li>
            </ul>
        </div>
        <div class="body absolute scrollbar-10">
            <router-view></router-view>
        </div>
    </div>
</template>
<script>
export default {
    setup () {
        return {}
    }
}
</script>
<style lang="stylus" scoped>
.menus
    white-space: nowrap;
    border-right: 1px solid #ddd;
    width: 15rem;
    left: 0;
    top: 0;

    ul
        padding: 1rem 0;

        li
            text-align: center;

            a
                padding: 1rem 0;
                display: block;
                color: #333;
                font-size: 1.4rem;
                transition: 0.3s;

                &:hover, &.router-link-active
                    font-weight: bold;
                    background-color: #eee;

.body
    padding: 1rem;
    box-sizing: border-box;
    left: 15rem;
</style>
<style lang="stylus">
.vuepress-markdown-body
    font-family: none;
    font-size: 1.4rem;

    li
        list-style: auto;

.vuepress-markdown-body p, .vuepress-markdown-body blockquote, .vuepress-markdown-body ul, .vuepress-markdown-body ol, .vuepress-markdown-body dl, .vuepress-markdown-body table, .vuepress-markdown-body pre
    margin-bottom: 1.6rem;

.vuepress-markdown-body tr:nth-child(2n)
    background-color: transparent;

.vuepress-markdown-body tr:nth-child(3n)
    background-color: #f6f8fa;
</style>