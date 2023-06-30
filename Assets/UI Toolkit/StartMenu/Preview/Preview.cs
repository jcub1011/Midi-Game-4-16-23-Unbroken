using MIDIGame.UI.Documents;
using MIDIGame.Runway;
using Melanchall.DryWetMidi.Interaction;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Preview : MonoBehaviour, IDocHandler, IRunwayParamsInput
{
    #region Constants
    const string SLIDER = "TimeSlider";
    const string BACK_BUTTON = "BackButton";
    const string PREVIEW_RUNWAY_NAME = "PreviewRunway";
    #endregion

    #region Properties
    PreviewRunway _runway;
    TempoMap _tempoMap;
    #endregion

    #region Interface
    public void OnShow()
    {
        Debug.Log("Displaying preview.");
    }

    public void OnShow(List<Note> notes, long lastTick, long tickLeadup, TempoMap tempoMap, float strikeBarHeight = 0.2f)
    {
        // Init interactive elements.
        var root = DocHandler.GetRoot(DocNames.Preview);

        var slider = root.Q(SLIDER) as Slider;
        slider.value = 0;
        slider.lowValue = 0;
        slider.highValue = lastTick + tickLeadup;
        _tempoMap = tempoMap;

        slider.RegisterValueChangedCallback(UpdatePlaybackTime);

        // Init Preview Runway.
        if (_runway == null) _runway = UnityEngine.GameObject.Find(PREVIEW_RUNWAY_NAME).GetComponent<PreviewRunway>();

        _runway.Unload();
        _runway.Initalize(notes, strikeBarHeight, tickLeadup, 0);
    }

    public void OnHide()
    {
        Debug.Log("Hiding preview.");
        _runway.Unload();
    }

    public void OnDocAdd()
    {
        Debug.Log("Preview panel added.");

        var backButton = DocHandler.GetRoot(DocNames.Preview).Q(BACK_BUTTON) as Button;
        backButton.clicked += DocHandler.ReturnToPrev;
    }

    public void OnDocRemove()
    {
        Debug.Log("Preview panel removed.");
    }
    #endregion

    #region Methods
    void UpdatePlaybackTime(ChangeEvent<float> evt)
    {
        //Debug.Log($"New Time: {evt.newValue}");
        var ticks = TimeConverter.ConvertFrom(new MetricTimeSpan( 0, 0, 0, (int)evt.newValue), _tempoMap);
        _runway.UpdateTime(ticks);
    }
    #endregion
}
