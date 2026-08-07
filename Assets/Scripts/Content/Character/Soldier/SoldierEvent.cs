namespace Content.Character.Soldier
{
    public enum SoldierEvent
    {
        // 목표 지점 도달
        StopAndWait,
        // 다음 목표 지점으로 움직이기 시작
        StartMove,
        // 경계
        Alert,
    }
}