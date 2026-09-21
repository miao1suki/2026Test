namespace Project.CameraModes
{
    public enum CameraViewMode
    {
        Side2D = 0,
        Perspective3D = 1,
    }

    public interface ICameraViewModeSwitcher
    {
        CameraViewMode CurrentMode { get; }

        CameraViewMode TargetMode { get; }

        void SwitchTo2D(bool immediate = false);

        void SwitchTo3D(bool immediate = false);

        void ToggleMode(bool immediate = false);
    }
}
