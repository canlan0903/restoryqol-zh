# asm-probe

只读的地游戏程序集探针。查「这个类有什么成员」「这个方法到底调了什么」「谁引用了 `OnXxx` 事件」，不需要 ILSpy / dnSpy。

依赖只有 .NET SDK 8+（Mono.Cecil 会自动还原）。**只读，不修改程序集。**

```bash
# 类型名模糊匹配
dotnet run --project tools/asm-probe -- "<GAME>/Restory_Data/Managed/Restory.Assembly.dll" types Sound

# 某个类型的成员
dotnet run --project tools/asm-probe -- "<asm>" members ThreadedElementSFX

# 打印 IL（可只打某个方法）
dotnet run --project tools/asm-probe -- "<asm>" il ThreadedElementSFX ResolveImmediateShortInteraction

# 谁引用了某个符号（找事件监听方 / 调用方）
dotnet run --project tools/asm-probe -- "<asm>" callers OnImmediateShortInteraction

# 谁写了这段字符串
dotnet run --project tools/asm-probe -- "<asm>" strings ScrewingSound
```

`<GAME>` 是你的正版游戏安装目录，例如 `E:\SteamLibrary\steamapps\common\Restory`。
本仓库不分发任何游戏程序集。
