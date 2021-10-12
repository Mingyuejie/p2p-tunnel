<!--
 * @Author: snltty
 * @Date: 2021-10-12 16:30:26
 * @LastEditors: snltty
 * @LastEditTime: 2021-10-12 16:30:26
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\about\ServerTcpforward.vue
-->
<template>
    <div>
        <v-md-preview :text="md"></v-md-preview>
    </div>
</template>

<script>
import md from './server-tcp-fprward.md'
export default {
    setup () {
        return { md }
    }
}
</script>

<style lang="stylus" scoped></style>