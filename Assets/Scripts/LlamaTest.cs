using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DefaultNamespace;
using Debug = UnityEngine.Debug;
using LlamaCppLib;
using UnityEngine.UI;

public class LlamaCppTest : MonoBehaviour
{
    // Model Options
    [SerializeField] public uint pSeed  = uint.MaxValue;
    [SerializeField] public int pContextSize = 512;
    [SerializeField] public int pBatchSize = 512;
    [SerializeField] public bool pUseMemoryMapping = true;
    [SerializeField] public bool pUseMemoryLocking;
    [SerializeField] public int pGpuLayers = 24;
    [SerializeField] public float pRopeFrequencyBase = 10000f;
    [SerializeField] public float pRopeFrequencyScale = 1f;
    [SerializeField] public bool pLowVRAM; 

    // Generate Options
    [SerializeField] public int pThreadCount = 4;
    [SerializeField] public int pTopK = 40;
    [SerializeField] public float pTopP = 0.95f;
    [SerializeField] public float pTemperature = 0.8f;
    [SerializeField] public float pRepeatPenalty = 1.1f;
    [SerializeField] public int pLastTokenCountPenalty = 64;
    [SerializeField] public bool pPenalizeNewLine; 
    [SerializeField] public float pTfsZ = 1f;
    [SerializeField] public float pTypicalP = 1f;
    [SerializeField] public float pFrequencyPenalty;
    [SerializeField] public float pPresencePenalty;
    [SerializeField] public Mirostat pMirostat = Mirostat.Mirostat2;
    [SerializeField] public float pMirostatTAU = 5f;
    [SerializeField] public float pMirostatETA = 0.1f;
    
    // Model Path
    [SerializeField] public string pModelName = "Llama2/13q4.gguf";
        
    // Prompt
    public string pTestPrompt = "Will we get into SAGE 2023?";
    private static LlamaService llamaInstance;
    private bool _promptHasRun = false;
    private Text _displayText;
    void Start()
    {
        var modelOptions = new LlamaCppModelOptions 
        {
            Seed = pSeed,
            ContextSize = pContextSize,
            BatchSize = pBatchSize,
            UseMemoryMapping = pUseMemoryMapping,
            UseMemoryLocking = pUseMemoryLocking,
            GpuLayers = pGpuLayers,
            RopeFrequencyBase = pRopeFrequencyBase,
            RopeFrequencyScale = pRopeFrequencyScale,
            LowVRAM = pLowVRAM,
        };

        var generateOptions = new LlamaCppGenerateOptions
        {
            ThreadCount = pThreadCount,
            TopK = pTopK,
            TopP = pTopP,
            Temperature = pTemperature,
            RepeatPenalty = pRepeatPenalty,
            LastTokenCountPenalty = pLastTokenCountPenalty,
            PenalizeNewLine = pPenalizeNewLine,
            TfsZ = pTfsZ,
            TypicalP = pTypicalP,
            FrequencyPenalty = pFrequencyPenalty,
            PresencePenalty = pPresencePenalty,
            Mirostat = pMirostat,
            MirostatTAU = pMirostatTAU,
            MirostatETA = pMirostatETA,
        };

        llamaInstance = LlamaService.Instance;
        llamaInstance.LoadModel(modelOptions, generateOptions, pModelName);

        // For displaying text to screen
        GameObject canvasGameObject = GameObject.Find("QueryResponseCanvas");
        _displayText = canvasGameObject.GetComponent<Text>();
        llamaInstance.TextUpdate += UpdateDisplayText;
        
    }

    public void Update()
    {
          if (Input.GetKeyDown(KeyCode.Q)) // Restart Query
              llamaInstance.Query(pTestPrompt).Forget();
          else if (Input.GetKeyDown(KeyCode.C)) // Cancel Query
              llamaInstance.CancelQuery().Forget();
          
          if (_promptHasRun) return;
          _promptHasRun = true;

          llamaInstance.Query(pTestPrompt).Forget();
    }
    
    private void UpdateDisplayText(string text)
    {
        _displayText.text = text;
    }

    private void OnDestroy()
    {
        if (llamaInstance != null)
           llamaInstance.TextUpdate -= UpdateDisplayText;
    }
}
