using UnityEditor;
using UnityEngine;

[System.Serializable]
public class Pause : UnityEngine.Events.UnityEvent { }
public class Start : UnityEngine.Events.UnityEvent { }
public class NewGame : UnityEngine.Events.UnityEvent { }
public class Restart : UnityEngine.Events.UnityEvent { }
public class OpenScrubMode : UnityEngine.Events.UnityEvent { }