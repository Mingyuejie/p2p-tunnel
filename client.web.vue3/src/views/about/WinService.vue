<!--
 * @Author: snltty
 * @Date: 2021-09-04 00:02:26
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-04 00:02:27
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\about\Winservice.vue
-->
<template>
    <div>
        <v-md-preview :text="md"></v-md-preview>
    </div>
</template>

<script>
import md from './winservice.md'
export default {
    setup () {
        return { md }
    }
}
</script>

<style lang="stylus" scoped></style>