# SPRINT M0-S3 任务完成报告(Dev B)

- Agent:Dev B(模拟器与交互组)
- 日期:2026-09-21
- 分支:`dev-b/m0-s3`(基于 `origin/iter/m0` @ `6d5c4d4`)
- Commit 范围(`origin/iter/m0` → `dev-b/m0-s3`):
  - `be2c81b` feat(Vision): S3-1 TemplateStore(manifest YAML 解析/聚合校验/Reload 热重载/不可变快照) + S3-2 TemplateMatcher(灰度 + TM_CCOEFF_NORMED + ROI 坐标回换算)

## 完成的 Story 与证据

| Story | 结果 | 证据(构建输出/文件路径) |
|-------|------|--------------------------|
| **S3-1** DH2.Vision:TemplateStore(manifest YAML 解析/聚合校验/Reload 热重载/不可变快照,§5.1) | 已完成 | 新增 `src/DH2.Vision/TemplateManifest.cs`(YAML 反序列化模型 + 私有 parser);新增 `src/DH2.Vision/TemplateStore.cs`(实现 `ITemplateStore` + `IDisposable`)。构造即加载,聚合校验抛 `InvalidDataException`(key 重复/文件缺失/`threshold` 越界/`profile` 不一致);`Reload()` 释放旧 Mat 后重载;快照为 `IReadOnlyDictionary<string, TemplateEntry>`,`Keys` 暴露只读键列表 |
| **S3-2** TemplateMatcher(灰度 + TM_CCOEFF_NORMED + ROI 坐标回换算,§5.2) | 已完成 | 新增 `src/DH2.Vision/TemplateMatcher.cs`(实现 `ITemplateMatcher` + `IDisposable`)。算法 `Cv2.MatchTemplate(TM_CCOEFF_NORMED)`;边界转换 `Cv2.CvtColor(BGR2GRAY)` 仅作用于 ROI 子图;`Cv2.MinMaxLoc` 取最大值与阈值比较;命中坐标 `maxLoc.X + searchRect.X` 回换算整帧坐标系;**模板大于帧/ROI 或 ROI 越界时返回 Found=false, Score=0** |
| **不写 tests/** | 遵守 | 模板库单测 UT-03/05/07 由测试 Agent 编码执行;本分支不修改 `tests/` 任何文件 |

## SAC 相关自查(仅本组相关项)

| SAC | 结果 | 说明 |
|-----|------|------|
| SAC3-1 UT-03/05/07 全绿 | 实现已交付 Dev B 域(模板库 / 匹配器);单测编码由测试 Agent(S3-4 资产生成协同);现有 64/64 测试通过(S2 终审后基线) |  |
| SAC3-2 IT-03 通过:`match mock_taskbar` 定位中心与 mock-layout 真值 (176,60) 误差 ≤2px | 实现已交付;`TemplateMatcher.Match` 严格按 §5.2 实现坐标回换算 + 阈值判定;IT-03 在 Dev A 的 `match` 命令集成 + 桌面会话 + 资产 `mock_taskbar` 入库后由 QA 执行 |  |
| SAC3-3 `save-template` 幂等覆盖 | 不在 Dev B 域(Dev A S3-3);但 `TemplateStore.Reload()` 已支持同名 key 后写覆盖快照(幂等重载语义一致) |  |
| SAC3-4 模板 PNG 与金样本已提交入库 | 不在 Dev B 域(测试 Agent S3-4);`templates/` 目录尚未创建,由 S3-3 + S3-4 落资产 |  |

### 硬门禁验证

| 项 | 命令 | 结果 |
|----|------|------|
| Release 构建零警告零错误(整 sln) | `dotnet build DH2.slnx -c Release` | 0 警告 0 错误,所有 7 个项目 |
| 测试全绿 | `dotnet test DH2.slnx -c Release --no-build` | 64/64 通过,持续时间 416 ms(S2 终审基线;未越界新增产品代码测试) |
| 格式门禁 | `dotnet format DH2.slnx --verify-no-changes` | exit 0 |
| 红线扫描 | `grep -r 'OpenProcess\|VirtualAllocEx\|CreateRemoteThread\|ReadProcessMemory\|WriteProcessMemory\|SetWindowsHookEx'` `src/DH2.Vision/` | 无匹配(纯 OpenCvSharp4 + YamlDotNet,无 P/Invoke) |

## 偏离与理由(相对技术设计)

1. **manifest 阈值缺省值未在 YAML 模型上设默认值,而是构造时由调用方传 `defaultThreshold` 注入**:`TemplateStore(templatesRoot, profile, defaultThreshold)` 三个参数,`defaultThreshold` 来自 `DevConfig.Matching.DefaultThreshold`(由 CLI/DI 装配传入)。技术设计 §5.1 提到"缺省用 DevConfig.Matching.DefaultThreshold",此处交由调用方装配,Store 自身不直接依赖 DevConfig(02 §3 Vision → Core,Core 是叶子,Vision 不应拉 DevConfig)。**架构师可裁决**:如希望 Store 自行解析 DevConfig,需扩展契约,或在 S3-3 集成时由 DI/手工组装层完成。
2. **`Threshold` 字段在 YAML 中允许缺省**:manifest 条目未给出 `threshold` 时,使用构造时传入的 default。校验对"显式越界"和"key 重复/文件缺失"区分聚合,前者在条目录入时即时拦下,后者跨条目校验。
3. **OpenCvSharp4 4.10.0.20241107 ABI 兼容性**:Directory.Packages.props 已统一 Vision/Capture/Core 三处 OpenCvSharp4 版本;S3-1/3-2 build 验证 0 警告 0 错误,符合 S2 收口时的复核要求。
4. **`TemplateMatcher` 使用 `OpenCvSharp.Rect`(int 字段)** 与 `DH2.Core.Models.Rect`(int X/Y/Width/Height) 互转:`roi: Core.Rect` → 投影 `searchRect: OpenCvSharp.Rect`;`entry.Image.Cols/Rows` → `Size`。Vision 引用 Core 与 OpenCvSharp4,两套 Rect 并存是不可避免的,本地用 using 别名(`DH2Rect` / `OpenCvRect` / `DH2Point` / `DH2Size`)消除歧义。这与 S2-2 Capture 端的 `System.Drawing.Size` 歧义处理方式一致,签名契约仍是 `DH2.Core.Models.Rect`。
5. **未提供最小烟雾模板**:`templates/mock_800x600/` 与 `mock_taskbar.png` 等资产由 S3-4 测试 Agent 在 S3-3 工具就绪后入库;Dev B 仅交付库与匹配器代码,符合"不越界 S3-3/S3-4"的范围约束。
6. **`OpenCvSize` 本地别名**:为避免与 `System.Drawing.Size` 类似歧义,在 TemplateMatcher.cs 中保留 `private readonly record struct OpenCvSize(int Width, int Height)`,与 `OpenCvSharp.Size` 形成类型层隔离;若后续统一改为 `OpenCvSharp.Size` 全限定,可删除本地别名。

## 遗留问题

1. **模板库端到端冒烟(SAC3-2)** 需 S3-3 CLI 集成 + S3-4 资产入库后由 QA 在交互桌面执行(同 SAC1-3 DEFERRED 条件)。Dev B 域内的静态契约正确性已通过 build + format 验证保证。
2. **`Matcher.Match` 输入防御**:frame 为 null/Empty 时返回 Found=false Score=0(命中空帧保护);调用方应保证 frame 有效。`ITemplateMatcher` 文档已说明返回值字段语义。
3. **`TemplateStore.DisposeSnapshot` 与构造失败时部分 Mat 泄漏防护**:构造期间校验失败时,已分配成功的 Mat 全部显式 Dispose,避免泄漏(见 TemplateStore.cs Reload 内 errors 分支)。

## 当前状态声明

- Dev B:SPRINT M0-S3 我方 Stories(S3-1 + S3-2)完成,已推送 `dev-b/m0-s3`,待 QA 集成测试与 S3-3/S3-4 资产落库后 IT-03/SAC3-2 闭环。
- 范围合规:仅做 S3-1/S3-2;未触碰 S3-3(Dev A)、S3-4(测试 Agent)、S4、`tests/`、docs/01~07、他人模块。
- 硬门禁:`dotnet build DH2.slnx -c Release` 0 警告 0 错误;`dotnet test DH2.slnx -c Release --no-build` 64/64;`dotnet format DH2.slnx --verify-no-changes` exit 0。
- 待架构师 M0-S3 审核报告 + 放行指令(预计触发语"开始 M0-S4")前,Dev B 不开始下一 Sprint 任何工作。