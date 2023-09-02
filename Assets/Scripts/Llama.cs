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
        private string _modelPath;
        private string _message;

        public void LoadModel(in LlamaCppModelOptions modelOptions, in LlamaCppGenerateOptions generateOptions,in string modelPath)
        {
            var stopwatch = new Stopwatch();
            _generateOptions = generateOptions;
            _modelOptions = modelOptions;
            _modelPath = Application.streamingAssetsPath + "/" + modelPath;
            
            // Load model file
            _model = new LlamaCppModel();
            stopwatch.Start();
            _model.Load(_modelPath, _modelOptions);
            stopwatch.Stop();
            Debug.Log($"model.Load(...) took {stopwatch.ElapsedMilliseconds} ms to execute.");
            stopwatch.Reset();
        }
        
        private LlamaService()
        {
            #if UNITY_EDITOR
                EditorApplication.playModeStateChanged += LastResults;
            #endif
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