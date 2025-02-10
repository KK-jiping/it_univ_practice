using UnityEngine;

public class CameraControl : MonoBehaviour
{
    public float m_DampTime = 0.2f;                 // カメラが再フォーカスするのにかかるおおよその時間。
    public float m_ScreenEdgeBuffer = 4f;           // 最も上または下にいるターゲットと画面端との間のスペース。
    public float m_MinSize = 6.5f;                  // カメラの最小の正投影サイズ。
    public Transform[] m_Targets; // カメラが収める必要のあるすべてのターゲット。

    private Camera m_Camera;                        // カメラへの参照。
    private float m_ZoomSpeed;                      // 正投影サイズをスムーズに変化させるための基準速度。
    private Vector3 m_MoveVelocity;                 // 位置をスムーズに変化させるための基準速度。
    private Vector3 m_DesiredPosition;              // カメラが移動しようとしている位置。

    private void Awake ()
    {
        m_Camera = GetComponentInChildren<Camera> ();
    }

    private void FixedUpdate ()
    {
        // カメラを目的の位置に移動させる。
        Move ();

        // カメラのサイズを変更する。
        Zoom ();
    }

    private void Move ()
    {
        // ターゲットの平均位置を求める。
        FindAveragePosition ();

        // その位置へスムーズに移動する。
        transform.position = Vector3.SmoothDamp(transform.position, m_DesiredPosition, ref m_MoveVelocity, m_DampTime);
    }

    private void FindAveragePosition ()
    {
        Vector3 averagePos = new Vector3 ();
        int numTargets = 0;

        // すべてのターゲットの位置を合計する。
        for (int i = 0; i < m_Targets.Length; i++)
        {
            // ターゲットが非アクティブなら次のターゲットへ。
            if (!m_Targets[i].gameObject.activeSelf)
                continue;

            // 平均位置に加算し、ターゲット数を増やす。
            averagePos += m_Targets[i].position;
            numTargets++;
        }

        // ターゲットが存在する場合、位置の合計をターゲット数で割り、平均位置を求める。
        if (numTargets > 0)
            averagePos /= numTargets;

        // y座標はそのまま維持する。
        averagePos.y = transform.position.y;

        // 目的の位置を平均位置に設定する。
        m_DesiredPosition = averagePos;
    }

    private void Zoom ()
    {
        // 目的の位置に基づいて必要なサイズを計算し、それにスムーズに移行する。
        float requiredSize = FindRequiredSize();
        m_Camera.orthographicSize = Mathf.SmoothDamp (m_Camera.orthographicSize, requiredSize, ref m_ZoomSpeed, m_DampTime);
    }

    private float FindRequiredSize ()
    {
        // カメラリグが移動しようとしている位置をローカル座標で求める。
        Vector3 desiredLocalPos = transform.InverseTransformPoint(m_DesiredPosition);

        // カメラのサイズ計算をゼロから開始する。
        float size = 0f;

        // すべてのターゲットを調べる。
        for (int i = 0; i < m_Targets.Length; i++)
        {
            // ターゲットが非アクティブなら次へ。
            if (!m_Targets[i].gameObject.activeSelf)
                continue;

            // ターゲットのカメラローカル座標での位置を求める。
            Vector3 targetLocalPos = transform.InverseTransformPoint(m_Targets[i].position);

            // カメラのローカル空間で、ターゲットの位置と目的の位置の距離を求める。
            Vector3 desiredPosToTarget = targetLocalPos - desiredLocalPos;

            // 現在のサイズと、ターゲットがカメラから「上」または「下」にある距離のうち、大きい方を選ぶ。
            size = Mathf.Max(size, Mathf.Abs(desiredPosToTarget.y));

            // 現在のサイズと、ターゲットがカメラの「左」または「右」にある距離を画面比率で割った値のうち、大きい方を選ぶ。
            size = Mathf.Max(size, Mathf.Abs(desiredPosToTarget.x) / m_Camera.aspect);
        }

        // 画面端のバッファをサイズに追加する。
        size += m_ScreenEdgeBuffer;

        // カメラのサイズが最小サイズを下回らないようにする。
        size = Mathf.Max (size, m_MinSize);

        return size;
    }

    public void SetStartPositionAndSize ()
    {
        // 目的の位置を求める。
        FindAveragePosition ();

        // ダンピングなしでカメラの位置を目的の位置に設定する。
        transform.position = m_DesiredPosition;

        // 必要なカメラサイズを求め、設定する。
        m_Camera.orthographicSize = FindRequiredSize ();
    }
}
