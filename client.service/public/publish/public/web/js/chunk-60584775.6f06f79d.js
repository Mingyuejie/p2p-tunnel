(window["webpackJsonp"]=window["webpackJsonp"]||[]).push([["chunk-60584775"],{"406c":function(e,t,n){"use strict";n.r(t);var c=n("7a23");Object(c["pushScopeId"])("data-v-5a2e734d");var a={class:"plugin-setting-wrap"},o={class:"head"},l=Object(c["createTextVNode"])("刷新列表"),r=Object(c["createTextVNode"])("配置");function i(e,t,n,i,b,d){var u=Object(c["resolveComponent"])("el-button"),s=Object(c["resolveComponent"])("el-table-column"),j=Object(c["resolveComponent"])("el-switch"),O=Object(c["resolveComponent"])("SettingModal"),p=Object(c["resolveComponent"])("el-table"),f=Object(c["resolveDirective"])("loading");return Object(c["openBlock"])(),Object(c["createElementBlock"])("div",a,[Object(c["createElementVNode"])("div",o,[Object(c["createVNode"])(u,{size:"mini",onClick:i.getData},{default:Object(c["withCtx"])((function(){return[l]})),_:1},8,["onClick"])]),Object(c["withDirectives"])(Object(c["createVNode"])(p,{data:e.list,border:"",size:"mini"},{default:Object(c["withCtx"])((function(){return[Object(c["createVNode"])(s,{prop:"Name",label:"插件名"}),Object(c["createVNode"])(s,{prop:"Desc",label:"描述"}),Object(c["createVNode"])(s,{prop:"Author",label:"作者"}),Object(c["createVNode"])(s,{prop:"enable",label:"启用",width:"80",class:"t-c"},{default:Object(c["withCtx"])((function(e){return[Object(c["createVNode"])(j,{modelValue:e.row.Enable,"onUpdate:modelValue":function(t){return e.row.Enable=t},onChange:function(t){return i.handleEnableChange(e.row)}},null,8,["modelValue","onUpdate:modelValue","onChange"])]})),_:1}),Object(c["createVNode"])(s,{prop:"todo",label:"操作",width:"80",class:"t-c"},{default:Object(c["withCtx"])((function(e){return[Object(c["createVNode"])(O,{className:e.row.ClassName,onSuccess:i.getData,key:e.row.ClassName},{default:Object(c["withCtx"])((function(){return[Object(c["createVNode"])(u,{size:"mini"},{default:Object(c["withCtx"])((function(){return[r]})),_:1})]})),_:2},1032,["className","onSuccess"])]})),_:1})]})),_:1},8,["data"]),[[f,e.loading]])])}Object(c["popScopeId"])();var b=n("5530"),d=n("a1e9"),u=n("c040"),s=n("7485"),j={components:{SettingModal:s["a"]},setup:function(){var e=Object(d["r"])(null),t=Object(d["p"])({loading:!1,showAdd:!1,list:[],rules:{}}),n=function(){Object(u["a"])().then((function(e){t.list=e}))};n();var c=function(e){Object(u["e"])(e.ClassName,e.Enable).then((function(){n()})).catch((function(){e.Enable=!e.Enable}))};return Object(b["a"])(Object(b["a"])({},Object(d["z"])(t)),{},{editor:e,getData:n,handleEnableChange:c})}},O=(n("a2f1"),n("6b0d")),p=n.n(O);const f=p()(j,[["render",i],["__scopeId","data-v-5a2e734d"]]);t["default"]=f},"452b":function(e,t,n){},a2f1:function(e,t,n){"use strict";n("452b")}}]);