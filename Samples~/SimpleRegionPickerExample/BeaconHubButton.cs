using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BeaconHubButton : MonoBehaviour
{
    [SerializeField]
    private string _labelComponentPath = "BtnLabel";

    [SerializeField]
    private string _goodIconComponentPath = "LatencyIcon/LatencyGood";

    [SerializeField]
    private string _midIconComponentPath = "LatencyIcon/LatencyMid";

    [SerializeField]
    private string _poorIconComponentPath = "LatencyIcon/LatencyPoor";

    [SerializeField]
    private float _goodThreshold = 50;

    [SerializeField]
    private float _midThreshold = 100;

    [SerializeField]
    private float _poorThreshold = 200;

    private TextMeshProUGUI _btnLabel;
    private GameObject _goodLatencyIcon;
    private GameObject _midLatencyIcon;
    private GameObject _poorLatencyIcon;
    private float _ping;

    private void Awake()
    {
        _btnLabel = transform.Find(_labelComponentPath).GetComponent<TextMeshProUGUI>();
        _goodLatencyIcon = transform.Find(_goodIconComponentPath).gameObject;
        _midLatencyIcon = transform.Find(_midIconComponentPath).gameObject;
        _poorLatencyIcon = transform.Find(_poorIconComponentPath).gameObject;

        if (_btnLabel == null)
        {
            Debug.LogWarning($"Unable to find component {_labelComponentPath} in gameObject hierarchy.");
        }

        if (_goodLatencyIcon == null)
        {
            Debug.LogWarning($"Unable to find component {_goodIconComponentPath} in gameObject hierarchy.");
        }

        if (_midLatencyIcon == null)
        {
            Debug.LogWarning($"Unable to find component {_midIconComponentPath} in gameObject hierarchy.");
        }

        if (_poorLatencyIcon == null)
        {
            Debug.LogWarning($"Unable to find component {_poorIconComponentPath} in gameObject hierarchy.");
        }
    }

    public string GetLabel()
    {
        return _btnLabel.text;
    }

    public float GetPing()
    {
        return _ping;
    }

    public void SetLabel(string txt)
    {
        _btnLabel.text = txt;
    }

    public void SetLatencyIcon(float ping)
    {
        _ping = ping;

        if (ping < _goodThreshold)
        {
            _goodLatencyIcon.SetActive(true);
            _midLatencyIcon.SetActive(false);
            _poorLatencyIcon.SetActive(false);
        }
        else if (ping < _midThreshold)
        {
            _goodLatencyIcon.SetActive(false);
            _midLatencyIcon.SetActive(true);
            _poorLatencyIcon.SetActive(false);
        }
        else if (ping < _poorThreshold)
        {
            _goodLatencyIcon.SetActive(false);
            _midLatencyIcon.SetActive(false);
            _poorLatencyIcon.SetActive(true);
        }
        else
        {
            _goodLatencyIcon.SetActive(false);
            _midLatencyIcon.SetActive(false);
            _poorLatencyIcon.SetActive(false);
            GetComponent<Button>().interactable = false;
        }
    }
}
