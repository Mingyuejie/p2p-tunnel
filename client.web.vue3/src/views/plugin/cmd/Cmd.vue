<!--
 * @Author: snltty
 * @Date: 2021-09-30 14:47:08
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-30 19:44:24
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\plugin\cmd\Cmd.vue
-->
<template>
    <div class="wrap absolute" ref="wrapDom" @click="handleWrapClick">
        <div class="content" ref="contentDom">
            <div class="outputs">
                <template v-for="(item,index) in outputs" :key="index">
                    <div v-html="item"></div>
                </template>
            </div>
            <div class="input flex">
                <span>></span>
                <span class="flex-1">
                    <el-icon color="#ffffff" v-show="loading">
                        <loading />
                    </el-icon>
                    <input v-show="!loading" ref="inputDom" type="text" v-model="input" @keyup="handleKeypress" />
                </span>
            </div>
        </div>
    </div>
</template>

<script>
import { reactive, ref, toRefs } from '@vue/reactivity'
import { nextTick } from '@vue/runtime-core';
import { sendCmd } from '../../../apis/plugins/cmd'
export default {
    setup () {
        const state = reactive({
            outputs: [],
            input: '',
            loading: false
        });
        const output = (str) => {
            state.outputs.push(str.replace(/\n/g, '<br/>'));
            nextTick(() => {
                wrapDom.value.scrollTop = contentDom.value.offsetHeight;
            });
        }
        const wrapDom = ref(null);
        const contentDom = ref(null);
        const inputDom = ref(null);
        const handleWrapClick = () => {
            inputDom.value.focus();
        }

        const cmdHostory = [];
        let cmdIndex = -1;
        const handleKeypress = (event) => {
            switch (event.keyCode) {
                case 13:
                    handleKeyenter();
                    break;
                case 38:
                    handleKeyup();
                    break;
                case 40:
                    handleKeydown();
                    break;
            }

        }
        const handleKeyenter = () => {
            if (!state.input) return;

            output(`>>${state.input}`);

            cmdHostory.push(state.input);
            if (cmdHostory.length > 50) {
                cmdHostory.shift();
            }
            cmdIndex = cmdHostory.length;

            if (state.input == 'cls') {
                state.outputs = [];
            } else {
                state.loading = true;
                sendCmd(0, state.input).then((res) => {
                    state.loading = false;
                    if (res.Res) {
                        output(res.Res);
                    }
                    if (res.Err) {
                        output(res.Err);
                    }
                    nextTick(() => { handleWrapClick(); })
                }).catch(() => {
                    state.loading = false;
                })
            }
            state.input = '';
        }
        const handleKeyup = () => {
            cmdIndex--;
            if (cmdIndex <= 0) {
                cmdIndex = 0;
            }
            state.input = cmdHostory[cmdIndex];
        }
        const handleKeydown = () => {
            cmdIndex++;
            if (cmdIndex >= cmdHostory.length - 1) {
                cmdIndex = cmdHostory.length - 1;
            }
            state.input = cmdHostory[cmdIndex];
        }

        return {
            ...toRefs(state), wrapDom, contentDom, inputDom,
            handleWrapClick,
            handleKeypress,
        }
    }
}
</script>

<style lang="stylus" scoped>
.wrap
    background-color: #000;
    color: #fff;
    overflow: auto;
    font-size: 1.4rem;
    border-radius: 0.4rem;
    padding: 1rem;

.input
    line-height: 2.8rem;

    .el-icon
        vertical-align: middle;

    input
        width: 100%;
        box-sizing: border-box;
        background: none;
        border: 0;
        color: #fff;
        padding: 0 0.6rem;
        outline: none;
</style>