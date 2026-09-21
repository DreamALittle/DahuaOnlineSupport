# M0-S3 L2 桌面资产生成与 IT-03 走查手册(用户代跑)

> **本手册供用户在交互桌面会话执行**。测试 Agent(QA,headless)无法启动 Avalonia GUI
> 也无法在无窗口会话截屏,因此 S3-4 资产生成(mock_taskbar/mock_btn_go/mock_btn_return
> 模板 PNG + gold-idle.png)与 IT-03 ≤2px 误差真机端到端,**全部由用户在桌面完成**。
>
> 本手册分两阶段:
> 1. **阶段 A — S3-4 资产生成**(3 模板 PNG + gold-idle.png + manifest.yaml 登记);
> 2. **阶段 B — IT-03 真机端到端**(`dh2ctl match mock_taskbar` 中心 ≤2px 断言)。
>
> 完成后请把证据(`docs\iterations\M0\qa\evidence\M0-S3-l2\results\` 下的全部文件 +
> `templates\mock_800x600\png\*` 与 `manifest.yaml`)拷贝或 git add 提交,QA 据此写入 M0-S3
> QA 报告 SAC3-2 / SAC3-3 校验段。

## 前置条件

- 操作系统:Windows 10/11
- 显示缩放:**100%**(150% 缩放会引入物理像素 ×1.5 折算,详见 README §8 日志坐标系说明;
  本手册断言是逻辑坐标 `(176, 60) / (86, 204)`,100% 缩放下 = 物理像素,可直接比对)
- .NET SDK 10.0.103 已安装
- 当前工作目录:`D:\Repos\DH2-QA`(已 git pull 到 iter/m0 最新)
- MockGame 已能正常启动(M0-S2 SAC2 已 PASS);无其他同名 `DH2.MockGame` 窗口运行
- 关闭窗口 DPI 缩放覆盖(右键 exe → 属性 → 兼容性 → "更改高 DPI 设置" →
  "高 DPI 缩放替代:应用程序")
- `configs\dev.yaml` 默认 `profile: mock_800x600`(本手册全程沿用)

---

## 阶段 A — S3-4 资产生成

### A.0 验证构建产物

```powershell
Set-Location D:\Repos\DH2-QA
dotnet build DH2.slnx -c Release
# 应输出 0 警告 0 错误
```

如果构建失败,**停** —— 这是产品代码问题,QA 不修复,记录错误信息给架构师。

### A.1 启动 MockGame(后台,置于 Idle 状态)

```powershell
$MockGameExe = 'D:\Repos\DH2-QA\artifacts\bin\DH2.MockGame\release\DH2.MockGame.exe'
$ResultsDir = 'D:\Repos\DH2-QA\docs\iterations\M0\qa\evidence\M0-S3-l2\results'
New-Item -ItemType Directory -Path $ResultsDir -Force | Out-Null

Start-Process -FilePath $MockGameExe -PassThru | ForEach-Object {
    $_.Id | Out-File "$ResultsDir\mockgame.pid.txt"
    Write-Host "MockGame started, PID: $($_.Id)"
}

# 等待 MockGame 窗口出现 + 进入 Idle 状态
$TempDir = Join-Path $env:TEMP 'dh2-mockgame'
$StateFile = Join-Path $TempDir 'state.json'
$timeout = (Get-Date).AddSeconds(15)
while ((-not (Test-Path $StateFile) -or ((Get-Content $StateFile -Raw | ConvertFrom-Json).state -ne 'Idle')) -and (Get-Date) -lt $timeout) {
    Start-Sleep -Milliseconds 200
}
if (-not (Test-Path $StateFile)) {
    Write-Error "state.json 未出现,MockGame 可能未启动"
    exit 1
}
Get-Content $StateFile -Raw | Out-File "$ResultsDir\A1-state-idle.json"
Write-Host "MockGame Idle"
```

**期望**:窗口标题 `DH2.MockGame`,尺寸 800×600,state.json 中 `state=Idle, counter=N`(N 为本次启动序号)。

### A.2 取 MockGame 窗口 hwnd

```powershell
$Dh2CtlExe = 'D:\Repos\DH2-QA\artifacts\bin\DH2.App\release\dh2ctl.exe'

$Hwnd = & $Dh2CtlExe enumerate --config configs\dev.yaml 2>&1 |
    Select-String -Pattern '^[0-9]+\s' |
    ForEach-Object { ($_ -split "`t")[0] } |
    Select-Object -First 1

Write-Host "Hwnd = $Hwnd"
$Hwnd | Out-File "$ResultsDir\A2-hwnd.txt"
```

**期望**:输出一行 hwnd 数字(十六进制窗口句柄转十进制)。若 enumerate 无输出,先确认
MockGame 已启动且标题严格匹配 `^DH2\.MockGame$`。

### A.3 生成 mock_taskbar 模板 + 金样本

**几何真值**(从 `configs\mock-layout.yaml`):任务栏 `(16, 16, 320, 88)`,中心 `(176, 60)`。

```powershell
$Hwnd = Get-Content "$ResultsDir\A2-hwnd.txt"   # ← 实际数字

# 任务栏模板:(16, 16, 320, 88)
& $Dh2CtlExe save-template `
    --hwnd $Hwnd `
    --key mock_taskbar `
    --x 16 --y 16 --w 320 --h 88 2>&1 |
    Tee-Object -FilePath "$ResultsDir\A3a-save-taskbar.json" | Out-Host
```

**期望**:
- 退出码 = 0
- stdout 一行 JSON:`{"saved":true,"key":"mock_taskbar","file":"png/mock_taskbar.png","threshold":0.85,"size":[320,88]}`
- 文件落盘:`templates\mock_800x600\png\mock_taskbar.png`(320×88)
- 文件落盘:`templates\mock_800x600\manifest.yaml`(含 templates[0] 条目:`key=mock_taskbar`)

### A.4 生成 mock_btn_go 模板

**几何真值**:按钮 `(16, 180, 140, 48)`,中心 `(86, 204)`。

```powershell
& $Dh2CtlExe save-template `
    --hwnd $Hwnd `
    --key mock_btn_go `
    --x 16 --y 180 --w 140 --h 48 2>&1 |
    Tee-Object -FilePath "$ResultsDir\A4-save-btn-go.json" | Out-Host
```

**期望**:退出码 0,JSON `{"saved":true,...,"key":"mock_btn_go",...,"size":[140,48]}`;落盘 `templates\mock_800x600\png\mock_btn_go.png`。

### A.5 生成 mock_btn_return 模板

(S3-4 范围:第三个模板,位置同 mock_btn_go —— 同区域复用,作为金样本的"返回按钮"副本,
可与 mock_btn_go 区分 key 后用于 match 多次匹配一致性。)

```powershell
& $Dh2CtlExe save-template `
    --hwnd $Hwnd `
    --key mock_btn_return `
    --x 16 --y 180 --w 140 --h 48 2>&1 |
    Tee-Object -FilePath "$ResultsDir\A5-save-btn-return.json" | Out-Host
```

**期望**:退出码 0,落盘 `templates\mock_800x600\png\mock_btn_return.png`。

### A.6 生成 gold-idle.png(整帧金样本)

```powershell
$GoldOut = 'D:\Repos\DH2-QA\artifacts\gold-s3'
New-Item -ItemType Directory -Path $GoldOut -Force | Out-Null

# capture --count 1 等价:不提供 --count 即默认 10;此处我们只要 1 张
& $Dh2CtlExe capture --hwnd $Hwnd --count 1 --out $GoldOut 2>&1 |
    Tee-Object -FilePath "$ResultsDir\A6-capture-gold.txt" | Out-Host

Get-ChildItem $GoldOut -Filter '*.png' | ForEach-Object {
    Copy-Item $_.FullName "$ResultsDir\gold-idle.png" -Force
    Write-Host "gold: $($_.FullName)"
}
```

**期望**:1 张 800×600 PNG 落盘;复制为 `results\gold-idle.png`。

### A.7 验证 manifest.yaml 三模板登记

```powershell
$Manifest = 'D:\Repos\DH2-QA\templates\mock_800x600\manifest.yaml'
Get-Content $Manifest | Out-File "$ResultsDir\A7-manifest.yaml"

Get-Content $Manifest | Select-String -Pattern '^\s*- key:' -Context 1
```

**期望**:3 行 `- key: mock_taskbar` / `mock_btn_go` / `mock_btn_return`,每行后跟
`file: png/<key>.png` + `threshold: 0.85` + `clickOffset: { x: 0, y: 0 }`。

### A.8 退出 MockGame

```powershell
$MockGamePid = Get-Content "$ResultsDir\mockgame.pid.txt"
Stop-Process -Id $MockGamePid -Force
Write-Host "MockGame stopped (pid=$MockGamePid)"
```

### A.9 上传与回写

```powershell
Set-Location D:\Repos\DH2-QA

# 模板 PNG + manifest 入仓
git add templates/mock_800x600/
git add docs/iterations/M0/qa/evidence/M0-S3-l2/

# 提交信息参考(可改写)
git commit -m "qa(s3): S3-4 资产生成 — mock_taskbar/mock_btn_go/mock_btn_return PNG + manifest + gold-idle.png (桌面走查,待架构师复核)"
git push origin iter/m0
```

---

## 阶段 B — IT-03 真机端到端:match mock_taskbar 中心 ≤2px

### B.0 启动 MockGame 重新加载模板(同 A.1 步骤,MockGame 必须处于 Idle)

```powershell
Start-Process -FilePath $MockGameExe -PassThru | ForEach-Object {
    $_.Id | Out-File "$ResultsDir\mockgame.pid.txt"
}
Start-Sleep -Seconds 2   # 等待 Idle 稳定
```

### B.1 dh2ctl enumerate 确认窗口

```powershell
$Hwnd = & $Dh2CtlExe enumerate --config configs\dev.yaml 2>&1 |
    Select-String -Pattern '^DH2\.MockGame' -Context 3
Write-Host $Hwnd
```

**期望**:表格数据行 Title=`DH2.MockGame`,Width=800,Height=600。

### B.2 match mock_taskbar(整帧无 ROI)

```powershell
& $Dh2CtlExe match --hwnd $Hwnd --key mock_taskbar 2>&1 |
    Tee-Object -FilePath "$ResultsDir\B2-match-taskbar.json" | Out-Host
```

**期望**(100% 缩放 / 逻辑坐标 == 物理像素):
- 退出码 = 0
- stdout 一行 JSON:形如 `{"found":true,"score":<~1.0>,"location":[16,16],"size":[320,88],"center":[176,60]}`
- 断言:`center.X` 与 `(176, 60)` 误差 ≤2px,`score ≥ 0.85`

### B.3 match mock_btn_go

```powershell
& $Dh2CtlExe match --hwnd $Hwnd --key mock_btn_go 2>&1 |
    Tee-Object -FilePath "$ResultsDir\B3-match-btn-go.json" | Out-Host
```

**期望**:JSON `{"found":true,...,"center":[86,204]}`。

### B.4 match mock_btn_return(同一 ROI 同区域不同 key)

```powershell
& $Dh2CtlExe match --hwnd $Hwnd --key mock_btn_return 2>&1 |
    Tee-Object -FilePath "$ResultsDir\B4-match-btn-return.json" | Out-Host
```

**期望**:JSON `{"found":true,...,"center":[86,204]}`(与 mock_btn_go 中心一致,key 不同)。

### B.5 多次匹配一致性(同帧连续 5 次)

```powershell
1..5 | ForEach-Object {
    & $Dh2CtlExe match --hwnd $Hwnd --key mock_taskbar 2>&1
} | Out-File "$ResultsDir\B5-consistency.json"
```

**断言**:5 次输出的 `location` 与 `center` 字节级一致(模板不可变快照 + 帧捕获确定性)。

### B.6 退出 MockGame

```powershell
$MockGamePid = Get-Content "$ResultsDir\mockgame.pid.txt"
Stop-Process -Id $MockGamePid -Force
```

### B.7 提交与上传

```powershell
git add docs/iterations/M0/qa/evidence/M0-S3-l2/
git commit -m "qa(s3): IT-03 真机走查 — match mock_taskbar/mock_btn_go/btn_return 中心 ≤2px(桌面)"
git push origin iter/m0
```

---

## 期望证据清单(给 QA 写入报告)

| 步骤 | 证据文件 | 断言 |
|------|----------|------|
| A.1  | `A1-state-idle.json` | MockGame 进入 Idle |
| A.2  | `A2-hwnd.txt` | 记录窗口 hwnd |
| A.3  | `A3a-save-taskbar.json` + `png/mock_taskbar.png` | save-template 成功 + 320×88 PNG |
| A.4  | `A4-save-btn-go.json` + `png/mock_btn_go.png` | save-template 成功 + 140×48 PNG |
| A.5  | `A5-save-btn-return.json` + `png/mock_btn_return.png` | save-template 成功 |
| A.6  | `gold-idle.png` | 800×600 完整帧金样本 |
| A.7  | `A7-manifest.yaml` | 3 模板条目完整 |
| B.2  | `B2-match-taskbar.json` | 中心 (176, 60) 误差 ≤2px |
| B.3  | `B3-match-btn-go.json` | 中心 (86, 204) 误差 ≤2px |
| B.4  | `B4-match-btn-return.json` | 中心 (86, 204) 误差 ≤2px |
| B.5  | `B5-consistency.json` | 5 次匹配一致 |

## 已知约束

- **headless QA 仅做命令层验证**:无窗口 / 无截屏 / 无 Avalonia GUI → QA 仅跑
  `dh2ctl save-template/match` 在缺参 / 错参 / 无效窗口路径;**IT-03 ≤2px 与 S3-4 资产生成
  100% 由本手册覆盖**,QA 不代填、不伪造。
- **150% 缩放折算**(README §8):物理像素 = 逻辑像素 ×1.5。任务栏中心物理像素 (264, 90);
  若用户测试机为 150% 缩放,断言请按物理像素折算并标注环境系数,严谨记录。
- **DPI 缩放覆盖**(前置):若 MockGame 应用清单未生效,可能因系统 DPI 虚拟化造成截屏尺寸
  与 MockGame 逻辑尺寸不一致(800 DIP → 1200 物理像素),此时 save-template ROI 坐标按
  物理像素传入,match 中心按物理像素断言。**推荐先关闭 DPI 缩放覆盖**保持 100% 一致。
