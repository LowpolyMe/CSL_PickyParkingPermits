using System;
using System.Collections.Generic;
using UnityEngine;
using PickyParking.Features.ParkingRules;
using PickyParking.Features.ParkingPolicing.Runtime;

namespace PickyParking.Features.ParkingPolicing
{
    internal sealed class RuleLotSpatialIndex
    {
        private const float CellSize = 8f;
        private readonly Dictionary<CellKey, HashSet<ushort>> _cells = new Dictionary<CellKey, HashSet<ushort>>();
        private readonly List<Vector3> _spacePositions = new List<Vector3>(64);
        private readonly HashSet<ushort> _candidateBuildingIds = new HashSet<ushort>();
        private int _rulesVersion = -1;

        internal struct RuleLotQuery
        {
            public ParkingRuntimeContext Context;
            public Vector3 Position;
            public float MaxSnapDistanceSqr;
        }

        internal struct RuleLotQueryResult
        {
            public ushort BuildingId;
            public ParkingRulesConfigDefinition Rule;
        }

        public void Clear()
        {
            _cells.Clear();
            _rulesVersion = -1;
        }

        public bool TryFindBuilding(RuleLotQuery query, out RuleLotQueryResult result)
        {
            result = default(RuleLotQueryResult);

            ParkingRuntimeContext context = query.Context;
            Vector3 position = query.Position;
            float maxSnapDistanceSqr = query.MaxSnapDistanceSqr;
            if (context == null || context.ParkingRulesConfigRegistry == null)
                return false;

            EnsureBuilt(context);

            _candidateBuildingIds.Clear();
            int cellX = CellFor(position.x);
            int cellZ = CellFor(position.z);

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    CellKey key = new CellKey(cellX + dx, cellZ + dz);
                    HashSet<ushort> list;
                    if (!_cells.TryGetValue(key, out list))
                        continue;

                    foreach (ushort candidateId in list)
                        _candidateBuildingIds.Add(candidateId);
                }
            }

            foreach (ushort candidateId in _candidateBuildingIds)
            {
                ParkingRulesConfigDefinition rule;
                if (!context.ParkingRulesConfigRegistry.TryGet(candidateId, out rule))
                    continue;

                if (!ParkingCandidateBlocker.IsInScope(context, candidateId))
                    continue;

                _spacePositions.Clear();
                if (!context.GameAccess.TryCollectParkingSpacePositions(candidateId, _spacePositions))
                    continue;

                for (int i = 0; i < _spacePositions.Count; i++)
                {
                    Vector3 spacePos = _spacePositions[i];
                    float dx = spacePos.x - position.x;
                    float dz = spacePos.z - position.z;

                    if (dx * dx + dz * dz <= maxSnapDistanceSqr)
                    {
                        result.BuildingId = candidateId;
                        result.Rule = rule;
                        return true;
                    }
                }
            }

            return false;
        }

        private void EnsureBuilt(ParkingRuntimeContext context)
        {
            int version = context.ParkingRulesConfigRegistry.Version;
            if (version == _rulesVersion)
                return;

            _cells.Clear();
            _rulesVersion = version;

            foreach (KeyValuePair<ushort, ParkingRulesConfigDefinition> kvp in context.ParkingRulesConfigRegistry.Enumerate())
            {
                _spacePositions.Clear();
                if (!context.GameAccess.TryCollectParkingSpacePositions(kvp.Key, _spacePositions))
                    continue;

                for (int i = 0; i < _spacePositions.Count; i++)
                {
                    CellKey key = new CellKey(CellFor(_spacePositions[i].x), CellFor(_spacePositions[i].z));
                    HashSet<ushort> list;
                    if (!_cells.TryGetValue(key, out list))
                    {
                        list = new HashSet<ushort>();
                        _cells.Add(key, list);
                    }

                    list.Add(kvp.Key);
                }
            }
        }

        private static int CellFor(float coord)
        {
            return Mathf.FloorToInt(coord / CellSize);
        }

        private struct CellKey : IEquatable<CellKey>
        {
            public readonly int X;
            public readonly int Z;

            public CellKey(int x, int z)
            {
                X = x;
                Z = z;
            }

            public bool Equals(CellKey other)
            {
                return X == other.X && Z == other.Z;
            }

            public override bool Equals(object obj)
            {
                return obj is CellKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + X;
                    hash = hash * 31 + Z;
                    return hash;
                }
            }
        }
    }
}
