using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Helper script to quickly set up poker UI in the editor.
/// Attach to Canvas and click "Setup UI" button in inspector.
/// </summary>
public class PokerUISetup : MonoBehaviour
{
    [ContextMenu("Setup UI")]
    public void SetupUI()
    {
        // Create main UI container
        var uiRoot = new GameObject("PokerUI");
        uiRoot.transform.SetParent(transform, false);
        var uiRect = uiRoot.AddComponent<RectTransform>();
        uiRect.anchorMin = Vector2.zero;
        uiRect.anchorMax = Vector2.one;
        uiRect.sizeDelta = Vector2.zero;

        // Create Info Panel (top center)
        CreateInfoPanel(uiRoot.transform);

        // Create Action Panel (bottom center)
        CreateActionPanel(uiRoot.transform);

        // Create Player Panels (around table)
        CreatePlayerPanels(uiRoot.transform);

        Debug.Log("Poker UI Setup Complete!");
    }

    private void CreateInfoPanel(Transform parent)
    {
        var panel = new GameObject("InfoPanel");
        panel.transform.SetParent(parent, false);
        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0, -20);
        rect.sizeDelta = new Vector2(400, 100);

        // Background
        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

        // Pot Text
        var potObj = new GameObject("PotText");
        potObj.transform.SetParent(panel.transform, false);
        var potRect = potObj.AddComponent<RectTransform>();
        potRect.anchorMin = new Vector2(0, 0.5f);
        potRect.anchorMax = new Vector2(1, 1);
        potRect.sizeDelta = Vector2.zero;
        var potText = potObj.AddComponent<TextMeshProUGUI>();
        potText.text = "Pot: $0";
        potText.fontSize = 24;
        potText.alignment = TextAlignmentOptions.Center;
        potText.color = Color.yellow;

        // Phase Text
        var phaseObj = new GameObject("PhaseText");
        phaseObj.transform.SetParent(panel.transform, false);
        var phaseRect = phaseObj.AddComponent<RectTransform>();
        phaseRect.anchorMin = new Vector2(0, 0);
        phaseRect.anchorMax = new Vector2(1, 0.5f);
        phaseRect.sizeDelta = Vector2.zero;
        var phaseText = phaseObj.AddComponent<TextMeshProUGUI>();
        phaseText.text = "Phase: Not Started";
        phaseText.fontSize = 20;
        phaseText.alignment = TextAlignmentOptions.Center;
        phaseText.color = Color.white;
    }

    private void CreateActionPanel(Transform parent)
    {
        var panel = new GameObject("ActionPanel");
        panel.transform.SetParent(parent, false);
        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0, 20);
        rect.sizeDelta = new Vector2(600, 80);

        // Background
        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

        // Horizontal Layout
        var layout = panel.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10;
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        // Start Hand Button
        CreateButton(panel.transform, "StartHandButton", "Start Hand", new Color(0.2f, 0.8f, 0.2f));

        // Fold Button
        CreateButton(panel.transform, "FoldButton", "Fold", new Color(0.8f, 0.2f, 0.2f));

        // Check Button
        CreateButton(panel.transform, "CheckButton", "Check", new Color(0.3f, 0.3f, 0.8f));

        // Call Button
        CreateButton(panel.transform, "CallButton", "Call", new Color(0.3f, 0.6f, 0.8f));

        // Raise Button
        CreateButton(panel.transform, "RaiseButton", "Raise", new Color(0.8f, 0.6f, 0.2f));
    }

    private GameObject CreateButton(Transform parent, string name, string text, Color color)
    {
        var btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        
        var img = btnObj.AddComponent<Image>();
        img.color = color;
        
        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;

        var txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        var txtRect = txtObj.AddComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.sizeDelta = Vector2.zero;
        
        var tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 18;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return btnObj;
    }

    private void CreatePlayerPanels(Transform parent)
    {
        // Create 6 player panels positioned around the table
        Vector2[] positions = new Vector2[]
        {
            new Vector2(0, -300),      // Bottom center (Player 1 - Human)
            new Vector2(-400, -150),   // Bottom left
            new Vector2(-500, 100),    // Left
            new Vector2(0, 300),       // Top center
            new Vector2(500, 100),     // Right
            new Vector2(400, -150)     // Bottom right
        };

        for (int i = 0; i < 6; i++)
        {
            CreatePlayerPanel(parent, i, positions[i]);
        }
    }

    private void CreatePlayerPanel(Transform parent, int index, Vector2 position)
    {
        var panel = new GameObject($"PlayerPanel_{index}");
        panel.transform.SetParent(parent, false);
        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(200, 100);

        // Background
        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

        // Add PlayerUIPanel component
        panel.AddComponent<PlayerUIPanel>();

        // Player Name
        var nameObj = new GameObject("PlayerName");
        nameObj.transform.SetParent(panel.transform, false);
        var nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0.7f);
        nameRect.anchorMax = new Vector2(1, 1);
        nameRect.sizeDelta = Vector2.zero;
        var nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = $"Player {index + 1}";
        nameText.fontSize = 16;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.color = Color.white;

        // Stack Text
        var stackObj = new GameObject("StackText");
        stackObj.transform.SetParent(panel.transform, false);
        var stackRect = stackObj.AddComponent<RectTransform>();
        stackRect.anchorMin = new Vector2(0, 0.4f);
        stackRect.anchorMax = new Vector2(1, 0.7f);
        stackRect.sizeDelta = Vector2.zero;
        var stackText = stackObj.AddComponent<TextMeshProUGUI>();
        stackText.text = "$1000";
        stackText.fontSize = 18;
        stackText.alignment = TextAlignmentOptions.Center;
        stackText.color = Color.green;

        // Status Text
        var statusObj = new GameObject("StatusText");
        statusObj.transform.SetParent(panel.transform, false);
        var statusRect = statusObj.AddComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0, 0.1f);
        statusRect.anchorMax = new Vector2(1, 0.4f);
        statusRect.sizeDelta = Vector2.zero;
        var statusText = statusObj.AddComponent<TextMeshProUGUI>();
        statusText.text = "";
        statusText.fontSize = 14;
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.color = Color.yellow;

        // Card slots (simple placeholders)
        CreateCardSlots(panel.transform);
    }

    private void CreateCardSlots(Transform parent)
    {
        var cardsObj = new GameObject("Cards");
        cardsObj.transform.SetParent(parent, false);
        var cardsRect = cardsObj.AddComponent<RectTransform>();
        cardsRect.anchorMin = new Vector2(0.5f, 0);
        cardsRect.anchorMax = new Vector2(0.5f, 0);
        cardsRect.pivot = new Vector2(0.5f, 0);
        cardsRect.anchoredPosition = new Vector2(0, 5);
        cardsRect.sizeDelta = new Vector2(80, 30);

        var layout = cardsObj.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 5;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        // Create 2 card slots
        for (int i = 0; i < 2; i++)
        {
            var cardSlot = new GameObject($"CardSlot_{i}");
            cardSlot.transform.SetParent(cardsObj.transform, false);
            var img = cardSlot.AddComponent<Image>();
            img.color = new Color(0.8f, 0.8f, 0.8f, 0.5f);
            cardSlot.SetActive(false); // Hidden by default
        }
    }
}
