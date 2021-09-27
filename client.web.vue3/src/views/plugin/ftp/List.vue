<!--
 * @Author: snltty
 * @Date: 2021-09-26 19:49:58
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-27 22:33:47
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\plugin\ftp\List.vue
-->
<template>
    <div class="list flex flex-nowrap h-100">
        <div class="local flex-1">
            <Local></Local>
        </div>
        <div class="split"></div>
        <div class="remote flex-1">
            <Remote></Remote>
        </div>
    </div>
</template>

<script>
import Local from './Local.vue'
import Remote from './Remote.vue'
import { provideFilesData } from './list-share-data'
export default {
    components: { Local, Remote },
    setup () {
        provideFilesData();
        return {}
    }
}
</script>

<style lang="stylus" scoped>
.split
    width: 0.6rem;
</style>