using UnityEngine;
using LlamaCppLib;

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
        model.Load(modelPath, modelOptions);

        // Configure some prediction options
        var generateOptions = new LlamaCppGenerateOptions
        {
            ThreadCount = 4,
            TopK = 40,
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
        var prompt = "what is your purpose?";


        // Set-up prompt using template
        // prompt = String.Format(template, prompt);

        // Generate tokens
        await foreach (var token in session.GenerateTokenStringAsync(prompt, generateOptions))
            Debug.Log(token);

    }

}
