<!--
 * @Author: snltty
 * @Date: 2021-09-04 00:32:20
 * @LastEditors: xr
 * @LastEditTime: 2022-01-24 22:51:10
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\about\publish.md
-->
# 环境参数
1. <a href="https://docs.microsoft.com/zh-cn/dotnet/core/rid-catalog" target="_blank">常用参数及说明</a>
2. <a href="https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.NETCore.Platforms/src/runtime.json" target="_blank">完整参数</a>

# 发布命令
命令行进入项目目录 
```
dotnet publish 

-c release                 配置
-o ./publish-linux         输出目录
-r linux-x64               环境
--self-contained=true      包含环境
-p:PublishSingleFile=true  单文件（native dll 单独）
-p:PublishTrimmed=true     剪裁未使用程序集
-p:IncludeNativeLibrariesForSelfExtract=true  打包 native dll
```

# 启用动态PGO
```
设置环境变量，暂时不支持在配置文件配置，NET7可能会加上
set DOTNET_TieredPGO=1
set DOTNET_TC_QuickJitForLoops=1
set DOTNET_ReadyToRun=0  
```
