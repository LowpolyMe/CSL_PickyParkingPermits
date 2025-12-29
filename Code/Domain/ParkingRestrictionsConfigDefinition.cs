using System;
using System.IO;

namespace PickyParking.Domain
{
    
    
    
    [Serializable]
    public struct ParkingRestrictionsConfigDefinition
    {
        public readonly bool ResidentsWithinRadiusOnly;
        public readonly ushort ResidentsRadiusMeters;

        public readonly bool WorkSchoolWithinRadiusOnly;
        public readonly ushort WorkSchoolRadiusMeters;

        public readonly bool VisitorsAllowed;

        public ParkingRestrictionsConfigDefinition(
            bool residentsWithinRadiusOnly,
            ushort residentsRadiusMeters,
            bool workSchoolWithinRadiusOnly,
            ushort workSchoolRadiusMeters)
        {
            ResidentsWithinRadiusOnly = residentsWithinRadiusOnly;
            ResidentsRadiusMeters = residentsRadiusMeters;
            WorkSchoolWithinRadiusOnly = workSchoolWithinRadiusOnly;
            WorkSchoolRadiusMeters = workSchoolRadiusMeters;
            VisitorsAllowed = false;
        }

        public ParkingRestrictionsConfigDefinition(
            bool residentsWithinRadiusOnly,
            ushort residentsRadiusMeters,
            bool workSchoolWithinRadiusOnly,
            ushort workSchoolRadiusMeters,
            bool visitorsAllowed)
        {
            ResidentsWithinRadiusOnly = residentsWithinRadiusOnly;
            ResidentsRadiusMeters = residentsRadiusMeters;
            WorkSchoolWithinRadiusOnly = workSchoolWithinRadiusOnly;
            WorkSchoolRadiusMeters = workSchoolRadiusMeters;
            VisitorsAllowed = visitorsAllowed;
        }

        public bool IsUnrestricted => !ResidentsWithinRadiusOnly && !WorkSchoolWithinRadiusOnly && !VisitorsAllowed;

        public void Write(BinaryWriter writer)
        {
            writer.Write(ResidentsWithinRadiusOnly);
            writer.Write(ResidentsRadiusMeters);
            writer.Write(WorkSchoolWithinRadiusOnly);
            writer.Write(WorkSchoolRadiusMeters);
            writer.Write(VisitorsAllowed);
        }

        public static ParkingRestrictionsConfigDefinition ReadV1(BinaryReader reader)
        {
            bool res = reader.ReadBoolean();
            ushort resRad = reader.ReadUInt16();
            bool ws = reader.ReadBoolean();
            ushort wsRad = reader.ReadUInt16();
            return new ParkingRestrictionsConfigDefinition(res, resRad, ws, wsRad, visitorsAllowed: false);
        }

        public static ParkingRestrictionsConfigDefinition ReadV2(BinaryReader reader)
        {
            bool res = reader.ReadBoolean();
            ushort resRad = reader.ReadUInt16();
            bool ws = reader.ReadBoolean();
            ushort wsRad = reader.ReadUInt16();
            bool visitorsAllowed = reader.ReadBoolean();
            return new ParkingRestrictionsConfigDefinition(res, resRad, ws, wsRad, visitorsAllowed);
        }
    }
}



