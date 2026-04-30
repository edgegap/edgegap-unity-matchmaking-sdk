using UnityEngine;
using UnityEngine.UI;

public class BeaconHubButton : MonoBehaviour
{
    public string LabelComponentDefaultPath = "BtnLabel";

    public string GoodIconComponentDefaultPath = "LatencyIcon/LatencyGood";

    public string MidIconComponentDefaultPath = "LatencyIcon/LatencyMid";

    public string PoorIconComponentDefaultPath = "LatencyIcon/LatencyPoor";

    public float GoodThreshold = 50;

    public float MidThreshold = 100;

    public float PoorThreshold = 200;

    public Text BtnLabel;
    public GameObject GoodLatencyIcon;
    public GameObject MidLatencyIcon;
    public GameObject PoorLatencyIcon;
    
    private float _ping;

    private void Awake()
    {
        if (BtnLabel == null)
        {
            BtnLabel = transform.Find(LabelComponentDefaultPath).GetComponent<Text>();

            if (BtnLabel == null)
            {
                Debug.LogWarning($"Unable to find component {LabelComponentDefaultPath} in gameObject hierarchy.");
            }
        }

        if (GoodLatencyIcon == null)
        {
            GoodLatencyIcon = transform.Find(GoodIconComponentDefaultPath).gameObject;

            if (GoodLatencyIcon == null)
            {
                Debug.LogWarning($"Unable to find component {GoodIconComponentDefaultPath} in gameObject hierarchy.");
            }
        }

        if (MidLatencyIcon == null)
        {
            MidLatencyIcon = transform.Find(MidIconComponentDefaultPath).gameObject;

            if (MidLatencyIcon == null)
            {
                Debug.LogWarning($"Unable to find component {MidIconComponentDefaultPath} in gameObject hierarchy.");
            }
        }

        if (PoorLatencyIcon == null)
        {
            PoorLatencyIcon = transform.Find(PoorIconComponentDefaultPath).gameObject;

            if (PoorLatencyIcon == null)
            {
                Debug.LogWarning($"Unable to find component {PoorIconComponentDefaultPath} in gameObject hierarchy.");
            }
        }
    }

    public float GetPing()
    {
        return _ping;
    }

    public void SetLatencyIcon(float ping)
    {
        _ping = ping;

        if (ping < GoodThreshold)
        {
            GoodLatencyIcon.SetActive(true);
            MidLatencyIcon.SetActive(false);
            PoorLatencyIcon.SetActive(false);
        }
        else if (ping < MidThreshold)
        {
            GoodLatencyIcon.SetActive(false);
            MidLatencyIcon.SetActive(true);
            PoorLatencyIcon.SetActive(false);
        }
        else if (ping < PoorThreshold)
        {
            GoodLatencyIcon.SetActive(false);
            MidLatencyIcon.SetActive(false);
            PoorLatencyIcon.SetActive(true);
        }
        else
        {
            GoodLatencyIcon.SetActive(false);
            MidLatencyIcon.SetActive(false);
            PoorLatencyIcon.SetActive(false);
            GetComponent<Button>().interactable = false;
        }
    }
}
