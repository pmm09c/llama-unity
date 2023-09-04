using TMPro;
using UnityEngine;

public class TextMeshProContentSizeFitter : MonoBehaviour
{
    private RectTransform _rectTransform;
    private  TextMeshPro _meshPro;

    void Start()
    {
        _meshPro = GetComponent<TextMeshPro>();
        _rectTransform = GetComponent<RectTransform>();

        UpdateRectTransform();
    }
    
    public void UpdateRectTransform()
    {
        _meshPro.ForceMeshUpdate();
    
        Vector2 newSize = _meshPro.GetPreferredValues();
    
        _rectTransform.sizeDelta = new Vector2(_rectTransform.sizeDelta.x, newSize.y);
    }
    
    public void OnTextChanged()
    {
        UpdateRectTransform();
    }
}