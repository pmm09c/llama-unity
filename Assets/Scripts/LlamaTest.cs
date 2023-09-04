using System;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DefaultNamespace;
using Debug = UnityEngine.Debug;
using LlamaCppLib;
using TMPro;
using UnityEngine.UI;
using Whisper;
using Whisper.Utils;
using TMPro; 

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
    
    // Whisper Whisper Whisper...
    public WhisperManager whisper;
    public MicrophoneRecord microphoneRecord;
    private string _buffer;
    public bool streamSegments = true;

    // Display Display Display
    private TextMeshPro _displayText;
    private TextMeshProContentSizeFitter _displayFitter;

    private void Awake()
    {
        microphoneRecord.OnRecordStop += OnRecordStop;
        whisper.OnNewSegment += OnNewSegment;
    }

    async void Start()
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
        GameObject canvasGameObject = GameObject.Find("Transcript");
        _displayFitter = canvasGameObject.GetComponent<TextMeshProContentSizeFitter>();
        _displayText = canvasGameObject.GetComponent<TextMeshPro>();

        llamaInstance.TextUpdate += UpdateDisplayText;
        microphoneRecord.vadStop = true;
    }
    
    private void ToggleRecording()
    {
        
        if (!microphoneRecord.IsRecording)
        {
            microphoneRecord.StartRecord();
        }
        else
            microphoneRecord.StopRecord();
    }

    private void ToggleVad()
    {
        microphoneRecord.vadStop =  !microphoneRecord.vadStop;
    }
            
    private void OnNewSegment(WhisperSegment segment)
    {
        if (!streamSegments || !_displayText)
            return;

        _buffer += segment.Text;
        _displayText.text = _buffer + "...";

    }
    
    private async void OnRecordStop(float[] data, int frequency, int channels, float length)
    {
        var res = await whisper.GetTextAsync(data, frequency, channels);
        if (res == null) 
            return;
    
        llamaInstance.Query( res.Result).Forget();
    }
    
    public void Update()
    {
          if (Input.GetKeyDown(KeyCode.Q)) // Restart Query
              llamaInstance.Query(pTestPrompt).Forget();
          else if (Input.GetKeyDown(KeyCode.C)) // Cancel Query
              llamaInstance.CancelQuery().Forget();
          else if (Input.GetKeyDown(KeyCode.V)) // Toggle  Voice activity detection
              ToggleVad(); 
          else if (Input.GetKeyDown(KeyCode.R)) // Toggle Recording
              ToggleRecording(); 
    }
    
    private void UpdateDisplayText(string text)
    {
        _displayText.text = text;
        _displayFitter.UpdateRectTransform();
    }
    
    private void OnDestroy()
    {
        if (llamaInstance != null)
           llamaInstance.TextUpdate -= UpdateDisplayText;
    }
}
