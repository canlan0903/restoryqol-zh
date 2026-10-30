# 手动安装（不想跑脚本时）

1. 找到游戏目录：Steam 库里右键《维修物语》→ 管理 → 浏览本地文件。目录里应有 `Restory.exe`。
2. 装 MelonLoader：下载
   `https://github.com/LavaGang/MelonLoader/releases/download/v0.7.3/MelonLoader.x64.zip`
   把压缩包**解压到游戏根目录**（和 `Restory.exe` 同一层）。解压后应出现 `MelonLoader\` 文件夹和 `version.dll`。
3. 建 `Mods` 文件夹，把本仓库 `dist/RestoryQOL.dll` 放进去。
4. **从 Steam 启动游戏**（直接双击 exe 会因 `SteamAPI_Init` 失败自动退出）。
5. 进游戏按 **F8**，看到「维修物语 QoL 设置」即成功。

## 验证

打开 `<游戏目录>\MelonLoader\Latest.log`，应包含：

```
Melon Assembly loaded: '.\Mods\RestoryQOL.dll'
1 Mod loaded.
[RestoryQOL] RestoryQOL 已加载（中文界面版），按 F8 开关菜单。
```

## 卸载

删掉游戏根目录的 `MelonLoader\`、`Mods\`、`version.dll` 三个即可。
