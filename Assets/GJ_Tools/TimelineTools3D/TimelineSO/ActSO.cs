using UnityEngine;
using UnityEngine.Timeline;

[CreateAssetMenu(
    fileName = "ActSO",
    menuName = "TimelineKit/ActSO")]
public sealed class ActSO : ScriptableObject
{
    [Tooltip("动作编号。")]
    [SerializeField]
    private int actionId;

    [Tooltip("动作名字。")]
    [SerializeField]
    private string actionName;

    [Tooltip("该动作播放的 TimelineAsset。")]
    [SerializeField]
    private TimelineAsset timeline;

    public int ActionId => actionId;
    public string ActionName => actionName;
    public TimelineAsset Timeline => timeline;
}
