using System;
using UnityEngine;
using LlamaCppLib;
using Cysharp.Threading.Tasks;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System.Threading;
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
        
        public delegate void UpdateTextHandler(string text);
        public event UpdateTextHandler TextUpdate;
 
        private CancellationTokenSource _cts;
        
        public void LoadModel(in LlamaCppModelOptions modelOptions, in LlamaCppGenerateOptions generateOptions,
            in string modelPath)
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
                CancelQuery().Forget();
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

        public async UniTask CancelQuery()
        {
            if (_cts != null && _cts.Token.CanBeCanceled)
            {
                _cts.Cancel();

                // wait for cancellation to complete
                while (_cts != null)
                    await UniTask.Delay(100);
            }
            
        }
        
        public async UniTask Query(string prompt)
        {
            // Make sure there isn't any active queries
            await CancelQuery();
            
            // Token we can use for cancellation
            _cts = new CancellationTokenSource();

            try
            {
                await UniTask.SwitchToThreadPool();
                await QueryHelper(prompt, _cts.Token);
            }
            // I had a catch block here as well but for some reason the OperationCanceledException isn't getting captured
            finally
            {
                _cts = null;
                await UniTask.SwitchToMainThread();
            }
            
        }

        private async UniTask QueryHelper(string prompt, CancellationToken cancellationToken)
        {
            _message = $"Prompt: {prompt}\n\nMr. Robot:  \n";
            
            var stopwatch = new Stopwatch();
            
            // Create conversation session
            var session = _model.CreateSession();

            stopwatch.Start();
            var tokens = 0;
            await foreach (var token in session.GenerateTokenStringAsync(prompt, _generateOptions, cancellationToken))
            {
                // Check for cancellation and throw if it's requested
                if (cancellationToken.IsCancellationRequested)
                {
                    Debug.Log("Cancelled Query");
                    throw new OperationCanceledException(token);  
                }

                tokens += 1;
                Debug.Log(token);
                _message += token;
                // TODO find another way to handle this so it doesn't need to switch threads all the time. 
                await UniTask.SwitchToMainThread();
                TextUpdate?.Invoke(_message);
                await UniTask.SwitchToThreadPool(); 
            }

            stopwatch.Stop();
            var tokenRate = (double)stopwatch.ElapsedMilliseconds/(double)tokens;
            Debug.Log($"session.GenerateTokenStringAsync(...) took {stopwatch.ElapsedMilliseconds} ms to execute.");
            Debug.Log($"session.GenerateTokenStringAsync(...) token rate {tokenRate} ms per token.");
        }
    }


}