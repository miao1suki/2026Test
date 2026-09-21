using UnityEngine.Playables;
using UnityEngine;
using UnityEngine.Timeline;

[TrackBindingType(typeof(TimelineCamRig))]
[TrackClipType(typeof(CameraTimelineClip))]
public class CameraTimelineTrack : TrackAsset
{
    public override Playable CreateTrackMixer(
        PlayableGraph graph,
        GameObject go,
        int inputCount)
    {
        CameraTimelineMixerBehaviour behaviour = new CameraTimelineMixerBehaviour
        {
            track = this
        };
        return ScriptPlayable<CameraTimelineMixerBehaviour>.Create(
            graph,
            behaviour,
            inputCount);
    }
}
