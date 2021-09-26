<!--
 * @Author: snltty
 * @Date: 2021-09-26 19:10:48
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-26 19:53:29
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\plugin\ftp\Index.vue
-->
<template>
    <div class="ftp-wrap h-100 flex flex-column flex-nowrap">
        <div class="flex-1">
            <List></List>
        </div>
        <div class="split"></div>
        <div>
            <Progress></Progress>
        </div>
    </div>
</template>

<script>
import Progress from './Progress.vue'
import List from './List.vue'
export default {
    components: { List, Progress },
    setup () {
        return {}
    }
}
</script>

<style lang="stylus" scoped>
.ftp-wrap
    padding: 1rem;
    box-sizing: border-box;

    .split
        height: 0.6rem;
</style>