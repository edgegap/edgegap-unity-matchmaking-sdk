using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BeaconHubButton : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _cityName;

    [SerializeField]
    private GameObject _goodLatencyIcon;

    [SerializeField]
    private GameObject _midLatencyIcon;

    [SerializeField]
    private GameObject _poorLatencyIcon;

    [SerializeField]
    private float _goodThreshold = 50;

    [SerializeField]
    private float _midThreshold = 100;

    [SerializeField]
    private float _poorThreshold = 200;

    private float _ping;

    public string GetCityName()
    {
        return _cityName.text;
    }

    public float GetPing()
    {
        return _ping;
    }

    public void SetCityName(string name)
    {
        _cityName.text = name;
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
