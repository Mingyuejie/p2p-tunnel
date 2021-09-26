<!--
 * @Author: xr
 * @Date: 2021-03-23 19:30:59
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-26 09:44:11
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\components\auth\AuthWrap.vue
-->
<template>
    <slot></slot>
</template>
<script>
import { ref } from '@vue/reactivity';
import { loadPlugins } from '../../apis/plugins/setting'
import { provide } from '@vue/runtime-core';
export default {
    setup () {
        const plugins = ref([]);
        loadPlugins().then((res) => {
            plugins.value = res;
        });
        provide('btn-auth-plugins', plugins);
    }
}
</script>