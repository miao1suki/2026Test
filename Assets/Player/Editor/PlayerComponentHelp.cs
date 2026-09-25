using UnityEditor;

namespace Project.Player.Editor
{
    [InitializeOnLoad]
    internal static class PlayerComponentHelp
    {
        static PlayerComponentHelp()
        {
            ComponentHelpText.Register(
                typeof(PlayerController).FullName,
                "PlayerController · 玩家总控制器",
                "【这是什么】\n" +
                "玩家运行时的总入口。它负责基础移动、跳跃、冲刺、蹲下、" +
                "2D/3D 视角操作、梯子攀爬、平台承载和动作状态切换。\n\n" +
                "【输入怎么分流】\n" +
                "· Move / Look / Sprint / Crouch / CameraModeSwitch：控制器直接处理；\n" +
                "· Jump 未绑定 ActSO：执行内置物理跳跃；\n" +
                "· Jump 或其他输入绑定 ActSO：进入 Action 状态并播放 Timeline。\n\n" +
                "【动作绑定】\n" +
                "“输入动作绑定”把 InputActionId 映射到 ActSO。" +
                "ActSO 负责 Timeline、优先级、打断、锁移动和默认下一动作。\n\n" +
                "【状态重置】\n" +
                "跳跃、主动作、攀爬或 Timeline 接管控制时，" +
                "会取消蹲下和冲刺的点击切换状态。\n\n" +
                "【控制权】\n" +
                "PushControlLock / PopControlLock / SetControlLocked 用于过场或外部系统暂时接管；" +
                "Timeline Signal 可以直接调用 ReceiveTimelineSignal，或调用以后新增的具体信号方法。\n\n" +
                "【注意】\n" +
                "CameraModeController 仍是 2D/3D 唯一权威；这里只提交视角申请，不直接写 Camera。");

            ComponentHelpText.Register(
                typeof(PlayerActionRunner).FullName,
                "PlayerActionRunner · Timeline 动作运行器",
                "【这是什么】\n" +
                "把 ActSO.Timeline 交给 PlayableDirector 播放的运行时适配器。\n\n" +
                "【怎么用】\n" +
                "通常由 PlayerController 自动调用，不需要手动播放。" +
                "Inspector 中的“播放导演”留空时会自动查找同物体上的 PlayableDirector。\n\n" +
                "【运行时状态】\n" +
                "CurrentAction 表示当前动作数据盒；IsPlaying 表示导演是否仍在播放。");

            ComponentHelpText.Register(
                typeof(ActSO).FullName,
                "ActSO · Timeline 动作数据盒",
                "【这是什么】\n" +
                "一个动作的配置资产，包含动作编号、名字、Timeline 和播放规则。\n\n" +
                "【字段说明】\n" +
                "· 默认下一动作：当前 Timeline 结束后没有新输入时自动切换；\n" +
                "· 优先级：数值越大越优先；\n" +
                "· 允许打断：关闭后不能被其他动作打断；\n" +
                "· 锁定移动：动作期间清除玩家水平速度；\n" +
                "· 播放速度：Timeline 的播放倍率。\n\n" +
                "【注意】\n" +
                "玩法逻辑不要写进 ActSO 本身。动画、特效和时间点交给 Timeline；" +
                "伤害、状态、冷却等逻辑由脚本或 Timeline Signal 处理。");
        }
    }
}
