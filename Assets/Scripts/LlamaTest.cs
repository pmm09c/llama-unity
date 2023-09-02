using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DefaultNamespace;
using Debug = UnityEngine.Debug;

public class LlamaCppTest : MonoBehaviour
{
    
    private static LlamaService llamaInstance;
    private bool promptHasRun = false;
    private string TestPrompt = "Are you still there?";
    void Start()
    {
        llamaInstance = LlamaService.Instance;
    }

    public void Update()
    {
        if (!promptHasRun)
        {
            promptHasRun = true;

            llamaInstance.Query(TestPrompt).Forget();
        }
    }
}
