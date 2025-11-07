using UnityEngine;
using UnityEngine.Playables;

public class PerformanceDirector : MonoBehaviour
{
    public PlayableDirector timeline;
    public IncenseController incenseController;
    public Camera mainCamera;

    [Header("Camera Animation")]
    public AnimationCurve cameraDistanceCurve;
    public float startDistance = 5f;
    public float endDistance = 3f;

    private float performanceDuration = 60f;
    private float elapsedTime = 0f;

    void Start()
    {
        if (cameraDistanceCurve == null || cameraDistanceCurve.keys.Length == 0)
        {
            cameraDistanceCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        }
    }

    void Update()
    {
        elapsedTime += Time.deltaTime;

        float t = elapsedTime / performanceDuration;
        float distance = Mathf.Lerp(startDistance, endDistance, cameraDistanceCurve.Evaluate(t));
        mainCamera.transform.position = new Vector3(0, 2, -distance);

        if (elapsedTime >= performanceDuration)
        {
            Debug.Log("Performance ended!");
        }
    }
}
