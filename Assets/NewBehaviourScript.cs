using UnityEngine;
using LlamaCppLib;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class LlamaCppTest : MonoBehaviour
{
    // Start is called before the first frame update
    async void Start()
    {
        // Configure some model options
        var modelOptions = new LlamaCppModelOptions
        {
            ContextSize = 2048,
            GpuLayers = 24,
            // ...
        };

        // Load model file
        using var model = new LlamaCppModel();
        string modelPath = Application.streamingAssetsPath + "/13q4.gguf";
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        model.Load(modelPath, modelOptions);
        stopwatch.Stop();
        Debug.Log($"model.Load(...) took {stopwatch.ElapsedMilliseconds} ms to execute.");
        stopwatch.Reset();
        
        // Configure some prediction options
        var generateOptions = new LlamaCppGenerateOptions
        {
            ThreadCount = 10,
            TopK = 1,
            TopP = 0.95f,
            Temperature = 0.1f,
            RepeatPenalty = 1.1f,
            Mirostat = Mirostat.Mirostat2,
            // ...
        };

        // Create conversation session
        var session = model.CreateSession();
        
        // Get a prompt
        Debug.Log("> ");
        var prompt = "in one sentence max, what is your purpose?";


        // Set-up prompt using template
        // prompt = String.Format(template, prompt);

        // Generate tokens
        string message = "robot: ";
        stopwatch.Start();
        await foreach (var token in session.GenerateTokenStringAsync(prompt, generateOptions))
            message = message + " " + token;
        stopwatch.Stop();
        Debug.Log($"session.GenerateTokenStringAsync(...) took {stopwatch.ElapsedMilliseconds} ms to execute.");
        Debug.Log(message);
    }

}
