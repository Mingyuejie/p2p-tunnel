<!--
 * @Author: snltty
 * @Date: 2021-08-19 22:30:19
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-20 18:24:07
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\Register.vue
-->
<template>
    <div class="register-form">
        <el-form label-width="8.5rem" ref="formDom" :model="model" :rules="rules">
            <el-form-item label="" label-width="0">
                <el-row>
                    <el-col :span="12">
                        <el-form-item label="客户名称" prop="ClientName">
                            <el-input v-model="model.ClientName" maxlength="32" show-word-limit></el-input>
                        </el-form-item>
                    </el-col>
                    <el-col :span="12">
                        <el-form-item label="分组编号" prop="GroupId">
                            <el-input v-model="model.GroupId" maxlength="32" show-word-limit></el-input>
                        </el-form-item>
                    </el-col>
                </el-row>
            </el-form-item>
            <el-form-item label="服务信息">
                <el-row>
                    <el-col :span="8">
                        <el-form-item label="地址" :label-width="55" prop="ServerIp">
                            <el-input v-model="model.ServerIp" placeholder="域名或IP地址"></el-input>
                        </el-form-item>
                    </el-col>
                    <el-col :span="8">
                        <el-form-item label="UDP端口" prop="ServerPort">
                            <el-input v-model="model.ServerPort"></el-input>
                        </el-form-item>
                    </el-col>
                    <el-col :span="8">
                        <el-form-item label="TCP端口" prop="ServerTcpPort">
                            <el-input v-model="model.ServerTcpPort"></el-input>
                        </el-form-item>
                    </el-col>
                </el-row>
            </el-form-item>
            <el-form-item label="客户信息">
                <el-row>
                    <el-col :span="8">
                        <el-form-item label="IP" :label-width="55" prop="Ip">
                            <el-input readonly v-model="registerState.RemoteInfo.Ip"></el-input>
                        </el-form-item>
                    </el-col>
                    <el-col :span="8">
                        <el-form-item label="UDP端口" prop="Port">
                            <el-input readonly v-model="registerState.LocalInfo.Port"></el-input>
                        </el-form-item>
                    </el-col>
                    <el-col :span="8">
                        <el-form-item label="TCP端口" prop="TcpPort">
                            <el-input readonly v-model="registerState.LocalInfo.TcpPort"></el-input>
                        </el-form-item>
                    </el-col>
                </el-row>
            </el-form-item>
            <el-form-item label="注册信息">
                <el-row>
                    <el-col :span="8">
                        <el-form-item label="ID" :label-width="55" prop="ConnectId">
                            <el-input readonly v-model="registerState.RemoteInfo.ConnectId"></el-input>
                        </el-form-item>
                    </el-col>
                    <el-col :span="8">
                        <el-form-item label="外网距离" prop="RouteLevel">
                            <el-input readonly v-model="registerState.LocalInfo.RouteLevel"></el-input>
                        </el-form-item>
                    </el-col>
                    <el-col :span="8">
                        <el-form-item label="TCP端口" prop="TcpPort">
                            <el-input readonly v-model="registerState.RemoteInfo.TcpPort"></el-input>
                        </el-form-item>
                    </el-col>
                </el-row>
            </el-form-item>
            <el-form-item label="Mac地址">
                <el-row>
                    <el-col :span="8">
                        <el-form-item label="地址" :label-width="55" prop="Mac">
                            <el-input readonly v-model="registerState.LocalInfo.Mac"></el-input>
                        </el-form-item>
                    </el-col>
                    <el-col :span="8">
                        <el-form-item label="上报mac" prop="UseMac">
                            <el-switch v-model="model.UseMac" />
                        </el-form-item>
                    </el-col>
                </el-row>
            </el-form-item>
            <el-form-item label="注册状态">
                <el-row>
                    <el-col :span="8">
                        <el-form-item label="UDP" prop="Connected">
                            <el-switch disabled v-model="registerState.LocalInfo.Connected" />
                        </el-form-item>
                    </el-col>
                    <el-col :span="8">
                        <el-form-item label="TCP" prop="TcpConnected">
                            <el-switch disabled v-model="registerState.LocalInfo.TcpConnected" />
                        </el-form-item>
                    </el-col>
                    <el-col :span="8">
                        <el-form-item label="自动注册" prop="AutoReg">
                            <el-switch v-model="model.AutoReg" />
                        </el-form-item>
                    </el-col>
                </el-row>
            </el-form-item>
            <el-form-item label="" label-width="0" class="t-c">
                <el-button type="primary" :loading="registerState.LocalInfo.IsConnecting" @click="handleSubmit">注册</el-button>
            </el-form-item>
            <el-form-item label="" label-width="0">
                <el-alert title="分组编号" type="info" show-icon :closable="false">
                    <p style="line-height:2rem">1、当【分组编号】不为空时，注册才会保存【分组编号】，以便于下次使用相同【分组编号】</p>
                    <p style="line-height:2rem">2、相同【分组编号】直接的客户端可见，请尽量设置一个重复可能性最小的值</p>
                </el-alert>
                <el-alert title="客户名称" type="info" show-icon :closable="false">
                    <p style="line-height:2rem">1、TCP转发时，将【客户名称】作为目标客户端的标识符，请尽量各客户端之间不重名</p>
                </el-alert>
            </el-form-item>
        </el-form>
    </div>
</template>

<script>
import { ref, toRefs, reactive } from '@vue/reactivity';
import { injectRegister } from '../states/register'
import { sendRegisterMsg, getRegisterInfo } from '../apis/register'
import { sendConfigMsg } from '../apis/config'

import { ElMessage } from 'element-plus'
import { watch } from '@vue/runtime-core';
export default {
    setup () {
        const formDom = ref(null);
        const registerState = injectRegister();
        const state = reactive({
            model: {
                ClientName: '',
                ServerIp: '',
                ServerPort: 0,
                ServerTcpPort: 0,
                AutoReg: false,
                UseMac: false,
                GroupId: ''
            },
            rules: {
                ClientName: [{ required: true, message: '必填', trigger: 'blur' }],
                ServerIp: [{ required: true, message: '必填', trigger: 'blur' }],
                ServerPort: [
                    { required: true, message: '必填', trigger: 'blur' },
                    {
                        type: 'number', min: 1, max: 65535, message: '数字 1-65535', trigger: 'blur', transform (value) {
                            return Number(value)
                        }
                    }
                ],
                ServerTcpPort: [
                    { required: true, message: '必填', trigger: 'blur' },
                    {
                        type: 'number', min: 1, max: 65535, message: '数字 1-65535', trigger: 'blur', transform (value) {
                            return Number(value)
                        }
                    }
                ]
            }
        });

        //获取一下可修改的数据
        getRegisterInfo().then((json) => {
            state.model.ClientName = registerState.ClientConfig.Name = json.ClientConfig.Name;
            state.model.GroupId = registerState.ClientConfig.GroupId = json.ClientConfig.GroupId;
            state.model.AutoReg = registerState.ClientConfig.AutoReg = json.ClientConfig.AutoReg;
            state.model.UseMac = registerState.ClientConfig.UseMac = json.ClientConfig.UseMac;

            state.model.ServerIp = registerState.ServerConfig.Ip = json.ServerConfig.Ip;
            state.model.ServerPort = registerState.ServerConfig.Port = json.ServerConfig.Port;
            state.model.ServerTcpPort = registerState.ServerConfig.TcpPort = json.ServerConfig.TcpPort;
        }).catch((msg) => {
            // ElMessage.error(msg);
        });
        watch(() => registerState.ClientConfig.GroupId, () => {
            state.model.GroupId = registerState.ClientConfig.GroupId;
        });

        const handleSubmit = () => {
            formDom.value.validate((valid) => {
                if (!valid) {
                    return false;
                }
                let data = {
                    ClientConfig: {
                        Name: state.model.ClientName,
                        GroupId: state.model.GroupId,
                        AutoReg: state.model.AutoReg,
                        UseMac: state.model.UseMac,
                    },
                    ServerConfig: {
                        Ip: state.model.ServerIp,
                        Port: +state.model.ServerPort,
                        TcpPort: +state.model.ServerTcpPort,
                    }
                };
                registerState.LocalInfo.IsConnecting = true;
                sendConfigMsg(data).then(() => {
                    sendRegisterMsg().then((res) => {
                    }).catch((msg) => {
                        ElMessage.error(msg);
                    });
                }).catch((msg) => {
                    ElMessage.error(msg);
                })
            });
        }

        return {
            ...toRefs(state), registerState, formDom, handleSubmit
        }
    }
}
</script>

<style lang="stylus" scoped>
.register-form
    padding: 5rem;
</style>