using System.Threading;

namespace Core.StateMachine
{
    /// <summary>
    /// 상태 머신 취소 토큰의 Wrapper 클래스.
    /// <p/>
    /// 상태 머신당 하나씩 할당되며, 각 상태에 파라미터로 전달.
    /// 상태 머신 당 취소 토큰 하나 = "상태 머신은 하나의 Activate된
    /// 상태만 가질 수 있다" 가 보장됨.
    /// </summary>
    public class CancellationTokenHolder
    {
        public CancellationTokenSource CancellationToken { get; private set; }

        public CancellationTokenHolder()
        {
            CancellationToken = new CancellationTokenSource();
            Refresh();
        }

        public CancellationToken Token => CancellationToken.Token;

        /// <summary>
        /// 토큰 Refresh. 이미 취소된 상태일 수도 있고, 진행 중인 작업이 있을
        /// 수도 있음. 여기선 고려하지 않고 무조건 취소.
        /// <p/>
        /// `new CancellationTokenSource()`로 신규 객체를 계속 할당해서 리셋함.
        /// `CancellationToken.Reset()`같은 함수가 있거나 나중에 추가되면 수정요망.
        /// (힙 할당을 피하기 위해)
        /// </summary>
        public void Refresh()
        {
            CancellationToken.Cancel();
            CancellationToken.Dispose();
            CancellationToken = new CancellationTokenSource();
        }
    }
}