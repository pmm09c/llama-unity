using TMPro;
using UnityEngine;

public class ScreenLogger : MonoBehaviour
{
    public TextMeshPro displayText; 
    private string _logText = "";
    private readonly int _maxLogLines = 10;

    void OnEnable()
    {
        displayText = GetComponent<TextMeshPro>();
        Application.logMessageReceived += HandleLog;
        HandleLog(Application.streamingAssetsPath ,"",LogType.Log);
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        // Trim the log to a certain number of lines
        string[] lines = _logText.Split(new[] { '\n' }, System.StringSplitOptions.None);
        if (lines.Length > _maxLogLines)
        {
            _logText = string.Join("\n", lines, lines.Length - _maxLogLines, _maxLogLines);
        }

        // Append the new log and update the UI text
        _logText += logString + "\n";
        displayText.text = _logText;
    }
}
