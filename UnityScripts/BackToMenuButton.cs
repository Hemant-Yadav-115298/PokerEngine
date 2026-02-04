using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple button to return to main menu.
/// </summary>
[RequireComponent(typeof(Button))]
public class BackToMenuButton : MonoBehaviour
{
    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        SceneController.Instance.LoadMainMenu();
    }
}
