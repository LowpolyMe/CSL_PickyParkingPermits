namespace PickyParking.Domain
{
    public struct ParkingPosition
    {
        public readonly float X;
        public readonly float Z;

        public ParkingPosition(float x, float z)
        {
            X = x;
            Z = z;
        }

        public static float SqrDistance(ParkingPosition a, ParkingPosition b)
        {
            float dx = a.X - b.X;
            float dz = a.Z - b.Z;
            return dx * dx + dz * dz;
        }
    }
}
