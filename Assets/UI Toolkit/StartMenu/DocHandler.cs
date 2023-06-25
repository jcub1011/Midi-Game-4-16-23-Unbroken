using Melanchall.DryWetMidi.Core;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine.UIElements;

namespace MainStartMenu
{
    public interface IDocHandler
    {
        /// <summary>
        /// Called when the document is made visible.
        /// </summary>
        public void OnShow();
        /// <summary>
        /// Called when the documnet is no longer visible.
        /// </summary>
        public void OnHide();
        /// <summary>
        /// Called when the document is first being created.
        /// </summary>
        public void OnDocAdd();
        /// <summary>
        /// Called when the document is being destroyed.
        /// </summary>
        public void OnDocRemove();
    }

    public interface IFileInput
    {
        /// <summary>
        /// Called when document is made visible.
        /// </summary>
        /// <param name="filePath">Path of file.</param>
        public void OnShow(string filePath);
    }

    public interface IRunwayParamsInput
    {
        /// <summary>
        /// Called when document is made visible.
        /// </summary>
        /// <param name="notes">List of notes to display.</param>
        /// <param name="endTime">Time of the end of the last note.</param>
        /// <param name="strikeBarHeight">Height of strike bar from bottom as a percent. (1.0 = 100%)</param>
        /// <param name="msLeadup">Miliseconds before first note hits the strike bar.</param>
        /// <param name="time">Time to begin playback at.</param>
        public void OnShow(List<NoteEvtData> notes, float endTime,
            float strikeBarHeight = 0.2f, float msLeadup = 4000f, float time = 0f);
    }

    public static class Documents
    {
        public static string Main = "Main";
        public static string SongSelect = "Song Selector";
        public static string SongSetts = "Song Settings";
        public static string Preview = "Preview";

    }

    public struct DocScriptBundle
    {
        public UIDocument Doc;
        public IDocHandler Script;
    }

    public static class DocHandler
    {
        #region Properties
        static readonly Dictionary<string, DocScriptBundle> _documents = new();
        static readonly Stack<string> _docHistory = new();
        #endregion

        #region Methods
        /// <summary>
        /// Registers the document with the given name. Document starts out hidden.
        /// </summary>
        /// <param name="name">Name to give document.</param>
        /// <param name="document">Document to register.</param>
        static public void Add(string name, UIDocument document, IDocHandler script)
        {
            _documents.Add(name, new DocScriptBundle { Doc = document, Script = script });
            script.OnDocAdd();
            document.rootVisualElement.visible = false;
        }

        /// <summary>
        /// Returns the root of the specified ui document.
        /// </summary>
        /// <param name="name">Name of ui document to get root of.</param>
        /// <returns>The root visual element of the document.</returns>
        /// <exception cref="System.ArgumentException">Document name doesn't exist.</exception>
        static public VisualElement GetRoot(string name)
        {
            if (_documents.ContainsKey(name)) return _documents[name].Doc.rootVisualElement;
            throw new System.ArgumentException($"Document name '{name}' doesn't exist.");
        }

        /// <summary>
        /// Gets the document associated with the given name.
        /// </summary>
        /// <param name="name">Name of ui document to get.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Document name doesn't exist.</exception>
        static public UIDocument GetDoc(string name)
        {
            if (_documents.ContainsKey(name)) return _documents[name].Doc;
            throw new System.ArgumentException($"Document name '{name}' doesn't exist.");
        }

        /// <summary>
        /// Unregisters the document with the given name, removing it from document history too.
        /// </summary>
        /// <param name="name">Name of document to remove.</param>
        /// <exception cref="System.ArgumentException">Document name doesn't exist.</exception>
        static public void RemoveDoc(string name)
        {
            throw new System.NotImplementedException("Not implemented.");
            /*
            if (_documents.ContainsKey(name)) _documents.Remove(name);
            else throw new System.ArgumentException($"Document name '{name}' doesn't exist.");

            // Filter out removed document from document history.
            var temp = new Stack<string>();
            while (_docHistory.Count > 0)
            {
                var docName = _docHistory.Pop();
                if (docName != name) temp.Push(docName);
            }
            while (temp.Count > 0) _docHistory.Push(temp.Pop());*/
        }

        /// <summary>
        /// Gets the document dictionary.
        /// </summary>
        /// <returns>Dict.</returns>
        static public Dictionary<string, DocScriptBundle> GetDocumentDict()
        {
            return _documents;
        }

        /// <summary>
        /// Gets the root of each document with the document name as the key.
        /// </summary>
        /// <returns>dict.</returns>
        static public Dictionary<string, VisualElement> GetDocumentRootDict()
        {
            var rootDict = new Dictionary<string, VisualElement>();

            foreach (var kvp in _documents)
            {
                rootDict[kvp.Key] = kvp.Value.Doc.rootVisualElement;
            }

            return rootDict;
        }

        static private void Show(string name)
        {
            GetDoc(name).rootVisualElement.visible = true;
            _documents[name].Script.OnShow();
        }

        static private void Hide(string name)
        {
            GetDoc(name).rootVisualElement.visible = false;
            _documents[name].Script.OnHide();
        }

        /// <summary>
        /// Hides the currently visible document and unhides the specified document.
        /// </summary>
        /// <param name="name">Name of document to display.</param>
        static public void DisplayDoc(string name)
        {
            if (_docHistory.Count > 0) Hide(_docHistory.Peek());
            Show(name);
            _docHistory.Push(name);
        }

        /// <summary>
        /// Hides the currently visible document and unhides the specified document.
        /// </summary>
        /// <param name="name">Name of document to display.</param>
        /// <param name="filePath">Path of file to pass to document being shown.</param>
        /// <exception cref="System.MethodAccessException"></exception>
        static public void DisplayDoc(string name, string filePath)
        {
            DisplayDoc(name);
            var script = _documents[name].Script as IFileInput ?? throw new System.MethodAccessException($"'{name}' cannot take a file path argument.");
            script.OnShow(filePath);
        }

        /// <summary>
        /// Hides the currently visible document and unhides the specified document.
        /// </summary>
        /// <param name="name">Name of document to display.</param>
        /// <param name="notes">Notes to show in preview.</param>
        /// <param name="endTime">End time of last note.</param>
        /// <param name="strikeBarHeight">Height of strike bar from bottom of runway as percent of runway. (1.0 = 100%)</param>
        /// <param name="msLeadup">Ms before first note touches strike bar.</param>
        /// <param name="time">Time to begin preview at.</param>
        /// <exception cref="System.MethodAccessException"></exception>
        static public void DisplayDoc(string name, List<Note> notes, float endTime, float strikeBarHeight = 0.2f,
        float msLeadup = 4000f, float time = 0f)
        {
            DisplayDoc(name);
            var script = _documents[name].Script as IRunwayParamsInput ?? throw new System.MethodAccessException($"'{name}' cannot take runway arguments.");
            script.OnShow(notes, endTime, strikeBarHeight, msLeadup, time);
        }

        /// <summary>
        /// Returns to the previously open document.
        /// </summary>
        /// <exception cref="System.IndexOutOfRangeException">When there is no previously open document.</exception>
        static public void ReturnToPrev()
        {
            if (_docHistory.Count < 2) throw new System.IndexOutOfRangeException(
                "There is no previous document to return to.");

            var current = _docHistory.Pop();
            var previous = _docHistory.Peek();

            Hide(current);
            Show(previous);
        }
        #endregion

        #region Utilities
        public static void SetScrollSpeed(ScrollView scrollingElement, float ScrollSpeed = 500f)
        {
            scrollingElement.RegisterCallback<WheelEvent>((evt) =>
            {
                scrollingElement.scrollOffset = new UnityEngine.Vector2(0, scrollingElement.scrollOffset.y + ScrollSpeed * evt.delta.y);
                evt.StopPropagation();
            });
        }
        #endregion
    }

}
