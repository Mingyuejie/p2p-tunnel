<!--
 * @Author: snltty
 * @Date: 2021-09-30 14:47:08
 * @LastEditors: snltty
 * @LastEditTime: 2022-02-11 16:57:09
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\service\cmd\Ssh.vue
-->
<template>
    <div class="wrap absolute">
        <div id="terminal" ref="terminalDom" class="absolute"></div>
    </div>
</template>

<script>
import { reactive, ref, toRefs } from '@vue/reactivity'
import { nextTick, onMounted, onUnmounted } from '@vue/runtime-core';
import { sendCmd } from '../../../apis/service/cmd'
import { injectCmd } from '../../../states/cmd'
import { Terminal } from 'xterm';
import { WebLinksAddon } from 'xterm-addon-web-links';
import { FitAddon } from 'xterm-addon-fit';
import { SearchAddon } from 'xterm-addon-search';
require('xterm/css/xterm.css');
export default {
    setup () {
        const stateCmd = injectCmd();
        const state = reactive({
            outputs: [],
            input: '',
            loading: false
        });

        const terminalDom = ref(null);
        const terminal = new Terminal({
            rendererType: "canvas",
            convertEol: false, //启用时，光标将设置为下一行的开头
            disableStdin: false, //是否应禁用输入。
            cursorStyle: "underline", //光标样式
            cursorBlink: true, //光标闪烁
            theme: {
                cursor: "help", //设置光标
                lineHeight: 16
            }
        });
        terminal.loadAddon(new WebLinksAddon());
        const fitAddon = new FitAddon();
        terminal.loadAddon(fitAddon);
        terminal.loadAddon(new SearchAddon());

        const resizeScreen = () => {
            try {
                fitAddon.fit();
                terminal.onResize(size => {
                    _this.onSend({ Op: "resize", Cols: size.cols, Rows: size.rows });
                });
            } catch (e) {
                console.log("e", e.message);
            }
        }
        onMounted(() => {
            window.addEventListener("resize", resizeScreen);
            terminal.open(terminalDom.value);
            resizeScreen();
            terminal.onData((data) => { });
        });
        onUnmounted(() => {
            window.removeEventListener("resize", resizeScreen);
            terminal.dispose();
        });

        return {
            ...toRefs(state), terminalDom
        }
    }
}
</script>

<style lang="stylus" scoped>
.wrap
    border-radius: 0.4rem;
    padding: 1rem;
</style>