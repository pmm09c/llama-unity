using System;
using UnityEngine;
using LlamaCppLib;
using Cysharp.Threading.Tasks;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace DefaultNamespace
{
    
    public class LlamaService
    {
        private static LlamaService instance;
        private LlamaCppModel _model;
        private LlamaCppGenerateOptions _generateOptions;
        private LlamaCppModelOptions _modelOptions;
        private string _modelPath = Application.streamingAssetsPath + "/codellama-13b-instruct.Q4_K_M.gguf";
        private string _message;
 
        private LlamaService()
        {
            #if UNITY_EDITOR
                EditorApplication.playModeStateChanged += LastResults;
            #endif
            
            var stopwatch = new Stopwatch();
            // Configure some model options
            _modelOptions = new LlamaCppModelOptions
            {
                ContextSize = 2048, 
                GpuLayers = 24,
            };
            
            // Load model file
            _model = new LlamaCppModel();
            stopwatch.Start();
            _model.Load(_modelPath, _modelOptions);
            stopwatch.Stop();
            Debug.Log($"model.Load(...) took {stopwatch.ElapsedMilliseconds} ms to execute.");
            stopwatch.Reset();
        
            // Configure some prediction options
            _generateOptions = new LlamaCppGenerateOptions
            {
                ThreadCount = 10,
                TopK = 1,
                TopP = 0.95f,
                Temperature = 0.1f,
                RepeatPenalty = 1.3f,
                PenalizeNewLine = true,
                Mirostat = Mirostat.Mirostat2,
            };
        }
        
        #if UNITY_EDITOR
        private void LastResults(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                EditorApplication.playModeStateChanged -= LastResults;
                Debug.Log(_message);
            }
        }
        #endif
        
        public static LlamaService Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new LlamaService();
                }
                return instance;
            }
        }

        public async UniTask<string> Query(string prompt)
        {
            _message = "robot: ";
            var stopwatch = new Stopwatch();
            // Create conversation session
            var session = _model.CreateSession();

            // Generate tokens
            stopwatch.Start();
            await foreach (var token in session.GenerateTokenStringAsync(prompt, _generateOptions))
            {
                Debug.Log(token);
                _message += token;
                await UniTask.Yield();
            }
        
            stopwatch.Stop();
            Debug.Log($"session.GenerateTokenStringAsync(...) took {stopwatch.ElapsedMilliseconds} ms to execute.");
            Debug.Log(_message);
            return _message;
        }
    }


}