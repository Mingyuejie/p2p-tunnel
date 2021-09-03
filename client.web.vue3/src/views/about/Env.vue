<!--
 * @Author: snltty
 * @Date: 2021-09-03 14:44:40
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-03 14:44:41
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\about\Env.vue
-->
<template>
    <div>
        <v-md-preview :text="md"></v-md-preview>
    </div>
</template>

<script>
import md from './env.md'
export default {
    setup () {
        return { md }
    }
}
</script>

<style lang="stylus" scoped></style>