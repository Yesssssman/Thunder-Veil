using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Content.Character.Soldier
{
    /// <summary>
    /// 정찰 지점, 대기 시간으로 이루어진 Pair 데이터
    /// StandbyTime은 정찰 지점에 도착한 후 기다릴 시간
    /// </summary>
    [Serializable]
    public struct Waypoint : IEquatable<Waypoint>
    {
        [FormerlySerializedAs("Point")]
        [Tooltip("정찰 위치")]
        public Vector2 point;

        [FormerlySerializedAs("StandbyTime")]
        [Tooltip("정찰 대기 시간 (Fixed tick)")]
        public int standbyTime;

        public Waypoint(Vector2 point, int standbyTime)
        {
            this.point = point;
            this.standbyTime = standbyTime;
        }

        bool IEquatable<Waypoint>.Equals(Waypoint other)
        {
            return Equals(other);
        }

        public override bool Equals(object other)
        {
            return other is Waypoint oWaypoint
                   && point.Equals(oWaypoint.point)
                   && standbyTime == oWaypoint.standbyTime;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(point, standbyTime);
        }

        public static bool operator ==(in Waypoint left, in Waypoint right)
        {
            return left.point == right.point && left.standbyTime == right.standbyTime;
        }

        public static bool operator !=(in Waypoint left, in Waypoint right)
        {
            return !(left == right);
        }
    }
}