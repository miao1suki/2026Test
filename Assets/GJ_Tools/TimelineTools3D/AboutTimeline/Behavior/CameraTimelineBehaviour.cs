using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class CameraTimelineBehaviour : PlayableBehaviour
{
    public CameraTimelineClip clip;

    private TimelineCamRig _rig;
    private bool _inited;
    private bool _warned;
    private Vector3 _originWorldPos;
    private Quaternion _originWorldRot;
    private float _planarYawAtClipStart;
    private float _surroundStartAngle;

    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        _inited = false;
        _warned = false;
        _planarYawAtClipStart = 0f;
    }

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        if (clip == null)
        {
            return;
        }

        TimelineCamRig rig = playerData as TimelineCamRig;
        if (rig == null)
        {
            if (!_warned)
            {
                Debug.LogWarning("[CameraTimeline] 未绑定 TimelineCamRig，运镜不生效");
                _warned = true;
            }
            return;
        }

        _rig = rig;

        float totalDuration = (float)playable.GetDuration();
        if (totalDuration <= 0.0001f)
        {
            return;
        }

        if (!_inited)
        {
            _inited = true;
            rig.CaptureNormalState();
            _planarYawAtClipStart = rig.PlanarYaw;
            if (!clip.useLastFrameAsOrigin)
            {
                _originWorldPos = rig.CurrentPosition;
                _originWorldRot = rig.CurrentRotation;
            }

            Transform surroundAnchor = rig.AnchorTransform;
            Vector3 surroundOrigin = rig.CurrentPosition -
                                     surroundAnchor.position;
            surroundOrigin.y = 0f;
            _surroundStartAngle =
                Mathf.Atan2(surroundOrigin.x, surroundOrigin.z) *
                Mathf.Rad2Deg;
        }

        if (rig.userDriving)
        {
            return;
        }

        float currentTime = (float)playable.GetTime();
        float deltaTime = Mathf.Max(0f, (float)info.deltaTime);
        Vector3 currentPosition = rig.CurrentPosition;
        Quaternion currentRotation = rig.CurrentRotation;
        Transform anchor = rig.AnchorTransform;
        Vector3 lookPoint = anchor.position + Vector3.up * rig.lookHeight;

        if (clip.cameraMoveMode == CamMoveMode.ResetOrigin)
        {
            if (!rig.HasNormalState)
            {
                return;
            }

            Vector3 normalPosition = rig.NormalPosition;
            Quaternion normalRotation = rig.NormalRotation;
            float blend = Mathf.Clamp01(clip.resetLerpFactor * deltaTime);
            Vector3 resetPosition = clip.resetSubMode == ResetCamSubMode.Teleport
                ? normalPosition
                : Vector3.Lerp(currentPosition, normalPosition, blend);
            Quaternion resetRotation = clip.resetSubMode == ResetCamSubMode.Teleport
                ? normalRotation
                : Quaternion.Slerp(currentRotation, normalRotation, blend);
            if (clip.lockLookAtPlayer)
            {
                resetRotation = LookRotation(resetPosition, lookPoint, resetRotation);
            }

            ApplyShot(
                rig,
                resetPosition,
                resetRotation,
                clip.resetSubMode == ResetCamSubMode.Teleport ? 0f : clip.projectionTransitionDuration);
            return;
        }

        float baseProgress = clip.useVariableSpeed
            ? GetProgress(currentTime, totalDuration, clip.startSpeed, clip.endSpeed)
            : Mathf.Clamp01(currentTime / totalDuration);
        if (clip.useMotionCurve && clip.motionCurve != null)
        {
            baseProgress = Mathf.Clamp01(clip.motionCurve.Evaluate(baseProgress));
        }

        float planarTurnAngle = EvaluatePlanarYaw(
            rig,
            currentTime,
            totalDuration);
        Quaternion planarTurn = Quaternion.Euler(0f, planarTurnAngle, 0f);
        Vector3 worldTargetPosition =
            anchor.TransformPoint(planarTurn * clip.cameraTargetLocalPos);
        Quaternion worldTargetRotation =
            anchor.rotation *
            planarTurn *
            Quaternion.Euler(clip.cameraTargetEuler);
        Vector3 originPosition = clip.useLastFrameAsOrigin
            ? currentPosition
            : _originWorldPos;
        Quaternion originRotation = clip.useLastFrameAsOrigin
            ? currentRotation
            : _originWorldRot;

        Vector3 finalTargetPosition;
        if (!clip.useSurroundMode)
        {
            finalTargetPosition = worldTargetPosition;
        }
        else
        {
            Vector3 foot = anchor.position;
            float radiusScale =
                clip.useSurroundRadiusCurve && clip.surroundRadiusCurve != null
                    ? clip.surroundRadiusCurve.Evaluate(baseProgress)
                    : 1f;
            float heightScale =
                clip.useSurroundHeightCurve && clip.surroundHeightCurve != null
                    ? clip.surroundHeightCurve.Evaluate(baseProgress)
                    : 1f;
            Vector3 circleCenter = new Vector3(
                foot.x,
                foot.y + clip.surroundFixedHeight * heightScale,
                foot.z);
            float currentAngle =
                _surroundStartAngle +
                clip.surroundTotalAngle * baseProgress;
            float radians = currentAngle * Mathf.Deg2Rad;
            finalTargetPosition = circleCenter + new Vector3(
                Mathf.Sin(radians) * clip.surroundRadius * radiusScale,
                0f,
                Mathf.Cos(radians) * clip.surroundRadius * radiusScale);
        }

        Vector3 nextPosition;
        Quaternion nextRotation;
        if (clip.cameraMoveMode == CamMoveMode.Teleport)
        {
            nextPosition = baseProgress > 0f ? finalTargetPosition : currentPosition;
            nextRotation = baseProgress > 0f ? worldTargetRotation : currentRotation;
            if (clip.lockLookAtPlayer && baseProgress > 0f)
            {
                nextRotation = LookRotation(nextPosition, lookPoint, nextRotation);
            }
        }
        else if (clip.useSurroundMode)
        {
            nextPosition = finalTargetPosition;
            nextRotation = clip.lockLookAtPlayer
                ? LookRotation(nextPosition, lookPoint, worldTargetRotation)
                : worldTargetRotation;
        }
        else
        {
            if (clip.useMotionCurve)
            {
                nextPosition = Vector3.Lerp(
                    originPosition,
                    finalTargetPosition,
                    baseProgress);
                Quaternion curveRotation = Quaternion.Slerp(
                    originRotation,
                    worldTargetRotation,
                    baseProgress);
                nextRotation = clip.lockLookAtPlayer
                    ? LookRotation(nextPosition, lookPoint, curveRotation)
                    : curveRotation;
            }
            else
            {
                Vector3 lerpPosition = Vector3.Lerp(
                    originPosition,
                    finalTargetPosition,
                    baseProgress);
                nextPosition = Vector3.Lerp(
                    currentPosition,
                    lerpPosition,
                    Mathf.Clamp01(clip.smoothLerpFactor * deltaTime));
                Quaternion lerpRotation = Quaternion.Slerp(
                    originRotation,
                    worldTargetRotation,
                    baseProgress);
                nextRotation = clip.lockLookAtPlayer
                    ? LookRotation(nextPosition, lookPoint, lerpRotation)
                    : lerpRotation;
            }
        }

        nextPosition = ApplyOrthographicConstraints(rig, nextPosition, originPosition);
        ApplyShot(
            rig,
            nextPosition,
            nextRotation,
            clip.cameraMoveMode == CamMoveMode.Teleport ? 0f : clip.projectionTransitionDuration);
    }

    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        _rig = null;
        _inited = false;
        _warned = false;
    }

    public override void OnGraphStop(Playable playable)
    {
        _rig = null;
        _inited = false;
        _warned = false;
    }

    private static float GetProgress(
        float currentTime,
        float duration,
        float startSpeed,
        float endSpeed)
    {
        float totalDisplacement = (startSpeed + endSpeed) * 0.5f * duration;
        if (totalDisplacement <= 0.0001f)
        {
            return Mathf.Clamp01(currentTime / duration);
        }

        float travel = startSpeed * currentTime +
                       (endSpeed - startSpeed) * currentTime * currentTime / (2f * duration);
        return Mathf.Clamp01(travel / totalDisplacement);
    }

    private float EvaluatePlanarYaw(
        TimelineCamRig rig,
        float currentTime,
        float totalDuration)
    {
        if (clip == null ||
            !clip.overrideProjection ||
            clip.projection != TimelineCameraProjection.Orthographic)
        {
            return 0f;
        }
        if (clip.turnTiming == Camera2DTurnTiming.None)
        {
            return rig.PlanarYaw;
        }

        float duration = Mathf.Clamp(clip.turnDuration, 0.01f, totalDuration);
        float localTime;
        if (clip.turnTiming == Camera2DTurnTiming.AtClipStart)
        {
            localTime = currentTime;
        }
        else
        {
            localTime = currentTime - (totalDuration - duration);
        }

        float progress = Mathf.Clamp01(localTime / duration);
        if (clip.turnCurve != null)
        {
            progress = Mathf.Clamp01(clip.turnCurve.Evaluate(progress));
        }
        float yaw = _planarYawAtClipStart + clip.turnAngleDegrees * progress;
        rig.SetPlanarYaw(yaw);
        return rig.PlanarYaw;
    }

    private static Quaternion LookRotation(
        Vector3 position,
        Vector3 lookPoint,
        Quaternion fallback)
    {
        Vector3 direction = lookPoint - position;
        return direction.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(direction.normalized, Vector3.up)
            : fallback;
    }

    private void ApplyShot(
        TimelineCamRig rig,
        Vector3 position,
        Quaternion rotation,
        float projectionTransitionDuration)
    {
        if (rig == null || clip == null)
        {
            return;
        }

        rig.SetShotTransform(
            position,
            rotation,
            clip.overrideProjection,
            clip.projection,
            clip.orthographicSize,
            clip.fieldOfView,
            projectionTransitionDuration);
    }

    private Vector3 ApplyOrthographicConstraints(
        TimelineCamRig rig,
        Vector3 position,
        Vector3 originPosition)
    {
        if (!clip.constrainOrthographicAxes)
        {
            return position;
        }

        bool isOrthographic = clip.overrideProjection
            ? clip.projection == TimelineCameraProjection.Orthographic
            : rig.IsCurrentProjectionOrthographic;
        if (!isOrthographic)
        {
            return position;
        }

        if (!clip.allowPositionX)
        {
            position.x = originPosition.x;
        }
        if (!clip.allowPositionY)
        {
            position.y = originPosition.y;
        }
        if (!clip.allowPositionZ)
        {
            position.z = originPosition.z;
        }

        if (clip.clampPositionX)
        {
            position.x = ClampRange(position.x, clip.positionXRange);
        }
        if (clip.clampPositionY)
        {
            position.y = ClampRange(position.y, clip.positionYRange);
        }
        if (clip.clampPositionZ)
        {
            position.z = ClampRange(position.z, clip.positionZRange);
        }

        return position;
    }

    private static float ClampRange(float value, Vector2 range)
    {
        float minimum = Mathf.Min(range.x, range.y);
        float maximum = Mathf.Max(range.x, range.y);
        return Mathf.Clamp(value, minimum, maximum);
    }
}

public class CameraTimelineMixerBehaviour : PlayableBehaviour
{
    public CameraTimelineTrack track;

    private TimelineCamRig _rig;
    private bool _hasControl;

    public override void ProcessFrame(
        Playable playable,
        FrameData info,
        object playerData)
    {
        TimelineCamRig rig = playerData as TimelineCamRig;
        if (rig == null)
        {
            ReleaseControl();
            return;
        }

        if (_rig != rig)
        {
            ReleaseControl();
            _rig = rig;
        }

        bool hasActiveClip = false;
        int inputCount = playable.GetInputCount();
        for (int index = 0; index < inputCount; index++)
        {
            if (playable.GetInputWeight(index) > 0f)
            {
                hasActiveClip = true;
                break;
            }
        }

        if (hasActiveClip && !_hasControl)
        {
            _rig.userManualAllowed =
                track != null && track.allowManualCamera;
            _rig.SetReturnMode(
                track == null || track.restoreOriginOnEnd);
            _rig.Acquire(_rig.ReturnTransitionDuration);
            _hasControl = true;
        }
        else if (!hasActiveClip && _hasControl && !HasRemainingClip(playable))
        {
            ReleaseControl();
        }
    }

    public override void OnGraphStop(Playable playable)
    {
        ReleaseControl();
    }

    public override void OnPlayableDestroy(Playable playable)
    {
        ReleaseControl();
    }

    private void ReleaseControl()
    {
        if (_rig != null && _hasControl)
        {
            _rig.Release(_rig.SmoothReturnOnRelease);
        }

        _rig = null;
        _hasControl = false;
    }

    private bool HasRemainingClip(Playable playable)
    {
        if (track == null)
        {
            return false;
        }

        double currentTime = playable.GetTime();
        foreach (TimelineClip clip in track.GetClips())
        {
            if (clip.end > currentTime + 0.0001d)
            {
                return true;
            }
        }

        return false;
    }
}
