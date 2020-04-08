# AsInvoker

AsInvoker 是一个 Windows 可执行文件降权工具。它可以移除 exe 程序清单里的管理员权限要求。

## 用法

用法: `AsInvoker.exe 目标.exe`

或者直接把目标程序拖到 AsInvoker 上。

AsInvoker 使用 `kernel32.dll` 里的资源 API 来从 exe 里读取程序清单，搜索 `requestedExecutionLevel` 元素，并替换掉管理员权限的要求。

`requestedExecutionLevel` 详解（英文）：https://docs.microsoft.com/en-us/previous-versions/bb756929(v=msdn.10)

（也可搜索“requireAdministrator 提权”）

## 用例

这里的演示程序是微软 Sysinternals 的 [Disk2vhd](https://docs.microsoft.com/en-us/sysinternals/downloads/disk2vhd) 小工具，
用来把磁盘分区转换成虚拟磁盘文件。这个程序在清单里注明了需要管理员权限。

运行这个工具的时候，UAC 画面会跳出来。如下图，程序运行正常。

![正常](/Readme/Images/disk2vhd_normal.png)

现在我们用 AsInvoker 将 Disk2vhd 降权。Disk2vhd 的图标上，UAC 盾牌标志不见了。

运行时系统不再弹出 UAC 画面了。此外工具现在不能读取磁盘分区列表了，因为它现在没有管理员权限。

![修改后](/Readme/Images/disk2vhd_patched.png)

## 问题

如果把降权后的程序在 Resource Hacker 里打开，能看到原来的清单文件其实还存在于资源里。
这意味着修改过后的程序现在有两个程序清单了。
然而修改后的清单排在原版清单的前面，大概就是它生效的原因了。

我目前还不清楚怎么删掉原来的清单。如果调用 `UpdateResource` 并把 `lpData` 和 `cb`
置零，会直接报错无效参数，并且无法添加新清单或者保存修改。
