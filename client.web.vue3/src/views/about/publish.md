<!--
 * @Author: snltty
 * @Date: 2021-09-04 00:32:20
 * @LastEditors: snltty
 * @LastEditTime: 2021-10-20 22:13:45
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

-c release                         配置
-o ./publish-linux             输出目录
-r linux-x64                     环境
--self-contained=true          包含环境
-p:PublishSingleFile=true    单文件（native dll 单独）
-p:PublishTrimmed=true      剪裁未使用程序集
-p:IncludeNativeLibrariesForSelfExtract=true  打包 native dll
```