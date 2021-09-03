<!--
 * @Author: snltty
 * @Date: 2021-09-04 00:31:50
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-04 00:32:15
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\about\Publish.vue
-->
<template>
    <div>
        <v-md-preview :text="md"></v-md-preview>
    </div>
</template>

<script>
import md from './publish.md'
export default {
    setup () {
        return { md }
    }
}
</script>

<style lang="stylus" scoped></style>