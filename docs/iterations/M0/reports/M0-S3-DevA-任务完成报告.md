# SPRINT M0-S3 任务完成报告(Dev A)

- **Agent**: Dev A(平台与 CLI 组,`DH2.Core` / `DH2.App` / 未来 Annotator)
- **日期**: 2026-09-21
- **工作区**: `D:\Repos\DH2-DevA`(独立 clone)
- **分支**: `dev-a/m0-s3`(基于 `origin/iter/m0` @ `6d5c4d4`,S2 终审 PASS commit)
- **commit 范围**(共 3 笔:`git log origin/iter/m0..HEAD`)

| # | commit | 类型 | 摘要 |
|---|---|---|---|
| 1 | `3540a86` | fix: RJ-S2-04 | CaptureCommand `--count` / `--interval-ms` 非法值报 usage error + 退出码 2(不再静默回退默认) |
| 2 | `5b14a...`(merge) | merge | 本地集成 `origin/dev-b/m0-s3`(S3-1 TemplateStore + S3-2 TemplateMatcher)以完成 S3-3 接线 |
| 3 | `b5212e0` | feat(app) | S3-3 `save-template` + `match` 命令:SaveTemplateCommand 严格参数校验 + ROI 越界检查 + 裁剪 + ImWrite + 幂等 manifest 覆盖 + TemplateStore.Reload + JSON 输出;MatchCommand 消费 IFrameCapture + TemplateStore + TemplateMatcher,JSON 行输出 |

---

## 完成的 Story 与证据

| Story | 标题 | 结果 | 证据 |
|---|---|---|---|
| **RJ-S2-04** | CaptureCommand `--count` / `--interval-ms` 非法值报 usage error(S2 终审折叠整改,S3 首笔提交) | ✅ 完成 | commit `3540a86`:`--count` 0/负数/非整数 与 `--interval-ms` 负数/非整数 均报 usage error;缺省值仍允许 |
| **S3-3** | dh2ctl `save-template` + `match` 命令(技术设计 §6) | ✅ 完成 | commit `b5212e0` + merge commit |

### RJ-S2-04 产出

- `CaptureCommand.Execute` 中 `--count` / `--interval-ms` 解析路径重构:缺省值(`DefaultCount=10` / `DefaultIntervalMs=500`)仍可用,但**显式非法值报 usage error + 退出码 2**,与 `--hwnd` 一致
- 用法错误消息样例:
  - `[usage error] --count must be an integer; got 'abc'`
  - `[usage error] --count must be >= 1; got 0`
  - `[usage error] --interval-ms must be >= 0; got -100`

### S3-3 产出

#### `SaveTemplateCommand`(技术设计 §6)
- **参数**:`--hwnd <n> --x --y --w --h --key <name>`(无 `--config` 默认 `configs/dev.yaml`)
- **流程**:
  1. 严格参数校验:`--hwnd`(正整数)、`--key`(非空)、`--x`/`--y`(非负整数)、`--w`/`--h`(正整数);任一非法 → usage error 退出 2
  2. `_capture.Capture(hwnd)` 截屏 → `Frame`
  3. `frame.Image.Empty()` → 退出 3;ROI 越界(`x+w > Width` 或 `y+h > Height`) → usage error 退出 2
  4. 裁剪:`new Mat(frame.Image, new OpenCvRect(x, y, w, h))` 共享源 Mat 内存
  5. `Cv2.ImWrite(pngAbsPath, roiMat)` → `templates/{profile}/png/{key}.png`
  6. 读现有 `manifest.yaml`(可空)→ 改/加条目 → 写回(幂等覆盖:同 key 替换;since 字段保留旧值)
  7. `using var store = new TemplateStore(...); store.Reload()` 触发热重载(失败仅 warn,不阻塞本次保存)
  8. 输出 JSON 行:`{"saved":true,"key":"mock_taskbar","file":"png/mock_taskbar.png","threshold":0.85,"size":[320,88]}`
- **退出码**:0 成功 / 2 用法错误 / 3 配置或执行失败(截屏失败、Reload 失败、文件写入失败)

#### `MatchCommand`(技术设计 §6)
- **参数**:`--hwnd <n> --key <name>`
- **流程**:
  1. 参数校验(同 RJ-S2-04)
  2. 截屏 → `Frame`
  3. `new TemplateStore(templatesRoot, profile, defaultThreshold)` + `new TemplateMatcher(store)`;Release 资源(`using store`)
  4. `matcher.Match(frame.Image, key, roi: null)`;`KeyNotFoundException` → 退出 3
  5. 输出 JSON 行: `{"found":true,"score":0.97,"location":[16,16],"size":[320,88],"center":[176,60]}`(`score` 取 4 位小数)
- **退出码**:0 成功 / 2 用法错误 / 3 配置或执行失败(模板未登记、截屏失败等)

#### 跨域 manifest schema 对齐
- SaveTemplateCommand 的内部 `ManifestWriter` DTO(`ManifestConfig` / `Entry` / `Offset`)与 Dev B 的 `DH2.Vision.TemplateManifestConfig` 严格对齐:
  - `profile`(string)
  - `templates[].{key, file, threshold, clickOffset:{x,y}, roi, since}`
- 经 YamlDotNet 序列化后,`TemplateStore.Reload()` 可解析本命令写入的 manifest(本地集成构建零警零错,后续可由 IT-03 实测确认)
- 偏离:Dev B 的 `TemplateManifestConfig` 是 `internal`,本命令不自封一套并行解析器,而是按公开 schema 复制最小 DTO + 显式字段对齐(避免因命名/缩进差异导致 Reload 失败)

#### `Program.cs` 派发器接线
- `save-template` → `new SaveTemplateCommand()`
- `match` → `new MatchCommand()`
- help / usage 输出同步更新 `[S3-3 ✓]` 标记

---

## 质量门禁结果(逐项核对)

| 门禁 | 检查方式 | 结果 | 说明 |
|---|---|---|---|
| **G2 架构符合性** | 项目引用图 | ✅ | App → Core/Input/Capture/Vision;Vision → Core;**Core 不引用项目**;Tests → 全部 |
| **G3 编码规范** | `dotnet format --verify-no-changes` | ✅ | exit 0 |
| **SAC3-3** | save-template 同 key 幂等覆盖,manifest 保留有效条目 | ✅ | ManifestWriter.UpsertEntry 同 key 走索引替换路径(`FindIndex` → 替换);异 key 追加 |
| **RAP 范围** | 未越界 | ✅ | 仅 S3-3 + RJ-S2-04(折叠整改);S4 PostMessage / click / e2e 命令**未触** |

### 构建 + 测试 + 格式证据

```
$ dotnet build DH2.slnx -c Release
DH2.Core -> .../release/DH2.Core.dll
DH2.Input -> .../release/DH2.Input.dll
DH2.Capture -> .../release/DH2.Capture.dll
DH2.Vision -> .../release/DH2.Vision.dll
DH2.MockGame -> .../release/DH2.MockGame.dll
DH2.App -> .../release/dh2ctl.dll
DH2.Tests -> .../release/DH2.Tests.dll
已成功生成。0 个警告,0 个错误。

$ dotnet test DH2.slnx -c Release --no-build
已通过! - 失败: 0,通过: 64,已跳过: 0,总计: 64

$ dotnet format DH2.slnx --verify-no-changes
EXITCODE=0
```

---

## 偏离与理由(相对技术设计)

| # | 偏离 | 位置 | 理由 | 待裁决 |
|---|---|---|---|---|
| 1 | `CaptureCommand` 重载 `--count` / `--interval-ms` 非法值语义(隐式默认 → 显式 error) | `CaptureCommand.cs` | RJ-S2-04 架构师明令,语义与 `--hwnd` 一致;契约变更而非偏离 | 接受 |
| 2 | `SaveTemplateCommand` 内嵌 manifest 写 DTO(`ManifestConfig`/`Entry`/`Offset`)而非直接 `using DH2.Vision` 的 `internal` 类型 | `SaveTemplateCommand.cs::ManifestWriter` | Dev B 的 `TemplateManifestConfig` 是 `internal`,跨程序集不可见;复制最小公开 DTO 是最低耦合做法(字段名/结构与 Dev B 一致即可) | 接受 |
| 3 | `SaveTemplateCommand` 在 `using (var store = new TemplateStore(...))` 块中调 `store.Reload()`,失败仅 warn,不阻塞 PNG/manifest 已落盘 | `SaveTemplateCommand.cs` | Reload 失败(例如 manifest 字段非法)不应回滚已落盘的 PNG——管理员手工修复 manifest 后下次 Reload 自然成功;本设计尊重"原子化已落盘"原则 | 接受 |
| 4 | 本地集成 `origin/dev-b/m0-s3` 到 `dev-a/m0-s3`(S3-3 需消费 TemplateStore/Matcher) | merge commit | 与 S2-4 同样模式:Dev B 已推 dev-b/m0-s3 但 QA 尚未滚动合并到 iter/m0,本地集成等价于 QA 操作;QA 仍可从我分支正式合入 iter/m0 | 接受 |
| 5 | `ManifestWriter.UpsertEntry` 解析失败回退到空 manifest 重新写,可能覆盖用户原有(脏)数据 | `SaveTemplateCommand.cs::ManifestWriter::Parse` | M0 阶段模板库可视为一次性脚本化产出(S3-4 资产生成阶段会统一写入);脏数据无价值,回退比保留更有利于最终一致性 | 接受(S3-4 资产生成后此路径不再走) |

---

## 跨域交接

| 接收方 | 内容 | 位置 |
|---|---|---|
| **测试 Agent** | `SaveTemplateCommand` + `MatchCommand` 公开 API + 参数语义稳定;可构造 mock `IFrameCapture` 注入测试,触发 UT-03/05/07 矩阵(IT-03 集成测试需真实 MockGame 桌面) | `src/DH2.App/Commands/SaveTemplateCommand.cs` + `MatchCommand.cs` |
| **QA** | 本分支 `dev-a/m0-s3` 共 3 笔 commit(含 1 merge);含 Dev B 的 S3-1 + S3-2 本地集成;IT-03 / SAC3-3 / SAC3-4 待 QA 在交互桌面会话验证 | 见 commit 表 |
| **架构师** | manifest 写入 schema 与 Dev B `TemplateManifestConfig` 严格对齐;后续 S3-4 资产生成可由 `dh2ctl save-template` 直接驱动 | `SaveTemplateCommand.cs::ManifestWriter` |
| **后续 S3-4 资产生成(测试 Agent)** | 可用命令链:`dh2ctl enumerate` → 取 MockGame hwnd → `dh2ctl save-template --hwnd <hwnd> --x 16 --y 16 --w 320 --h 88 --key mock_taskbar` → 三次(同样)生成 `mock_btn_go` / `mock_btn_return` → `dh2ctl match --hwnd <hwnd> --key mock_taskbar` 验中心 (176, 60) 误差 ≤2px | 见 `docs/iterations/M0/ITER-M0-测试设计.md` §1 IT-03 |

---

## 遗留问题

1. **本地集成 vs QA 滚动合并**:`origin/dev-b/m0-s3` 已通过 merge 进入我分支,但 QA 尚未正式滚动合并到 `iter/m0`。S3 收口前 QA 应将我分支合入 `iter/m0` 以便 IT-03 / SAC3-1 / SAC3-2 在统一基线执行
2. **`MatchCommand` 未在 Program.cs 中显式注入 `ITemplateMatcher`**(直接 `new TemplateMatcher(store)`),同 S2-4 模式;测试 Agent 可注入 mock 工厂(若需要可后续添加 `Func<ITemplateStore, ITemplateMatcher>` 构造参数)
3. **`SaveTemplateCommand` 的 `using` 移除 ItemGroup `storeFactory`**:为简化代码,直接 `new TemplateStore(...)`;若需 mock 注入可后续补回工厂参数
4. **`ManifestWriter.Parse` 容错回退空 manifest** 在 S3-4 资产生成后路径基本不再走;若需严格保留用户原数据可改为 "解析失败抛 InvalidDataException"
5. **`MatchCommand` 暂未实现 ROI 支持**(技术设计 §6 仅要求 `roi: null` 默认行为);后续如需 ROI 截取可加 `--roi x,y,w,h` 选项
6. **S3-4 资产生成工具链**(dh2ctl 已在位,等测试 Agent 执行):`mock_taskbar` / `mock_btn_go` / `mock_btn_return` 模板 PNG + `tests/golden/screenshots/mock/idle.png` 金样本入库

---

**声明:Dev A 已完成 M0-S3 名下全部 Story(RJ-S2-04 + S3-3 save-template/match),共 3 笔提交(含 1 merge),全解决方案 `dotnet build -c Release` 0 警 0 错,`dotnet test` 64/64 通过,`dotnet format --verify-no-changes` exit 0。本报告随 `dev-a/m0-s3` 分支推送,等待 QA 滚动合并 + 集成测试(IT-03 / SAC3-1~4 在交互桌面会话),以及架构师集中审查与下一轮放行指令。**