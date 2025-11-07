using UnityEngine;
using TMPro;

public class IncenseController : MonoBehaviour
{
    [Header("References")]
    public Transform incensePivot;
    public Transform incenseStick;
    public Light glowLight;
    public ParticleSystem smokeParticles;
    public OSCSender oscSender;

    [Header("Falling Settings")]
    public float startHeight = 2f;          // 起始高度
    public float targetHeight = 0.5f;       // 目標高度（香爐位置）
    public float fallDuration = 30f;        // 下落時間

    [Header("Struggle Settings")]
    public float windForceMin = 20f;
    public float windForceMax = 60f;
    public float windChangeSpeed = 5f;
    public float playerControlStrength = 100f;

    [Header("Visual Settings")]
    [Header("UI")]
    public TextMeshProUGUI messageText;
    public TextMeshProUGUI angleText;
    public TextMeshProUGUI timerText;
    public float maxGlowIntensity = 5f;

    private float currentAngle = 90f;
    private float targetWindAngle = 90f;
    private float lastAngleValue = 90f;
    private float stability = 1f;
    private float fallTimer = 0f;
    private bool hasLanded = false;
    private float finalSincerity = 0f;

    void Start()
    {
        // 設置初始位置
        if (incensePivot != null)
        {
            Vector3 pos = incensePivot.position;
            pos.y = startHeight;
            incensePivot.position = pos;
        }

        // 隨機初始角度
        currentAngle = Random.Range(60f, 120f);
        targetWindAngle = currentAngle;

        if (glowLight != null)
        {
            glowLight.intensity = 2f;
        }

        Debug.Log("香開始下落！用滑鼠控制角度！");
    }

    void Update()
    {
        if (!hasLanded)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
                );
            }
            float remaining = fallDuration - fallTimer;
            Debug.Log($"剩餘時間: {remaining:F1}秒, 當前角度: {currentAngle:F1}°");
            UpdateFalling();
            HandleInput();
            ApplyWind();
            CalculateStability();
            UpdateVisuals();
            UpdateUI();
            SendOSC();
        }
        else
        {
            // 已經落地，顯示最終結果
            DisplayFinalResult();
        }

    }

    void UpdateFalling()
    {
        fallTimer += Time.deltaTime;

        if (fallTimer >= fallDuration)
        {
            // 落地！
            hasLanded = true;
            fallTimer = fallDuration;

            // 計算最終誠意度
            finalSincerity = 1f - Mathf.Abs(currentAngle - 90f) / 60f;
            finalSincerity = Mathf.Clamp01(finalSincerity);

            Debug.Log($"香落地了！最終角度: {currentAngle:F1}°, 誠意度: {finalSincerity * 100:F1}%");
            return;
        }

        // 計算當前高度（線性下降）
        float t = fallTimer / fallDuration;
        float currentHeight = Mathf.Lerp(startHeight, targetHeight, t);

        if (incensePivot != null)
        {
            Vector3 pos = incensePivot.position;
            pos.y = currentHeight;
            incensePivot.position = pos;
        }

        // 根據下落進度調整風力（越接近地面風越小）
        float windMultiplier = 1f - (t * 0.5f); // 風力減少到 50%
        float currentWindMax = windForceMax * windMultiplier;

        // 風力擺動
        float windChange = Random.Range(-currentWindMax, currentWindMax) * Time.deltaTime;
        targetWindAngle += windChange;
        targetWindAngle = Mathf.Clamp(targetWindAngle, 30f, 150f);

        currentAngle = Mathf.Lerp(currentAngle, targetWindAngle, windChangeSpeed * Time.deltaTime);
        currentAngle = Mathf.Clamp(currentAngle, 30f, 150f);

        if (incensePivot != null)
        {
            incensePivot.localRotation = Quaternion.Euler(0, 0, currentAngle - 90f);
        }
    }

    void HandleInput()
    {
        if (Input.GetMouseButton(0))
        {
            float mouseX = Input.GetAxis("Mouse X");
            currentAngle += mouseX * playerControlStrength * Time.deltaTime;
            currentAngle = Mathf.Clamp(currentAngle, 30f, 150f);
        }
    }

    void ApplyWind()
    {
        // 風力已經整合到 UpdateFalling 中
    }

    void CalculateStability()
    {
        float angleChange = Mathf.Abs(currentAngle - lastAngleValue);
        stability = Mathf.Clamp01(1f - angleChange / 10f);
        lastAngleValue = currentAngle;
    }

    void UpdateVisuals()
    {
        float angleDeviation = Mathf.Abs(currentAngle - 90f);
        float linearSincerity = 1f - (angleDeviation / 30f);
        linearSincerity = Mathf.Clamp01(linearSincerity);
        float sincerity = linearSincerity * linearSincerity;

        if (glowLight != null)
        {
            Color lightColor = Color.Lerp(Color.red, Color.yellow, sincerity);
            glowLight.color = lightColor;
            glowLight.intensity = Mathf.Lerp(1f, maxGlowIntensity, sincerity);
        }

        if (smokeParticles != null)
        {
            var emission = smokeParticles.emission;
            emission.rateOverTime = Mathf.Lerp(5f, 50f, 1f - stability);
        }
    }

    void SendOSC()
    {
        if (oscSender != null)
        {
            float angleDeviation = Mathf.Abs(currentAngle - 90f);
            float linearSincerity = 1f - (angleDeviation / 20f);  // 改成除以 30（原本是 60）
            linearSincerity = Mathf.Clamp01(linearSincerity);
            float sincerity = linearSincerity * linearSincerity;

            float normalizedAngle = currentAngle / 180f;
            float progress = fallTimer / fallDuration; // 下落進度

            oscSender.SendAngle(normalizedAngle);
            oscSender.SendSincerity(sincerity);
            oscSender.SendStability(stability);
            oscSender.SendWind(progress); // 用下落進度當作 Wind 參數
        }
    }

    void DisplayFinalResult()
    {
        // 已落地，持續顯示最終狀態
        if (glowLight != null)
        {
            // 根據最終誠意度閃爍
            float flicker = 0.8f + Mathf.Sin(Time.time * 3f) * 0.2f;
            glowLight.intensity = maxGlowIntensity * finalSincerity * flicker;
        }
        // 顯示最終結果 UI

        if (timerText != null)
        {
            timerText.text = "時間到!";
            timerText.color = Color.yellow;
        }

        if (angleText != null)
        {
            angleText.text = $"最終角度: {currentAngle:F0}° 願您能得到神明的滿意 人在做天在看";
        }

        if (messageText != null)
        {
            // 重新計算最終誠意度
            float angleDeviation = Mathf.Abs(currentAngle - 90f);
            finalSincerity = 1f - (angleDeviation / 30f);
            finalSincerity = Mathf.Clamp01(finalSincerity);

            if (finalSincerity >= 0.95f)
            {
                messageText.text = "😊 完美插香!最高誠意!";
                messageText.color = Color.yellow;
            }
            else if (finalSincerity >= 0.75f)
            {
                messageText.text = $"不錯!誠意度: {finalSincerity * 100:F0}%";
                messageText.color = new Color(0.5f, 1f, 0.5f);
            }
            else
            {
                messageText.text = $"可惜!誠意度: {finalSincerity * 100:F0}%";
                messageText.color = new Color(1f, 0.6f, 0.2f);
            }
        }
    }
    void UpdateUI()
    {
        // 更新角度顯示
        if (angleText != null)
        {
            angleText.text = $"角度: {currentAngle:F0}°";
        }

        // 更新倒數計時（新增）
        if (timerText != null)
        {
            float remaining = fallDuration - fallTimer;
            timerText.text = $"倒數 {remaining:F1}秒";  

            // 時間少於 5 秒時閃爍
            if (remaining <= 5f)
            {
                float flicker = Mathf.Sin(Time.time * 10f) * 0.5f + 0.5f;  // 快速閃爍
                timerText.color = Color.Lerp(Color.red, Color.yellow, flicker);

                // 字體放大
                timerText.fontSize = 40 + Mathf.Sin(Time.time * 8f) * 5f;
            }
            else if (remaining <= 10f)
            {
                timerText.color = Color.yellow;
                timerText.fontSize = 40;
            }
            else
            {
                timerText.color = Color.white;
                timerText.fontSize = 40;
            }
        }

        // 計算誠意並更新訊息
        if (messageText != null)
        {
            float angleDeviation = Mathf.Abs(currentAngle - 90f);
            float linearSincerity = 1f - (angleDeviation / 30f);
            linearSincerity = Mathf.Clamp01(linearSincerity);
            float sincerity = linearSincerity * linearSincerity;

            string message = "";
            Color messageColor = Color.white;

            if (sincerity >= 0.95f)
            {
                message = "😊 完美!最高誠意!";
                messageColor = new Color(0.2f, 1f, 0.2f); // 亮綠色
            }
            else if (sincerity >= 0.75f)
            {
                message = "不錯!還算虔誠";
                messageColor = new Color(0.5f, 1f, 0.5f); // 淺綠色
            }
            else if (sincerity >= 0.60f)
            {
                message = "普通 可以更好";
                messageColor = new Color(1f, 1f, 0.3f); // 黃色
            }
            else if (sincerity >= 0.40f)
            {
                message = "誠意不足...需努力";
                messageColor = new Color(1f, 0.6f, 0.2f); // 橘色
            }
            else
            {
                message = "太歪了!需要反省";
                messageColor = new Color(1f, 0.2f, 0.2f); // 紅色
            }

            messageText.text = message;
            messageText.color = messageColor;
        }
    }
}

