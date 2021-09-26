<!--
 * @Author: xr
 * @Date: 2021-03-23 20:41:22
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-26 09:45:08
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\components\auth\AuthItem.vue
-->
<template>
    <template v-if="plugins.indexOf(name) >= 0">
        <slot></slot>
    </template>
</template>

<script>
import { inject } from 'vue'
export default {
    props: ['name'],
    setup () {
        const plugins = inject('btn-auth-plugins');
        return {
            plugins
        }
    }
}
</script>
<style lang="stylus" scoped></style>