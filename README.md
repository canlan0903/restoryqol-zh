# RestoryQOL 中文版 / ReStory QoL Mod — Chinese UI Build

《维修物语》(ReStory: Chill Electronics Repairs, Steam **3812600**) 的 QoL MOD —— **完整中文界面版**。

上游是 [hykilpikonna/RestoryQOL](https://github.com/hykilpikonna/RestoryQOL)（作者 Azalea / Nexus 上的 azaneko，MIT 协议）。
本仓库只做两件事：**把界面文字全部译成中文**，**把中文字体接进 Unity IMGUI**，补丁逻辑与上游完全一致，一行没动。

> 原始 MOD 的菜单是纯英文，Unity 自带 GUI 字体又不含汉字（直接写中文会变方块）。这里用 `Font.CreateDynamicFontFromOSFont` 换成系统雅黑解决。

---

## 一键安装（Windows / PowerShell）

```powershell
# 1. 克隆
git clone https://github.com/canlan0903/restoryqol-zh.git
cd restoryqol-zh

# 2. 装（脚本会自动在 Steam 库里找你装的 ReStory，也会自己装 MelonLoader）
powershell -ExecutionPolicy Bypass -File scripts/install.ps1
```

装完**从 Steam 启动游戏**（不要双击 `Restory.exe`），进游戏按 **F8** 开菜单。

手动安装 / 别的平台看 [`docs/install-manual.md`](docs/install-manual.md)。

### 只想拿 DLL，不想用脚本

[**下载 Release `v1.0.2-zh`**](https://github.com/canlan0903/restoryqol-zh/releases) → `RestoryQOL.dll`
→ 丢进 `<游戏目录>\Mods\`。MelonLoader 仍需自己装一次（或跑 `scripts/install.ps1`）。
sha256 `34b663b068e824be192d0d9019c4bb5bba27ab9a5234e6be6b9d1b89353e1f77`

---

## 为什么不用 Nexus 上的 BepInEx 框架

游戏是 **Unity 6000.3.10f1**（版本 1.0.033r）。BepInEx **5.x 不支持 Unity 6**，Nexus 上那个 `ReStory-BepInEx-Framework`（642 KB）实测就是 `BepInEx_win_x64_5.4.23.6` 的原样重打包，对当前版本无效。
**正确框架是 MelonLoader**（上游 README 里标 *Recommended*），本仓库固定用 `v0.7.3` x64，实测可跑 Unity 6000.3.10f1。

---

## 装了/放了什么

| 目标 | 来源 |
|---|---|
| `<游戏根目录>\MelonLoader\` + `version.dll` | [LavaGang/MelonLoader](https://github.com/LavaGang/MelonLoader/releases) v0.7.3 x64（官方 Release） |
| `<游戏根目录>\Mods\RestoryQOL.dll` | 本仓库 `dist/`（由 `src/` 编译） |

游戏本体文件**一个都没改**，卸载 = 删这 3 项。

---

## 验证（别只看"装完了"）

1. 启动游戏后看 `<游戏根目录>\MelonLoader\Latest.log`，应出现：

```
Melon Assembly loaded: '.\Mods\RestoryQOL.dll'
1 Mod loaded.
[RestoryQOL] RestoryQOL 已加载（中文界面版），按 F8 开关菜单。
```

2. 进游戏按 F8，窗口标题应为 **`维修物语 QoL 设置`**，且中文不是方块。

---

## 功能一览

**作弊**：购买不扣钱 · 无限金钱 · 比赛失败重置计时 · 超声波清洗瞬间完成
**便利优化**：跳过启动 logo · 启动时打开菜单 · 高亮缺失的零件 · 放上设备自动开零件页 · 零件商店按笔记本顺序排序 · 零件箱按设备/装配顺序排序 · 超声波内零件计入笔记本 · 自动选工具（脏件用上次清洁工具，烧焦用烙铁）· 屏蔽商店页广告横幅
**快捷键**：ALT 零件吸附入槽 · SHIFT 丢件按状态自动归类 · CTRL+R 刷新商店 · G 聚拢散落零件 · 按住 Z 全部拧紧 / X 全部拧松
**Bug 修复**：修复存档卡死

完整中英对照见 [`docs/translation-table.md`](docs/translation-table.md)。

---

## 自己改字再编译

```bash
dotnet build -c Release \
  -p:GamePath="<你的游戏根目录>" \
  -p:BepInExPath="<可选：装有 BepInEx 5 的目录，仅编译期引用>"
```

只需要 .NET SDK 8+。翻译只涉及 `src/Core.cs` 里的字符串与字体逻辑。

---

## ⚠️ 已知风险（实测记录）

游戏内置 `RuntimeIntegrityService`，启动时扫描游戏根目录的 `version.dll` / `winhttp.dll` / `winmm.dll` / `dobby.dll` / `doorstop_config.ini` 以及 `BepInEx` / `MelonLoader` / `Mods` 目录，命中会写日志：

```
[RuntimeIntegrityService] External interference detected. Markers: version.dll, MelonLoader, Mods
```

- **已验证**：游戏正常运行、无弹窗、存档正常、成就未锁。
- **按机制推断**：标记大概率只进游戏自家 analytics，未见阻断逻辑；游戏本身自带作弊面板（`Cheat command: UnlockAchievement` 等），对改游戏并非零容忍。
- **未验证**：游戏大版本更新后是否会因检测到 loader 而拒绝启动。

介意就删掉 `MelonLoader\`、`Mods\`、`version.dll`。

---

## License

MIT（继承上游）。见 [`LICENSE`](LICENSE) 与 [`NOTICE.md`](NOTICE.md)。上游版权归 Azalea (hykilpikonna) 所有；中文界面改动归本仓库作者。
本仓库**不包含任何游戏本体文件**（游戏 DLL、贴图、字体），需自备正版游戏。
