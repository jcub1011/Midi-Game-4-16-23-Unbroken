using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public static class MainMenuDocHandler
{
    #region Properties
    static Dictionary<string, UIDocument> _documents = new();
    static Stack<string> _docHistory = new();
    #endregion

    #region Methods
    /// <summary>
    /// Registers the document with the given name. Document starts out hidden.
    /// </summary>
    /// <param name="name">Name to give document.</param>
    /// <param name="document">Document to register.</param>
    static public void Add(string name, UIDocument document)
    {
        document.rootVisualElement.style.display = DisplayStyle.None;
        _documents.Add(name, document);
    }

    /// <summary>
    /// Returns the root of the specified ui document.
    /// </summary>
    /// <param name="name">Name of ui document to get root of.</param>
    /// <returns>The root visual element of the document.</returns>
    /// <exception cref="System.ArgumentException">Document name doesn't exist.</exception>
    static public VisualElement GetRoot(string name)
    {
        if (_documents.ContainsKey(name)) return _documents[name].rootVisualElement;
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
        if (_documents.ContainsKey(name)) return _documents[name];
        throw new System.ArgumentException($"Document name '{name}' doesn't exist.");
    }

    /// <summary>
    /// Unregisters the document with the given name, removing it from document history too.
    /// </summary>
    /// <param name="name">Name of document to remove.</param>
    /// <exception cref="System.ArgumentException">Document name doesn't exist.</exception>
    static public void RemoveDoc(string name)
    {
        if (_documents.ContainsKey(name)) _documents.Remove(name);
        else throw new System.ArgumentException($"Document name '{name}' doesn't exist.");

        // Filter out removed document from document history.
        var temp = new Stack<string>();
        while (_docHistory.Count > 0)
        {
            var docName = _docHistory.Pop();
            if (docName != name) temp.Push(docName);
        }
        while (temp.Count > 0) _docHistory.Push(temp.Pop());
    }

    /// <summary>
    /// Gets the document dictionary.
    /// </summary>
    /// <returns>Dict.</returns>
    static public Dictionary<string, UIDocument> GetDocumentDict()
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
            rootDict[kvp.Key] = kvp.Value.rootVisualElement;
        }

        return rootDict;
    }

    /// <summary>
    /// Hides the currently visible document and unhides the specified document.
    /// </summary>
    /// <param name="name">Name of document to display.</param>
    static public void Show(string name)
    {
        if (_docHistory.Count > 0) GetRoot(_docHistory.Peek()).style.display = DisplayStyle.None;
        GetRoot(name).style.display = DisplayStyle.Flex;
        _docHistory.Push(name);
    }

    /// <summary>
    /// Returns to the previously open document.
    /// </summary>
    /// <exception cref="System.IndexOutOfRangeException">When there is no previously open document.</exception>
    static public void ReturnToPrev()
    {
        if (_docHistory.Count < 2) throw new System.IndexOutOfRangeException(
            "There is no previous document to return to.");

        GetRoot(_docHistory.Pop()).style.display = DisplayStyle.None;
        GetRoot(_docHistory.Peek()).style.display = DisplayStyle.Flex;
    }
    #endregion
}
