using System;
using SceneKit;

namespace ARNativePortal
{
    public static class SCNOpenTKTransformations
    {
        public static SCNMatrix4 ToSCNMatrix4(this OpenTK.NMatrix4 matrix)
        {
            return new SCNMatrix4(matrix.Row0, matrix.Row1, matrix.Row2, matrix.Row3);
        }

        public static SCNVector3 ToSCNVector3(this OpenTK.NVector3 vector)
        {
            return new SCNVector3(vector.X, vector.Y, vector.Z);
        }

        public static SCNVector3 ToSCNVector3(this OpenTK.Vector4 vector)
        {
            return new SCNVector3(vector.Xyz);
        }
    }
}
