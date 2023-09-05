using System;
using System.Collections.Generic;
using UnityEngine;
using LlamaCppLib;
using Cysharp.Threading.Tasks;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
        private string _metricsText;
        public delegate void UpdateTextHandler(string text, string metrics);
        public event UpdateTextHandler TextUpdate;

        private CancellationTokenSource _cts;
        
        public void LoadModel(in LlamaCppModelOptions modelOptions, in LlamaCppGenerateOptions generateOptions,
            in string modelPath)
        {
            var stopwatch = new Stopwatch();
            _generateOptions = generateOptions;
            _modelOptions = modelOptions;
            #if UNITY_ANDROID
                _modelPath = "/storage/emulated/0/Models/" + modelPath;
            #else
                _modelPath = Application.streamingAssetsPath + "/" + modelPath;
            #endif
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

        private string GetStats(List<double> array)
        {
            List<double> subArray = array.Skip(1).ToList();
            var mean = Math.Round(subArray.Average(),1);
            var min = Math.Round(subArray.Min(),1);
            var max = Math.Round(subArray.Max(),1);
            var stdDev  = Math.Round(Math.Sqrt(subArray.Average(v => Math.Pow(v - mean, 2))),1);
            var sortedRates = subArray.OrderBy(n => n).ToList();
            var median  =  subArray.Count % 2 == 0 ? 
                Math.Round((sortedRates[(subArray.Count / 2) - 1] + sortedRates[subArray.Count / 2]) / 2.0,1): 
                Math.Round(sortedRates[subArray.Count / 2],1);
            
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"CURR:   {array.Last():F1}");
            sb.AppendLine($"MEAN:   {mean:F1}");
            sb.AppendLine($"MEDIAN: {median:F1}");
            sb.AppendLine($"MIN:    {min:F1}");
            sb.AppendLine($"MAX:    {max:F1}");
            sb.AppendLine($"STD:    {stdDev:F1}");
            sb.AppendLine($"FIRST:    {array[0]:F1}");
            sb.AppendLine("(in ms/Token)");

            return sb.ToString();
        }
        
        private async UniTask QueryHelper(string prompt, CancellationToken cancellationToken)
        {
            _message = $"Prompt: {prompt}\n\nMr. Robot:  \n";
            List<double> tokenRates = new List<double>();
            var stopwatch = new Stopwatch();
            
            // Create conversation session
            var session = _model.CreateSession();
            double tokenRate = 0;
            stopwatch.Start();
            var tokens = 0;
            await foreach (var token in session.GenerateTokenStringAsync(prompt, _generateOptions, cancellationToken))
            {
                stopwatch.Stop();
                // Check for cancellation and throw if it's requested
                if (cancellationToken.IsCancellationRequested)
                {
                    Debug.Log("Cancelled Query");
                    throw new OperationCanceledException(token);  
                }

                tokens += 1;
                Debug.Log(token);
                _message += token;
                tokenRate = (double)stopwatch.ElapsedMilliseconds;//;/(double)tokens;
                tokenRates.Add(tokenRate);
                if (tokens > 1)
                    _metricsText = GetStats(tokenRates);
                // TODO find another way to handle this so it doesn't need to switch threads all the time. 
                await UniTask.SwitchToMainThread();
                TextUpdate?.Invoke(_message, _metricsText);
                await UniTask.SwitchToThreadPool(); 
                stopwatch.Reset();
                stopwatch.Start();
            }
            //Debug.Log($"session.GenerateTokenStringAsync(...) took {stopwatch.ElapsedMilliseconds} ms to execute.");
            Debug.Log($"session.GenerateTokenStringAsync(...) token rate {tokenRate} ms per token.");
        }
    }


}