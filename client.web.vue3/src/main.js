/*
 * @Author: snltty
 * @Date: 2021-08-20 09:12:44
 * @LastEditors: snltty
 * @LastEditTime: 2021-10-23 23:55:55
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\main.js
 */
import { createApp } from 'vue'
import App from './App.vue'
import router from './router'

const app = createApp(App);

import './assets/style.css'
import './extends/index'
import auth from './components/auth'
app.use(auth);

import ElementPlus from 'element-plus';
import 'element-plus/lib/theme-chalk/index.css';

import VMdPreview from '@kangc/v-md-editor/lib/preview';
import '@kangc/v-md-editor/lib/style/preview.css';
import vuepressTheme from '@kangc/v-md-editor/lib/theme/vuepress.js';
import '@kangc/v-md-editor/lib/theme/style/vuepress.css';
VMdPreview.use(vuepressTheme);


import { Loading, FolderDelete } from '@element-plus/icons'
app.component(Loading.name, Loading);
app.component(FolderDelete.name, FolderDelete);

app.use(VMdPreview).use(ElementPlus).use(router).mount('#app');
