using System;

namespace ImageCenterFinder
{
    public interface IMouseCaptureProxy
    {
        event EventHandler Capture;
        event EventHandler Release;

        void OnMouseDown(object sender, MouseCaptureArgs e);
        void OnMouseMove(object sender, MouseCaptureArgs e);
        void OnMouseUp(object sender, MouseCaptureArgs e);
    }
}