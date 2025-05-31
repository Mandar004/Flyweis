using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Tile : MonoBehaviour, IPointerClickHandler
{
    public int tileIndex;
    public bool isOn;
    public List<int> toggleTargets = new List<int>();

    private SignalGridManager gridManager;
    private Image img;

    private void Start()
    {
        gridManager = SignalGridManager.Instance;
        img = GetComponent<Image>();
        UpdateVisual();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        gridManager.ToggleGroup(toggleTargets);
    }

    public void SetState(bool on)
    {
        isOn = on;
        UpdateVisual();
    }

    public void FlipState()
    {
        isOn = !isOn;
        UpdateVisual();
    }

    public void UpdateVisual()
    {
        if (img == null) img = GetComponent<Image>();
        img.color = isOn ? Color.yellow : Color.gray;
    }

    public IEnumerator FlashEffect()
    {
        Color original = img.color;
        img.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        img.color = original;
    }
}
