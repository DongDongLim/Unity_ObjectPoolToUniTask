using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Pool;
using UnityEngine;
using UnityEngine.UI;

namespace ObjectPoolSample
{

    public class ObjectCreater : MonoBehaviour
    {
        [SerializeField] private GameObject _sampleObject;

        [SerializeField] private Button _startButton;
        [SerializeField] private Button _cancelButton;

        private SampleObject _samplePrefab;

        private Transform _poolParentTransform;
        private CancellationTokenSource _cancelToken;

        private ObjectPool<SampleObject> _samplePool;
        private List<SampleObject> _samplePopList;

        private void Awake()
        {
            _poolParentTransform = transform;
            _samplePrefab = _sampleObject.GetComponent<SampleObject>();
            _samplePopList = new List<SampleObject>();
            _samplePool = new ObjectPool<SampleObject>(_poolParentTransform, _samplePrefab, false);            
            _startButton.onClick.AddListener(() => StartLoop().Forget());
            _cancelButton.onClick.AddListener(CancelLoop);           
        }

        private CancellationTokenSource CreateCancellationTokenSource()
        {
            var cst = new CancellationTokenSource();
            return cst;
        }

        private void CancelLoop()
        {
            _cancelToken.Cancel();
            RemoveAll();
            _samplePool.Shrink(0f, 10);
        }

        private void RemoveAll()
        {
            foreach(var sample in _samplePopList)
            {
                _samplePool.Return(sample);
            }

            _samplePopList.Clear();
        }

        private async UniTaskVoid StartLoop()
        {
            if (_cancelToken == null || _cancelToken.IsCancellationRequested == true)
            {
                _cancelToken = CreateCancellationTokenSource();
            }

            await PrePoolObject();
            LoopPopObject().Forget();
        }

        private async UniTask PrePoolObject()
        {
            await _samplePool.PreLoadAsync(50, 10);
        }

        private async UniTaskVoid LoopPopObject()
        {
            while(_cancelToken.IsCancellationRequested == false)
            {
                var sample = _samplePool.Rent();

                _samplePopList.Add(sample);

                ReturnObject(sample, _cancelToken.Token).Forget();

                await UniTask.Yield(cancellationToken: _cancelToken.Token);
            }
        }

        private async UniTaskVoid ReturnObject(SampleObject sample, CancellationToken canselToken)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(3), cancellationToken: canselToken);

            _samplePopList.Remove(sample);
            _samplePool.Return(sample);
        }
    }
}