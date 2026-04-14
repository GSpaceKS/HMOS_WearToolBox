# HMOS Wear ToolBox

[![Release](https://img.shields.io/badge/Release-v1.0.0--beta2-orange)](https://github.com/GSpaceKS/HMOS_WearToolBox/releases)

**HMOS Wear ToolBox** 是一款为鸿蒙手表（HarmonyOS NEXT）用户设计的 PC 端管理工具。提供设备信息查看、应用管理、HDC 终端等核心功能，无需安装，点开即用。

**ps:** 使用了AI来辅助优化了代码。作为鄙人第一个脱离老师课程外编写的项目。一开始的计划是在项目中边写边学，所以更新可能较慢🙏

---

## 功能特性

-  **设备信息 (默认每3分钟自动获取一次数据)**  
  可获取设备名称、型号、系统版本、API 版本、CPU 架构、屏幕分辨率等信息。

- **获取电池与存储**  
  实时查看电量、电压、充电状态、健康度，以及存储空间使用情况

- **软件管理**  
  列出设备上已安装的所有应用，支持安装（**目前只能安装已经签名过的hap安装包，正在研究如何整合签名**）和卸载，内置常用应用名称映射（如微信、运动健康等）**（但不保证一定是对的。请注意甄别。如果你发现了错误或者想添加软件名，可以通过 [Issues](https://github.com/GSpaceKS/HMOS_WearToolBox/issues) 来告诉我）**。

- **HDC 终端**  
  内置 HDC 命令行终端，可直接在工具内执行 HDC 命令，支持历史命令（上下键）、右键复制/粘贴 等交互优化。

- **设置**  
  可以开关自动更新时间以及自定义更新时间、改变软件字体等。如果你有更好的想法，可以通过 [Issues](https://github.com/GSpaceKS/HMOS_WearToolBox/issues) 来告诉我添加什么设置

---

## 食用方法

### 1. 解压并运行
- 解压后直接运行 `HMOS_WearToolBox.exe` 程序即可。无需任何环境配置。

### 2. 连接手表
- 确保手表已开启 无线 调试，并与电脑处于同一网络下才可以连接。
- **要设置手表常亮，因为手表熄屏后为了安全，是无法使用 HDC 获取数据的**
- 在工具中点击“添加设备”，输入手表 IP 地址（如 `127.0.0.0:5555`）。

---

## 功能状态

- [x] 安装软件
- [x] 内置HDC终端
- [ ] 软件签名

---

## LICENSE

```text
MIT License

Copyright (c) 2026 GSpace

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
