using System;
using System.IO;

namespace PickyParking.Features.ParkingRules
{
    [Serializable]
    public struct ParkingRulesConfigDefinition
    {
        public readonly bool ResidentsWithinRadiusOnly;
        public readonly ushort ResidentsRadiusMeters;

        public readonly bool WorkSchoolWithinRadiusOnly;
        public readonly ushort WorkSchoolRadiusMeters;

        public readonly bool VisitorsAllowed;

        public ParkingRulesConfigDefinition(
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

        public ParkingRulesConfigDefinition(
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

        public void Write(BinaryWriter writer)
        {
            writer.Write(ResidentsWithinRadiusOnly);
            writer.Write(ResidentsRadiusMeters);
            writer.Write(WorkSchoolWithinRadiusOnly);
            writer.Write(WorkSchoolRadiusMeters);
            writer.Write(VisitorsAllowed);
        }

        public static ParkingRulesConfigDefinition ReadV1(BinaryReader reader)
        {
            bool res = reader.ReadBoolean();
            ushort resRad = reader.ReadUInt16();
            bool ws = reader.ReadBoolean();
            ushort wsRad = reader.ReadUInt16();
            return new ParkingRulesConfigDefinition(res, resRad, ws, wsRad, visitorsAllowed: false);
        }

        public static ParkingRulesConfigDefinition ReadV2(BinaryReader reader)
        {
            bool res = reader.ReadBoolean();
            ushort resRad = reader.ReadUInt16();
            bool ws = reader.ReadBoolean();
            ushort wsRad = reader.ReadUInt16();
            bool visitorsAllowed = reader.ReadBoolean();
            return new ParkingRulesConfigDefinition(res, resRad, ws, wsRad, visitorsAllowed);
        }
    }
}
