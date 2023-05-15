using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ScrubbingOverlay : MonoBehaviour
{
    public VisualElement VisualElement { get; protected set; }

    void InitOverlay()
    {
        VisualElement = transform.GetComponent<UIDocument>().rootVisualElement;
    }

    public bool Visible
    {
        get
        {
            return gameObject.activeSelf;
        }
        set
        {
            gameObject.SetActive(value);
        }
    }

    private void Start()
    {
        InitOverlay();
    }
}
