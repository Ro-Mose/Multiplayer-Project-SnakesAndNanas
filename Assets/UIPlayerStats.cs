using UnityEngine;
using TMPro;
using Unity.VisualScripting;

public class UIPlayerStats : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI lengthText;

    private void OnEnable()
    {
        PlayerLength.ChangedLengthEvent += ChangeLengthText;
    }

    private void OnDisable()
    {
        PlayerLength.ChangedLengthEvent -= ChangeLengthText;
    }

    //updates length
    private void ChangeLengthText(ushort length)
    {
        lengthText.text = length.ToString();
    }
}
