using Godot;

namespace VoxelEditorForGodotDotNet.EditTools
{
    public static class SDFMath
    {
        public static class ShapesDistanceOutsideIsPositive
        {
            public static float Sphere(Vector3 samplePoint, float radius)
            {
                return samplePoint.Length() - radius;
            }

            public static float Box(Vector3 samplePoint, Vector3 sideLengths)
            {
                Vector3 absPoint = new Vector3(
                    Mathf.Abs(samplePoint.X),
                    Mathf.Abs(samplePoint.Y),
                    Mathf.Abs(samplePoint.Z)
                );

                Vector3 halfExtents = 0.5f * sideLengths;
                Vector3 delta = absPoint - halfExtents;

                return Mathf.Max(delta.X, Mathf.Max(delta.Y, delta.Z));
            }

            public static float PlaneFloor(Vector3 samplePoint)
            {
                return samplePoint.Y;
            }

            public static float PlaneFloor(Vector3 samplePoint, float floorHeight)
            {
                return samplePoint.Y - floorHeight;
            }

            public static float PlaneCeiling(Vector3 samplePoint)
            {
                return -PlaneFloor(samplePoint);
            }

            public static float PlaneCeiling(Vector3 samplePoint, float ceilingHeight)
            {
                return -PlaneFloor(samplePoint, ceilingHeight);
            }

            public static float DistanceToPlane(Vector3 point, Vector3 origin, Vector3 normalDirection)
            {
                return (point - origin).Dot(normalDirection);
            }

            public static float DistanceToCapsule(Vector3 point, Vector3 lineStart, Vector3 lineEnd, float radius)
            {
                return DistanceToLineSegment(point, lineStart, lineEnd) - radius;
            }

            public static float DistanceToCylinder(Vector3 point, Vector3 lineStart, Vector3 lineEnd, float radius)
            {
                float capsule = DistanceToCapsule(point, lineStart, lineEnd, radius);

                float planeStart = DistanceToPlane(point, lineStart, lineEnd - lineStart);
                float planeEnd = DistanceToPlane(point, lineEnd, lineStart - lineEnd);

                float cut = CombinationFunctionsOutsideIsPositive.Subtract(capsule, planeStart);
                cut = CombinationFunctionsOutsideIsPositive.Subtract(cut, planeEnd);

                return cut;
            }

            public static float DistanceToLineSegment(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
            {
                Vector3 ab = lineEnd - lineStart;
                Vector3 ap = point - lineStart;

                float abLengthSquared = ab.LengthSquared();
                if (abLengthSquared == 0) return ap.Length();

                float t = ap.Dot(ab) / abLengthSquared;
                t = Mathf.Clamp(t, 0f, 1f);

                Vector3 closestPoint = lineStart + t * ab;
                return (point - closestPoint).Length();
            }

            public static float DistanceToLevelPlaneFilledBelow(Vector3 point, Vector3 pointA, Vector3 pointB)
            {
                Vector3 ab = pointB - pointA;
                float abLength = Mathf.Sqrt(ab.X * ab.X + ab.Z * ab.Z);

                if (abLength == 0) return point.Y - pointA.Y;

                float t = ((point.X - pointA.X) * (pointB.X - pointA.X) + (point.Z - pointA.Z) * (pointB.Z - pointA.Z)) / (abLength * abLength);
                t = Mathf.Clamp(t, 0f, 1f);

                float heightAtPoint = Mathf.Lerp(pointA.Y, pointB.Y, t);

                return point.Y - heightAtPoint;
            }
        }

        public static class CombinationFunctionsOutsideIsPositive
        {
            /*
            Boolean logic:
                A     ->  A
                !A    -> -A
                A & B ->  Max(A, B)
                A | B ->  Min(A, B)
            */

            public static float Add(float a, float b)
            {
                return Mathf.Min(a, b);
            }

            public static float Subtract(float a, float b)
            {
                return Mathf.Max(a, -b);
            }

            public static float Intersect(float a, float b)
            {
                return Mathf.Max(a, b);
            }
        }
    }
}