(window["webpackJsonp"]=window["webpackJsonp"]||[]).push([["chunk-5185a830"],{1276:function(e,t,n){"use strict";var o=n("d784"),r=n("44e7"),a=n("825a"),c=n("1d80"),l=n("4840"),i=n("8aa5"),u=n("50c4"),d=n("577e"),m=n("14c3"),f=n("9263"),b=n("9f7f"),s=n("d039"),p=b.UNSUPPORTED_Y,j=[].push,O=Math.min,h=4294967295,g=!s((function(){var e=/(?:)/,t=e.exec;e.exec=function(){return t.apply(this,arguments)};var n="ab".split(e);return 2!==n.length||"a"!==n[0]||"b"!==n[1]}));o("split",(function(e,t,n){var o;return o="c"=="abbc".split(/(b)*/)[1]||4!="test".split(/(?:)/,-1).length||2!="ab".split(/(?:ab)*/).length||4!=".".split(/(.?)(.?)/).length||".".split(/()()/).length>1||"".split(/.?/).length?function(e,n){var o=d(c(this)),a=void 0===n?h:n>>>0;if(0===a)return[];if(void 0===e)return[o];if(!r(e))return t.call(o,e,a);var l,i,u,m=[],b=(e.ignoreCase?"i":"")+(e.multiline?"m":"")+(e.unicode?"u":"")+(e.sticky?"y":""),s=0,p=new RegExp(e.source,b+"g");while(l=f.call(p,o)){if(i=p.lastIndex,i>s&&(m.push(o.slice(s,l.index)),l.length>1&&l.index<o.length&&j.apply(m,l.slice(1)),u=l[0].length,s=i,m.length>=a))break;p.lastIndex===l.index&&p.lastIndex++}return s===o.length?!u&&p.test("")||m.push(""):m.push(o.slice(s)),m.length>a?m.slice(0,a):m}:"0".split(void 0,0).length?function(e,n){return void 0===e&&0===n?[]:t.call(this,e,n)}:t,[function(t,n){var r=c(this),a=void 0==t?void 0:t[e];return void 0!==a?a.call(t,r,n):o.call(d(r),t,n)},function(e,r){var c=a(this),f=d(e),b=n(o,c,f,r,o!==t);if(b.done)return b.value;var s=l(c,RegExp),j=c.unicode,g=(c.ignoreCase?"i":"")+(c.multiline?"m":"")+(c.unicode?"u":"")+(p?"g":"y"),v=new s(p?"^(?:"+c.source+")":c,g),N=void 0===r?h:r>>>0;if(0===N)return[];if(0===f.length)return null===m(v,f)?[f]:[];var V=0,C=0,w=[];while(C<f.length){v.lastIndex=p?0:C;var x,D=m(v,p?f.slice(C):f);if(null===D||(x=O(u(v.lastIndex+(p?C:0)),f.length))===V)C=i(f,C,j);else{if(w.push(f.slice(V,C)),w.length===N)return w;for(var R=1;R<=D.length-1;R++)if(w.push(D[R]),w.length===N)return w;C=V=x}}return w.push(f.slice(V)),w}]}),!g,p)},"47a3":function(e,t,n){"use strict";n.r(t);var o=n("7a23");Object(o["pushScopeId"])("data-v-f9916b1a");var r={class:"h-100 ddns-wrap flex flex-column flex-nowrap"},a={class:"head flex"},c=Object(o["createElementVNode"])("span",{class:"split"},null,-1),l=Object(o["createElementVNode"])("span",{class:"flex-1"},null,-1),i=Object(o["createTextVNode"])("配置插件"),u={class:"flex-1"};function d(e,t,n,d,m,f){var b=Object(o["resolveComponent"])("Domains"),s=Object(o["resolveComponent"])("el-button"),p=Object(o["resolveComponent"])("SettingModal"),j=Object(o["resolveComponent"])("Record");return Object(o["openBlock"])(),Object(o["createElementBlock"])("div",r,[Object(o["createElementVNode"])("div",a,[Object(o["createVNode"])(b),c,l,Object(o["createVNode"])(p,{className:"DdnsSettingPlugin"},{default:Object(o["withCtx"])((function(){return[Object(o["createVNode"])(s,{size:"mini"},{default:Object(o["withCtx"])((function(){return[i]})),_:1})]})),_:1})]),Object(o["createElementVNode"])("div",u,[Object(o["createVNode"])(j)])])}Object(o["popScopeId"])();var m=n("a1e9"),f=n("7485"),b=Object(o["createElementVNode"])("span",{class:"split-pad"},null,-1),s={class:"flex"},p=Object(o["createElementVNode"])("span",{class:"flex-1"},null,-1),j=Object(o["createElementVNode"])("span",{class:"split-pad"},null,-1),O=Object(o["createTextVNode"])("增加域名");function h(e,t,n,r,a,c){var l=Object(o["resolveComponent"])("el-option"),i=Object(o["resolveComponent"])("el-option-group"),u=Object(o["resolveComponent"])("el-select"),d=Object(o["resolveComponent"])("folder-delete"),m=Object(o["resolveComponent"])("el-icon"),f=Object(o["resolveComponent"])("el-popconfirm"),h=Object(o["resolveComponent"])("el-button");return Object(o["openBlock"])(),Object(o["createElementBlock"])("div",null,[Object(o["createVNode"])(u,{modelValue:r.shareData.group,"onUpdate:modelValue":t[0]||(t[0]=function(e){return r.shareData.group=e}),"value-key":"key",placeholder:"选择平台",size:"mini",style:{width:"20rem"}},{default:Object(o["withCtx"])((function(){return[(Object(o["openBlock"])(!0),Object(o["createElementBlock"])(o["Fragment"],null,Object(o["renderList"])(r.shareData.platforms,(function(e,t){return Object(o["openBlock"])(),Object(o["createElementBlock"])(o["Fragment"],{key:t},[(Object(o["openBlock"])(!0),Object(o["createElementBlock"])(o["Fragment"],null,Object(o["renderList"])(e.Groups,(function(t){return Object(o["openBlock"])(),Object(o["createBlock"])(i,{key:t.key,label:e.Name},{default:Object(o["withCtx"])((function(){return[Object(o["createVNode"])(l,{value:t,label:t.key},null,8,["value","label"])]})),_:2},1032,["label"])})),128))],64)})),128))]})),_:1},8,["modelValue"]),b,Object(o["createVNode"])(u,{modelValue:r.shareData.domain,"onUpdate:modelValue":t[2]||(t[2]=function(e){return r.shareData.domain=e}),"value-key":"DomainName",placeholder:"选择域名",size:"mini",style:{width:"15rem"}},{default:Object(o["withCtx"])((function(){return[(Object(o["openBlock"])(!0),Object(o["createElementBlock"])(o["Fragment"],null,Object(o["renderList"])(r.shareData.domains.Domains,(function(e,n){return Object(o["openBlock"])(),Object(o["createBlock"])(l,{key:n,value:e,label:e.DomainName},{default:Object(o["withCtx"])((function(){return[Object(o["createElementVNode"])("div",s,[Object(o["createElementVNode"])("span",null,Object(o["toDisplayString"])(e.DomainName),1),p,Object(o["createVNode"])(f,{title:"删除不可逆，是否确认,且只可删除添加的域名,无法删除服务商域名",onConfirm:function(t){return r.handleDeleteDomain(e)}},{reference:Object(o["withCtx"])((function(){return[Object(o["createElementVNode"])("a",{href:"javascript:;",onClick:t[1]||(t[1]=Object(o["withModifiers"])((function(){}),["stop"]))},[Object(o["createVNode"])(m,{size:20,color:"#ccc",class:"middle"},{default:Object(o["withCtx"])((function(){return[Object(o["createVNode"])(d)]})),_:1})])]})),_:2},1032,["onConfirm"])])]})),_:2},1032,["value","label"])})),128))]})),_:1},8,["modelValue"]),j,Object(o["createVNode"])(h,{size:"mini",onClick:r.handleAddDomain},{default:Object(o["withCtx"])((function(){return[O]})),_:1},8,["onClick"])])}n("159b"),n("99af"),n("4de4"),n("d81d"),n("ac1f"),n("1276");var g=n("97af"),v=function(){var e=arguments.length>0&&void 0!==arguments[0]?arguments[0]:{Platform:"",Group:""};return Object(g["b"])("ddns/Platforms",e)},N=function(){var e=arguments.length>0&&void 0!==arguments[0]?arguments[0]:{Platform:"",Group:"",PageNumber:1,PageSize:500};return Object(g["b"])("ddns/Domains",e)},V=function(){var e=arguments.length>0&&void 0!==arguments[0]?arguments[0]:{Platform:"",Group:"",Domain:""};return Object(g["b"])("ddns/AddDomain",e)},C=function(){var e=arguments.length>0&&void 0!==arguments[0]?arguments[0]:{Platform:"",Group:"",Domain:""};return Object(g["b"])("ddns/DeleteDomain",e)},w=function(){var e=arguments.length>0&&void 0!==arguments[0]?arguments[0]:{Platform:"",Group:"",Domain:"",PageNumber:1,PageSize:500};return Object(g["b"])("ddns/GetRecords",e)},x=function(){var e=arguments.length>0&&void 0!==arguments[0]?arguments[0]:{Platform:"",Group:"",Domain:"",RecordId:"",Status:""};return Object(g["b"])("ddns/SetRecordStatus",e)},D=function(){var e=arguments.length>0&&void 0!==arguments[0]?arguments[0]:{Platform:"",Group:"",Domain:"",RecordId:""};return Object(g["b"])("ddns/DelRecord",e)},R=function(){var e=arguments.length>0&&void 0!==arguments[0]?arguments[0]:{Platform:"",Group:"",Domain:"",RecordId:"",Remark:""};return Object(g["b"])("ddns/RemarkRecord",e)},k=function(){return Object(g["b"])("ddns/GetRecordTypes")},P=function(){var e=arguments.length>0&&void 0!==arguments[0]?arguments[0]:{Platform:"",Group:"",Domain:""};return Object(g["b"])("ddns/GetRecordLines",e)},T=function(){var e=arguments.length>0&&void 0!==arguments[0]?arguments[0]:{Platform:"",Group:"",DomainName:"",RecordId:"",RR:"",Type:"",Value:"",TTL:600,Priority:10,Line:""};return e.TTL=+e.TTL,e.Priority=+e.Priority,0==e.Priority&&(e.Priority=1),Object(g["b"])("ddns/AddRecord",e)},y=function(){var e=arguments.length>0&&void 0!==arguments[0]?arguments[0]:{Platform:"",Group:"",Domain:"",Record:"",AutoUpdate:!1};return console.log(e),Object(g["b"])("ddns/SwitchRecord",e)},L=(n("a4d3"),n("e01a"),n("d3b7"),Symbol()),_=function(e){Object(o["provide"])(L,e)},E=function(){return Object(o["inject"])(L)},B=n("7864"),S=n("5c40"),I={setup:function(){var e=E(),t=function(){v().then((function(t){if(t.forEach((function(e){e.Groups.forEach((function(t){t.platform=e.Name,t.key="".concat(e.Name,"_").concat(t.Name),t.loading=!1}))})),e.platforms=t,t.length>0){var n=t.filter((function(t){return t.Name==e.group.platform}))[0];if(n||(n=t[0]),n.Groups.length>0){var o=n.Groups.filter((function(t){return t.Name==e.group.Name}))[0];o||(o=n.Groups[0]),e.group=o}}}))};Object(S["nc"])((function(){return e.group}),(function(){e.group.recordJson=e.group.Records.map((function(e){return e.split("|")})).reduce((function(e,t){return e[t[1]]||(e[t[1]]=[]),e[t[1]].push(t[0]),e}),{}),n()})),Object(S["nc"])((function(){return e.domain}),(function(){e.domain.records=e.group.recordJson[e.domain.DomainName]||[]}));var n=function(){N({Platform:e.group.platform,Group:e.group.Name,PageSize:e.domains.PageSize,PageNumber:e.domains.PageNumber}).then((function(t){if(e.domains=t,t.Domains.length>0){var n=t.Domains.filter((function(t){return t.DomainName==e.domain.DomainName}))[0];e.domain=n||t.Domains[0]}else e.domain={}}))};Object(S["rb"])((function(){t()}));var o=function(n){C({Platform:e.group.platform,Group:e.group.Name,Domain:n.DomainName}).then((function(){t()})).catch((function(){n.loading=!1}))},r=function(){B["c"].prompt("添加域名","添加域名",{confirmButtonText:"确定",cancelButtonText:"取消"}).then((function(t){var o=t.value;o&&V({Platform:e.group.platform,Group:e.group.Name,Domain:o}).then((function(){n()})).catch((function(e){}))}))};return{shareData:e,handleAddDomain:r,handleDeleteDomain:o}}},G=n("6b0d"),A=n.n(G);const U=A()(I,[["render",h]]);var z=U;Object(o["pushScopeId"])("data-v-591be38a");var F={class:"h-100 flex flex-column flex-nowrap"},J={class:"head"},q=Object(o["createTextVNode"])("新增解析"),M=Object(o["createTextVNode"])("刷新列表"),Y={class:"body flex-1"},H={class:"flex"},K=Object(o["createElementVNode"])("span",{class:"flex-1"},null,-1),Q={class:"op"},W=["onClick"],X=["onClick"],Z=Object(o["createElementVNode"])("a",{href:"javascript:;"},"删除",-1),$=["onClick"],ee={key:0,class:"pages t-c"},te=Object(o["createTextVNode"])("取 消"),ne=Object(o["createTextVNode"])("确 定");function oe(e,t,n,r,a,c){var l=Object(o["resolveComponent"])("el-button"),i=Object(o["resolveComponent"])("el-switch"),u=Object(o["resolveComponent"])("el-popover"),d=Object(o["resolveComponent"])("el-table-column"),m=Object(o["resolveComponent"])("el-popconfirm"),f=Object(o["resolveComponent"])("el-table"),b=Object(o["resolveComponent"])("el-pagination"),s=Object(o["resolveComponent"])("el-option"),p=Object(o["resolveComponent"])("el-select"),j=Object(o["resolveComponent"])("el-form-item"),O=Object(o["resolveComponent"])("el-col"),h=Object(o["resolveComponent"])("el-row"),g=Object(o["resolveComponent"])("el-input"),v=Object(o["resolveComponent"])("el-form"),N=Object(o["resolveComponent"])("el-dialog"),V=Object(o["resolveDirective"])("loading");return Object(o["openBlock"])(),Object(o["createElementBlock"])(o["Fragment"],null,[Object(o["createElementVNode"])("div",F,[Object(o["createElementVNode"])("div",J,[Object(o["createVNode"])(l,{type:"info",size:"mini",onClick:r.handleAdd},{default:Object(o["withCtx"])((function(){return[q]})),_:1},8,["onClick"]),Object(o["createVNode"])(l,{size:"mini",onClick:r.loadData},{default:Object(o["withCtx"])((function(){return[M]})),_:1},8,["onClick"])]),Object(o["createElementVNode"])("div",Y,[Object(o["withDirectives"])(Object(o["createVNode"])(f,{data:e.records.DomainRecords,border:"",size:"mini",height:"100%"},{default:Object(o["withCtx"])((function(){return[Object(o["createVNode"])(d,{prop:"RR",label:"主机记录"},{default:Object(o["withCtx"])((function(e){return[Object(o["createElementVNode"])("div",H,[Object(o["createElementVNode"])("span",null,Object(o["toDisplayString"])(e.row.RR),1),K,Object(o["createVNode"])(u,{placement:"bottom-end",title:"啥意思",trigger:"hover",content:"当IP变化时，是否更新此条解析记录"},{reference:Object(o["withCtx"])((function(){return[Object(o["createVNode"])(i,{modelValue:e.row.autoUpdate,"onUpdate:modelValue":function(t){return e.row.autoUpdate=t},onChange:function(t){return r.handleRecordAutoUpdateChange(e.row)}},null,8,["modelValue","onUpdate:modelValue","onChange"])]})),_:2},1024)])]})),_:1}),Object(o["createVNode"])(d,{prop:"Type",label:"记录类型",width:"80"}),Object(o["createVNode"])(d,{prop:"Line",label:"解析线路",width:"80"},{default:Object(o["withCtx"])((function(t){return[Object(o["createElementVNode"])("span",null,Object(o["toDisplayString"])(e.recordLinesJson[t.row.Line]),1)]})),_:1}),Object(o["createVNode"])(d,{prop:"Value",label:"记录值"}),Object(o["createVNode"])(d,{prop:"Status",label:"状态",width:"50"},{default:Object(o["withCtx"])((function(t){return[Object(o["createElementVNode"])("span",null,Object(o["toDisplayString"])(e.status[t.row.Status]),1)]})),_:1}),Object(o["createVNode"])(d,{prop:"TTL",label:"TTL",width:"80"}),Object(o["createVNode"])(d,{prop:"Remark",label:"备注"}),Object(o["createVNode"])(d,{prop:"todo",label:"操作",width:"165",fixed:"right",class:"t-c"},{default:Object(o["withCtx"])((function(t){return[Object(o["createElementVNode"])("div",Q,[Object(o["createElementVNode"])("a",{href:"javascript:;",onClick:function(e){return r.handleEdit(t.row)}},"编辑",8,W),Object(o["createElementVNode"])("a",{href:"javascript:;",onClick:function(e){return r.handleSwitchStatus(t.row)}},Object(o["toDisplayString"])(e.statusBtn[t.row.Status]),9,X),Object(o["createVNode"])(m,{title:"删除不可逆，是否确认",onConfirm:function(e){return r.handleDel(t.row)}},{reference:Object(o["withCtx"])((function(){return[Z]})),_:2},1032,["onConfirm"]),Object(o["createElementVNode"])("a",{href:"javascript:;",onClick:function(e){return r.handleRemark(t.row)}},"备注",8,$)])]})),_:1})]})),_:1},8,["data"]),[[V,e.loading]])]),e.records.TotalCount>0?(Object(o["openBlock"])(),Object(o["createElementBlock"])("div",ee,[Object(o["createVNode"])(b,{total:e.records.TotalCount,currentPage:e.records.PageNumber,"onUpdate:currentPage":t[0]||(t[0]=function(t){return e.records.PageNumber=t}),"page-size":e.records.PageSize,onCurrentChange:r.loadData,background:"",layout:"total,prev, pager, next"},null,8,["total","currentPage","page-size","onCurrentChange"])])):Object(o["createCommentVNode"])("",!0)]),Object(o["createVNode"])(N,{title:"增加解析",modelValue:e.showAdd,"onUpdate:modelValue":t[8]||(t[8]=function(t){return e.showAdd=t}),center:"","close-on-click-modal":!1,width:"50rem"},{footer:Object(o["withCtx"])((function(){return[Object(o["createVNode"])(l,{onClick:t[7]||(t[7]=function(t){return e.showAdd=!1})},{default:Object(o["withCtx"])((function(){return[te]})),_:1}),Object(o["createVNode"])(l,{type:"primary",loading:e.loading,onClick:r.handleSubmit},{default:Object(o["withCtx"])((function(){return[ne]})),_:1},8,["loading","onClick"])]})),default:Object(o["withCtx"])((function(){return[Object(o["createVNode"])(v,{ref:"formDom",model:e.form,rules:e.rules,"label-width":"8rem"},{default:Object(o["withCtx"])((function(){return[Object(o["createVNode"])(j,{label:"","label-width":0},{default:Object(o["withCtx"])((function(){return[Object(o["createVNode"])(h,null,{default:Object(o["withCtx"])((function(){return[Object(o["createVNode"])(O,{span:12},{default:Object(o["withCtx"])((function(){return[Object(o["createVNode"])(j,{label:"记录类型",prop:"Type"},{default:Object(o["withCtx"])((function(){return[Object(o["createVNode"])(p,{modelValue:e.form.Type,"onUpdate:modelValue":t[1]||(t[1]=function(t){return e.form.Type=t})},{default:Object(o["withCtx"])((function(){return[(Object(o["openBlock"])(!0),Object(o["createElementBlock"])(o["Fragment"],null,Object(o["renderList"])(e.recordTypes,(function(e){return Object(o["openBlock"])(),Object(o["createBlock"])(s,{key:e,value:e,label:e},null,8,["value","label"])})),128))]})),_:1},8,["modelValue"])]})),_:1})]})),_:1}),Object(o["createVNode"])(O,{span:12},{default:Object(o["withCtx"])((function(){return[Object(o["createVNode"])(j,{label:"解析线路",prop:"Line"},{default:Object(o["withCtx"])((function(){return[Object(o["createVNode"])(p,{modelValue:e.form.Line,"onUpdate:modelValue":t[2]||(t[2]=function(t){return e.form.Line=t})},{default:Object(o["withCtx"])((function(){return[(Object(o["openBlock"])(!0),Object(o["createElementBlock"])(o["Fragment"],null,Object(o["renderList"])(e.recordLines,(function(e){return Object(o["openBlock"])(),Object(o["createBlock"])(s,{key:e.LineCode,value:e.LineCode,label:e.LineName},null,8,["value","label"])})),128))]})),_:1},8,["modelValue"])]})),_:1})]})),_:1})]})),_:1})]})),_:1}),Object(o["createVNode"])(j,{label:"","label-width":0},{default:Object(o["withCtx"])((function(){return[Object(o["createVNode"])(h,null,{default:Object(o["withCtx"])((function(){return[Object(o["createVNode"])(O,{span:12},{default:Object(o["withCtx"])((function(){return[Object(o["createVNode"])(j,{label:"主机记录",prop:"RR"},{default:Object(o["withCtx"])((function(){return[Object(o["createVNode"])(g,{modelValue:e.form.RR,"onUpdate:modelValue":t[3]||(t[3]=function(t){return e.form.RR=t})},null,8,["modelValue"])]})),_:1})]})),_:1}),Object(o["createVNode"])(O,{span:12},{default:Object(o["withCtx"])((function(){return[Object(o["createVNode"])(j,{label:"记录值",prop:"Value"},{default:Object(o["withCtx"])((function(){return[Object(o["createVNode"])(g,{modelValue:e.form.Value,"onUpdate:modelValue":t[4]||(t[4]=function(t){return e.form.Value=t})},null,8,["modelValue"])]})),_:1})]})),_:1})]})),_:1})]})),_:1}),Object(o["createVNode"])(j,{label:"","label-width":0},{default:Object(o["withCtx"])((function(){return[Object(o["createVNode"])(h,null,{default:Object(o["withCtx"])((function(){return[Object(o["createVNode"])(O,{span:12},{default:Object(o["withCtx"])((function(){return[Object(o["createVNode"])(j,{label:"TTL",prop:"TTL"},{default:Object(o["withCtx"])((function(){return[Object(o["createVNode"])(p,{modelValue:e.form.TTL,"onUpdate:modelValue":t[5]||(t[5]=function(t){return e.form.TTL=t})},{default:Object(o["withCtx"])((function(){return[(Object(o["openBlock"])(!0),Object(o["createElementBlock"])(o["Fragment"],null,Object(o["renderList"])(e.recordTTLs,(function(e){return Object(o["openBlock"])(),Object(o["createBlock"])(s,{key:e.value,value:e.value,label:e.text},null,8,["value","label"])})),128))]})),_:1},8,["modelValue"])]})),_:1})]})),_:1}),Object(o["createVNode"])(O,{span:12},{default:Object(o["withCtx"])((function(){return[Object(o["createVNode"])(j,{label:"优先级",prop:"Priority"},{default:Object(o["withCtx"])((function(){return[Object(o["createVNode"])(g,{modelValue:e.form.Priority,"onUpdate:modelValue":t[6]||(t[6]=function(t){return e.form.Priority=t})},null,8,["modelValue"])]})),_:1})]})),_:1})]})),_:1})]})),_:1})]})),_:1},8,["model","rules"])]})),_:1},8,["modelValue"])],64)}Object(o["popScopeId"])();var re=n("5530"),ae={setup:function(){var e=E();Object(S["nc"])((function(){return e.domain}),(function(){n()}));var t=Object(m["p"])({loading:!1,records:{DomainRecords:[],PageNumber:1,PageSize:100,TotalCount:0},status:{ENABLE:"正常",DISABLE:"禁用"},statusBtn:{ENABLE:"禁用",DISABLE:"启用"}}),n=function(){e.group.platform&&e.domain.DomainName&&(t.loading=!0,w({Platform:e.group.platform,Group:e.group.Name,Domain:e.domain.DomainName,PageSize:t.records.PageSize,PageNumber:t.records.PageNumber}).then((function(n){t.loading=!1,n.DomainRecords.forEach((function(t){t.autoUpdate=e.domain.records.indexOf(t.RR)>=0})),t.records=n})).catch((function(e){t.loading=!1})),P({Platform:e.group.platform,Domain:e.domain.DomainName,Group:e.group.Name}).then((function(e){t.loading=!1,i.form.Line=null,i.recordLines=e,i.recordLinesJson=e.reduce((function(e,t){return e[t.LineCode]=t.LineName,e}),{})})).catch((function(e){t.loading=!1})))},o=function(o){t.loading=!0,D({RecordId:o.RecordId,Domain:e.domain.DomainName,Group:e.group.Name,Platform:e.group.platform}).then((function(){t.loading=!1,n()})).catch((function(e){t.loading=!1}))},r=function(o){t.loading=!0,x({RecordId:o.RecordId,Status:o.Status,Domain:e.domain.DomainName,Group:e.group.Name,Platform:e.group.platform}).then((function(){t.loading=!1,n()})).catch((function(e){t.loading=!1}))},a=function(o){B["c"].prompt("备注","备注",{confirmButtonText:"确定",cancelButtonText:"取消",inputValue:o.Remark}).then((function(r){var a=r.value;a&&(t.loading=!0,R({RecordId:o.RecordId,Remark:a,Domain:e.domain.DomainName,Group:e.group.Name,Platform:e.group.platform}).then((function(){t.loading=!1,n()})).catch((function(e){t.loading=!1})))}))},c=function(o){t.loading=!0,y({Platform:e.group.platform,Group:e.group.Name,Domain:e.domain.DomainName,Record:o.RR,AutoUpdate:o.autoUpdate}).then((function(){t.loading=!1,n()})).catch((function(e){t.loading=!1}))};Object(S["rb"])((function(){k().then((function(e){i.recordTypes=e}))}));var l=Object(m["r"])(null),i=Object(m["p"])({showAdd:!1,recordTypes:[],recordLines:[],recordLinesJson:{},recordTTLs:[{text:"10分钟",value:600},{text:"30分钟",value:1800},{text:"1小时",value:3600},{text:"12小时",value:43200},{text:"1天",value:86400}],form:{RR:"",Type:"A",Value:"",TTL:600,Line:null,Priority:10,DomainName:"",Platform:"",RecordId:"",AutoUpdate:!1},rules:{RR:[{required:!0,message:"必填",trigger:"blur"}],Type:[{required:!0,message:"必填",trigger:"blur"}],Line:[{required:!0,message:"必填",trigger:"blur"}],Value:[{required:!0,message:"必填",trigger:"blur"}]}}),u=function(){i.form.RR="",i.form.RecordId="",i.showAdd=!0},d=function(e){i.form.RR=e.RR,i.form.RecordId=e.RecordId,i.form.Type=e.Type,i.form.Value=e.Value,i.form.TTL=e.TTL,i.form.Priority=e.Priority,i.form.Line=e.Line,i.showAdd=!0},f=function(){l.value.validate((function(n){if(!n)return!1;t.loading=!0,T({Platform:e.group.platform,Group:e.group.Name,DomainName:e.domain.DomainName,RecordId:i.form.RecordId,RR:i.form.RR,Type:i.form.Type,Value:i.form.Value,TTL:i.form.TTL,Priority:i.form.Priority,Line:i.form.Line}).then((function(){t.loading=!1,i.showAdd=!1,c(i.form.RR,i.form.AutoUpdate)})).catch((function(e){t.loading=!1}))}))};return Object(re["a"])(Object(re["a"])(Object(re["a"])({},Object(m["z"])(t)),{},{loadData:n,handleDel:o,handleSwitchStatus:r,handleRemark:a,handleRecordAutoUpdateChange:c},Object(m["z"])(i)),{},{formDom:l,handleAdd:u,handleEdit:d,handleSubmit:f})}};n("8781");const ce=A()(ae,[["render",oe],["__scopeId","data-v-591be38a"]]);var le=ce,ie={components:{SettingModal:f["a"],Domains:z,Record:le},setup:function(){var e=Object(m["p"])({platforms:[],group:{Name:"",platform:"",Records:[],recordJson:{}},domains:{Domains:[],PageNumber:1,PageSize:100,TotalCount:0},domain:{records:[]}});return _(e),{}}};n("4eb1");const ue=A()(ie,[["render",d],["__scopeId","data-v-f9916b1a"]]);t["default"]=ue},"4eb1":function(e,t,n){"use strict";n("f88f")},"7a55":function(e,t,n){},8781:function(e,t,n){"use strict";n("7a55")},d81d:function(e,t,n){"use strict";var o=n("23e7"),r=n("b727").map,a=n("1dde"),c=a("map");o({target:"Array",proto:!0,forced:!c},{map:function(e){return r(this,e,arguments.length>1?arguments[1]:void 0)}})},f88f:function(e,t,n){}}]);