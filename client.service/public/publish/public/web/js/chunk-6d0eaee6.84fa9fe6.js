(window["webpackJsonp"]=window["webpackJsonp"]||[]).push([["chunk-6d0eaee6"],{"3e67":function(e,t,n){},bb51:function(e,t,n){"use strict";n.r(t);n("a15b"),n("fb6a"),n("ac1f"),n("1276");var o=n("7a23");Object(o["pushScopeId"])("data-v-e5910e20");var c={class:"home"},r=["onClick"],l={style:{"margin-right":".6rem"}},a={class:"t-c"},i=Object(o["createTextVNode"])("连它"),d=Object(o["createTextVNode"])("连我"),u=Object(o["createTextVNode"])("重启它"),b={class:"remark"},s=Object(o["createElementVNode"])("p",{style:{"line-height":"2rem"}},[Object(o["createTextVNode"])("1、注册信息里 ["),Object(o["createElementVNode"])("strong",null,"客户信息"),Object(o["createTextVNode"])("]的"),Object(o["createElementVNode"])("strong",null,"【TCP端口】"),Object(o["createTextVNode"])("与 ["),Object(o["createElementVNode"])("strong",null,"注册信息"),Object(o["createTextVNode"])("]的"),Object(o["createElementVNode"])("strong",null,"【TCP端口】"),Object(o["createTextVNode"])("一致时，被连接成功概率高")],-1),j=Object(o["createElementVNode"])("p",{style:{"line-height":"2rem"}},"2、所以会有 【连它】和 【连我】 之分，尽量让两个TCP端口一致的一方作为被连接的一方",-1),O=Object(o["createTextVNode"])("取 消"),f=Object(o["createTextVNode"])("确 定");function m(e,t,n,m,p,C){var V=Object(o["resolveComponent"])("el-tag"),w=Object(o["resolveComponent"])("el-table-column"),h=Object(o["resolveComponent"])("el-switch"),N=Object(o["resolveComponent"])("el-button"),g=Object(o["resolveComponent"])("el-table"),x=Object(o["resolveComponent"])("el-alert"),k=Object(o["resolveComponent"])("el-input"),T=Object(o["resolveComponent"])("el-form-item"),v=Object(o["resolveComponent"])("el-form"),_=Object(o["resolveComponent"])("el-dialog");return Object(o["openBlock"])(),Object(o["createElementBlock"])(o["Fragment"],null,[Object(o["createElementVNode"])("div",c,[Object(o["createVNode"])(g,{data:e.clients,border:"",size:"mini"},{default:Object(o["withCtx"])((function(){return[Object(o["createVNode"])(w,{prop:"Name",label:"客户端"},{default:Object(o["withCtx"])((function(e){return[Object(o["createElementVNode"])("div",{onClick:function(t){return m.handleClientClick(e.row)}},[Object(o["createElementVNode"])("span",l,Object(o["toDisplayString"])(e.row.Name),1),m.localIp==e.row.Ip.split(".").slice(0,3).join(".")?(Object(o["openBlock"])(),Object(o["createBlock"])(V,{key:0,size:"mini",effect:"plain"},{default:Object(o["withCtx"])((function(){return[Object(o["createTextVNode"])("局域网("+Object(o["toDisplayString"])(e.row.Ip)+")",1)]})),_:2},1024)):(Object(o["openBlock"])(),Object(o["createBlock"])(V,{key:1,size:"mini",effect:"plain",type:"success"},{default:Object(o["withCtx"])((function(){return[Object(o["createTextVNode"])("广域网("+Object(o["toDisplayString"])(e.row.Ip)+")",1)]})),_:2},1024))],8,r)]})),_:1}),Object(o["createVNode"])(w,{prop:"Mac",label:"Mac"}),Object(o["createVNode"])(w,{prop:"UDP",label:"UDP",width:"80"},{default:Object(o["withCtx"])((function(e){return[Object(o["createVNode"])(h,{disabled:"",onClick:t[0]||(t[0]=Object(o["withModifiers"])((function(){}),["stop"])),modelValue:e.row.Connected,"onUpdate:modelValue":function(t){return e.row.Connected=t}},null,8,["modelValue","onUpdate:modelValue"])]})),_:1}),Object(o["createVNode"])(w,{prop:"TCP",label:"TCP",width:"80"},{default:Object(o["withCtx"])((function(e){return[Object(o["createVNode"])(h,{disabled:"",onClick:t[1]||(t[1]=Object(o["withModifiers"])((function(){}),["stop"])),modelValue:e.row.TcpConnected,"onUpdate:modelValue":function(t){return e.row.TcpConnected=t}},null,8,["modelValue","onUpdate:modelValue"])]})),_:1}),Object(o["createVNode"])(w,{prop:"todo",label:"操作",width:"280",fixed:"right",class:"t-c"},{default:Object(o["withCtx"])((function(e){return[Object(o["createElementVNode"])("div",a,[Object(o["createVNode"])(N,{disabled:e.row.Connected&&e.row.TcpConnected,loading:e.row.Connecting||e.row.TcpConnecting,size:"mini",onClick:function(t){return m.handleConnect(e.row)}},{default:Object(o["withCtx"])((function(){return[i]})),_:2},1032,["disabled","loading","onClick"]),Object(o["createVNode"])(N,{disabled:e.row.Connected&&e.row.TcpConnected,loading:e.row.Connecting||e.row.TcpConnecting,size:"mini",onClick:function(t){return m.handleConnectReverse(e.row)}},{default:Object(o["withCtx"])((function(){return[d]})),_:2},1032,["disabled","loading","onClick"]),Object(o["createVNode"])(N,{loading:e.row.Connecting||e.row.TcpConnecting,size:"mini",onClick:function(t){return m.handleReset(e.row)}},{default:Object(o["withCtx"])((function(){return[u]})),_:2},1032,["loading","onClick"])])]})),_:1})]})),_:1},8,["data"]),Object(o["createElementVNode"])("div",b,[Object(o["createVNode"])(x,{title:"说明",type:"info","show-icon":"",closable:!1},{default:Object(o["withCtx"])((function(){return[s,j]})),_:1})])]),Object(o["createVNode"])(_,{title:"试一下发送数据效率",modelValue:e.showTest,"onUpdate:modelValue":t[5]||(t[5]=function(t){return e.showTest=t}),center:"","close-on-click-modal":!1,width:"50rem"},{footer:Object(o["withCtx"])((function(){return[Object(o["createVNode"])(N,{onClick:t[4]||(t[4]=function(t){return e.showTest=!1})},{default:Object(o["withCtx"])((function(){return[O]})),_:1}),Object(o["createVNode"])(N,{type:"primary",loading:e.loading,onClick:m.handleSubmit},{default:Object(o["withCtx"])((function(){return[f]})),_:1},8,["loading","onClick"])]})),default:Object(o["withCtx"])((function(){return[Object(o["createVNode"])(v,{ref:"formDom",model:e.form,rules:e.rules,"label-width":"10rem"},{default:Object(o["withCtx"])((function(){return[Object(o["createVNode"])(T,{label:"包数量",prop:"Count"},{default:Object(o["withCtx"])((function(){return[Object(o["createVNode"])(k,{modelValue:e.form.Count,"onUpdate:modelValue":t[2]||(t[2]=function(t){return e.form.Count=t})},null,8,["modelValue"])]})),_:1}),Object(o["createVNode"])(T,{label:"包大小(KB)",prop:"KB"},{default:Object(o["withCtx"])((function(){return[Object(o["createVNode"])(k,{modelValue:e.form.KB,"onUpdate:modelValue":t[3]||(t[3]=function(t){return e.form.KB=t})},null,8,["modelValue"])]})),_:1}),Object(o["createVNode"])(T,{label:"结果",prop:""},{default:Object(o["withCtx"])((function(){return[Object(o["createTextVNode"])(Object(o["toDisplayString"])(e.result),1)]})),_:1})]})),_:1},8,["model","rules"])]})),_:1},8,["modelValue"])],64)}Object(o["popScopeId"])();var p=n("5530"),C=(n("99af"),n("a1e9")),V=n("3fd2"),w=n("9709"),h=n("97af"),N=function(e){return Object(h["b"])("clients/connect",{id:e})},g=function(e){return Object(h["b"])("clients/connectreverse",{id:e})},x=function(e){return Object(h["b"])("reset/reset",{id:e})},k=function(e,t,n){return Object(h["b"])("test/packet",{id:+e,count:+t,kb:+n})},T={name:"Home",components:{},setup:function(){var e=Object(V["a"])(),t=Object(w["a"])(),n=Object(C["c"])((function(){return t.LocalInfo.LocalIp.split(".").slice(0,3).join(".")})),o=function(e){N(e.Id)},c=function(e){g(e.Id)},r=function(e){x(e.Id)},l=Object(C["p"])({showTest:!1,loading:!1,Id:0,result:"",form:{Count:1e4,KB:1},rules:{Count:[{required:!0,message:"必填",trigger:"blur"}],KB:[{required:!0,message:"必填",trigger:"blur"}]}}),a=Object(C["r"])(null),i=function(){a.value.validate((function(e){if(!e)return!1;l.loading=!0,k(l.Id,l.form.Count,l.form.KB).then((function(e){l.loading=!1,l.result="".concat(e.Ms," ms、").concat(e.Us," us、").concat(e.Ticks," ticks")})).catch((function(e){l.loading=!1}))}))},d=function(e){l.Id=e.Id,l.showTest=!0};return Object(p["a"])(Object(p["a"])(Object(p["a"])({},Object(C["z"])(l)),{},{handleSubmit:i,formDom:a,handleClientClick:d},Object(C["z"])(e)),{},{handleConnect:o,handleReset:r,handleConnectReverse:c,localIp:n})}},v=(n("cf4d"),n("6b0d")),_=n.n(v);const I=_()(T,[["render",m],["__scopeId","data-v-e5910e20"]]);t["default"]=I},cf4d:function(e,t,n){"use strict";n("3e67")}}]);