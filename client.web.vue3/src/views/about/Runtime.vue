<!--
 * @Author: snltty
 * @Date: 2021-09-03 14:44:40
 * @LastEditors: snltty
 * @LastEditTime: 2021-10-16 00:50:46
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\about\Runtime.vue
-->
<template>
    <div>
        <v-md-preview :text="md"></v-md-preview>
    </div>
</template>

<script>
import md from './runtime.md'
export default {
    setup () {
        return { md }
    }
}
</script>

<style lang="stylus" scoped></style>