# Fire

一款使用 Unity 开发的低多边形 FPS 丧尸波次生存游戏。玩家需要抵御从多个传送门生成的敌群，通过射击、自动技能和 Buff 构筑完成连续战斗。

项目基于第三方 FPS 框架完成角色、武器和动画基础能力，并在其上实现敌人 AI、共享流场导航、波次系统、成长系统、对象池、技能和移动端输入适配。

## 核心玩法

- 使用枪械消灭不同波次生成的敌人
- 击杀敌人获得经验，升级后选择随机 Buff
- 通过 Buff 调整伤害、射速、弹匣、生命值和技能属性
- 解锁 Drone、IceBomb 等自动技能处理密集敌群
- 清除全部波次获得胜利，生命归零进入失败结算

## 技术栈

- Unity `2022.3.48f1`
- C#
- Universal Render Pipeline `14.0.11`
- Unity Input System
- AI Navigation
- Compute Shader
- ScriptableObject
- UGUI
- DOTween

## 核心系统

### Enemy FSM

敌人通过状态机管理 `Birth`、`Chase`、`Attack` 和 `Dead` 生命周期。状态切换分别处理出生动画、导航追击、攻击判定、经验发放和对象回收，避免将全部行为堆叠在单个更新函数中。

### FlowField 群体导航

地图障碍预先烘焙为 `FlowFieldAsset`，运行时围绕玩家目标执行 BFS，所有敌人共享方向场并通过数组查询移动方向，减少为每个敌人重复计算完整路径的需求。

系统同时支持目标更新、局部重建、可达位置查询和障碍方向判断。

### SpatialGrid 与局部避让

`SpatialGrid` 按空间网格登记活动敌人，为分离、局部避让和刷怪合法性检查提供邻居查询。敌人以流场方向作为主要移动依据，并结合局部邻居信息减少重叠。

### AI LOD 与分帧调度

`EnemyManager` 将状态更新和导航更新拆分，并根据敌人与玩家的距离调整更新频率。导航任务分批执行，用于降低大量敌人同时更新造成的 CPU 峰值。

### 战斗与成长

`Player` 管理生命、经验、等级、Buff 和自动技能。`PlayerBuffManager` 统一处理伤害、射速、弹匣容量、吸血、最大生命值以及技能范围、伤害和冷却时间。

波次、Buff、敌人属性和地图数据通过 ScriptableObject 配置，使玩法数值与运行逻辑保持分离。

### 对象生命周期

敌人、弹丸、粒子和技能对象通过对象池或统一回收流程管理。攻击范围检测使用预分配缓冲区和 NonAlloc API，降低高频战斗逻辑中的临时内存分配。

### GPU Skinning

项目提供基于 Compute Shader 的敌人 GPU Skinning 路径，并保留标准渲染回退配置。具体性能收益需要在固定设备、画质、分辨率和敌人数条件下通过 Unity Profiler 与 Frame Debugger 验证。

### FPS 框架与移动端适配

第三方 Infima Low Poly Shooter Pack 负责 Character、Movement、Camera、Inventory、Weapon 和基础动画系统。

自研层通过公开接口和 `EventManager` 接入玩家属性、Buff、敌人、技能、波次及移动端触控。虚拟摇杆和触摸视角经过独立输入路径转发，避免触摸输入重复写入造成视角抖动。

## 个人职责

- 设计敌人 FSM，并实现出生、追击、攻击、死亡和回收流程
- 实现基于 BFS 的共享 FlowField 导航及局部重建机制
- 使用 SpatialGrid 支撑敌人邻居查询、分离和刷怪检查
- 实现 AI LOD、导航分批更新和对象池等性能优化机制
- 接入 Compute Shader GPU Skinning 路径及标准渲染回退
- 实现波次、经验、Buff、Drone 和 IceBomb 技能系统
- 基于第三方 FPS 框架完成业务扩展与移动端输入适配
- 编写项目技术文档、框架边界文档和验证清单

## 项目结构

```text
Assets/
├── Scripts/
│   ├── Framework/       游戏、事件、时间、敌人和波次管理
│   ├── Player/          玩家属性、Buff 与技能
│   ├── Enemy/           敌人逻辑、移动、动画与 GPU Skinning
│   ├── EnemyState/      敌人行为状态
│   ├── FSM/             状态机实现
│   ├── FlowField/       共享流场与 SpatialGrid
│   ├── Combat/          弹丸池与战斗逻辑
│   ├── Wave/            Portal 与刷怪点
│   ├── UI/              桌面及移动端交互
│   └── Editor/          项目编辑器工具
├── Data/
│   ├── WaveData/        波次配置
│   ├── PlayerData/      等级与 Buff 配置
│   ├── EnemyData/       敌人属性配置
│   └── MapData/         FlowField 烘焙数据
├── Scenes/              Loading 与 Demo 场景
└── Prefabs/             Player、Enemy、Portal 与技能预制体
```

## 运行项目

1. 安装 Unity Hub。
2. 使用 Unity `2022.3.48f1` 打开仓库目录。
3. 等待 Package Manager 完成依赖导入。
4. 确认 Build Settings 中包含 `Loading` 和 `Demo` 场景。
5. 从 `Loading` 场景进入 Play Mode。

## 文档

- [项目技术文档](技术文档.md)
- [第三方 FPS 框架技术文档](FPS框架技术文档.md)
- [项目答辩文档](答辩文档.md)

## 第三方资源说明

角色、武器和部分美术、动画及工具资源来自第三方资源包，包括 Infima Games Low Poly Shooter Pack、DOTween、SharpPoly Zombies 和 Dark UI。

这些第三方内容不属于本项目原创成果。使用、发布或再分发仓库内容前，请分别核对相关资源的许可条款。

## License

本仓库采用自定义的 [Fire Portfolio License](LICENSE)，仅允许作品集浏览、
招聘评估、学术或技术审阅，以及非商业性的个人学习。

未经版权所有者书面许可，不得复制、修改、再分发、商业使用或将本项目原创
内容合并到其他项目中。本许可证不是开源许可证。

仓库中的第三方框架、插件、美术、模型、动画、音频和其他资源不受该许可证
授权，仍分别遵循其原始许可条款。

## 当前限制

- 当前为单机本地逻辑，不包含网络同步
- 高密度敌群和 GPU Skinning 的性能收益仍需固定环境测试
- 动态障碍下的 FlowField 重建策略需要进一步验证
- 仓库暂未提供可下载构建和自动化测试
