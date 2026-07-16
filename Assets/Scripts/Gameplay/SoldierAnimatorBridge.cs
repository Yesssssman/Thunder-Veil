using UnityEngine;

namespace ThunderVeil.Gameplay
{
    /// <summary>
    /// One-way bridge: reflects the gameplay FSM state onto the Animator, which is a dumb
    /// visual mirror (Idle / Move / Death). No gameplay logic lives in Mecanim.
    ///
    /// Soldier와 
    /// 
    /// </summary>
    public class SoldierAnimatorBridge : MonoBehaviour
    {
        private static readonly int IsMoving = Animator.StringToHash("IsMoving");
        private static readonly int IsDead = Animator.StringToHash("IsDead");

        [SerializeField]
        private Soldier soldier;

        [SerializeField]
        private Animator animator;

        private void Reset()
        {
            soldier = GetComponent<Soldier>();
            animator = GetComponentInChildren<Animator>();
        }

        private void Awake()
        {
            // Reset()만으로는 컴포넌트를 나중에 붙인 기존 인스턴스의 참조가 비어있을 수 있으므로
            // 런타임에서도 비어있는 참조를 스스로 채워 넣는다. (없으면 Update가 조기 return되어 Idle에 고정됨)
            if (soldier == null) soldier = GetComponent<Soldier>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            if (soldier == null || animator == null) return;

            if (!soldier.IsAlive)
            {
                animator.SetBool(IsDead, true);
                return;
            }
            
            animator.SetBool(IsMoving, soldier.Current == Soldier.State.Move);
        }
    }
}
