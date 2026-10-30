# 踩坑记录 / Field notes

这个 MOD 从「Nexus 下不到文件」到「两处上游 BUG 修完」的全过程记录。写给以后接手的人（和 AI agent）——省掉我踩过的那些。

---

## 1. Nexus 这条路走不通（下载侧）

- `nexusmods.com` 的文件下载链接需要登录态。匿名请求 CDN 会 302 到 `/CDN/e403`，`directDownloadEnabled` 全为 `false`。
- 用无痕 Chromium 抓包会被 Cloudflare 卡在验证页；用户本机 Chrome 用的是独立 profile（`C:\ChromeProfiles\...`），**没有开调试端口**，接不进去。
- 结论：**不要试图绕过 Nexus 登录**。改走上游作者的 GitHub 源码（MIT，可自由编译/再分发），也是本仓库存在的原因。

## 2. Nexus 上那个「BepInEx 框架」是陷阱（框架侧）

- 游戏是 **Unity 6000.3.10f1**（版本号 1.0.033r）。
- BepInEx **5.x 不支持 Unity 6**（它的 runtime 注入依赖 Mono 2.x 的 `UnityEngine.CoreModule` 布局）。
- Nexus 上 `ReStory-BepInEx-Framework`（642 KB）实测就是 `BepInEx_win_x64_5.4.23.6` 的原样重打包 —— 装上去对当前版本游戏无效，还可能因为 `winhttp.dll` 注入失败导致游戏一闪退。
- **正确框架：MelonLoader**（上游 README 标 Recommended）。实测 `v0.7.3` x64 在 Unity 6000.3.10f1 上工作正常。

## 3. 必须从 Steam 启动

直接双击 `Restory.exe` → `SteamAPI_Init() failed` → 进程自己退出。
脚本/自动化请用：`Start-Process "steam://rungameid/3812600"`。

## 4. 游戏自带反篡改扫描（行为已实测）

`RuntimeIntegrityService` 在启动时扫游戏根目录：

```
version.dll  winhttp.dll  winmm.dll  dobby.dll  doorstop_config.ini
BepInEx/     MelonLoader/  Mods/
```

命中会写日志：

```
[RuntimeIntegrityService] External interference detected. Markers: version.dll, MelonLoader, Mods
```

实测（1.0.033r）：游戏正常运行、无弹窗、存档正常、成就未锁。
未验证：大版本更新后是否会因此拒绝启动。**报告用户时必须说清"已验证"和"按机制推断"的区别。**

## 5. IMGUI 里三个必踩的坑

### 5.1 中文全是方块
Unity 内置 GUI 字体（Arial 内置子集）不含汉字。必须换动态系统字体：

```csharp
var f = Font.CreateDynamicFontFromOSFont(
    new[] { "Microsoft YaHei", "微软雅黑", "SimHei", "黑体", "SimSun", "宋体" }, 14);
GUI.skin.font = f;              // 全局皮肤
GUI.skin.label.font = f;        // 以及每个要用的内置样式
GUI.skin.toggle.font = f;
_lbl.font = f; _toggle.font = f; // 自己 new 出来的 GUIStyle 也要单独设
```

只设 `GUI.skin.font` 不够——自己 `new GUIStyle(GUI.skin.label)` 出来的样式会保留创建时的字体引用。

### 5.2 标题栏拖不动（上游 BUG）
`GUI.DragWindow(Rect)` 的参数是**窗口内坐标**，不是屏幕坐标。上游把 `_windowRect`（屏幕坐标 `20,80,380,420`）传了进去，拖拽区就被整体推到窗口中下部 —— 标题栏（y 0..26）恰好落在区外。

同时上游还有一段手动拖拽：
```csharp
if (Event.current.type == EventType.MouseDown && _windowRect.Contains(Event.current.mousePosition))
```
窗口函数里 `Event.current.mousePosition` 是**窗口内坐标**，拿它比屏幕矩形，结论自然是错的（点开关时反而触发拖动）。

正确写法只有一行：
```csharp
GUI.DragWindow(new Rect(0f, 0f, _windowRect.width, 26f));
```

### 5.3 窗口高度自适应
`GUILayoutUtility.GetLastRect()` 在 `GUILayout.EndVertical()` 之后取到的是内容最后一项的矩形，`height = last.yMax + 34f` 即可跟内容走。注意只在 `EventType.Repaint` 里改 `_windowRect.height`，否则布局会和输入事件错位。

## 6. 音效无限循环（上游 BUG，FMOD）

现象：按住 Z 一键拧紧后，拧螺丝的声音一直响，关不掉。

追法（在没有反编译器的机器上也能做）：用 `tools/asm-probe` 读游戏程序集：

```bash
dotnet run --project tools/asm-probe -- "<游戏>/Restory_Data/Managed/Restory.Assembly.dll" callers OnImmediateShortInteraction
```

得到 `Restory.Audio.ThreadedElementSFX`，再看它的 IL：

- `ResolveImmediateShortInteraction()`：先 `KillShortSound()`，再
  `TryToStartSoundEvent(tool.ScrewingSound, ...)` —— 这条 FMOD 事件是**循环**的。
- `KillShortSound()`：`audioPlayer.StopSoundEventInstance(shortSoundInstance, false)` + `clearHandle()`。

也就是说它只记住「最后一个实例」。一键拧紧在同一帧对 N 颗螺丝各触发一次事件，前 N-1 个实例被覆盖（泄漏），最后一个没人停 → 无限循环。

修复（`src/mods/AutoScrew.cs`）：给每颗被拧的螺丝记时间戳，0.45 s 后反射调用该组件的私有 `KillShortSound()` / `KillHoldSound()`；松键时立即全部停止。日志会打一行：

```
[AutoScrew] 音效收尾：停止 N 个螺丝音效
```

> 通用教训：凡是「一帧内触发 N 次 vanilla 交互」的 mod，都要检查那些交互是否落下了需要配对收尾的循环音 / 计时器 / tween。

## 7. 编译坑

- 上游 `RestoryQOL.csproj` 的 `PostBuild` 用 `Exec COPY /Y "$(TargetPath)" "$(GamePath)\Mods"`：`GamePath` 里带正斜杠（`E:/SteamLibrary/...`）时 `COPY` 退出码 1，整个构建报 MSB3073。已改为 MSBuild `Copy` 任务。
- 游戏正在运行时 `Mods\RestoryQOL.dll` 被占用，拷贝失败。用 `-p:SkipPostCopy=true` 只编译不部署。
- `Directory.Build.props` 里作者本机的绝对路径已清空，必须显式 `-p:GamePath=...`，避免误绑别的机器路径。
- 编译期需要 BepInEx 5 的 `core\BepInEx.dll` 作引用（上游是 BepInEx + MelonLoader 双入口）。**运行时不需要**。`scripts/build.ps1` 会自动下载一份当引用用。

## 8. 验证方法（别靠"看起来好了"）

1. `<游戏目录>\MelonLoader\Latest.log` 里必须有：
   ```
   Melon Assembly loaded: '.\Mods\RestoryQOL.dll'
   1 Mod loaded.
   [RestoryQOL] RestoryQOL 已加载（中文界面版），按 F8 开关菜单。
   ```
2. 截图核对中文渲染（方块/缺字都算失败）。
3. 拖动类改动：改动后日志会打 `[窗口] 位置 -> (x, y)`，能区分「没收到拖拽」和「拖了但位移为 0」。
4. 音效类改动：看 `[AutoScrew] 音效收尾：停止 N 个螺丝音效`，以及松键后声音是否立即停。
5. 自动化拖动测试（不依赖人眼）：`SetCursorPos` + `mouse_event(LEFTDOWN/LEFTUP)` 沿标题栏拖动，再截图对比。注意游戏场景本身在动，像素 diff 噪声大 —— 优先用日志里的坐标而不是 diff。

## 9. Windows / PowerShell 细节

- PowerShell 5.1 按 ANSI 解析 `.ps1`（除非带 BOM），**脚本里出现中文会在中文系统上乱码/语法错**。本仓库所有 `.ps1` 都是纯 ASCII。
- 中文系统里 PowerShell 输出经 bash 管道会乱码，判断结果只信退出码和数字。
- 截图脚本用 `Add-Type System.Drawing` + `Graphics.CopyFromScreen`；读取像素用 `Bitmap.LockBits` + `Marshal.Copy` 比 `GetPixel` 快两个数量级。

## 10. 发布相关（给 agent）

- 推送需要 `public_repo` 权限；本机已把 token 存进 Windows 凭据管理器（GCM），`git push` 不再交互。
- 如果弹出 `CredentialHelperSelector` 窗口，说明 `credential.helper` 还是 `helper-selector`：`git config --global credential.helper manager` 一次永久解决。
- Release 资产上传走 `uploads.github.com/repos/<owner>/<repo>/releases/<id>/assets?name=...`，`Content-Type: application/octet-stream`。
- 发布后必须**匿名重新下载一次资产并比对 sha256**，确认别人真能拿到且内容一致。
