using UnityEngine;
using OscJack;

public class OSCSender : MonoBehaviour
{
    [Header("OSC Settings")]
    public string ipAddress = "127.0.0.1";
    public int port = 8000;

    private OscClient client;

    void Start()
    {
        client = new OscClient(ipAddress, port);
        Debug.Log($"OSC Client connected to {ipAddress}:{port}");
    }

    public void SendAngle(float angle)
    {
        client.Send("/incense/angle", angle);
    }

    public void SendSincerity(float sincerity)
    {
        client.Send("/incense/sincerity", sincerity);
    }

    public void SendStability(float stability)
    {
        client.Send("/incense/stability", stability);
    }

    public void SendWind(float wind)
    {
        client.Send("/incense/wind", wind);
    }

    void OnDestroy()
    {
        client?.Dispose();
    }
}