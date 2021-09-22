<!--
 * @Author: snltty
 * @Date: 2021-09-06 16:05:30
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-06 16:23:01
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\fileserver\FolderTree.vue
-->
<template>
    <el-dropdown-menu>
        <template v-for="(item,index) in childs" :key="index">
            <el-dropdown-item :command="item">{{item.Name}}</el-dropdown-item>
            <template v-if="item.Child.length > 0">
                <el-dropdown-item>
                    <folder-tree :childs="item.Child"></folder-tree>
                </el-dropdown-item>
            </template>
        </template>
    </el-dropdown-menu>
</template>
<script>
export default {
    props: {
        childs: {
            type: Array,
            default: []
        }
    }
}
</script>