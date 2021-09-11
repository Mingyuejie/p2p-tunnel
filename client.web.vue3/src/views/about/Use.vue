<!--
 * @Author: snltty
 * @Date: 2021-09-11 22:50:50
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-11 22:50:50
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\about\Use.vue
-->
<template>
    <div>
        <v-md-preview :text="md"></v-md-preview>
    </div>
</template>

<script>
import md from './use.md'
export default {
    setup () {
        return { md }
    }
}
</script>

<style lang="stylus" scoped></style>