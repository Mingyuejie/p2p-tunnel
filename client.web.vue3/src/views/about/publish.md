<!--
 * @Author: snltty
 * @Date: 2021-09-04 00:32:20
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-04 00:35:25
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
dotnet publish -c release -o ./publish-win -r win-x64

dotnet publish -c release -o ./publish-osx -r osx-x64

dotnet publish -c release -o ./publish-linux -r linux-x64
```