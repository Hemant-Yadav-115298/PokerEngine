using UnityEngine;

/// <summary>
/// Shows the dealer button indicator on the current dealer.
/// </summary>
public class DealerButtonDisplay : MonoBehaviour
{
    [SerializeField] private GameObject dealerButtonObject;

    public void SetActive(bool active)
    {
        if (dealerButtonObject != null)
            dealerButtonObject.SetActive(active);
    }
}
