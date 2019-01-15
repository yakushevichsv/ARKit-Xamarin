using System;
namespace ARNativePortal.Helpers
{
    enum VolumeRange: int
    {
        low = 200,
        medium = 400,
        high = 1000
    }

    public static class Constants
    {
        public static string PlaneNodeName = "PlaneNode";
        public static string EqualizerNodeName = "EqualizerNode";
    }
}
