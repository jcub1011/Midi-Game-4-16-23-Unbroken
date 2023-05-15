using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ScrubbingOverlay : MonoBehaviour
{
    public VisualElement VisualElement { get; protected set; }
    public Slider PlaybackScrubber { get; protected set; }
    private bool _sliderInitalized = false;

    void GetElements()
    {
        VisualElement = GetComponent<UIDocument>().rootVisualElement;
        PlaybackScrubber = VisualElement.Q("Scrubber") as Slider;
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
        GetElements();
    }

    public void InitSlider(float minPlaybackTime, float maxPlaybackTime)
    {
        _sliderInitalized = true;
        PlaybackScrubber.lowValue = minPlaybackTime;
        PlaybackScrubber.highValue = maxPlaybackTime;
        PlaybackScrubber.value = minPlaybackTime;
    }

    public void UpdateSlider(float playbackTime)
    {
        if (!_sliderInitalized) return;
        PlaybackScrubber.value = playbackTime;
    }
}
