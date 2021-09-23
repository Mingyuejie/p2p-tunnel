<!--
 * @Author: snltty
 * @Date: 2021-08-19 22:30:19
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-23 10:18:44
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\plugin\WakeUp.vue
-->
<template>
    <div class="wakeup-form">
        <el-form label-width="5.5rem" ref="formDom" :model="form" :rules="rules">
            <el-form-item label="Ip" prop="Ip">
                <el-input v-model="form.Ip" placeholder="主机IP地址"></el-input>
            </el-form-item>
            <el-form-item label="Mac" prop="Mac">
                <el-input v-model="form.Mac" placeholder="主机MAC地址"></el-input>
            </el-form-item>
            <el-form-item label="端口" prop="Port">
                <el-input v-model="form.Port" placeholder="随便一个端口"></el-input>
            </el-form-item>
            <el-form-item label="" label-width="0" class="t-c">
                <el-button type="primary" @click="handleSubmit">发送</el-button>
            </el-form-item>
            <el-form-item label="" label-width="0">
                <el-alert title="说明" type="info" show-icon :closable="false">
                    <p style="line-height:2rem">1、在电脑主板和网卡支持WOL唤醒的前提下</p>
                    <p style="line-height:2rem">2、BIOS 开启唤醒，网卡开启唤醒</p>
                    <p style="line-height:2rem">3、网卡保持充电，外接网卡网卡一般不行</p>
                </el-alert>
            </el-form-item>
        </el-form>
    </div>
</template>

<script>
import { reactive, ref, toRefs } from '@vue/reactivity';
import { sendWakeUp } from '../../apis/plugins/wakeup'
import { ElMessage } from 'element-plus'
export default {
    setup () {
        const formDom = ref(null);
        const state = reactive({
            form: {
                Ip: '',
                Mac: '',
                Port: 8099,
            },
            rules: {
                Ip: [{ required: true, message: '必填', trigger: 'blur' }],
                Mac: [{ required: true, message: '必填', trigger: 'blur' }],
                Port: [
                    { required: true, message: '必填', trigger: 'blur' },
                    {
                        type: 'number', min: 1, max: 65535, message: '数字 1-65535', trigger: 'blur', transform (value) {
                            return Number(value)
                        }
                    }
                ]
            }
        });
        const handleSubmit = () => {
            formDom.value.validate((valid) => {
                if (!valid) {
                    return false;
                }
                sendWakeUp({
                    ID: 0,
                    Ip: state.form.Ip,
                    Mac: state.form.Mac,
                    Port: +state.form.Port,
                }).then(() => {
                    ElMessage.success('已发送');
                }).catch((e) => {
                    ElMessage.error('发送失败' + e);
                });
            });
        }

        return {
            ...toRefs(state), formDom, handleSubmit
        }
    }
}
</script>

<style lang="stylus" scoped>
.wakeup-form
    padding: 5rem;
    width: 50%;
    margin: 0 auto;
</style>